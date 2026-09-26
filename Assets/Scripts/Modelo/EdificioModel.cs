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
            get { return tiempoConstruccionSeg; }
            protected set { tiempoConstruccionSeg = value; }
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

        private void ConstruirEnHilo()
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

            OnConstruccionTerminada?.Invoke(this);
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
                if (_senalParar.Wait(1000)) break;

                // Un edificio destruido no produce
                if (!EstaEnPie) continue;

                _dueno.Conseguir_Recursos(recursoQueProduce, produccionPorSegundo);
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
