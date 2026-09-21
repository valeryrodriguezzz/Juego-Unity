using System;
using System.Threading;

namespace ImperiosEnGuerra.Modelo.Armas
{
    /// <summary>
    /// Clase base de todas las armas. Clase C# pura: NO hereda de MonoBehaviour,
    /// igual que RecursoModel.
    ///
    /// CONCURRENCIA (esta es la parte que pide la materia):
    /// cada arma EQUIPADA levanta su propio hilo de mantenimiento, que le devuelve
    /// durabilidad poco a poco (el arma se afila / se repara sola mientras no la usas).
    /// Ese hilo ESCRIBE la durabilidad al mismo tiempo que el hilo del juego la LEE
    /// (al golpear un arbol, al atacar, al pintar la barra de la UI), por eso todo
    /// acceso al campo _durabilidad va protegido con el mismo 'lock'.
    ///
    /// Es el mismo patron que ya usas en RecursoModel con IniciarRegeneracion(),
    /// solo que aqui el recurso que se regenera es la propia arma.
    /// </summary>
    public abstract class ArmaModel
    {
        // Candado privado y propio de cada arma. Privado para que nadie de afuera
        // pueda bloquearlo y provocar un deadlock.
        private readonly object _candado = new object();

        // Estado mutable: SIEMPRE se toca dentro del lock.
        private int _durabilidad;

        // Infraestructura del hilo.
        private Thread _hiloMantenimiento;
        private volatile bool _mantenimientoActivo;

        // Señal para poder despertar al hilo de inmediato cuando queremos detenerlo,
        // en vez de quedarnos esperando a que termine su Thread.Sleep.
        private readonly ManualResetEventSlim _senalParar = new ManualResetEventSlim(false);

        // ---- Datos fijos (vienen del catalogo, no cambian) ----
        public TipoArma Tipo { get; }
        public string Nombre { get; }
        public int DanoBase { get; }
        public int DurabilidadMaxima { get; }
        public int PrecioOro { get; }
        public bool EsComprable { get; }

        /// <summary>Cada cuantos milisegundos el hilo repara un punto de durabilidad.</summary>
        public int MsEntreReparaciones { get; protected set; } = 2000;

        /// <summary>
        /// Evento que avisa que cambio la durabilidad.
        /// OJO: se dispara desde el hilo de mantenimiento, asi que quien lo escuche
        /// NO puede tocar la API de Unity directamente. El Controlador lo mete en una
        /// cola y lo procesa en Update() (ver TiendaArmasController).
        /// </summary>
        public event Action<ArmaModel> DurabilidadCambio;

        protected ArmaModel(TipoArma tipo)
        {
            InfoArma info = CatalogoArmas.Ficha(tipo);

            Tipo = info.Tipo;
            Nombre = info.Nombre;
            DanoBase = info.DanoBase;
            DurabilidadMaxima = info.DurabilidadMaxima;
            PrecioOro = info.PrecioOro;
            EsComprable = info.EsComprable;

            _durabilidad = info.DurabilidadMaxima;
        }

        // ---- Lecturas thread-safe ------------------------------------------

        public int Durabilidad
        {
            get { lock (_candado) { return _durabilidad; } }
        }

        public bool EstaRota
        {
            get { lock (_candado) { return _durabilidad <= 0; } }
        }

        /// <summary>Porcentaje 0..1, util para pintar una barra en la UI.</summary>
        public float PorcentajeDurabilidad
        {
            get
            {
                lock (_candado)
                {
                    return DurabilidadMaxima <= 0 ? 0f : (float)_durabilidad / DurabilidadMaxima;
                }
            }
        }

        // ---- Polimorfismo: cada arma concreta define esto -------------------

        /// <summary>
        /// Que tan buena es el arma recolectando cierto recurso.
        /// 1.0 = normal, 2.0 = el doble, 0.25 = casi a mano.
        /// El hacha rinde con la madera, el pico con el oro, el cuchillo con la comida.
        /// </summary>
        public abstract float MultiplicadorRecoleccion(TipoRecurso recurso);

        /// <summary>Que tan buena es el arma peleando (multiplica al DanoBase).</summary>
        public abstract float MultiplicadorCombate { get; }

        /// <summary>true en las armas pensadas para pelear (lanza, espada, arco).</summary>
        public virtual bool EsDeCombate => false;

        /// <summary>Solo el martillo construye mas rapido.</summary>
        public virtual float MultiplicadorConstruccion => 1f;

        // ---- Uso del arma ---------------------------------------------------

        /// <summary>
        /// Usa el arma y devuelve cuanto rindio. Devuelve 0 si esta rota.
        /// Thread-safe: el jugador puede estar golpeando mientras el hilo de
        /// mantenimiento esta reparando.
        /// </summary>
        public int Golpear(int desgaste = 1)
        {
            int dano;

            lock (_candado)
            {
                if (_durabilidad <= 0)
                    return 0;

                _durabilidad = Math.Max(0, _durabilidad - Math.Max(0, desgaste));
                dano = (int)Math.Round(DanoBase * MultiplicadorCombate);
            }

            // El evento se dispara FUERA del lock: nunca hay que llamar codigo ajeno
            // (un suscriptor que no controlamos) teniendo el candado tomado, porque
            // ese codigo podria intentar tomar otro lock y provocar un deadlock.
            NotificarCambio();
            return dano;
        }

        /// <summary>
        /// Cuanto recurso se lleva el jugador de un nodo, usando esta arma.
        /// El resultado se le pasa despues a RecursoModel.Recoleccion(cantidad).
        /// </summary>
        public int CalcularRecoleccion(TipoRecurso recurso, int cantidadBase, int desgaste = 1)
        {
            float multiplicador;

            lock (_candado)
            {
                if (_durabilidad <= 0)
                    return 0;

                _durabilidad = Math.Max(0, _durabilidad - Math.Max(0, desgaste));
                multiplicador = MultiplicadorRecoleccion(recurso);
            }

            NotificarCambio();
            return Math.Max(0, (int)Math.Round(cantidadBase * multiplicador));
        }

        /// <summary>Reparacion instantanea (por ejemplo, pagando en la tienda).</summary>
        public void Reparar(int cantidad)
        {
            lock (_candado)
            {
                _durabilidad = Math.Min(DurabilidadMaxima, _durabilidad + Math.Max(0, cantidad));
            }

            NotificarCambio();
        }

        // ---- Hilo de mantenimiento ------------------------------------------

        /// <summary>
        /// Arranca el hilo propio del arma. Se llama cuando el arma se EQUIPA,
        /// no cuando se crea: asi las armas que estan en el stock de la tienda no
        /// gastan un hilo cada una (si no, tendriamos decenas de hilos dormidos).
        /// </summary>
        public void IniciarMantenimiento()
        {
            lock (_candado)
            {
                if (_mantenimientoActivo)
                    return; // ya estaba corriendo, no arrancamos un segundo hilo

                _mantenimientoActivo = true;
                _senalParar.Reset();

                _hiloMantenimiento = new Thread(BucleMantenimiento)
                {
                    // IsBackground = true es CLAVE en Unity: si no, el hilo sigue vivo
                    // al salir del Play Mode y el editor se queda congelado.
                    IsBackground = true,
                    Name = "Mantenimiento-" + Nombre
                };

                _hiloMantenimiento.Start();
            }
        }

        /// <summary>
        /// Detiene el hilo. Llamalo al desequipar el arma y tambien desde
        /// OnDestroy / OnApplicationQuit del Controlador.
        /// </summary>
        public void DetenerMantenimiento()
        {
            Thread hilo;

            lock (_candado)
            {
                if (!_mantenimientoActivo)
                    return;

                _mantenimientoActivo = false;
                hilo = _hiloMantenimiento;
                _hiloMantenimiento = null;
            }

            // Despierta al hilo aunque este en medio de su espera.
            _senalParar.Set();

            // Esperamos un poco a que termine, pero sin bloquear el juego para siempre.
            if (hilo != null && hilo.IsAlive)
                hilo.Join(500);
        }

        private void BucleMantenimiento()
        {
            while (_mantenimientoActivo)
            {
                // Wait devuelve true si nos pidieron parar; false si se cumplio el tiempo.
                // Equivale a Thread.Sleep(MsEntreReparaciones) pero interrumpible.
                if (_senalParar.Wait(MsEntreReparaciones))
                    break;

                bool hubo = false;

                lock (_candado)
                {
                    if (_durabilidad < DurabilidadMaxima)
                    {
                        _durabilidad++;
                        hubo = true;
                    }
                }

                if (hubo)
                    NotificarCambio();
            }
        }

        private void NotificarCambio()
        {
            // Copia local: el suscriptor podria desuscribirse justo entre el if y la llamada.
            Action<ArmaModel> manejador = DurabilidadCambio;
            manejador?.Invoke(this);
        }

        public override string ToString()
        {
            return Nombre + " (" + Durabilidad + "/" + DurabilidadMaxima + ")";
        }
    }
}