using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaudMod
{
    [BepInPlugin("com.vikingdmike.musicmod", "MusicMod", "1.3.0")]
    public class LaudPlugin : BaseUnityPlugin
    {
        // Los tres instrumentos del mod
        public static Instrumento Laud, Flauta, Nacaires, Zanfona, Piano, Kalimba, Violin, Shamisen;
        public static Instrumento[] Todos = new Instrumento[0];

        // Piano: tecla para abrir/cerrar la tapa que miras y angulos de apertura
        public static ConfigEntry<KeyCode> PianoTeclaAbrir, PianoTeclaTocar;
        public static ConfigEntry<KeyCode> TeclaOctavaArriba, TeclaOctavaAbajo;
        public static ConfigEntry<float> PianoAnguloGrande, PianoAnguloTeclas;

        // Piezas de HooksHangMore (si esta instalado) para cambiar la posicion de colgado en vivo
        public static Type TipoAttachable;
        public static PropertyInfo PropPosicionColgado, PropGiroColgado;

        private ConfigEntry<KeyCode> teclaLocalizar;
        private static ConfigEntry<bool> todoA1;
        private static ManualLogSource Log;
        private float proximaRevisionPrecios;

        private const string islaInicio = "island 21 M (siren song)";
        private const string raizInicio = "island 21 M (siren song) scenery";

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo("MusicMod cargado correctamente: laud, flauta xiao, nacaires y zanfona");

            Laud = new Instrumento
            {
                Clave = "laud", NombrePrefab = "laud", NombreObjeto = "lute", Prefijo = "Lute - ",
                Indice = 20391, Precio = 450, Masa = 2f, AlturaMueble = 0.15f, Sostenido = false,
                Sonidos = new[] { "nota1_do", "nota2_re", "nota3_mi", "nota_fa", "nota4_sol", "nota5_la", "nota_si", "nota_do_alto" },
                SonidosSuaves = new[] { "nota1_do_suave", "nota2_re_suave", "nota3_mi_suave", "nota_fa_suave", "nota4_sol_suave",
                                        "nota5_la_suave", "nota_si_suave", "nota_do_alto_suave" },
                NombresNotas = new[] { "C", "D", "E", "F", "G", "A", "B", "HighC" },
                TeclasDefecto = new[] { KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
                                        KeyCode.Y, KeyCode.U, KeyCode.I },
                DefTocar = new[] { 0.44f, -0.38f, 0.125f, 300f, 180f, 0f },
                DefMano = new[] { 0.5f, 0f, 8f, 240f, 90f },
                DefEscena = islaInicio, DefRaiz = raizInicio,
                DefTienda = new[] { 28.444f, 2.28f, 30f, 25f, 0f },
                DefColgado = new[] { -0.004f, -0.39f, -0.15f, 0f, -90f, 90f }
            };

            // La flauta se vende en Emerald (Crab Beach) y los nacaires en Al'Ankh (Neverdin)
            Flauta = new Instrumento
            {
                Clave = "flauta", NombrePrefab = "flauta", NombreObjeto = "xiao flute", Prefijo = "Flute - ",
                Indice = 20392, Precio = 240, Masa = 0.5f, AlturaMueble = 0.05f, Sostenido = true,
                Sonidos = new[] { "flauta_nota1_re", "flauta_nota2_mi", "flauta_nota3_fa_sost", "flauta_nota4_sol",
                                  "flauta_nota5_la", "flauta_nota6_si", "flauta_nota7_do_sost", "flauta_nota8_re_alto" },
                NombresNotas = new[] { "D", "E", "FSharp", "G", "A", "B", "CSharp", "HighD" },
                TeclasDefecto = new[] { KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
                                        KeyCode.Y, KeyCode.U, KeyCode.I },
                DefTocar = new[] { 0.3f, -0.1f, 0f, 270f, 270f, 90f },
                DefMano = new[] { 0.45f, -0.2f, 180f, 0f, 180f },
                // 1.1.1 y anteriores: la flauta salia al reves (muesca lejos de la boca)
                DefTocarViejo = new[] { 0.3f, -0.1f, 0f, 270f, 90f, 90f },
                DefManoViejo = new[] { 0.45f, -0.2f, 180f, 0f, 0f },
                DefEscena = "island 11 E (Crab Beach)", DefRaiz = "island 11 E (crab beach) scenery",
                DefTienda = new[] { 384.554f, 12.714f, -216.758f, 0f, 90f },
                DefColgado = new[] { 0f, -0.1f, -0.1f, 0f, 0f, 0f }
            };

            Nacaires = new Instrumento
            {
                Clave = "nacaires", NombrePrefab = "nacaires", NombreObjeto = "nakers", Prefijo = "Nakers - ",
                Indice = 20393, Precio = 360, Masa = 3f, AlturaMueble = 0.12f, Sostenido = false,
                Sonidos = new[] { "nacaires_golpe1_grande_centro", "nacaires_golpe2_grande_borde",
                                  "nacaires_golpe3_pequeno_centro", "nacaires_golpe4_pequeno_borde" },
                NombresNotas = new[] { "BigCenter", "BigEdge", "SmallCenter", "SmallEdge" },
                TeclasDefecto = new[] { KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9 },
                DefTocar = new[] { 0.5f, -0.45f, 0f, 0f, 0f, 0f },
                DefMano = new[] { 0.45f, -0.1f, 0f, 0f, 0f },
                DefEscena = "island 3 A Neverdin", DefRaiz = "island 3 A (neverdin) scenery",
                DefTienda = new[] { 106.973f, 10.60f, -18.371f, 0f, 0f },
                Colgable = false
            };

            // En la version Debug (para probar) todos los ajustes salen en el .cfg y en el menu F1.
            // En la version Release (la que se publica) el jugador solo ve las teclas y el volumen;
            // el resto usa los valores de fabrica y no se guarda en ningun archivo.
            ConfigFile interno;
#if DEBUG
            interno = Config;
            KeyCode localizarDefecto = KeyCode.F10;
#else
            interno = new ConfigFile(Path.Combine(Paths.ConfigPath, "musicmod_interno_no_usado.cfg"), false);
            interno.SaveOnConfigSet = false;
            KeyCode localizarDefecto = KeyCode.None;
#endif

            // La zanfona se vende en la taberna de Aestra Abbey (la isla del monasterio)
            Zanfona = new Instrumento
            {
                Clave = "zanfona", NombrePrefab = "zanfona", NombreObjeto = "hurdy gurdy", Prefijo = "Hurdy Gurdy - ",
                Indice = 20394, Precio = 600, Masa = 3f, AlturaMueble = 0.12f, Sostenido = true, Colgable = false,
                Sonidos = new[] { "zanfona_nota01_mi", "zanfona_nota02_fa_sost", "zanfona_nota03_sol_sost", "zanfona_nota04_la",
                                  "zanfona_nota05_si", "zanfona_nota06_do_sost", "zanfona_nota07_re_sost", "zanfona_nota08_mi",
                                  "zanfona_nota09_fa_sost", "zanfona_nota10_sol_sost" },
                SonidoFondo = "zanfona_bordon",
                NombresNotas = new[] { "E", "FSharp", "GSharp", "A", "B", "CSharp", "DSharp", "HighE", "HighFSharp", "HighGSharp" },
                TeclasDefecto = new[] { KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
                                        KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P },
                DefTocar = new[] { 0.5f, -0.3f, 0.125f, 350f, 180f, 0f },
                DefMano = new[] { 0.5f, 0f, 8f, 240f, 90f },
                DefEscena = "island 34 M (monastery)", DefRaiz = "_scenery",
                DefTienda = new[] { 420.158f, 4.386f, 391.355f, 0f, 0f },
                DefColgado = new[] { 0f, 0f, 0f, 0f, 0f, 0f }
            };

            // El piano se vende en Happy Bay (por dentro la isla se llama "Oasis"). Se toca colocado: mirandolo y pulsando R
            Piano = new Instrumento
            {
                Clave = "piano", NombrePrefab = "piano", NombreObjeto = "square piano", Prefijo = "Piano - ",
                Indice = 20396, Precio = 8750, Masa = 40f, AlturaMueble = 0f, Sostenido = false, Colgable = false,
                // Una octava completa: las teclas negras usan la nota blanca de al lado subida medio tono
                Sonidos = new[] { "piano_nota1_do", "piano_nota1_do", "piano_nota2_re", "piano_nota2_re", "piano_nota3_mi",
                                  "piano_nota4_fa", "piano_nota4_fa", "piano_nota5_sol", "piano_nota5_sol",
                                  "piano_nota6_la", "piano_nota6_la", "piano_nota7_si", "piano_nota8_do_alto" },
                Desvios = new[] { 0f, 1f, 0f, 1f, 0f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 0f },
                // Sin version suave: con el teclado no se sabe la fuerza de la pulsacion, asi que suena siempre igual
                NombresNotas = new[] { "LowC", "CSharp", "D", "DSharp", "E", "F", "FSharp", "G", "GSharp", "A", "ASharp", "B", "HighC" },
                // Blancas en la fila de abajo (C V B N M , . -) y negras en la del medio (G H J K L)
                TeclasDefecto = new[] { KeyCode.C, KeyCode.G, KeyCode.V, KeyCode.H, KeyCode.B, KeyCode.N, KeyCode.J,
                                        KeyCode.M, KeyCode.K, KeyCode.Comma, KeyCode.L, KeyCode.Period, KeyCode.Minus },
                DefTocar = new[] { 1.2f, -0.9f, 0f, 0f, 180f, 0f },
                DefMano = new[] { 1.5f, -0.9f, 0f, 180f, 0f },
                DefEscena = "island 18 M (Oasis)", DefRaiz = "island 18 M (Oasis) scenery",
                DefTienda = new[] { -100.395f, 8.65f, 46.95f, 228f, 0f }
            };

            // La kalimba se vende en Al'Ankh, en la mesa de las tiendas de Al'Nilem.
            // Cada lamina suena una vez y se apaga sola, como el laud.
            Kalimba = new Instrumento
            {
                Clave = "kalimba", NombrePrefab = "kalimba", NombreObjeto = "kalimba", Prefijo = "Kalimba - ",
                Indice = 20395, Precio = 360, Masa = 0.8f, AlturaMueble = 0.03f, Sostenido = false, Colgable = false,
                Sonidos = new[] { "kalimba_nota01_do", "kalimba_nota02_re", "kalimba_nota03_mi", "kalimba_nota04_fa", "kalimba_nota05_sol",
                                  "kalimba_nota06_la", "kalimba_nota07_si", "kalimba_nota08_do_alto", "kalimba_nota09_re_alto", "kalimba_nota10_mi_alto" },
                NombresNotas = new[] { "C", "D", "E", "F", "G", "A", "B", "HighC", "HighD", "HighE" },
                TeclasDefecto = new[] { KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
                                        KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P },
                DefTocar = new[] { 0.4f, -0.15f, 0f, 30f, 180f, 0f },
                DefMano = new[] { 0.45f, -0.15f, 30f, 180f, 0f },
                DefEscena = "island 2 A AlNilem", DefRaiz = "island 2 A (alnilem) scenery",
                DefTienda = new[] { 267.963f, 13.485f, 56.3f, 0f, 0f },
                DefColgado = new[] { 0f, -0.1f, -0.1f, 0f, 0f, 0f }
            };

            // El violin: suena mientras mantienes la tecla, como la flauta, y el arco va y viene.
            // Se vende en Chronos (el viaje mas largo del juego), en una mesa
            Violin = new Instrumento
            {
                Clave = "violin", NombrePrefab = "violin", NombreObjeto = "violin", Prefijo = "Violin - ",
                Indice = 20397, Precio = 3000, Masa = 1f, AlturaMueble = 0.06f, Sostenido = true,
                Sonidos = new[] { "violin_nota1_la", "violin_nota2_si", "violin_nota3_do_sost", "violin_nota4_re",
                                  "violin_nota5_mi", "violin_nota6_fa_sost", "violin_nota7_sol_sost", "violin_nota8_la_alto" },
                NombresNotas = new[] { "A", "B", "CSharp", "D", "E", "FSharp", "GSharp", "HighA" },
                TeclasDefecto = new[] { KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
                                        KeyCode.Y, KeyCode.U, KeyCode.I },
                DefTocar = new[] { 0.3f, -0.2f, -0.1f, 10f, 160f, 15f },
                DefMano = new[] { 0.5f, -0.2f, 0f, 180f, 0f },
                DefEscena = "island 25 (chronos)", DefRaiz = "island 25 (chronos) scenery",
                DefTienda = new[] { 545.192f, 8.40f, 451.623f, 45f, 4f },
                DefColgado = new[] { 0f, -0.28f, -0.1f, 90f, 270f, 90f }
            };

            // El shamisen: como el laud, cada pulsacion suena una vez. Escala japonesa "in".
            // Se vende en Kicia Bay (Fire Fish Lagoon), junto a la mesa de las maquetas de barcos
            Shamisen = new Instrumento
            {
                Clave = "shamisen", NombrePrefab = "shamisen", NombreObjeto = "shamisen", Prefijo = "Shamisen - ",
                Indice = 20398, Precio = 3840, Masa = 1.5f, AlturaMueble = 0.06f, Sostenido = false,
                Sonidos = new[] { "shamisen_nota1_sol", "shamisen_nota2_lab", "shamisen_nota3_do", "shamisen_nota4_reb",
                                  "shamisen_nota5_fa", "shamisen_nota6_sol", "shamisen_nota7_lab", "shamisen_nota8_do_alto" },
                NombresNotas = new[] { "LowG", "LowAb", "C", "Db", "F", "G", "Ab", "HighC" },
                TeclasDefecto = new[] { KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
                                        KeyCode.Y, KeyCode.U, KeyCode.I },
                DefTocar = new[] { 0.34f, -0.4f, 0f, -25f, 10f, 15f },
                DefMano = new[] { 0.5f, -0.3f, 0f, 180f, 0f },
                DefEscena = "island 27 Lagoon SwampShipyard", DefRaiz = "island scenery Lagoon Shipyard",
                DefTienda = new[] { 3.9f, 5.44f, 34.322f, 90f, -15f },
                DefColgado = new[] { 0f, -0.3f, -0.08f, 0f, 180f, 0f }
            };

            Todos = new[] { Laud, Flauta, Nacaires, Zanfona, Piano, Kalimba, Violin, Shamisen };
            foreach (Instrumento ins in Todos)
            {
                ins.Atar(Config, interno);
            }

            // Cambio de octava (todos los instrumentos): manteniendo estas teclas, las notas suenan una octava mas aguda o mas grave
            TeclaOctavaArriba = Config.Bind("General - Octave", "OctaveUp", KeyCode.LeftShift, "Hold it while playing: the notes sound one octave higher (None = off)");
            TeclaOctavaAbajo = Config.Bind("General - Octave", "OctaveDown", KeyCode.LeftAlt, "Hold it while playing: the notes sound one octave lower (None = off)");
            PianoTeclaAbrir = Config.Bind("Piano - Keys", "OpenLid", KeyCode.T, "Key to open or close the piano lid you are looking at");
            PianoTeclaTocar = Config.Bind("Piano - Keys", "Play", KeyCode.R, "Key to start or stop playing the piano you are looking at (it must be placed, not in your hands)");
            PianoAnguloGrande = interno.Bind("Piano - Tapas", "AnguloTapaGrande", -45f, "Grados que se abre la tapa grande (si gira hacia el lado contrario, cambia el signo)");
            PianoAnguloTeclas = interno.Bind("Piano - Tapas", "AnguloTapaTeclas", -100f, "Grados que se abre la tapa de las teclas (si gira hacia el lado contrario, cambia el signo)");

            teclaLocalizar = interno.Bind("Pruebas", "TeclaLocalizar", localizarDefecto,
                "Al pulsarla escribe en el log la isla, la raiz y la posicion del punto al que miras (para colocar tiendas). None = desactivada");

            todoA1 = interno.Bind("Pruebas", "PreciosDePrueba", false,
                "Si esta activado, los instrumentos sin vender cuestan casi nada (para probarlos). Desactivalo para jugar normal");

            SceneManager.sceneLoaded += AlCargarEscena;

            // El paquete se carga YA (no en segundo plano) y los instrumentos se meten en la lista de objetos
            // del juego justo antes de que el juego la prepare (PrefabsDirectory.Start). Asi, cuando se carga
            // una partida, los instrumentos guardados siempre existen: no se pierden aunque cargues muy deprisa
            // ni aunque uses mods como Save Cleaner.
            CargarPaquete();
            foreach (Instrumento ins in Todos)
            {
                if (ins.Prefab != null)
                {
                    PrepararPrefab(ins);
                }
            }
            Harmony.CreateAndPatchAll(typeof(ParcheDirectorio), "com.vikingdmike.musicmod");
            ProtegerTiendasDeBarcos();

            // Por si el juego ya hubiera preparado su lista antes que nosotros (no deberia pasar)
            if (PrefabsDirectory.instance != null)
            {
                InsertarTodos(PrefabsDirectory.instance);
                AccessTools.Method(typeof(PrefabsDirectory), "PopulateShipItems").Invoke(PrefabsDirectory.instance, null);
            }

            StartCoroutine(Comprobar());
        }

        // Un instrumento que aun esta en la tienda (sin comprar) no debe "subirse" a un barco
        // amarrado cerca: si el juego lo mete en el barco, lo recoloca mal (debajo de la mesa, etc.)
        private void ProtegerTiendasDeBarcos()
        {
            Harmony h = new Harmony("com.vikingdmike.musicmod.tiendas");
            MethodInfo prefijo = typeof(LaudPlugin).GetMethod("NoSubirSiEnTienda", BindingFlags.Static | BindingFlags.NonPublic);
            int n = 0;
            foreach (Type t in typeof(ShipItem).Assembly.GetTypes())
            {
                foreach (MethodInfo m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (m.Name != "EnterBoat" || m.IsAbstract || m.ContainsGenericParameters) continue;
                    try
                    {
                        h.Patch(m, prefix: new HarmonyMethod(prefijo));
                        n++;
                        Logger.LogInfo("TIENDA: protegido " + t.Name + ".EnterBoat");
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning("TIENDA: no he podido proteger " + t.Name + ".EnterBoat: " + e.Message);
                    }
                }
            }
            if (n == 0) Logger.LogWarning("TIENDA: no he encontrado EnterBoat en el juego");
        }

        private static bool NoSubirSiEnTienda(object __instance)
        {
            InstrumentoItem item = __instance as InstrumentoItem;
            if (item == null)
            {
                Component c = __instance as Component;
                if (c != null) item = c.GetComponent<InstrumentoItem>();
                // Puede que el juego llame a EnterBoat en el cuerpo fisico, que es un objeto aparte
                if (item == null && c != null && c.name.EndsWith(":ItemRigidbody"))
                {
                    foreach (InstrumentoItem i in FindObjectsOfType<InstrumentoItem>())
                    {
                        if (i.itemRigidbodyC != null && i.itemRigidbodyC.gameObject == c.gameObject) { item = i; break; }
                    }
                }
            }
            if (item != null && !item.sold && item.held == null)
            {
                return false;   // sigue en la tienda: no lo metemos en el barco
            }
            return true;
        }

        [HarmonyPatch(typeof(PrefabsDirectory), "Start")]
        private static class ParcheDirectorio
        {
            private static void Prefix(PrefabsDirectory __instance)
            {
                InsertarTodos(__instance);
            }
        }

        // Blender guarda los colores "en lineal", pero Unity los lee como si fueran el color que se ve (sRGB),
        // así que salen oscurecidos dos veces (la madera se ve gris). Los devolvemos al color de Blender
        // y quitamos el "metálico", que en el juego refleja el cielo y apaga los colores.
        private static readonly System.Collections.Generic.HashSet<Material> coloresArreglados = new System.Collections.Generic.HashSet<Material>();
        private static void AjustarColores(GameObject go)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null || !coloresArreglados.Add(m)) continue;
                    if (m.HasProperty("_Color"))
                    {
                        Color c = m.color;
                        m.color = new Color(Mathf.LinearToGammaSpace(c.r), Mathf.LinearToGammaSpace(c.g), Mathf.LinearToGammaSpace(c.b), c.a);
                    }
                    if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                }
        }

        private void Update()
        {
            if (teclaLocalizar.Value != KeyCode.None && Input.GetKeyDown(teclaLocalizar.Value))
            {
                Localizar();
            }

            // Precio de prueba: se aplica en vivo a los instrumentos que aun estan en la tienda
            if (Time.time >= proximaRevisionPrecios)
            {
                proximaRevisionPrecios = Time.time + 1f;
                foreach (InstrumentoItem item in FindObjectsOfType<InstrumentoItem>())
                {
                    if (!item.sold && item.Datos != null)
                    {
                        item.value = Precio(item.Datos);
                        // Si un instrumento de la tienda se ha hundido o movido de su sitio, lo devolvemos
                        if (item.held == null && item.Datos.Puesto != null)
                        {
                            float d = Vector3.Distance(item.transform.position, item.Datos.Puesto.transform.position);
                            if (d > 0.05f && d < 3f) ColocarEnPuesto(item, item.Datos);
                        }
                    }
                }
                foreach (Instrumento ins in Todos)
                {
                    if (ins.Prefab != null && ins.Registrado)
                    {
                        ShipItem enPrefab = ins.Prefab.GetComponent<ShipItem>();
                        if (enPrefab != null) enPrefab.value = Precio(ins);
                    }
                }
            }

            // Si cambias la posicion de una tienda en el menu F1, el puesto y el instrumento sin vender se mueven al momento
            foreach (Instrumento ins in Todos)
            {
                MoverPuesto(ins);
            }
        }

        private static int Precio(Instrumento ins)
        {
            return todoA1.Value ? 2 : ins.Precio;
        }

        // Pone un instrumento de tienda (sin vender) exactamente en su puesto, con su física,
        // por si al aparecer se ha hundido o desplazado
        private static void ColocarEnPuesto(InstrumentoItem item, Instrumento ins)
        {
            Transform p = ins.Puesto.transform;
            Rigidbody rb = item.itemRigidbodyC != null ? item.itemRigidbodyC.GetComponent<Rigidbody>() : null;
            Matrix4x4 aFisica = rb != null ? rb.transform.localToWorldMatrix * item.transform.worldToLocalMatrix : Matrix4x4.identity;
            item.transform.position = p.position;
            item.transform.rotation = p.rotation;
            if (rb != null)
            {
                Vector3 pos = aFisica.MultiplyPoint3x4(p.position);
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = pos;
                rb.transform.position = pos;
            }
        }

        private void MoverPuesto(Instrumento ins)
        {
            if (ins.Puesto == null)
            {
                return;
            }

            Vector3 nueva = new Vector3(ins.TiendaX.Value, ins.TiendaY.Value, ins.TiendaZ.Value);
            if (nueva == ins.UltimaPos && ins.TiendaGiro.Value == ins.UltimoGiro && ins.TiendaInclinacion.Value == ins.UltimaInclinacion)
            {
                return;
            }

            Vector3 antes = ins.Puesto.transform.position;
            ins.Puesto.transform.localPosition = nueva;
            ins.Puesto.transform.localRotation = Quaternion.Euler(ins.TiendaInclinacion.Value, ins.TiendaGiro.Value, 0f);
            Vector3 desplazamiento = ins.Puesto.transform.position - antes;

            foreach (InstrumentoItem item in FindObjectsOfType<InstrumentoItem>())
            {
                if (item.Datos == ins && !item.sold && item.held == null && (item.transform.position - antes).sqrMagnitude < 9f)
                {
                    ColocarEnPuesto(item, ins);
                }
            }

            ins.UltimaPos = nueva;
            ins.UltimoGiro = ins.TiendaGiro.Value;
            ins.UltimaInclinacion = ins.TiendaInclinacion.Value;
        }

        // Escribe en el log donde esta el punto al que miras, para poder colocar tiendas nuevas
        private void Localizar()
        {
            Camera camara = Camera.main;
            if (camara == null)
            {
                return;
            }
            RaycastHit golpe;
            if (Physics.Raycast(camara.transform.position, camara.transform.forward, out golpe, 20f, ~0, QueryTriggerInteraction.Ignore))
            {
                Transform raiz = golpe.transform.root;
                Vector3 local = raiz.InverseTransformPoint(golpe.point);
                Logger.LogInfo("LOCALIZAR: escena '" + raiz.gameObject.scene.name + "', raiz '" + raiz.name +
                    "', objeto '" + golpe.transform.name + "', punto respecto a la raiz (" +
                    local.x.ToString("F3") + ", " + local.y.ToString("F3") + ", " + local.z.ToString("F3") + "), Y del mundo " +
                    golpe.point.y.ToString("F3") + ", escala de la raiz " + raiz.lossyScale.ToString("F2"));
            }
            else
            {
                Logger.LogInfo("LOCALIZAR: no estas mirando a nada cercano");
            }
            // Datos de los instrumentos de tienda cercanos: dónde está cada uno respecto a su puesto y hasta dónde llega su dibujo
            foreach (InstrumentoItem it in FindObjectsOfType<InstrumentoItem>())
            {
                if (it.sold || it.Datos == null || it.Datos.Puesto == null) continue;
                if (Vector3.Distance(it.transform.position, camara.transform.position) > 15f) continue;
                Bounds b = new Bounds(it.transform.position, Vector3.zero);
                foreach (Renderer r in it.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                Rigidbody rb = it.itemRigidbodyC != null ? it.itemRigidbodyC.GetComponent<Rigidbody>() : null;
                Logger.LogInfo("LOCALIZAR: " + it.Datos.Clave + " en Y " + it.transform.position.y.ToString("F3") +
                    ", puesto en Y " + it.Datos.Puesto.transform.position.y.ToString("F3") +
                    ", su dibujo va de Y " + b.min.y.ToString("F3") + " a " + b.max.y.ToString("F3") +
                    ", fisica en Y " + (rb != null ? rb.position.y.ToString("F3") : "-") + (rb != null && rb.isKinematic ? " (quieta)" : " (libre)") +
                    ", padre '" + (it.transform.parent != null ? it.transform.parent.name : "-") + "'");
            }
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            foreach (Instrumento ins in Todos)
            {
                if (escena.name == ins.TiendaEscena.Value)
                {
                    StartCoroutine(PonerEnTienda(ins));
                }
            }
        }

        private void CargarPaquete()
        {
            string carpeta = Path.GetDirectoryName(Info.Location);
            string ruta = Path.Combine(carpeta, "laud_bundle");
            if (!File.Exists(ruta))
            {
                Logger.LogError("No encuentro laud_bundle en: " + ruta);
                return;
            }

            AssetBundle paquete = AssetBundle.LoadFromFile(ruta);
            if (paquete == null)
            {
                Logger.LogError("No he podido abrir laud_bundle");
                return;
            }

            // Cada instrumento carga su prefab y SOLO sus sonidos, por su nombre
            foreach (Instrumento ins in Todos)
            {
                ins.Prefab = paquete.LoadAsset<GameObject>(ins.NombrePrefab);
                if (ins.Prefab == null)
                {
                    Logger.LogError("No encuentro el prefab '" + ins.NombrePrefab + "' dentro del paquete");
                    continue;
                }
                AjustarColores(ins.Prefab);
                ins.Clips = new AudioClip[ins.Sonidos.Length];
                int encontrados = 0;
                for (int i = 0; i < ins.Sonidos.Length; i++)
                {
                    ins.Clips[i] = paquete.LoadAsset<AudioClip>(ins.Sonidos[i]);
                    if (ins.Clips[i] == null)
                    {
                        Logger.LogWarning("Falta el sonido '" + ins.Sonidos[i] + "'");
                    }
                    else
                    {
                        encontrados++;
                    }
                }
                Logger.LogInfo(ins.Clave + ": sonidos cargados " + encontrados + " de " + ins.Sonidos.Length);
                if (ins.SonidosSuaves != null)
                {
                    ins.ClipsSuaves = new AudioClip[ins.SonidosSuaves.Length];
                    int suaves = 0;
                    for (int i = 0; i < ins.SonidosSuaves.Length; i++)
                    {
                        ins.ClipsSuaves[i] = paquete.LoadAsset<AudioClip>(ins.SonidosSuaves[i]);
                        if (ins.ClipsSuaves[i] != null) suaves++;
                    }
                    Logger.LogInfo(ins.Clave + ": sonidos suaves cargados " + suaves + " de " + ins.SonidosSuaves.Length);
                }
                if (ins.SonidoFondo != null)
                {
                    ins.ClipFondo = paquete.LoadAsset<AudioClip>(ins.SonidoFondo);
                    Logger.LogInfo(ins.Clave + ": sonido de fondo " + (ins.ClipFondo != null ? "cargado" : "NO encontrado (" + ins.SonidoFondo + ")"));
                }
            }
        }

        // Despues de cargar: tiendas, ganchos de HooksHangMore y comprobar que ningun otro mod nos pisa
        private IEnumerator Comprobar()
        {
            while (PrefabsDirectory.instance == null)
            {
                yield return new WaitForSeconds(1f);
            }
            RegistrarColgado();

            foreach (Instrumento ins in Todos)
            {
                if (ins.Registrado && SceneManager.GetSceneByName(ins.TiendaEscena.Value).isLoaded)
                {
                    StartCoroutine(PonerEnTienda(ins));
                }
            }

            while (GameState.currentlyLoading || GameState.justStarted || !GameState.playing)
            {
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(5f);

            foreach (Instrumento ins in Todos)
            {
                if (ins.Registrado && (PrefabsDirectory.instance.directory.Length <= ins.Indice || PrefabsDirectory.instance.directory[ins.Indice] != ins.Prefab))
                {
                    Logger.LogError("AVISO: otro mod ha pisado la posicion " + ins.Indice + " (" + ins.Clave + ")");
                }
            }
        }

        // Se hace una sola vez: anade al prefab el sonido y el codigo del instrumento
        private void PrepararPrefab(Instrumento ins)
        {
            GameObject prefab = ins.Prefab;

            AudioSource fuente = prefab.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.loop = ins.Sostenido;
            fuente.spatialBlend = 1f;
            fuente.rolloffMode = AudioRolloffMode.Logarithmic;
            fuente.minDistance = 2f;
            fuente.maxDistance = 60f;

            InstrumentoItem item;
            if (ins == Flauta)
            {
                item = prefab.AddComponent<FlautaItem>();
            }
            else if (ins == Nacaires)
            {
                item = prefab.AddComponent<NacairesItem>();
            }
            else if (ins == Zanfona)
            {
                item = prefab.AddComponent<ZanfonaItem>();
            }
            else if (ins == Violin)
            {
                item = prefab.AddComponent<ViolinItem>();
            }
            else if (ins == Kalimba)
            {
                item = prefab.AddComponent<KalimbaItem>();
            }
            else if (ins == Shamisen)
            {
                item = prefab.AddComponent<ShamisenItem>();
            }
            else if (ins == Piano)
            {
                item = prefab.AddComponent<PianoItem>();
                prefab.AddComponent<PianoTapas>();
            }
            else
            {
                item = prefab.AddComponent<LaudItem>();
            }
            item.holdDistance = ins.ManoDistancia.Value;
            item.furniturePlaceHeight = ins.AlturaMueble;
            item.mass = ins.Masa;
            item.value = Precio(ins);
            item.name = ins.NombreObjeto;
            item.category = TransactionCategory.toolsAndSupplies;
            item.inventoryScale = 1f;
            item.floaterHeight = 1.6f;
            item.allowPlacingItems = true;
            item.big = ins == Piano; // el piano es un mueble grande: se puede clavar al suelo con el martillo
            // sold se queda en falso: en una tienda el objeto empieza sin vender

            SaveablePrefab guardable = prefab.AddComponent<SaveablePrefab>();
            guardable.prefabIndex = ins.Indice;
        }

        // Mete los instrumentos en la lista de objetos del juego (se llama justo antes de PrefabsDirectory.Start)
        private static void InsertarTodos(PrefabsDirectory directorio)
        {
            foreach (Instrumento ins in Todos)
            {
                if (ins.Prefab != null)
                {
                    Insertar(directorio, ins);
                }
            }
        }

        private static void Insertar(PrefabsDirectory directorio, Instrumento ins)
        {
            int indice = ins.Indice;

            if (indice < directorio.directory.Length && directorio.directory[indice] != null)
            {
                if (directorio.directory[indice] != ins.Prefab)
                {
                    Log.LogError("La posicion " + indice + " ya esta ocupada por otro objeto: no registro " + ins.Clave + " para no pisarlo");
                }
                return;
            }

            int largo = Mathf.Max(directorio.directory.Length, indice + 1);
            GameObject[] listaNueva = new GameObject[largo];
            directorio.directory.CopyTo(listaNueva, 0);
            listaNueva[indice] = ins.Prefab;
            directorio.directory = listaNueva;

            ins.Registrado = true;
            Log.LogInfo(ins.Clave + " registrado en la posicion " + indice);
        }

        // Si el mod HooksHangMore esta instalado, le decimos como colgar cada instrumento (sin depender de el)
        private void RegistrarColgado()
        {
            try
            {
                Type tipo = null;
                foreach (Assembly ensamblado in AppDomain.CurrentDomain.GetAssemblies())
                {
                    tipo = ensamblado.GetType("HooksHangMore.HHM_Plugin");
                    if (tipo != null) break;
                }
                if (tipo == null)
                {
                    Logger.LogInfo("COLGAR: HooksHangMore no esta instalado, los instrumentos no se podran colgar");
                    return;
                }
                foreach (Assembly ensamblado in AppDomain.CurrentDomain.GetAssemblies())
                {
                    TipoAttachable = ensamblado.GetType("HooksHangMore.AttachableItem");
                    if (TipoAttachable != null) break;
                }
                if (TipoAttachable != null)
                {
                    PropPosicionColgado = TipoAttachable.GetProperty("PositionOffset", BindingFlags.Public | BindingFlags.Instance);
                    PropGiroColgado = TipoAttachable.GetProperty("RotationOffset", BindingFlags.Public | BindingFlags.Instance);
                }
                MethodInfo metodo = tipo.GetMethod("AddAttachedOffset", BindingFlags.Public | BindingFlags.Static);
                if (metodo == null)
                {
                    Logger.LogWarning("COLGAR: esta version de HooksHangMore no tiene AddAttachedOffset");
                    return;
                }
                foreach (Instrumento ins in Todos)
                {
                    if (!ins.Registrado || !ins.Colgable) continue;
                    Vector3 pos = new Vector3(ins.ColgPosX.Value, ins.ColgPosY.Value, ins.ColgPosZ.Value);
                    Vector3 rot = new Vector3(ins.ColgRotX.Value, ins.ColgRotY.Value, ins.ColgRotZ.Value);
                    object resultado = metodo.Invoke(null, new object[] { ins.NombreObjeto, pos, rot });
                    Logger.LogInfo("COLGAR: AddAttachedOffset('" + ins.NombreObjeto + "') -> " + resultado);
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning("COLGAR: no se pudo registrar el colgado: " + e.Message);
            }
        }

        private IEnumerator PonerEnTienda(Instrumento ins)
        {
            while (!ins.Registrado)
            {
                yield return new WaitForSeconds(1f);
            }

            // Se busca la raiz dentro de la escena de la isla: algunas islas tienen raices con nombres
            // genericos (como "_scenery") que tambien existen en otras islas
            GameObject raiz = null;
            for (int i = 0; i < 30; i++)
            {
                Scene escena = SceneManager.GetSceneByName(ins.TiendaEscena.Value);
                if (escena.isLoaded)
                {
                    foreach (GameObject candidato in escena.GetRootGameObjects())
                    {
                        if (candidato.name == ins.TiendaRaiz.Value)
                        {
                            raiz = candidato;
                            break;
                        }
                    }
                }
                if (raiz == null)
                {
                    raiz = GameObject.Find(ins.TiendaRaiz.Value);
                    if (raiz != null && raiz.scene.name != ins.TiendaEscena.Value && escena.isLoaded)
                    {
                        raiz = null;
                    }
                }
                if (raiz != null)
                {
                    break;
                }
                yield return new WaitForSeconds(1f);
            }

            if (raiz == null)
            {
                Logger.LogError("TIENDA: no encuentro " + ins.TiendaRaiz.Value);
                yield break;
            }

            string nombrePuesto = ins.Clave + " spawner";
            if (raiz.transform.Find(nombrePuesto) != null)
            {
                yield break;
            }

            GameObject puesto = new GameObject(nombrePuesto);
            puesto.transform.parent = raiz.transform;
            puesto.transform.localPosition = new Vector3(ins.TiendaX.Value, ins.TiendaY.Value, ins.TiendaZ.Value);
            puesto.transform.localRotation = Quaternion.Euler(ins.TiendaInclinacion.Value, ins.TiendaGiro.Value, 0f);
            ins.Puesto = puesto;
            ins.UltimaPos = puesto.transform.localPosition;
            ins.UltimoGiro = ins.TiendaGiro.Value;
            ins.UltimaInclinacion = ins.TiendaInclinacion.Value;

            MeshFilter filtro = puesto.AddComponent<MeshFilter>();
            filtro.sharedMesh = ins.Prefab.GetComponent<MeshFilter>().sharedMesh;
            puesto.AddComponent<MeshRenderer>();

            ShopItemSpawner spawner = puesto.AddComponent<ShopItemSpawner>();
            spawner.itemPrefab = ins.Prefab;

            Logger.LogInfo("TIENDA: spawner de " + ins.Clave + " creado en " + ins.TiendaRaiz.Value);
        }
    }
}
