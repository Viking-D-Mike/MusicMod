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
        public string[] NombresNotas; // como aparecen en el .cfg
        public KeyCode[] TeclasDefecto;

        public float[] DefTocar;      // distancia, altura, lateral, giroX, giroY, giroZ
        public float[] DefMano;       // distancia, altura, giroX, giroY, giroZ
        public string DefEscena, DefRaiz;
        public float[] DefTienda;     // X, Y, Z, giro, inclinacion
        public float[] DefColgado;    // X, Y, Z, giroX, giroY, giroZ

        // ---- Ajustes del .cfg ----
        public ConfigEntry<KeyCode>[] Teclas;
        public ConfigEntry<float> Volumen;
        public ConfigEntry<float> TocarDistancia, TocarAltura, TocarLateral, TocarGiroX, TocarGiroY, TocarGiroZ;
        public ConfigEntry<float> ManoDistancia, ManoAltura, ReposoGiroX, ReposoGiroY, ReposoGiroZ;
        public ConfigEntry<string> TiendaEscena, TiendaRaiz;
        public ConfigEntry<float> TiendaX, TiendaY, TiendaZ, TiendaGiro, TiendaInclinacion;
        public ConfigEntry<float> ColgPosX, ColgPosY, ColgPosZ, ColgRotX, ColgRotY, ColgRotZ;

        // ---- Lo que se rellena al jugar ----
        public AudioClip[] Clips = new AudioClip[0];
        public GameObject Prefab;
        public bool Registrado;
        public GameObject Puesto;
        public Vector3 UltimaPos;
        public float UltimoGiro, UltimaInclinacion;

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
                Teclas[i] = publico.Bind(Sec("Keys"), "Note" + (i + 1) + "_" + NombresNotas[i], TeclasDefecto[i],
                    "Key for note " + (i + 1) + " (" + NombresNotas[i] + "). Examples: Alpha6, Y, Z, Keypad1, F5");
            }
            Volumen = publico.Bind(Sec("Sound"), "Volume", 1f, "Volume (0 to 1)");

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
}
