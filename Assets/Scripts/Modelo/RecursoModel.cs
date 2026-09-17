using System.Threading;

namespace ImperiosEnGuerra.Modelo
{
    /// Clase base para todos los recursos del juego.
    /// Recoleccion() arranca un hilo en segundo plano que suma cantidad automáticamente mientras el jugador está en la ubicación del recurso.
    public abstract class RecursoModel
    {
        // ATRIBUTOS PRIVADOS (-)
        private string tipo;
        private int cantidad;
        private float coordenadas;

        // Control del hilo
        private Thread _hiloRecoleccion;
        private bool _recolectando = false;
        private readonly object _lock = new object(); // Evita conflictos entre hilos

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
            if (_recolectando) return; // Ya está recolectando

            _recolectando = true;
            _hiloRecoleccion = new Thread(RecolectarEnHilo);
            _hiloRecoleccion.IsBackground = true; // Se detiene si el juego cierra
            _hiloRecoleccion.Start();
        }

        // Lógica que corre en el hilo: suma recursos cada segundo
        private void RecolectarEnHilo()
        {
            while (_recolectando)
            {
                Thread.Sleep(1000); // Espera 1 segundo
                lock (_lock)        // Bloquea para evitar conflictos. Sin lock dos hilos pueden leer el mismo valor al mismo tiempo y perder datos.
                {
                    cantidad += ProduccionPorSegundo();
                }
            }
        }

        // El jugador se va del lugar → detiene el hilo:
        public void DetenerRecoleccion()
        {
            _recolectando = false;
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