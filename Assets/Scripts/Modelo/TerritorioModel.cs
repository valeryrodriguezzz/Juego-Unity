namespace ImperiosEnGuerra.Modelo
{
    /// Estado de un territorio en el mapa mundial
    public enum EstadoTerritorio
    {
        Base,       // Territorio inicial de Grecia — no se puede atacar
        Neutral,    // Aún no conquistado por nadie
        Conquistado // Ya fue conquistado por el jugador (Grecia)
    }

    /// Representa un territorio en el mapa mundial.
    ///
    /// El jugador (Grecia) ve el mapa mundial con varios territorios.
    /// Al hacer clic en uno, inicia una batalla (mapa 15x15) contra
    /// la civilización dueña de ese territorio.
    /// Si gana, el territorio pasa a ser "Conquistado".
    public class TerritorioModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        // Civilización que defiende este territorio (el oponente en batalla)
        public Civilizacion CivilizacionDuena { get; set; }

        // Estado actual del territorio
        public EstadoTerritorio Estado { get; set; }

        // Posición en el mapa mundial (para saber dónde dibujarlo en pantalla)
        public float PosicionX { get; set; }
        public float PosicionY { get; set; }

        // ¿Se puede atacar ahora?
        // Solo los territorios Neutral o adyacentes a uno conquistado son atacables
        public bool EsAtacable { get; set; }

        public bool EsConquistado => Estado == EstadoTerritorio.Conquistado;
        public bool EsBase        => Estado == EstadoTerritorio.Base;

        public TerritorioModel(int id, string nombre, Civilizacion civ,
                               float x, float y,
                               EstadoTerritorio estado = EstadoTerritorio.Neutral)
        {
            Id                 = id;
            Nombre             = nombre;
            CivilizacionDuena  = civ;
            PosicionX          = x;
            PosicionY          = y;
            Estado             = estado;
            EsAtacable         = (estado == EstadoTerritorio.Neutral);
        }
    }
}
