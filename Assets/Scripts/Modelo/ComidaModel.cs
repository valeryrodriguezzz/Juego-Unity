using ImperiosEnGuerra.Modelo;
namespace ImperiosEnGuerra.Modelo.Recursos
{
    // El jugador llega a la zona de caza → hilo suma Comida cada segundo
    public class ComidaModel : RecursoModel
    {
        public ComidaModel(float coordenadas)
            : base("Comida", 0, coordenadas) { }
        protected override int ProduccionPorSegundo() => 5;
    }
}