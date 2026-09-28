namespace ImperiosEnGuerra.Modelo
{
    public abstract class UnidadModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        // Estadísticas de combate
        public int Vida { get; set; }
        public int VidaMax { get; set; }
        public int Ataque { get; set; }
        public int Defensa { get; set; }

        // Posición en el mapa
        public int Fila { get; set; }
        public int Columna { get; set; }

        // Capacidades de movimiento y ataque
        public int RangoMovimiento { get; set; }
        public int RangoAtaque { get; set; }  // 0=no ataca, 1=cuerpo, 3+=distancia

        // Costos para entrenar esta unidad
        public int CostoOro { get; protected set; }
        public int CostoComida { get; protected set; }
        public int CostoArmas { get; protected set; }  // ← Las armas se producen con hilos
        public int TiempoEntrenamientoSeg { get; protected set; }

        // ¿Es del jugador humano (Grecia) o de la IA ?
        public bool EsDeJugador { get; set; } = true;

        // Estado
        public bool EstaViva => Vida > 0;

        public bool RecibirDanio(int danio)
        {
            int danioReal = System.Math.Max(0, danio - Defensa);
            Vida -= danioReal;
            if (Vida < 0) Vida = 0;
            return !EstaViva;
        }
    }
}
