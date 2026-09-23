namespace ImperiosEnGuerra.Modelo
{
    public abstract class EdificioModel
    {
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
            get { return vida; }
            set { vida = value; }
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
            get { return construido; }
            set { construido = value; }
        }

        public float PorcentajeConstruccion
        {
            get { return porcentajeConstruccion; }
            set { porcentajeConstruccion = value; }
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

        // Propiedad calculada — true si el edificio sigue en pie:
        public bool EstaEnPie => vida > 0;

        // --- MÉTODOS (+) ---

        public virtual void Construccion()    // Marca el edificio como construido
        {
            construido = true;
            porcentajeConstruccion = 100f;
        }

        public bool Destruccion(int daño)
        {
            vida -= daño;
            if (vida < 0) vida = 0;
            return !EstaEnPie;  // true si el edificio fue destruido
        }
    }
}