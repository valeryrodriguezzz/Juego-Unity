using System;
using System.Threading;

namespace ImperiosEnGuerra.Modelo
{
    /// El hambre del jugador. Clase C# pura (Modelo): no sabe que existe Unity.
    ///
    /// CONCURRENCIA: tiene su PROPIO hilo, que le va bajando el nivel cada
    /// cierto tiempo aunque el jugador este quieto. Cuando el nivel llega a
    /// cero, ese mismo hilo le empieza a quitar vida.
    ///
    /// Es el tercer sitio del proyecto donde un hilo modifica estado que el
    /// hilo principal esta leyendo al mismo tiempo (los otros dos son la
    /// regeneracion de los nodos y el mantenimiento de las armas), asi que
    /// todo el acceso al nivel va protegido con el mismo lock.
    ///
    /// OJO CON EL DAÑO: para quitar vida NO se hace "jugador.Vida -= 1",
    /// porque eso son tres operaciones (leer, restar, escribir) y entre ellas
    /// el hilo de la IA puede estar atacando: uno de los dos golpes se pierde.
    /// Se usa jugador.RecibirDanio(), que hace las tres cosas dentro del lock
    /// del propio jugador.

    public class HambreModel
    {
        private readonly object _lock = new object();
        private readonly JugadorModel _jugador;

        private int _nivel;

        private Thread _hilo;
        private volatile bool _activo;
        private readonly ManualResetEventSlim _senalParar = new ManualResetEventSlim(false);

        // --- Configuracion (se puede ajustar desde el Controlador) ---

        public int NivelMaximo { get; }

        /// Cada cuantos milisegundos baja el hambre.
        public int MsPorTick { get; set; } = 3000;

        /// Cuanto baja en cada tick.
        public int PuntosPorTick { get; set; } = 1;

        /// Vida que se pierde por tick cuando el hambre esta en cero.
        public int DanioPorTickSinComer { get; set; } = 2;

        /// Cuanto hambre recupera cada unidad de Comida que se gasta.
        public int PuntosPorComida { get; set; } = 10;

        // --- Eventos (se disparan desde el hilo del hambre, NO desde Unity) ---

        /// El nivel cambio. El parametro es el nivel nuevo.
        public event Action<int> NivelCambio;

        /// Se paso hambre y se perdio vida. El parametro es el daño.
        public event Action<int> DanioPorHambre;

        /// El jugador se quedo sin vida por hambre.
        public event Action MurioDeHambre;

        public HambreModel(JugadorModel jugador, int nivelMaximo = 100)
        {
            _jugador = jugador ?? throw new ArgumentNullException(nameof(jugador));
            NivelMaximo = nivelMaximo < 1 ? 1 : nivelMaximo;
            _nivel = NivelMaximo; // arranca lleno
        }

        // --- Lecturas thread-safe ---

        public int Nivel
        {
            get { lock (_lock) { return _nivel; } }
        }

        /// Pone el nivel de hambre en un valor exacto. Solo para cargar una
        /// partida guardada: en el juego el nivel solo baja con su hilo y sube
        /// comiendo.

        public void RestaurarNivel(int valor)
        {
            if (valor < 0) return;

            lock (_lock)
            {
                _nivel = valor > NivelMaximo ? NivelMaximo : valor;
            }

            NotificarNivel();
        }

        public float Porcentaje
        {
            get { lock (_lock) { return (float)_nivel / NivelMaximo; } }
        }

        public bool TieneHambre
        {
            get { lock (_lock) { return _nivel <= 0; } }
        }

        public bool EstaCorriendo => _activo;

        // --- Comer ---


        /// Gasta Comida del jugador y sube el hambre. Devuelve cuanto subio
        /// realmente (0 si no tenia comida o ya estaba lleno).
        ///
        /// El cobro se hace con GastarSiAlcanza, que es atomico, y solo si
        /// alcanzo se sube el nivel. Los dos candados (el del jugador y el de
        /// aqui) nunca se toman a la vez: primero se cobra y se suelta, y
        /// despues se sube. Asi no hay forma de que se traben entre si.

        public int Comer(int unidadesDeComida = 1)
        {
            if (unidadesDeComida <= 0) return 0;

            lock (_lock)
            {
                if (_nivel >= NivelMaximo) return 0; // ya esta lleno
            }

            if (!_jugador.GastarSiAlcanza(0, 0, unidadesDeComida, 0))
                return 0; // no tiene comida

            int subio;

            lock (_lock)
            {
                int antes = _nivel;
                _nivel += unidadesDeComida * PuntosPorComida;
                if (_nivel > NivelMaximo) _nivel = NivelMaximo;
                subio = _nivel - antes;
            }

            NotificarNivel();
            return subio;
        }

        // --- Hilo ---

        public void Iniciar()
        {
            lock (_lock)
            {
                if (_activo) return;

                _activo = true;
                _senalParar.Reset();

                _hilo = new Thread(Bucle)
                {
                    IsBackground = true, // sin esto Unity se congela al salir del Play
                    Name = "Hambre"
                };

                _hilo.Start();
            }
        }

        public void Detener()
        {
            Thread hilo;

            lock (_lock)
            {
                if (!_activo) return;

                _activo = false;
                hilo = _hilo;
                _hilo = null;
            }

            _senalParar.Set(); // lo despierta de una vez, sin esperar el tick

            if (hilo != null && hilo.IsAlive)
                hilo.Join(500);
        }

        // El cuerpo va en try-catch: si una vuelta falla (por ejemplo porque
        // un suscriptor de DanioPorHambre revienta), se anota y se sigue. Sin
        // esto el hilo moriria en silencio y el hambre dejaria de bajar para
        // el resto de la partida, sin ningun mensaje.
        private void Bucle()
        {
            while (_activo)
            {
                try
                {
                    Tick();
                }
                catch (System.Exception ex)
                {
                    RegistroDeErrores.Reportar("HambreModel.Bucle", ex);
                }
            }
        }

        /// Una vuelta del hilo. Devuelve por su cuenta cuando toca parar.
        private void Tick()
        {
            // Espera interrumpible: true si nos pidieron parar.
            if (_senalParar.Wait(MsPorTick)) { _activo = false; return; }

            bool pasandoHambre;

            lock (_lock)
            {
                if (_nivel > 0)
                {
                    _nivel -= PuntosPorTick;
                    if (_nivel < 0) _nivel = 0;
                }

                pasandoHambre = _nivel <= 0;
            }

            NotificarNivel();

            if (!pasandoHambre) return;

            // Con el estomago vacio se pierde vida.
            _jugador.RecibirDanio(DanioPorTickSinComer);
            RegistroDeErrores.Avisar("HambreModel.DanioPorHambre",
                () => DanioPorHambre?.Invoke(DanioPorTickSinComer));

            if (_jugador.Vida <= 0)
            {
                RegistroDeErrores.Avisar("HambreModel.MurioDeHambre",
                    () => MurioDeHambre?.Invoke());
                _activo = false;   // no tiene sentido seguir castigando a un muerto
            }
        }

        private void NotificarNivel()
        {
            // El evento se dispara FUERA del lock: si un suscriptor se demora,
            // no deja bloqueado a todo el que quiera leer el nivel.
            RegistroDeErrores.Avisar("HambreModel.NivelCambio",
                () => NivelCambio?.Invoke(Nivel));
        }
    }
}
