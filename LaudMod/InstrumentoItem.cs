using UnityEngine;

namespace LaudMod
{
    // Lo comun a los tres instrumentos: con la tecla de usar (R) entras y sales del "modo tocar",
    // y el instrumento se mueve suavemente entre la posicion "en la mano" y la de "tocar".
    // Cada instrumento decide que hacen sus teclas de nota (LeerTeclas).
    public abstract class InstrumentoItem : ShipItem
    {
        private const float velocidadCambio = 4f;

        protected AudioSource fuente;
        protected bool tocando;
        private bool pulsadoEsteFotograma;
        private bool pulsadoAntes;
        private bool botonLiberado = true;
        private float mezcla; // 0 = en la mano, 1 = en posicion de tocar
        private Component colgable; // componente de HooksHangMore, si esta instalado

        public abstract Instrumento Datos { get; }

        public override void OnLoad()
        {
            fuente = GetComponent<AudioSource>();
            Debug.Log(GetType().Name + " cargado. AudioSource encontrado: " + (fuente != null));
        }

        public override void OnBuy()
        {
            // Al comprarlo la tecla de usar sigue pulsada: esperamos a que se suelte
            botonLiberado = false;
        }

        // El juego llama a esto en cada fotograma mientras mantienes la tecla de usar
        public override void OnAltHeld()
        {
            pulsadoEsteFotograma = true;
        }

        public override void ExtraLateUpdate()
        {
            Instrumento d = Datos;
            if (d == null)
            {
                return;
            }

            bool pulsaAhora = pulsadoEsteFotograma;
            pulsadoEsteFotograma = false;
            bool recienPulsado = pulsaAhora && !pulsadoAntes;
            pulsadoAntes = pulsaAhora;
            if (!pulsaAhora)
            {
                botonLiberado = true;
            }

            // Si HooksHangMore esta instalado, la posicion de colgado se actualiza con los ajustes (menu F1)
            if (d.Colgable && LaudPlugin.TipoAttachable != null && LaudPlugin.PropPosicionColgado != null && LaudPlugin.PropGiroColgado != null)
            {
                if (colgable == null)
                {
                    colgable = GetComponent(LaudPlugin.TipoAttachable);
                }
                if (colgable != null)
                {
                    LaudPlugin.PropPosicionColgado.SetValue(colgable, new Vector3(d.ColgPosX.Value, d.ColgPosY.Value, d.ColgPosZ.Value), null);
                    LaudPlugin.PropGiroColgado.SetValue(colgable, new Vector3(d.ColgRotX.Value, d.ColgRotY.Value, d.ColgRotZ.Value), null);
                }
            }

            // Si no esta en la mano, se sale del modo tocar y todo vuelve a su sitio
            if (held == null)
            {
                tocando = false;
                mezcla = 0f;
                holdDistance = d.ManoDistancia.Value;
                holdHeight = d.ManoAltura.Value;
                heldRotationOffset = 0f;
                LeerTeclas(false);
                return;
            }

            if (recienPulsado && sold && botonLiberado)
            {
                tocando = !tocando;
                Debug.Log(GetType().Name + ": modo tocar " + (tocando ? "activado" : "desactivado"));
            }

            LeerTeclas(tocando);

            mezcla = Mathf.MoveTowards(mezcla, tocando ? 1f : 0f, Time.deltaTime * velocidadCambio);
            float suave = mezcla * mezcla * (3f - 2f * mezcla);

            holdDistance = Mathf.Lerp(d.ManoDistancia.Value, d.TocarDistancia.Value, suave);
            holdHeight = Mathf.Lerp(d.ManoAltura.Value, d.TocarAltura.Value, suave);

            Vector3 giroReposo = new Vector3(d.ReposoGiroX.Value, d.ReposoGiroY.Value, d.ReposoGiroZ.Value);
            Vector3 giroTocar = new Vector3(d.TocarGiroX.Value, d.TocarGiroY.Value, d.TocarGiroZ.Value);
            Vector3 giro = Vector3.Lerp(giroReposo, giroTocar, suave);
            if (giro.sqrMagnitude > 0.0001f)
            {
                transform.Rotate(giro, Space.Self);
            }

            Camera camara = Camera.main;
            if (camara != null && suave > 0.001f)
            {
                transform.position += camara.transform.right * d.TocarLateral.Value * suave;
            }
        }

        // Por defecto: cada pulsacion suena una vez (laud y nacaires)
        protected virtual void LeerTeclas(bool activo)
        {
            if (!activo)
            {
                return;
            }
            Instrumento d = Datos;
            for (int i = 0; i < d.Teclas.Length; i++)
            {
                if (d.Teclas[i] != null && Input.GetKeyDown(d.Teclas[i].Value))
                {
                    Golpe(i);
                }
            }
        }

        protected void Golpe(int nota)
        {
            Instrumento d = Datos;
            if (d.Clips == null || nota >= d.Clips.Length || d.Clips[nota] == null)
            {
                return;
            }
            if (fuente == null)
            {
                fuente = GetComponent<AudioSource>();
            }
            if (fuente == null)
            {
                return;
            }
            // Fuerza de cada golpe: a veces suave (sonido mas apagado y flojo), a veces fuerte
            AudioClip clip = d.Clips[nota];
            float fuerza;
            if (d.ClipsSuaves != null && nota < d.ClipsSuaves.Length && d.ClipsSuaves[nota] != null && Random.value < 0.45f)
            {
                clip = d.ClipsSuaves[nota];
                fuerza = Random.Range(0.6f, 0.85f);
            }
            else
            {
                fuerza = Random.Range(0.8f, 1f);
            }
            fuente.PlayOneShot(clip, d.Volumen.Value * fuerza);
        }
    }

    [DefaultExecutionOrder(1000)]
    public class NacairesItem : InstrumentoItem
    {
        public override Instrumento Datos { get { return LaudPlugin.Nacaires; } }
    }

    // Aplica el volumen muestra a muestra en el hilo de audio, interpolando entre el valor
    // anterior y el nuevo. Asi los fundidos son suaves de verdad: si se cambiara el volumen
    // de golpe en cada fotograma, se oirian pequenos chasquidos en cada escalon.
    public class RampaVolumen : MonoBehaviour
    {
        [System.NonSerialized] public volatile float objetivo;
        private volatile float actual;

        public float Actual { get { return actual; } }

        private void OnAudioFilterRead(float[] datos, int canales)
        {
            float fin = objetivo;
            int muestras = canales > 0 ? datos.Length / canales : 0;
            if (muestras <= 0)
            {
                return;
            }
            float g = actual;
            float paso = (fin - g) / muestras;
            for (int m = 0; m < muestras; m++)
            {
                g += paso;
                int baseIndice = m * canales;
                for (int c = 0; c < canales; c++)
                {
                    datos[baseIndice + c] *= g;
                }
            }
            actual = fin;
        }

        // Crea una "voz": un objeto hijo con su propia fuente de sonido y su rampa de volumen
        public static AudioSource CrearVoz(GameObject padre, AudioSource modelo, string nombre, out RampaVolumen rampa)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre.transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.volume = 1f;
            if (modelo != null)
            {
                s.spatialBlend = modelo.spatialBlend;
                s.rolloffMode = modelo.rolloffMode;
                s.minDistance = modelo.minDistance;
                s.maxDistance = modelo.maxDistance;
                s.outputAudioMixerGroup = modelo.outputAudioMixerGroup;
            }
            rampa = go.AddComponent<RampaVolumen>();
            rampa.objetivo = 0f;
            return s;
        }
    }

    // La flauta suena mientras mantienes la tecla, y se apaga suavemente al soltarla.
    // Usa varias fuentes de sonido: cada nota nueva entra por una que este en silencio,
    // mientras la anterior se apaga (fundido cruzado), con un pequeno deslizamiento de tono.
    // Asi, aunque cambies de nota muy deprisa, nunca se corta en seco un sonido (no hay chasquidos).
    [DefaultExecutionOrder(1000)]
    public class FlautaItem : InstrumentoItem
    {
        private const int numFuentes = 6;
        private const float subida = 12f;          // entra desde el silencio en ~0,08 s
        private const float bajada = 7f;           // se apaga al soltar en ~0,15 s
        private const float cruce = 16f;           // la nota nueva entra en ~0,06 s
        private const float salida = 6f;           // la nota anterior se apaga en ~0,17 s (se solapan, sin corte)
        private const float tiempoDeslizar = 0.06f;
        // Semitonos de cada nota respecto a la primera (escala mayor: flauta 8 notas, zanfona 10)
        private static readonly int[] semitonos = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16 };

        private AudioSource[] fuentes;
        private RampaVolumen[] rampas;
        private readonly float[] vols = new float[numFuentes];
        private int activa;
        private int notaActual = -1;
        private int notaSonando = -1;
        private float deslizar = tiempoDeslizar;
        private float tonoInicio = 1f;

        public override Instrumento Datos { get { return LaudPlugin.Flauta; } }

        private bool PrepararFuentes()
        {
            if (fuentes != null && fuentes[0] != null)
            {
                return true;
            }
            AudioSource modelo = GetComponent<AudioSource>();
            if (modelo == null)
            {
                return false;
            }
            fuentes = new AudioSource[numFuentes];
            rampas = new RampaVolumen[numFuentes];
            for (int i = 0; i < numFuentes; i++)
            {
                fuentes[i] = RampaVolumen.CrearVoz(gameObject, modelo, "voz " + i, out rampas[i]);
            }
            return true;
        }

        // La fuente mas silenciosa (que no sea la activa) para la nota nueva
        private int FuenteLibre()
        {
            int mejor = -1;
            float menor = float.MaxValue;
            for (int i = 0; i < numFuentes; i++)
            {
                if (i == activa && vols[i] > 0.001f)
                {
                    continue;
                }
                if (vols[i] < menor)
                {
                    menor = vols[i];
                    mejor = i;
                }
            }
            return mejor < 0 ? activa : mejor;
        }

        protected override void LeerTeclas(bool activo)
        {
            if (!PrepararFuentes())
            {
                return;
            }
            Instrumento d = Datos;
            int n = d.Teclas.Length;

            if (activo)
            {
                // La ultima tecla pulsada manda
                for (int i = 0; i < n; i++)
                {
                    if (d.Teclas[i] != null && Input.GetKeyDown(d.Teclas[i].Value))
                    {
                        notaActual = i;
                    }
                }
                // Si sueltas la que sonaba pero mantienes otra, pasa a esa
                if (notaActual >= 0 && !Input.GetKey(d.Teclas[notaActual].Value))
                {
                    notaActual = -1;
                    for (int i = n - 1; i >= 0; i--)
                    {
                        if (d.Teclas[i] != null && Input.GetKey(d.Teclas[i].Value))
                        {
                            notaActual = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                notaActual = -1;
            }

            bool hayNota = notaActual >= 0 && notaActual < d.Clips.Length && d.Clips[notaActual] != null;

            // Nota nueva: entra por una fuente en silencio
            if (hayNota && notaActual != notaSonando)
            {
                int anterior = notaSonando;
                activa = FuenteLibre();
                AudioSource nueva = fuentes[activa];
                if (nueva.clip != d.Clips[notaActual] || !nueva.isPlaying)
                {
                    nueva.Stop();
                    nueva.clip = d.Clips[notaActual];
                    nueva.loop = true;
                    vols[activa] = 0f;
                    rampas[activa].objetivo = 0f;
                    nueva.Play();
                }

                if (anterior >= 0 && anterior < semitonos.Length && notaActual < semitonos.Length)
                {
                    // Empieza un poco desplazada hacia la nota anterior (como mucho 1,5 semitonos,
                    // para que en los saltos grandes no suene un "silbido") y llega a la suya en 0,06 s
                    float desvio = Mathf.Clamp((semitonos[anterior] - semitonos[notaActual]) * 0.5f, -1.5f, 1.5f);
                    tonoInicio = Mathf.Pow(2f, desvio / 12f);
                    deslizar = 0f;
                }
                else
                {
                    tonoInicio = 1f;
                    deslizar = tiempoDeslizar;
                }
                nueva.pitch = tonoInicio;
                notaSonando = notaActual;
            }
            if (!hayNota)
            {
                notaSonando = -1;
            }

            // Deslizamiento de tono de la nota que entra
            if (deslizar < tiempoDeslizar)
            {
                deslizar += Time.deltaTime;
                float t = Mathf.Clamp01(deslizar / tiempoDeslizar);
                t = t * t * (3f - 2f * t);
                fuentes[activa].pitch = Mathf.Lerp(tonoInicio, 1f, t);
            }
            else
            {
                fuentes[activa].pitch = 1f;
            }

            // Volumen de cada fuente
            float volumenMax = Mathf.Max(d.Volumen.Value, 0.01f);
            bool cruzando = false;
            for (int i = 0; i < numFuentes; i++)
            {
                if (i != activa && vols[i] > 0.001f)
                {
                    cruzando = true;
                }
            }
            for (int i = 0; i < numFuentes; i++)
            {
                bool esActiva = i == activa;
                float objetivo = (esActiva && hayNota) ? d.Volumen.Value : 0f;
                float velocidad;
                if (objetivo > vols[i])
                {
                    velocidad = cruzando ? cruce : subida;
                }
                else
                {
                    velocidad = esActiva ? bajada : salida;
                }
                vols[i] = Mathf.MoveTowards(vols[i], objetivo, Time.deltaTime * velocidad * volumenMax);
                rampas[i].objetivo = vols[i];
                if (vols[i] <= 0.0001f && objetivo <= 0f && rampas[i].Actual <= 0.0001f && fuentes[i].isPlaying)
                {
                    fuentes[i].Stop();
                    fuentes[i].pitch = 1f;
                }
            }
        }
    }

    // La zanfona: las notas funcionan como la flauta (suenan mientras mantienes la tecla),
    // y ademas, mientras estas en modo tocar, suena el bordon de fondo y gira la manivela.
    [DefaultExecutionOrder(1000)]
    public class ZanfonaItem : FlautaItem
    {
        private const float velocidadFondo = 2.5f;   // el bordon entra y sale en ~0,4 s
        private const float vueltasPorSegundo = 1.2f;

        private AudioSource fondo;
        private RampaVolumen rampaFondo;
        private float volFondo;
        private Transform manivela;
        private bool buscadaManivela;

        public override Instrumento Datos { get { return LaudPlugin.Zanfona; } }

        protected override void LeerTeclas(bool activo)
        {
            base.LeerTeclas(activo);

            Instrumento d = Datos;
            if (d == null || d.ClipFondo == null)
            {
                return;
            }

            if (fondo == null)
            {
                fondo = RampaVolumen.CrearVoz(gameObject, GetComponent<AudioSource>(), "bordon", out rampaFondo);
                fondo.clip = d.ClipFondo;
            }

            float maximo = d.Volumen.Value * (d.VolumenFondo != null ? d.VolumenFondo.Value : 0.6f);
            float objetivo = activo ? maximo : 0f;
            volFondo = Mathf.MoveTowards(volFondo, objetivo, Time.deltaTime * velocidadFondo * Mathf.Max(maximo, 0.01f));
            rampaFondo.objetivo = volFondo;
            if (volFondo > 0.0001f && !fondo.isPlaying)
            {
                fondo.Play();
            }
            else if (volFondo <= 0.0001f && objetivo <= 0f && rampaFondo.Actual <= 0.0001f && fondo.isPlaying)
            {
                fondo.Stop();
            }

            // La manivela gira mientras suena el bordon (arranca y frena suave con su volumen)
            if (!buscadaManivela)
            {
                manivela = transform.Find("manivela");
                buscadaManivela = true;
            }
            if (manivela != null && maximo > 0.0001f)
            {
                float ritmo = volFondo / maximo;
                manivela.Rotate(Vector3.right, 360f * vueltasPorSegundo * ritmo * Time.deltaTime, Space.Self);
            }
        }
    }
}
