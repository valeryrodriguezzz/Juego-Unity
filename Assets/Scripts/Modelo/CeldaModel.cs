namespace ImperiosEnGuerra.Modelo
{
    public enum Propietario //enum: lista de opciones fijas, en este caso son las posibles para el propietario de una celda
    {
        Nadie,    // Territorio neutral, no conquistado
        Jugador,  // Controlado por Grecia (el jugador humano)
        IA        // Controlado por el Imperio Persa (la IA)
    }

    public enum TipoCelda
    {
        Libre,      // Sin nada encima, se puede ocupar
        Recurso,    // Tiene un nodo de recurso natural (oro, madera, comida)
        Edificio,   // Tiene un edificio construido
        Unidad,     // Tiene una unidad militar
        Bloqueado   // Terreno no transitable (agua, montaña)
    }

    public class CeldaModel
    {
        public int Fila     { get; set; }
        public int Columna  { get; set; }
        public TipoCelda Tipo { get; set; }

        // Quién controla esta celda
        public Propietario ControladaPor { get; set; } = Propietario.Nadie;

        // Contenido de la celda (solo uno a la vez según Tipo)
        public RecursoModel  Recurso  { get; set; }
        public EdificioModel Edificio { get; set; }
        public UnidadModel   Unidad   { get; set; }

        public bool EstaLibre => Tipo == TipoCelda.Libre;

        public CeldaModel(int fila, int columna)
        {
            Fila    = fila;
            Columna = columna;
            Tipo    = TipoCelda.Libre;
            ControladaPor = Propietario.Nadie;
        }

        public void Conquistar(Propietario nuevoDueno)
        {
            ControladaPor = nuevoDueno;
        }

        public void LimpiarContenido()
        {
            Tipo     = TipoCelda.Libre;
            Recurso  = null;
            Edificio = null;
            Unidad   = null;
        }
    }
}
