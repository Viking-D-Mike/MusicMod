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
            fuente.PlayOneShot(d.Clips[nota], d.Volumen.Value * Random.Range(0.9f, 1f));
        }
    }

    [DefaultExecutionOrder(1000)]
    public class NacairesItem : InstrumentoItem
    {
        public override Instrumento Datos { get { return LaudPlugin.Nacaires; } }
    }

    // La flauta suena mientras mantienes la tecla, y se apaga suavemente al soltarla.
    // Usa dos fuentes de sonido que se turnan: al cambiar de nota, la vieja se apaga
    // mientras la nueva entra (fundido cruzado), con un pequeno deslizamiento de tono.
    [DefaultExecutionOrder(1000)]
    public class FlautaItem : InstrumentoItem
    {
        private const float subida = 12f;          // entra desde el silencio en ~0,08 s
        private const float bajada = 7f;           // se apaga al soltar en ~0,15 s
        private const float cruce = 16f;           // fundido entre notas en ~0,06 s
        private const float tiempoDeslizar = 0.06f;
        // Semitonos de cada nota respecto a la primera (Re Mi Fa# Sol La Si Do# Re)
        private static readonly int[] semitonos = { 0, 2, 4, 5, 7, 9, 11, 12 };

        private AudioSource[] fuentes;
        private readonly float[] vols = new float[2];
        private int activa;
        private int notaActual = -1;
        private int notaSonando = -1;
        private float deslizar = tiempoDeslizar;
        private float tonoInicio = 1f;

        public override Instrumento Datos { get { return LaudPlugin.Flauta; } }

        private bool PrepararFuentes()
        {
            if (fuentes != null && fuentes[0] != null && fuentes[1] != null)
            {
                return true;
            }
            AudioSource a = GetComponent<AudioSource>();
            if (a == null)
            {
                return false;
            }
            AudioSource b = gameObject.AddComponent<AudioSource>();
            b.playOnAwake = false;
            b.loop = true;
            b.spatialBlend = a.spatialBlend;
            b.rolloffMode = a.rolloffMode;
            b.minDistance = a.minDistance;
            b.maxDistance = a.maxDistance;
            b.outputAudioMixerGroup = a.outputAudioMixerGroup;
            b.volume = 0f;
            a.loop = true;
            fuentes = new[] { a, b };
            return true;
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

            // Nota nueva: entra por la otra fuente si la actual aun suena
            if (hayNota && notaActual != notaSonando)
            {
                int anterior = notaSonando;
                if (vols[activa] > 0.001f)
                {
                    activa = 1 - activa;
                }
                AudioSource nueva = fuentes[activa];
                nueva.Stop();
                nueva.clip = d.Clips[notaActual];
                nueva.loop = true;
                vols[activa] = 0f;
                nueva.volume = 0f;

                if (anterior >= 0 && anterior < semitonos.Length && notaActual < semitonos.Length)
                {
                    // Empieza a medio camino desde la nota anterior y llega a la suya en 0,06 s
                    tonoInicio = Mathf.Pow(2f, (semitonos[anterior] - semitonos[notaActual]) / 24f);
                    deslizar = 0f;
                }
                else
                {
                    tonoInicio = 1f;
                    deslizar = tiempoDeslizar;
                }
                nueva.pitch = tonoInicio;
                nueva.Play();
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
            bool cruzando = vols[1 - activa] > 0.001f;
            for (int i = 0; i < 2; i++)
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
                    velocidad = esActiva ? bajada : cruce;
                }
                vols[i] = Mathf.MoveTowards(vols[i], objetivo, Time.deltaTime * velocidad * volumenMax);
                fuentes[i].volume = vols[i];
                if (vols[i] <= 0.0001f && objetivo <= 0f && fuentes[i].isPlaying)
                {
                    fuentes[i].Stop();
                }
            }
        }
    }
}
