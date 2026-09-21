using System.Threading;

namespace ImperiosEnGuerra.Modelo
{
    /// Clase base para todos los recursos del juego.
    /// Recoleccion() arranca un hilo en segundo plano que suma cantidad automáticamente mientras el jugador está en la ubicación del recurso.
    ///
    /// ===================================================================
    ///  VERSION REVISADA. Todo lo que cambio esta marcado con [1]..[5].
    ///  [1] es NECESARIO para conectar las armas.
    ///  [2] y [3] corrigen dos errores reales de concurrencia.
    ///  [4] y [5] son mejoras opcionales.
    ///  Las firmas que ya usabas (Recoleccion, DetenerRecoleccion, Gasto,
    ///  Tipo, Cantidad, Coordenadas) NO cambiaron: nada de lo que ya
    ///  escribieron se rompe.
    /// ===================================================================
    public abstract class RecursoModel
    {
        // ATRIBUTOS PRIVADOS (-)
        private string tipo;
        private int cantidad;
        private float coordenadas;

        // Control del hilo
        private Thread _hiloRecoleccion;

        // [2] CAMBIO: 'volatile'.
        // Sin volatile, el compilador puede guardarse este bool en un registro
        // dentro del while del hilo y no volver a mirar la memoria nunca.
        // Resultado: llamas DetenerRecoleccion() y el hilo sigue corriendo.
        // Es un error que casi nunca se ve en el editor y aparece en la build.
        private volatile bool _recolectando = false;

        private readonly object _lock = new object(); // Evita conflictos entre hilos

        // [4] NUEVO (opcional): señal para poder cortar la espera del hilo al instante.
        // Sin esto, DetenerRecoleccion() deja al hilo durmiendo hasta 1 segundo mas.
        private readonly ManualResetEventSlim _senalParar = new ManualResetEventSlim(false);

        // [5] NUEVO (opcional): tope. Sin tope, un arbol al que nadie le tala
        // durante 10 minutos termina con 600 de madera. Con tope, el nodo se
        // "llena" y deja de crecer, que es como funciona en Age of Empires.
        //
        // El set es publico (no protected) para que el Controlador pueda
        // configurarlo desde el Inspector de Unity: cada arbol de la escena
        // puede tener un tope distinto sin crear una subclase por cada uno.
        public int CantidadMaxima { get; set; } = int.MaxValue;

        // PROPIEDADES PÚBLICAS — permite leerlo y modificarlo de forma controlada
        public string Tipo
        {
            get { return tipo; }
            set { tipo = value; }
        }

        public int Cantidad
        {
            get { lock (_lock) { return cantidad; } }
        }

        public float Coordenadas
        {
            get { return coordenadas; }
            set { coordenadas = value; }
        }

        public bool EstaRecolectando => _recolectando;

        /// [5] NUEVO: util para pintar una barra de "cuanto le queda al arbol".
        public bool EstaAgotado
        {
            get { lock (_lock) { return cantidad <= 0; } }
        }

        // CONSTRUCTOR
        public RecursoModel(string tipo, int cantidadInicial, float coordenadas)
        {
            this.tipo = tipo;
            this.cantidad = cantidadInicial;
            this.coordenadas = coordenadas;
        }

        //MÉTODOS

        // El jugador llega al lugar → arranca el hilo. ACÁ ESTA LA CONCURRENCIA . El hilo suma recursos cada segundo mientras el jugador está en la zona.
        public void Recoleccion()
        {
            // [3] CAMBIO: la verificacion va DENTRO del lock.
            // Antes, si dos colliders disparaban Recoleccion() casi al mismo tiempo,
            // los dos podian pasar el 'if' antes de que alguno pusiera la bandera
            // en true, y se creaban DOS hilos sumando al mismo recurso: el arbol
            // producia el doble y el segundo hilo quedaba sin forma de detenerse.
            lock (_lock)
            {
                if (_recolectando) return; // Ya está recolectando

                _recolectando = true;
                _senalParar.Reset();

                _hiloRecoleccion = new Thread(RecolectarEnHilo);
                _hiloRecoleccion.IsBackground = true; // Se detiene si el juego cierra
                _hiloRecoleccion.Start();
            }
        }

        // Lógica que corre en el hilo: suma recursos cada segundo
        private void RecolectarEnHilo()
        {
            while (_recolectando)
            {
                // [4] CAMBIO: equivale a Thread.Sleep(1000), pero se puede interrumpir.
                // Devuelve true si nos pidieron parar antes de que pasara el segundo.
                if (_senalParar.Wait(1000))
                    break;

                lock (_lock)        // Bloquea para evitar conflictos. Sin lock dos hilos pueden leer el mismo valor al mismo tiempo y perder datos.
                {
                    cantidad += ProduccionPorSegundo();

                    // [5] CAMBIO: no pasarse del tope.
                    if (cantidad > CantidadMaxima)
                        cantidad = CantidadMaxima;
                }
            }
        }

        // El jugador se va del lugar → detiene el hilo:
        public void DetenerRecoleccion()
        {
            Thread hilo;

            lock (_lock)
            {
                if (!_recolectando) return;

                _recolectando = false;
                hilo = _hiloRecoleccion;
                _hiloRecoleccion = null;
            }

            _senalParar.Set(); // [4] despierta al hilo de una vez

            if (hilo != null && hilo.IsAlive)
                hilo.Join(500); // lo esperamos, pero maximo medio segundo
        }

        // [1] NUEVO — ESTE ES EL METODO QUE NECESITAN LAS ARMAS.
        //
        // Por que no basta con Gasto(): para saber cuanto se llevo el jugador
        // habria que hacer "leer Cantidad -> decidir -> llamar Gasto", y entre
        // esos tres pasos el hilo de regeneracion ya cambio el valor. Es el
        // error clasico de "comprobar y despues actuar" (check-then-act): el
        // jugador termina recibiendo madera que el arbol ya no tenia.
        //
        // Aqui se comprueba y se descuenta DENTRO del mismo lock, y se devuelve
        // cuanto se saco realmente. Si el arbol solo tenia 3 y pediste 10,
        // devuelve 3. Ese numero es el que se le suma al jugador.
        public int Recolectar(int solicitado)
        {
            if (solicitado <= 0) return 0;

            lock (_lock)
            {
                int obtenido = cantidad < solicitado ? cantidad : solicitado;
                cantidad -= obtenido;
                return obtenido;
            }
        }

        // El jugador gasta el recurso:
        public void Gasto(int gastado)
        {
            lock (_lock)
            {
                if (cantidad >= gastado)
                    cantidad -= gastado;
            }
        }

        // Cada subclase define cuánto produce por segundo:
        protected abstract int ProduccionPorSegundo();
    }
}