using BepInEx.Configuration;
using UnityEngine;

namespace LaudMod
{
    // Todo lo que define a un instrumento: sus datos fijos, sus ajustes (archivo .cfg) y lo que se carga al jugar
    public class Instrumento
    {
        // ---- Datos fijos ----
        public string Clave;          // nombre interno: laud, flauta, nacaires
        public string NombrePrefab;   // nombre del prefab dentro de laud_bundle
        public string NombreObjeto;   // nombre del objeto dentro del juego
        public string Prefijo;        // delante de las secciones del .cfg ("" para el laud, que ya existia)
        public int Indice;            // posicion fija en la lista de objetos del juego
        public int Precio;
        public float Masa;
        public float AlturaMueble;
        public bool Sostenido;        // true = suena mientras mantienes la tecla (flauta)
        public bool Colgable = true;  // false = no se puede colgar en los ganchos (nacaires)
        public string[] Sonidos;      // nombres de los sonidos dentro del paquete, en orden de tecla
        public string[] SonidosSuaves; // version suave de cada nota (punteo flojo); null = no hay
        public float[] Desvios;        // semitonos que se sube cada nota al sonar (las teclas negras del piano); null = ninguno
        public string SonidoFondo;    // sonido que suena de fondo mientras tocas (bordon de la zanfona); null = ninguno
        public string[] NombresNotas; // como aparecen en el .cfg
        public KeyCode[] TeclasDefecto;

        public float[] DefTocar;      // distancia, altura, lateral, giroX, giroY, giroZ
        public float[] DefMano;       // distancia, altura, giroX, giroY, giroZ
        public string DefEscena, DefRaiz;
        public float[] DefTienda;     // X, Y, Z, giro, inclinacion
        public float[] DefColgado;    // X, Y, Z, giroX, giroY, giroZ
        public float[] DefTocarViejo; // valores por defecto de una version anterior (null = ninguno)
        public float[] DefManoViejo;  // si el jugador aun los tiene, se cambian por los nuevos

        // ---- Ajustes del .cfg ----
        public ConfigEntry<KeyCode>[] Teclas;
        public ConfigEntry<float> Volumen, VolumenFondo;
        public ConfigEntry<float> TocarDistancia, TocarAltura, TocarLateral, TocarGiroX, TocarGiroY, TocarGiroZ;
        public ConfigEntry<float> ManoDistancia, ManoAltura, ReposoGiroX, ReposoGiroY, ReposoGiroZ;
        public ConfigEntry<string> TiendaEscena, TiendaRaiz;
        public ConfigEntry<float> TiendaX, TiendaY, TiendaZ, TiendaGiro, TiendaInclinacion;
        public ConfigEntry<float> ColgPosX, ColgPosY, ColgPosZ, ColgRotX, ColgRotY, ColgRotZ;

        // ---- Lo que se rellena al jugar ----
        public AudioClip[] Clips = new AudioClip[0];
        public AudioClip ClipFondo;
        public AudioClip[] ClipsSuaves = new AudioClip[0];
        public GameObject Prefab;
        public bool Registrado;
        public GameObject Puesto;
        public Vector3 UltimaPos;
        public float UltimoGiro, UltimaInclinacion;

        // true si los valores son iguales (con un margen pequeno)
        private static bool Iguales(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (Mathf.Abs(a[i] - b[i]) > 0.001f) return false;
            }
            return true;
        }

        // "CSharp" -> "C# (Do#)", "HighC" -> "High C (Do alto)". Los golpes de los nacaires se quedan igual
        private static string NombreBonito(string nota)
        {
            string resto = nota;
            bool alto = false;
            if (resto.StartsWith("High")) { alto = true; resto = resto.Substring(4); }
            else if (resto.StartsWith("Low")) { resto = resto.Substring(3); }
            bool sost = resto.EndsWith("Sharp");
            if (sost) resto = resto.Substring(0, resto.Length - 5);
            string[] letras = { "C", "D", "E", "F", "G", "A", "B" };
            string[] solfeo = { "Do", "Re", "Mi", "Fa", "Sol", "La", "Si" };
            int k = System.Array.IndexOf(letras, resto);
            if (k < 0) return nota;
            string ingles = (alto ? "High " : "") + letras[k] + (sost ? "#" : "");
            string espanol = solfeo[k] + (sost ? "#" : "") + (alto ? " alto" : "");
            return ingles + " (" + espanol + ")";
        }

        private string Sec(string seccion)
        {
            return Prefijo + seccion;
        }

        // publico: lo que ve el jugador en el .cfg (teclas y volumen)
        // interno: posiciones, tienda y gancho (en la version Release no se guardan en ningun archivo)
        public void Atar(ConfigFile publico, ConfigFile interno)
        {
            Teclas = new ConfigEntry<KeyCode>[Sonidos.Length];
            for (int i = 0; i < Sonidos.Length; i++)
            {
                // En el menu F1 se ve un titulo mas claro (con el nombre de la nota en ingles y en espanol),
                // pero en el archivo el ajuste se sigue llamando igual, asi nadie pierde sus teclas
                ConfigurationManagerAttributes titulo = new ConfigurationManagerAttributes
                {
                    DispName = "Note " + (i + 1) + ": " + NombreBonito(NombresNotas[i]),
                    Order = 100 - i
                };
                Teclas[i] = publico.Bind(Sec("Keys"), "Note" + (i + 1) + "_" + NombresNotas[i], TeclasDefecto[i],
                    new ConfigDescription("Key for note " + (i + 1) + " (" + NombresNotas[i] + "). Examples: Alpha6, Y, Z, Keypad1, F5", null, titulo));
            }
            Volumen = publico.Bind(Sec("Sound"), "Volume", 1f, "Volume (0 to 1)");
            if (SonidoFondo != null)
            {
                VolumenFondo = publico.Bind(Sec("Sound"), "DroneVolume", 0.6f, "Volume of the drone that plays in the background while you play (0 to 1)");
            }

            ConfigFile c = interno;
            TocarDistancia = c.Bind(Sec("Posicion al tocar"), "Distancia", DefTocar[0], "Distancia a la camara en modo tocar");
            TocarAltura = c.Bind(Sec("Posicion al tocar"), "Altura", DefTocar[1], "Altura respecto a la posicion normal en modo tocar");
            TocarLateral = c.Bind(Sec("Posicion al tocar"), "Lateral", DefTocar[2], "Desplazamiento a la derecha (negativo = izquierda)");
            TocarGiroX = c.Bind(Sec("Posicion al tocar"), "GiroX", DefTocar[3], "Giro en grados, eje X");
            TocarGiroY = c.Bind(Sec("Posicion al tocar"), "GiroY", DefTocar[4], "Giro en grados, eje Y");
            TocarGiroZ = c.Bind(Sec("Posicion al tocar"), "GiroZ", DefTocar[5], "Giro en grados, eje Z");

            ManoDistancia = c.Bind(Sec("Posicion en la mano"), "Distancia", DefMano[0], "Distancia a la camara al llevarlo en la mano (menor = mas cerca de ti)");
            ManoAltura = c.Bind(Sec("Posicion en la mano"), "Altura", DefMano[1], "Altura respecto a la posicion normal al llevarlo en la mano");
            ReposoGiroX = c.Bind(Sec("Posicion en la mano"), "GiroX", DefMano[2], "Giro en grados al llevarlo en la mano, eje X");
            ReposoGiroY = c.Bind(Sec("Posicion en la mano"), "GiroY", DefMano[3], "Giro en grados al llevarlo en la mano, eje Y");
            ReposoGiroZ = c.Bind(Sec("Posicion en la mano"), "GiroZ", DefMano[4], "Giro en grados al llevarlo en la mano, eje Z");

            // Si el jugador sigue con los valores de fabrica de una version anterior, se pasan a los nuevos.
            // Si los cambio a su gusto, no se tocan.
            if (Iguales(new[] { TocarDistancia.Value, TocarAltura.Value, TocarLateral.Value, TocarGiroX.Value, TocarGiroY.Value, TocarGiroZ.Value }, DefTocarViejo))
            {
                TocarDistancia.Value = DefTocar[0]; TocarAltura.Value = DefTocar[1]; TocarLateral.Value = DefTocar[2];
                TocarGiroX.Value = DefTocar[3]; TocarGiroY.Value = DefTocar[4]; TocarGiroZ.Value = DefTocar[5];
            }
            if (Iguales(new[] { ManoDistancia.Value, ManoAltura.Value, ReposoGiroX.Value, ReposoGiroY.Value, ReposoGiroZ.Value }, DefManoViejo))
            {
                ManoDistancia.Value = DefMano[0]; ManoAltura.Value = DefMano[1];
                ReposoGiroX.Value = DefMano[2]; ReposoGiroY.Value = DefMano[3]; ReposoGiroZ.Value = DefMano[4];
            }

            TiendaEscena = c.Bind(Sec("Tienda"), "Escena", DefEscena, "Escena (isla) de la tienda. Se aplica al reiniciar el juego");
            TiendaRaiz = c.Bind(Sec("Tienda"), "Raiz", DefRaiz, "Objeto raiz de la isla donde se coloca. Se aplica al reiniciar el juego");
            TiendaX = c.Bind(Sec("Tienda"), "PosicionX", DefTienda[0], "Posicion X en la tienda");
            TiendaY = c.Bind(Sec("Tienda"), "PosicionY", DefTienda[1], "Posicion Y en la tienda");
            TiendaZ = c.Bind(Sec("Tienda"), "PosicionZ", DefTienda[2], "Posicion Z en la tienda");
            TiendaGiro = c.Bind(Sec("Tienda"), "Giro", DefTienda[3], "Giro en la tienda, en grados");
            TiendaInclinacion = c.Bind(Sec("Tienda"), "Inclinacion", DefTienda[4], "Inclinacion en la tienda, en grados (90 = tumbado)");

            if (!Colgable)
            {
                return;
            }
            ColgPosX = c.Bind(Sec("Colgar en gancho"), "PosicionX", DefColgado[0], "Desplazamiento X respecto al gancho (mod HooksHangMore)");
            ColgPosY = c.Bind(Sec("Colgar en gancho"), "PosicionY", DefColgado[1], "Desplazamiento Y respecto al gancho");
            ColgPosZ = c.Bind(Sec("Colgar en gancho"), "PosicionZ", DefColgado[2], "Desplazamiento Z respecto al gancho");
            ColgRotX = c.Bind(Sec("Colgar en gancho"), "GiroX", DefColgado[3], "Giro en grados, eje X");
            ColgRotY = c.Bind(Sec("Colgar en gancho"), "GiroY", DefColgado[4], "Giro en grados, eje Y");
            ColgRotZ = c.Bind(Sec("Colgar en gancho"), "GiroZ", DefColgado[5], "Giro en grados, eje Z");
        }
    }

    // El menu F1 (ConfigurationManager) busca una clase con este nombre para saber como mostrar cada ajuste
    internal sealed class ConfigurationManagerAttributes
    {
        public string DispName;
        public int? Order;
    }
}
