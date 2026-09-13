namespace ImperiosEnGuerra.Modelo
{
    public enum TipoRecurso
    {
        Oro,
        Madera,
        Comida,
        Armas       // ← Producido por la Armería mediante un hilo en segundo plano
    }

    public class RecursoModel
    {
        public TipoRecurso Tipo { get; set; }
        public int Cantidad { get; set; }
        public int CantidadMaxima { get; set; }
        public int Fila { get; set; }
        public int Columna { get; set; }

        public bool Agotado => Cantidad <= 0;

        public RecursoModel(TipoRecurso tipo, int cantidad, int fila, int columna)
        {
            Tipo = tipo;
            Cantidad = cantidad;
            CantidadMaxima = cantidad;
            Fila = fila;
            Columna = columna;
        }
    }
}
