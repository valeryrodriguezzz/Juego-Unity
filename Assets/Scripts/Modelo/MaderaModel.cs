using ImperiosEnGuerra.Modelo;
namespace ImperiosEnGuerra.Modelo.Recursos
{
    // El jugador llega al Bosque → hilo suma Madera cada segundo
    public class MaderaModel : RecursoModel
    {
        public MaderaModel(float coordenadas)
            : base("Madera", 0, coordenadas) { }
        protected override int ProduccionPorSegundo() => 4;
    }
}