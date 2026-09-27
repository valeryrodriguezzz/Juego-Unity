using System;
using System.Diagnostics;
using System.Threading;

namespace ImperiosEnGuerra.Modelo
{
    /// Edificio del juego. La LOGICA CONCURRENTE vive aqui, en el Modelo:
    ///  - Hilo de construccion: avanza PorcentajeConstruccion segun TiempoConstruccionSeg.
    ///  - Hilo de produccion: cuando el edificio ya esta construido, suma su recurso
    ///    al jugador cada segundo (Granja, Armeria, Mina, Aserradero).
    /// El Controlador solo llama IniciarConstruccion() / Detener().
    /// Mismo patron que RecursoModel y JugadorModel: volatile(una variable puede ser modificada por múltiples hilos al mismo tiempo.) 
    /// + lock(proteger un bloque de código para que solo un hilo pueda ejecutarlo a la vez) + señal interrumpible (capacidad de un hilo 
    /// que está esperando una señal o bloqueado de ser despertado prematuramente mediante una interrupción) + Join(hacer que el hilo actual 
    /// espere a que otro hilo termine su ejecución.).
    public abstract class EdificioModel
    {
        // Un solo candado para el estado del edificio (vida, construccion, hilos).
        // Los hilos de construccion/produccion y el combate lo tocan a la vez.
        private readonly object _lock = new object();

        // ATRIBUTOS PRIVADOS (-)
        private string imperio;
        private int id;
        private string nombre;
        private int vida;
        private int vidaMax;
        private int fila;
        private int columna;
        private bool construido;
        private float porcentajeConstruccion;
        private float tiempoConstruccionSeg;
        private int costoOro;
        private int costoMadera;
        private string recursoQueProduce;
        private int produccionPorSegundo;
        private bool esDeJugador;

        // Control de hilos
        private Thread _hiloConstruccion;
        private Thread _hiloProduccion;
        private volatile bool _construyendo = false;
        private volatile bool _produciendo = false;
        private JugadorModel _dueno;
        private readonly ManualResetEventSlim _senalParar = new ManualResetEventSlim(false);

        // Se dispara desde el hilo de construccion (NO el principal de Unity)
        // cuando el edificio termina de construirse.
        public event Action<EdificioModel> OnConstruccionTerminada;

        // PROPIEDADES PÚBLICAS (+)
        public string Imperio
        {
            get { return imperio; }
            set { imperio = value; }
        }

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Nombre
        {
            get { return nombre; }
            set { nombre = value; }
        }

        public int Vida
        {
            get { lock (_lock) { return vida; } }
            set { lock (_lock) { vida = value; } }
        }

        public int VidaMax
        {
            get { return vidaMax; }
            set { vidaMax = value; }
        }

        public int Fila
        {
            get { return fila; }
            set { fila = value; }
        }

        public int Columna
        {
            get { return columna; }
            set { columna = value; }
        }

        public bool Construido
        {
            get { lock (_lock) { return construido; } }
            set { lock (_lock) { construido = value; } }
        }

        public float PorcentajeConstruccion
        {
            get { lock (_lock) { return porcentajeConstruccion; } }
            set { lock (_lock) { porcentajeConstruccion = value; } }
        }

        public float TiempoConstruccionSeg
        {
            get { lock (_lock) { return tiempoConstruccionSeg; } }
            protected set { lock (_lock) { tiempoConstruccionSeg = value; } }
        }

        /// <summary>
        /// Acorta el tiempo de obra segun lo buena que sea la herramienta del
        /// constructor: con el martillo (multiplicador 2) la obra tarda la
        /// mitad. Devuelve los segundos que quedaron.
        ///
        /// Solo tiene efecto ANTES de empezar: una vez el hilo de construccion
        /// arranco, cambiarle el tiempo a mitad de camino haria saltar el
        /// porcentaje de golpe. Por eso se comprueba y si ya arranco no hace
        /// nada.
        ///
        /// Va con lock porque el hilo de construccion lee tiempoConstruccionSeg
        /// para calcular cuanto avanza en cada vuelta.
        /// </summary>
        public float AplicarVelocidadDeConstruccion(float multiplicador)
        {
            if (multiplicador <= 0f) return TiempoConstruccionSeg;

            lock (_lock)
            {
                if (_construyendo || construido) return tiempoConstruccionSeg;

                tiempoConstruccionSeg = tiempoConstruccionSeg / multiplicador;

                // Nunca instantaneo: se tiene que alcanzar a ver la obra.
                if (tiempoConstruccionSeg < 0.5f) tiempoConstruccionSeg = 0.5f;

                return tiempoConstruccionSeg;
            }
        }

        public int CostoOro
        {
            get { return costoOro; }
            protected set { costoOro = value; }
        }

        public int CostoMadera
        {
            get { return costoMadera; }
            protected set { costoMadera = value; }
        }

        public string RecursoQueProduce
        {
            get { return recursoQueProduce; }
            protected set { recursoQueProduce = value; }
        }

        public int ProduccionPorSegundo
        {
            get { return produccionPorSegundo; }
            protected set { produccionPorSegundo = value; }
        }

        public bool EsDeJugador
        {
            get { return esDeJugador; }
            set { esDeJugador = value; }
        }

        public bool EstaConstruyendose => _construyendo;
        public bool EstaProduciendo => _produciendo;

        // Propiedad calculada — true si el edificio sigue en pie:
        public bool EstaEnPie => Vida > 0;

        // --- MÉTODOS (+) ---

        public virtual void Construccion()    // Marca el edificio como construido
        {
            lock (_lock)
            {
                construido = true;
                porcentajeConstruccion = 100f;
            }
        }

        public bool Destruccion(int daño)
        {
            lock (_lock)
            {
                vida -= daño;
                if (vida < 0) vida = 0;
                return vida <= 0;  // true si el edificio fue destruido
            }
        }

        // ── CONSTRUCCION (hilo) ─────────────────────────────────────────

        // Cobra el costo (oro y madera, todo o nada) y arranca el hilo de construccion.
        // Devuelve false si ya esta construido/construyendose o si no alcanzan los recursos.
        // Cuando el hilo termina, el edificio queda construido y, si produce algo,
        // empieza a producir automaticamente.
        public bool IniciarConstruccion(JugadorModel dueno)
        {
            if (dueno == null) return false;

            lock (_lock)
            {
                if (_construyendo || construido) return false;

                // Comprobar y cobrar en un solo paso (evita gastar el mismo oro dos veces)
                if (!dueno.GastarSiAlcanza(monedas: costoOro, madera: costoMadera))
                    return false;

                _dueno = dueno;
                _construyendo = true;
                porcentajeConstruccion = 0f;
                _senalParar.Reset();

                _hiloConstruccion = new Thread(ConstruirEnHilo) { IsBackground = true };
                _hiloConstruccion.Start();
                return true;
            }
        }

        // MANEJO DE EXCEPCIONES EN LOS HILOS
        //
        // Todo el cuerpo de un hilo va dentro de try-catch, y ademas cada
        // vuelta del bucle lleva el suyo. No es por adornar:
        //
        // Una excepcion que se escapa de un hilo secundario no cierra el
        // juego, lo cual suena bien pero es peor: mata ese hilo en silencio y
        // nadie se entera. La obra se quedaria congelada en el 40%, o la
        // granja dejaria de producir para siempre, sin un solo mensaje en la
        // consola. Con el try por vuelta, un fallo puntual se anota y la
        // siguiente vuelta sigue como si nada.
        //
        // Lo que se atrapa va a RegistroDeErrores, y de ahi a la consola y a
        // log_partida.txt.
        private void ConstruirEnHilo()
        {
            try
            {
                const int PASO_MS = 200;
                var reloj = Stopwatch.StartNew();

                while (_construyendo)
                {
                    // Espera interrumpible: true si pidieron parar
                    if (_senalParar.Wait(PASO_MS)) return;

                    float pct = tiempoConstruccionSeg <= 0f
                        ? 100f
                        : (float)(reloj.Elapsed.TotalSeconds / tiempoConstruccionSeg * 100.0);
                    if (pct > 100f) pct = 100f;

                    lock (_lock) { porcentajeConstruccion = pct; }

                    if (pct >= 100f) break;
                }

                lock (_lock)
                {
                    // Detener() pudo llamarse justo antes: si es asi, no se termina nada
                    if (!_construyendo) return;

                    _construyendo = false;
                    Construccion();
                    IniciarProduccionConLock();
                }

                // El evento llama a codigo de fuera (la Vista), que podria
                // fallar. Si lo hace, el edificio ya quedo construido: lo que
                // se pierde es el aviso, y eso se anota en vez de dejar la
                // excepcion suelta.
                try
                {
                    OnConstruccionTerminada?.Invoke(this);
                }
                catch (Exception ex)
                {
                    RegistroDeErrores.Reportar(Nombre + ".OnConstruccionTerminada", ex);
                }
            }
            catch (Exception ex)
            {
                // El edificio se queda a medias, pero el juego sigue y queda
                // constancia de por que.
                _construyendo = false;
                RegistroDeErrores.Reportar(Nombre + ".ConstruirEnHilo", ex);
            }
        }

        /// <summary>
        /// Deja el edificio como si ya se hubiera terminado de construir, y le
        /// vuelve a arrancar el hilo de produccion. Es lo que se usa al cargar
        /// una partida guardada: la granja que el jugador construyo antes de
        /// salir tiene que seguir dando comida al volver.
        ///
        /// No se puede hacer con Construccion() a secas: ese metodo solo marca
        /// el edificio como terminado. Quien arranca la produccion es el hilo
        /// de obra al llegar al 100%, y ese hilo aqui nunca corrio.
        /// </summary>
        public void RestaurarComoConstruido(JugadorModel dueno)
        {
            if (dueno == null) return;

            lock (_lock)
            {
                if (construido && _produciendo) return;

                _dueno = dueno;
                _construyendo = false;
                construido = true;
                porcentajeConstruccion = 100f;

                // La señal pudo quedar levantada de un Detener() anterior: si
                // no se baja, el hilo de produccion sale en su primera vuelta.
                _senalParar.Reset();

                IniciarProduccionConLock();
            }
        }

        // ── PRODUCCION (hilo) ───────────────────────────────────────────

        // Solo se llama con _lock tomado. Arranca el hilo si el edificio produce algo.
        private void IniciarProduccionConLock()
        {
            if (_produciendo || _dueno == null) return;
            if (string.IsNullOrWhiteSpace(recursoQueProduce) || produccionPorSegundo <= 0) return;

            _produciendo = true;
            _hiloProduccion = new Thread(ProducirEnHilo) { IsBackground = true };
            _hiloProduccion.Start();
        }

        private void ProducirEnHilo()
        {
            while (_produciendo)
            {
                try
                {
                    if (_senalParar.Wait(1000)) break;

                    // Un edificio destruido no produce
                    if (!EstaEnPie) continue;

                    _dueno.Conseguir_Recursos(recursoQueProduce, produccionPorSegundo);
                }
                catch (Exception ex)
                {
                    // Se pierde la produccion de este segundo, no la del
                    // edificio: la vuelta siguiente lo intenta otra vez.
                    RegistroDeErrores.Reportar(Nombre + ".ProducirEnHilo", ex);
                }
            }
        }

        // ── DETENER ─────────────────────────────────────────────────────

        // Apaga los hilos de construccion y produccion. Llamar desde OnDestroy /
        // OnApplicationQuit del Controlador o al terminar la partida.
        public void Detener()
        {
            Thread construccion, produccion;

            lock (_lock)
            {
                _construyendo = false;
                _produciendo = false;
                construccion = _hiloConstruccion;
                produccion = _hiloProduccion;
                _hiloConstruccion = null;
                _hiloProduccion = null;
            }

            _senalParar.Set();

            // Join FUERA del lock: los hilos necesitan el lock para poder salir
            Thread actual = Thread.CurrentThread;
            if (construccion != null && construccion != actual && construccion.IsAlive) construccion.Join(1500);
            if (produccion != null && produccion != actual && produccion.IsAlive) produccion.Join(1500);
        }
    }
}
