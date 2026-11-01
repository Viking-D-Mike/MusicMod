using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaudMod
{
    [BepInPlugin("com.vikingdmike.musicmod", "MusicMod", "1.1.0")]
    public class LaudPlugin : BaseUnityPlugin
    {
        // Los tres instrumentos del mod
        public static Instrumento Laud, Flauta, Nacaires, Zanfona;
        public static Instrumento[] Todos = new Instrumento[0];

        // Piezas de HooksHangMore (si esta instalado) para cambiar la posicion de colgado en vivo
        public static Type TipoAttachable;
        public static PropertyInfo PropPosicionColgado, PropGiroColgado;

        private ConfigEntry<KeyCode> teclaLocalizar;
        private ConfigEntry<bool> todoA1;
        private float proximaRevisionPrecios;

        private const string islaInicio = "island 21 M (siren song)";
        private const string raizInicio = "island 21 M (siren song) scenery";

        private void Awake()
        {
            Logger.LogInfo("MusicMod cargado correctamente: laud, flauta xiao, nacaires y zanfona");

            Laud = new Instrumento
            {
                Clave = "laud", NombrePrefab = "laud", NombreObjeto = "lute", Prefijo = "Lute - ",
                Indice = 20391, Precio = 150, Masa = 2f, AlturaMueble = 0.15f, Sostenido = false,
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
                Indice = 20392, Precio = 80, Masa = 0.5f, AlturaMueble = 0.05f, Sostenido = true,
                Sonidos = new[] { "flauta_nota1_re", "flauta_nota2_mi", "flauta_nota3_fa_sost", "flauta_nota4_sol",
                                  "flauta_nota5_la", "flauta_nota6_si", "flauta_nota7_do_sost", "flauta_nota8_re_alto" },
                NombresNotas = new[] { "D", "E", "FSharp", "G", "A", "B", "CSharp", "HighD" },
                TeclasDefecto = new[] { KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
                                        KeyCode.Y, KeyCode.U, KeyCode.I },
                DefTocar = new[] { 0.3f, -0.1f, 0f, 270f, 90f, 90f },
                DefMano = new[] { 0.45f, -0.2f, 180f, 0f, 0f },
                DefEscena = "island 11 E (Crab Beach)", DefRaiz = "island 11 E (crab beach) scenery",
                DefTienda = new[] { 384.554f, 12.714f, -216.758f, 0f, 90f },
                DefColgado = new[] { 0f, -0.1f, -0.1f, 0f, 0f, 0f }
            };

            Nacaires = new Instrumento
            {
                Clave = "nacaires", NombrePrefab = "nacaires", NombreObjeto = "nakers", Prefijo = "Nakers - ",
                Indice = 20393, Precio = 120, Masa = 3f, AlturaMueble = 0.12f, Sostenido = false,
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
                Indice = 20394, Precio = 200, Masa = 3f, AlturaMueble = 0.12f, Sostenido = true, Colgable = false,
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

            Todos = new[] { Laud, Flauta, Nacaires, Zanfona };
            foreach (Instrumento ins in Todos)
            {
                ins.Atar(Config, interno);
            }

            teclaLocalizar = interno.Bind("Pruebas", "TeclaLocalizar", localizarDefecto,
                "Al pulsarla escribe en el log la isla, la raiz y la posicion del punto al que miras (para colocar tiendas). None = desactivada");

            todoA1 = interno.Bind("Pruebas", "PreciosDePrueba", false,
                "Si esta activado, los instrumentos sin vender cuestan casi nada (para probarlos). Desactivalo para jugar normal");

            SceneManager.sceneLoaded += AlCargarEscena;
            StartCoroutine(Preparar());
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

        private int Precio(Instrumento ins)
        {
            return todoA1.Value ? 2 : ins.Precio;
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
                if (item.Datos == ins && !item.sold && item.held == null && (item.transform.position - antes).sqrMagnitude < 4f)
                {
                    item.transform.position += desplazamiento;
                    item.transform.rotation = ins.Puesto.transform.rotation;
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
                    local.x.ToString("F3") + ", " + local.y.ToString("F3") + ", " + local.z.ToString("F3") + ")");
            }
            else
            {
                Logger.LogInfo("LOCALIZAR: no estas mirando a nada cercano");
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

        private IEnumerator Preparar()
        {
            string carpeta = Path.GetDirectoryName(Info.Location);
            string ruta = Path.Combine(carpeta, "laud_bundle");
            if (!File.Exists(ruta))
            {
                Logger.LogError("No encuentro laud_bundle en: " + ruta);
                yield break;
            }

            AssetBundleCreateRequest peticion = AssetBundle.LoadFromFileAsync(ruta);
            yield return peticion;
            AssetBundle paquete = peticion.assetBundle;
            if (paquete == null)
            {
                Logger.LogError("No he podido abrir laud_bundle");
                yield break;
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

            while (PrefabsDirectory.instance == null)
            {
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(2f);

            foreach (Instrumento ins in Todos)
            {
                if (ins.Prefab != null)
                {
                    Registrar(ins);
                }
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

        private void Registrar(Instrumento ins)
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
            item.big = false;
            // sold se queda en falso: en una tienda el objeto empieza sin vender

            SaveablePrefab guardable = prefab.AddComponent<SaveablePrefab>();

            PrefabsDirectory directorio = PrefabsDirectory.instance;
            int indice = ins.Indice;

            if (indice < directorio.directory.Length && directorio.directory[indice] != null)
            {
                Logger.LogError("La posicion " + indice + " ya esta ocupada por otro objeto: no registro " + ins.Clave + " para no pisarlo");
                return;
            }

            int largo = Mathf.Max(Mathf.Max(directorio.directory.Length, directorio.shipItems.Length), indice + 1);

            GameObject[] listaNueva = new GameObject[largo];
            directorio.directory.CopyTo(listaNueva, 0);
            listaNueva[indice] = prefab;
            directorio.directory = listaNueva;

            ShipItem[] itemsNuevos = new ShipItem[largo];
            directorio.shipItems.CopyTo(itemsNuevos, 0);
            itemsNuevos[indice] = item;
            directorio.shipItems = itemsNuevos;

            guardable.prefabIndex = indice;
            ins.Registrado = true;
            Logger.LogInfo(ins.Clave + " registrado en la posicion " + indice);
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
