using ImperiosEnGuerra.Modelo;
namespace ImperiosEnGuerra.Modelo.Recursos
{
    // La tienda de armas del mapa.
    // Las armas NO crecen del suelo, se compran.
    // Por eso ProduccionPorSegundo devuelve 0.
    // El stock real lo maneja TiendaArmasModel con su propio hilo.
    public class ArmasModel : RecursoModel
    {
        public ArmasModel(float coordenadas)
            : base("Armas", 0, coordenadas) { }

        protected override int ProduccionPorSegundo() => 0; // no se regenera solo
    }
}
