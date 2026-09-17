using ImperiosEnGuerra.Modelo;
namespace ImperiosEnGuerra.Modelo.Recursos
{
    // El jugador llega a la Mina → hilo suma Oro cada segundo
    public class OroModel : RecursoModel
    {
        public OroModel(float coordenadas)
            : base("Oro", 0, coordenadas) { } //Constructor de OroModel. El : base(...) significa "llama al constructor del padre" con esos valores.
        protected override int ProduccionPorSegundo() => 4; //Reemplaza el método abstracto del padre.
    }
}