using System.Reflection;
using UnityEngine;

namespace LaudMod
{
    // El piano: ademas de tocarlo (R), la tecla de abrir (T) abre o cierra la tapa a la que miras.
    // - Tapa de las teclas: se abre sola mientras tocas.
    // - Tapa grande: al abrirse, el brazo de hierro se dobla para sujetarla.
    [DefaultExecutionOrder(1000)]
    public class PianoItem : InstrumentoItem
    {
        public override Instrumento Datos { get { return LaudPlugin.Piano; } }
        public bool Tocando { get { return tocando; } }
        public bool EnLaMano { get { return held != null; } }

        // El piano es un mueble: no forzamos ninguna postura al llevarlo en la mano.
        // Asi el juego (y los mods de mover objetos) lo tratan como cualquier otro mueble.
        // Mas adelante se tocara colocado, no en la mano.
        public override void ExtraLateUpdate()
        {
        }

        // En la mano, la tecla de usar gira el piano como cualquier mueble
        protected override bool UsaTeclaUsar { get { return false; } }

        // El texto que sale al mirarlo. Lo prepara PianoTapas; aqui se vuelve a poner
        // cada vez que el juego lo recalcula (si no, el juego lo pisa con el suyo)
        public string TextoAyuda;
        public override void UpdateLookText()
        {
            base.UpdateLookText();
            if (sold && held == null && !string.IsNullOrEmpty(TextoAyuda))
            {
                lookText = TextoAyuda;
            }
        }

        // Lo llama PianoTapas en cada fotograma: entra o sale del modo tocar y lee las teclas de nota
        public void TocarColocado(bool activo)
        {
            if (activo != tocando)
            {
                Debug.Log("PIANO: modo tocar " + (activo ? "activado" : "desactivado"));
            }
            tocando = activo;
            LeerTeclas(activo);
        }
    }

    // Las tapas y el brazo del piano. Va en un componente aparte para que funcione siempre,
    // tambien cuando el piano esta en el suelo o en la tienda.
    [DefaultExecutionOrder(1000)]
    public class PianoTapas : MonoBehaviour
    {
        private PianoItem piano;

        // Para no poder andar mientras tocas: el "mando" del jugador tiene un interruptor de movimiento
        private static System.Type tipoJugador;
        private static Object jugador;
        private static FieldInfo campoMover;
        private static bool buscadoCampo;
        private bool bloqueando;

        private const float velocidadTapa = 1.2f;   // aperturas por segundo
        private const float distanciaMirar = 3f;
        private const float distanciaSalir = 4f;   // si te alejas mas que esto, dejas de tocar

        private bool preparado;
        private Transform tapaGrande, tapaTeclas, brazoArriba, brazoAbajo;
        private Quaternion rotGrande0, rotTeclas0, rotArriba0, rotAbajo0;
        private Renderer rendTeclas, rendGrande;
        private Bounds zonaTeclas, zonaGrande;   // donde estan las tapas cerradas, en el espacio del piano
        private float abiertoGrande, abiertoTeclas;   // 0 = cerrada, 1 = abierta
        private bool quiereGrande, quiereTeclas;

        // Brazo: punta de cada pletina (en su propio espacio), largos y hacia que lado se dobla el codo
        private bool hayBrazo;
        private Vector3 puntaAbajoLocal, puntaArribaLocal, dirAbajo0, dirArriba0;
        private float largoAbajo, largoArriba, ladoCodo = 1f;
        private float anguloBrazo;   // apertura de la tapa grande con el brazo justo estirado (0 = usar el ajuste)

        private void Preparar()
        {
            preparado = true;
            tapaGrande = Buscar("tapa_grande");
            tapaTeclas = Buscar("tapa_teclas");
            brazoArriba = Buscar("brazo_arriba");
            brazoAbajo = Buscar("brazo_abajo");
            if (tapaGrande != null)
            {
                rotGrande0 = tapaGrande.localRotation;
                rendGrande = tapaGrande.GetComponent<Renderer>();
                zonaGrande = CajaLocal(tapaGrande);
            }
            if (tapaTeclas != null)
            {
                rotTeclas0 = tapaTeclas.localRotation;
                rendTeclas = tapaTeclas.GetComponent<Renderer>();
                zonaTeclas = CajaLocal(tapaTeclas);
            }
            Debug.Log("PIANO: piezas encontradas: tapa_grande " + (tapaGrande != null) + ", tapa_teclas " + (tapaTeclas != null) +
                      ", brazo_arriba " + (brazoArriba != null) + ", brazo_abajo " + (brazoAbajo != null));

            if (tapaGrande == null || brazoArriba == null || brazoAbajo == null)
            {
                return;
            }
            rotArriba0 = brazoArriba.localRotation;
            rotAbajo0 = brazoAbajo.localRotation;
            if (!Punta(brazoAbajo, out puntaAbajoLocal) || !Punta(brazoArriba, out puntaArribaLocal))
            {
                return;
            }
            largoAbajo = puntaAbajoLocal.magnitude;
            largoArriba = puntaArribaLocal.magnitude;
            dirAbajo0 = rotAbajo0 * puntaAbajoLocal;
            dirArriba0 = rotArriba0 * puntaArribaLocal;

            // Con la tapa cerrada el brazo esta plegado: miramos hacia que lado sale el codo
            Vector3 a = brazoAbajo.position, b = brazoArriba.position;
            Vector3 codo = brazoAbajo.TransformPoint(puntaAbajoLocal);
            Vector3 n = tapaGrande.right;
            Vector3 u = (b - a).normalized;
            Vector3 v = Vector3.Cross(n, u);
            ladoCodo = Vector3.Dot(codo - a, v) >= 0f ? 1f : -1f;
            hayBrazo = largoAbajo > 0.001f && largoArriba > 0.001f;

            // Probamos angulos de la tapa grande hasta que el brazo queda recto: esa es su apertura maxima
            if (hayBrazo)
            {
                float signo = LaudPlugin.PianoAnguloGrande.Value >= 0f ? 1f : -1f;
                float estirado = (largoAbajo + largoArriba) * 0.998f;
                for (float g = 1f; g <= 150f; g += 0.25f)
                {
                    tapaGrande.localRotation = rotGrande0 * Quaternion.AngleAxis(signo * g, Vector3.right);
                    if ((brazoArriba.position - brazoAbajo.position).magnitude >= estirado)
                    {
                        anguloBrazo = signo * g;
                        break;
                    }
                }
                tapaGrande.localRotation = rotGrande0;
                Debug.Log("PIANO: la tapa grande se abre " + anguloBrazo.ToString("F1") + " grados (brazo estirado)");
            }
            Debug.Log("PIANO: brazo largo abajo " + largoAbajo.ToString("F3") + " arriba " + largoArriba.ToString("F3") + " lado " + ladoCodo);
        }

        // Caja que ocupa una pieza (con la tapa cerrada), medida en el espacio del piano
        private Bounds CajaLocal(Transform pieza)
        {
            MeshFilter mf = pieza.GetComponent<MeshFilter>();
            Bounds r = new Bounds(transform.InverseTransformPoint(pieza.position), Vector3.zero);
            if (mf == null || mf.sharedMesh == null) return r;
            Bounds mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 e = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                r.Encapsulate(transform.InverseTransformPoint(pieza.TransformPoint(e)));
            }
            return r;
        }

        // Hay algun objeto suelto (de los que se cogen) encima de esta zona?
        private bool CosasEncima(Bounds zona)
        {
            // Que eje del piano apunta hacia arriba
            Vector3 arriba = transform.InverseTransformDirection(Vector3.up);
            Vector3 ax = new Vector3(Mathf.Abs(arriba.x), Mathf.Abs(arriba.y), Mathf.Abs(arriba.z));
            Vector3 eje = (ax.x >= ax.y && ax.x >= ax.z) ? new Vector3(Mathf.Sign(arriba.x), 0, 0) : (ax.y >= ax.z ? new Vector3(0, Mathf.Sign(arriba.y), 0) : new Vector3(0, 0, Mathf.Sign(arriba.z)));
            Vector3 tam = zona.size;
            Vector3 ejeAbs = new Vector3(Mathf.Abs(eje.x), Mathf.Abs(eje.y), Mathf.Abs(eje.z));
            float alto = 0.25f;
            Vector3 medio = Vector3.Scale(tam, Vector3.one - ejeAbs) + ejeAbs * alto;
            float superficie = Vector3.Dot(zona.center, eje) + Vector3.Dot(zona.extents, ejeAbs);
            Vector3 centro = zona.center - eje * Vector3.Dot(zona.center, eje) + eje * (superficie + alto * 0.5f);
            Vector3 centroMundo = transform.TransformPoint(centro);
            Vector3 mitad = Vector3.Scale(medio * 0.5f, transform.lossyScale);
            // En Sailwind el cuerpo fisico de cada objeto suelto es una pieza aparte llamada "nombre:ItemRigidbody"
            string propio = LaudPlugin.Piano != null ? LaudPlugin.Piano.NombreObjeto + ":ItemRigidbody" : "";
            foreach (Collider c in Physics.OverlapBox(centroMundo, mitad, transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(transform)) continue;
                string nombre = c.attachedRigidbody != null ? c.attachedRigidbody.name : c.name;
                if (nombre == propio) continue;
                if (nombre.Contains(":ItemRigidbody") || c.GetComponentInParent<ShipItem>() != null)
                {
                    Debug.Log("PIANO: no abro la tapa, hay algo encima: " + nombre);
                    return true;
                }
            }
            return false;
        }

        private Transform Buscar(string nombre)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t != transform && t.name == nombre) return t;
            }
            return null;
        }

        // El extremo de la pletina mas lejano a su punto de giro, segun la caja de su malla
        private static bool Punta(Transform t, out Vector3 punta)
        {
            punta = Vector3.zero;
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return false;
            Bounds b = mf.sharedMesh.bounds;
            Vector3 e = b.extents;
            Vector3 eje = (e.x >= e.y && e.x >= e.z) ? new Vector3(e.x, 0, 0) : (e.y >= e.z ? new Vector3(0, e.y, 0) : new Vector3(0, 0, e.z));
            Vector3 p1 = b.center + eje, p2 = b.center - eje;
            punta = p1.sqrMagnitude >= p2.sqrMagnitude ? p1 : p2;
            return true;
        }

        private void LateUpdate()
        {
            if (!preparado) Preparar();
            if (piano == null) piano = GetComponent<PianoItem>();
            bool enMano = piano != null && piano.EnLaMano;

            // Tocar el piano colocado: lo miras y pulsas R. Para dejarlo, R otra vez o alejate.
            if (piano != null)
            {
                bool quiere = piano.Tocando;
                Camera camTocar = Camera.main;
                if (enMano || !piano.sold || camTocar == null)
                {
                    quiere = false;
                }
                else
                {
                    if (LaudPlugin.PianoTeclaTocar != null && Input.GetKeyDown(LaudPlugin.PianoTeclaTocar.Value))
                    {
                        quiere = quiere ? false : LoMiro(camTocar);
                    }
                    if (quiere && Vector3.Distance(camTocar.transform.position, transform.position) > distanciaSalir)
                    {
                        quiere = false;
                    }
                }
                piano.TocarColocado(quiere);
            }
            bool tocando = piano != null && piano.Tocando;
            BloquearJugador(tocando);
            ActualizarTexto(enMano, tocando);

            // Tecla de abrir: abre o cierra la tapa a la que miras
            if (!enMano && LaudPlugin.PianoTeclaAbrir != null && Input.GetKeyDown(LaudPlugin.PianoTeclaAbrir.Value))
            {
                Camera cam = Camera.main;
                RaycastHit golpe = new RaycastHit();
                bool loMiro = false;
                string tocados = "";
                if (cam != null)
                {
                    RaycastHit[] golpes = Physics.RaycastAll(cam.transform.position, cam.transform.forward, distanciaMirar, ~0, QueryTriggerInteraction.Collide);
                    float mejor = float.MaxValue;
                    foreach (RaycastHit g in golpes)
                    {
                        tocados += g.collider.name + " (" + g.distance.ToString("F2") + " m) ";
                        if (g.collider.transform.IsChildOf(transform) && g.distance < mejor)
                        {
                            mejor = g.distance;
                            golpe = g;
                            loMiro = true;
                        }
                    }
                }
                // Cual es: 0 = ninguna, 1 = tapa de las teclas, 2 = tapa grande
                int cual = 0;
                float distancia = float.MaxValue;
                if (loMiro)
                {
                    distancia = golpe.distance;
                    Bounds zona = zonaTeclas;
                    zona.Expand(0.08f);
                    cual = zona.Contains(transform.InverseTransformPoint(golpe.point)) ? 1 : 2;
                }
                // Las tapas levantadas no tienen cuerpo fisico: miramos si el rayo pasa por ellas
                if (cam != null)
                {
                    Ray rayo = new Ray(cam.transform.position, cam.transform.forward);
                    float dist;
                    if (abiertoTeclas > 0.5f && rendTeclas != null && rendTeclas.bounds.IntersectRay(rayo, out dist) && dist < distanciaMirar && dist < distancia)
                    {
                        cual = 1; distancia = dist;
                    }
                    else if (abiertoGrande > 0.5f && rendGrande != null && rendGrande.bounds.IntersectRay(rayo, out dist) && dist < distanciaMirar && dist < distancia)
                    {
                        cual = 2; distancia = dist;
                    }
                }
                Debug.Log("PIANO: tecla de abrir pulsada. El rayo toca: " + (tocados == "" ? "nada" : tocados) + " -> tapa " + cual);
                if (cual != 0)
                {
                    bool teclas = cual == 1;
                    if (teclas)
                    {
                        if (quiereTeclas || !CosasEncima(zonaTeclas)) quiereTeclas = !quiereTeclas;
                    }
                    else
                    {
                        if (quiereGrande || (!CosasEncima(zonaGrande) && !CosasEncima(zonaTeclas))) quiereGrande = !quiereGrande;
                    }
                    Debug.Log("PIANO: tapa " + (teclas ? "de las teclas " + (quiereTeclas ? "abierta" : "cerrada") : "grande " + (quiereGrande ? "abierta" : "cerrada")));
                }
            }

            bool teclasAbiertas = quiereTeclas || tocando;
            abiertoTeclas = Mathf.MoveTowards(abiertoTeclas, teclasAbiertas ? 1f : 0f, Time.deltaTime * velocidadTapa);
            abiertoGrande = Mathf.MoveTowards(abiertoGrande, quiereGrande ? 1f : 0f, Time.deltaTime * velocidadTapa);

            if (tapaTeclas != null)
            {
                float ang = Suave(abiertoTeclas) * LaudPlugin.PianoAnguloTeclas.Value;
                tapaTeclas.localRotation = rotTeclas0 * Quaternion.AngleAxis(ang, Vector3.right);
            }
            if (tapaGrande != null)
            {
                float maximo = anguloBrazo != 0f ? anguloBrazo : LaudPlugin.PianoAnguloGrande.Value;
                float ang = Suave(abiertoGrande) * maximo;
                tapaGrande.localRotation = rotGrande0 * Quaternion.AngleAxis(ang, Vector3.right);
            }
            if (hayBrazo) MoverBrazo();
        }

        // Mientras tocas no puedes andar (al dejar de tocar se devuelve el movimiento)
        private void BloquearJugador(bool bloquear)
        {
            if (bloquear == bloqueando) return;
            if (!buscadoCampo)
            {
                buscadoCampo = true;
                // Buscamos el tipo por su nombre, porque esta en otro archivo del juego
                foreach (Assembly ensamblado in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    tipoJugador = ensamblado.GetType("OVRPlayerController");
                    if (tipoJugador != null) break;
                }
                if (tipoJugador != null)
                {
                    campoMover = tipoJugador.GetField("EnableLinearMovement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                Debug.Log("PIANO: jugador encontrado " + (tipoJugador != null) + ", interruptor de andar " + (campoMover != null));
                if (tipoJugador != null && campoMover == null)
                {
                    string lista = "";
                    foreach (FieldInfo f in tipoJugador.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (f.FieldType == typeof(bool)) lista += f.Name + " ";
                    }
                    Debug.Log("PIANO: no encuentro como parar al jugador. Interruptores: " + lista);
                }
            }
            if (jugador == null && tipoJugador != null) jugador = FindObjectOfType(tipoJugador);
            if (campoMover != null && jugador != null)
            {
                campoMover.SetValue(jugador, !bloquear);
            }
            bloqueando = bloquear;
        }

        private void OnDisable()
        {
            BloquearJugador(false);
        }

        // El texto que sale al mirar el piano: que teclas puedes usar
        private void ActualizarTexto(bool enMano, bool tocando)
        {
            if (piano == null || enMano || !piano.sold)
            {
                if (piano != null) piano.TextoAyuda = null;
                return;
            }
            string abrir = LaudPlugin.PianoTeclaAbrir != null ? LaudPlugin.PianoTeclaAbrir.Value.ToString() : "T";
            string tocar = LaudPlugin.PianoTeclaTocar != null ? LaudPlugin.PianoTeclaTocar.Value.ToString() : "R";
            if (tocando)
            {
                piano.TextoAyuda = "(" + tocar + ") stop playing\n(" + abrir + ") open/close lid";
            }
            else
            {
                piano.TextoAyuda = "(" + tocar + ") play\n(" + abrir + ") open/close lid";
            }
            if (piano.lookText != piano.TextoAyuda)
            {
                piano.UpdateLookText();
            }
        }

        // El jugador esta mirando el piano (de cerca)?
        private bool LoMiro(Camera cam)
        {
            Ray rayo = new Ray(cam.transform.position, cam.transform.forward);
            foreach (RaycastHit g in Physics.RaycastAll(rayo, distanciaMirar, ~0, QueryTriggerInteraction.Collide))
            {
                if (g.collider.transform.IsChildOf(transform)) return true;
            }
            float dist;
            if (rendTeclas != null && abiertoTeclas > 0.5f && rendTeclas.bounds.IntersectRay(rayo, out dist) && dist < distanciaMirar) return true;
            if (rendGrande != null && abiertoGrande > 0.5f && rendGrande.bounds.IntersectRay(rayo, out dist) && dist < distanciaMirar) return true;
            return false;
        }

        private static float Suave(float x)
        {
            return x * x * (3f - 2f * x);
        }

        // Dobla las dos pletinas para que sus puntas se junten en el codo
        private void MoverBrazo()
        {
            brazoAbajo.localRotation = rotAbajo0;
            Vector3 a = brazoAbajo.position, b = brazoArriba.position;
            Vector3 ab = b - a;
            float d = ab.magnitude;
            if (d < 0.0001f) return;
            Vector3 u = ab / d;
            Vector3 v = Vector3.Cross(tapaGrande.right, u).normalized;
            d = Mathf.Clamp(d, Mathf.Abs(largoAbajo - largoArriba) + 0.0001f, largoAbajo + largoArriba - 0.0001f);
            float x = (largoAbajo * largoAbajo - largoArriba * largoArriba + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, largoAbajo * largoAbajo - x * x));
            Vector3 codo = a + u * x + v * (h * ladoCodo);

            Vector3 deseadoAbajo = transform.InverseTransformPoint(codo) - brazoAbajo.localPosition;
            brazoAbajo.localRotation = Quaternion.FromToRotation(dirAbajo0, deseadoAbajo) * rotAbajo0;

            Vector3 deseadoArriba = tapaGrande.InverseTransformPoint(codo) - brazoArriba.localPosition;
            brazoArriba.localRotation = Quaternion.FromToRotation(dirArriba0, deseadoArriba) * rotArriba0;
        }
    }
}
