namespace ImperiosEnGuerra.Modelo
{
    /// El mapa del juego: una grilla de 15 filas x 15 columnas. Cada posición es una CeldaModel que sabe qué hay en ella.
    public class MapaModel
    {
        public const int FILAS = 15;
        public const int COLUMNAS = 15;

        // La grilla principal del juego
        public CeldaModel[,] Celdas { get; private set; }

        public MapaModel()
        {
            Celdas = new CeldaModel[FILAS, COLUMNAS];

            // Inicializar todas las celdas como libres
            for (int f = 0; f < FILAS; f++)
                for (int c = 0; c < COLUMNAS; c++)
                    Celdas[f, c] = new CeldaModel(f, c);
        }

        /// Verifica si una posición (fila, columna) existe dentro del mapa.
        public bool EstaDentroDelMapa(int fila, int columna)
        {
            return fila >= 0 && fila < FILAS &&
                   columna >= 0 && columna < COLUMNAS;
        }

        /// Retorna la celda en la posición indicada, o null si está fuera del mapa.
        public CeldaModel ObtenerCelda(int fila, int columna)
        {
            if (!EstaDentroDelMapa(fila, columna)) return null;
            return Celdas[fila, columna];
        }

        /// Retorna true si la celda existe Y está libre (se puede colocar algo).
        public bool CeldaEstaLibre(int fila, int columna)
        {
            var celda = ObtenerCelda(fila, columna);
            return celda != null && celda.EstaLibre;
        }

        /// Calcula la distancia en celdas entre dos posiciones (sin diagonal).
        public static int Distancia(int fila1, int col1, int fila2, int col2)
        {
            return System.Math.Abs(fila1 - fila2) + System.Math.Abs(col1 - col2);
        }
    }
}
