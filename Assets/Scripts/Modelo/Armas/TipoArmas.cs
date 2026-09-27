namespace ImperiosEnGuerra.Modelo.Armas
{
    /// <summary>
    /// Las 4 herramientas del juego. Todas son comprables en la tienda.
    ///
    /// Se quitaron Lanza, Espada y Arco: eran armas de combate para los otros
    /// personajes seleccionables, y al quedar el Pawn como unico jugador ya no
    /// tenian a quien pertenecer. Si mas adelante las unidades entrenables
    /// necesitan armamento propio, se agregan aqui de nuevo.
    /// </summary>
    public enum TipoArma
    {
        Hacha,
        Pico,
        Cuchillo,
        Martillo
    }
}
