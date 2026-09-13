namespace ImperiosEnGuerra.Modelo
{
    public abstract class EdificioModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        // Vida del edificio
        public int Vida { get; set; }
        public int VidaMax { get; set; }

        // Posición en el mapa
        public int Fila { get; set; }
        public int Columna { get; set; }

        // Estado de construcción
        public bool Construido { get; set; }
        public float PorcentajeConstruccion { get; set; }   // 0 a 100
        public int TiempoConstruccionSeg { get; protected set; }

        // Costos para construirlo
        public int CostoOro { get; protected set; }
        public int CostoMadera { get; protected set; }

        // Producción de recursos (si aplica)
        // El RecursoController usa estos campos para saber qué producir
        public TipoRecurso? RecursoQueProduce { get; protected set; } = null;
        public int ProduccionPorSegundo { get; protected set; } = 0;

        // ¿Es del jugador o de la IA?
        public bool EsDeJugador { get; set; }

        public bool EstaEnPie => Vida > 0;

        public bool RecibirDanio(int danio)
        {
            Vida -= danio;
            if (Vida < 0) Vida = 0;
            return !EstaEnPie;
        }
    }
}
