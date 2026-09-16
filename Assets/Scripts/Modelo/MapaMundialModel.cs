using System.Collections.Generic;
using System.Linq;

namespace ImperiosEnGuerra.Modelo
{
    /// Mapa mundial con todos los territorios disponibles para conquistar.
    /// El jugador gana cuando conquista TODOS los territorios enemigos.
    public class MapaMundialModel
    {
        // ATRIBUTOS PRIVADOS (-)
        private List<TerritorioModel> territorios;
        private TerritorioModel territorioSeleccionado;

        // PROPIEDADES PÚBLICAS
        public List<TerritorioModel> Territorios
        {
            get { return territorios; }
        }

        // Territorio que el jugador seleccionó con clic
        public TerritorioModel TerritorioSeleccionado
        {
            get { return territorioSeleccionado; }
            set { territorioSeleccionado = value; }
        }

        // ¿El jugador conquistó todos los territorios enemigos?
        public bool JugadorGanoTodo =>
            territorios.All(t => t.Estado() == EstadoTerritorio.Base ||
                                 t.Estado() == EstadoTerritorio.Conquistado);

        public MapaMundialModel()
        {
            territorios = new List<TerritorioModel>();
            InicializarTerritorios();
        }

        // Crea los territorios fijos del mapa con su civilización dueña
        private void InicializarTerritorios()
        {
            // Territorio base de Grecia (no se puede atacar)
            territorios.Add(new TerritorioModel(
                coordenada: 500f,
                imperio: "Grecia",
                estadoInicial: EstadoTerritorio.Base,
                civilizacion: new CivilizacionModel("Grecia")
            ));

            // Territorios enemigos (el jugador hace clic sobre ellos para atacar)
            territorios.Add(new TerritorioModel(
                coordenada: 700f,
                imperio: "Persia",
                civilizacion: new CivilizacionModel("Persia")
            ));

            territorios.Add(new TerritorioModel(
                coordenada: 350f,
                imperio: "Roma",
                civilizacion: new CivilizacionModel("Roma")
            ));

            territorios.Add(new TerritorioModel(
                coordenada: 550f,
                imperio: "Egipto",
                civilizacion: new CivilizacionModel("Egipto")
            ));

            territorios.Add(new TerritorioModel(
                coordenada: 420f,
                imperio: "Vikingos",
                civilizacion: new CivilizacionModel("Vikingos")
            ));
        }

        // El controlador llama este método pasando el territorio que la Vista detectó con el clic del jugador
        public void SeleccionarTerritorio(TerritorioModel territorio)
        {
            territorioSeleccionado = territorio;
        }

        // Conquista el territorio seleccionado (llamado al ganar una batalla):
        public void ConquistarTerritorio(TerritorioModel territorio)
        {
            territorio.ConquistarTerritorio("Grecia");
        }

    }
}