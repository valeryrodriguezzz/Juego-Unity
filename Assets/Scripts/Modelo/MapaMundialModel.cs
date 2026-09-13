using System.Collections.Generic;
using System.Linq;

namespace ImperiosEnGuerra.Modelo
{
    /// Mapa mundial con todos los territorios/imperios disponibles para conquistar.
    ///
    /// Grecia empieza en su territorio base (centro/sur del mapa).
    /// Los demás territorios pertenecen a otras civilizaciones.
    /// El jugador gana cuando conquista TODOS los territorios.
    public class MapaMundialModel
    {
        public List<TerritorioModel> Territorios { get; private set; }

        // El territorio que el jugador seleccionó para atacar
        public TerritorioModel TerritorioSeleccionado { get; set; }

        // ¿El jugador ganó el juego completo?
        public bool JugadorGanoTodo =>
            Territorios.All(t => t.EsBase || t.EsConquistado);

        public MapaMundialModel()
        {
            Territorios = new List<TerritorioModel>();
            InicializarTerritorios();
        }

        /// Crea los territorios del mapa mundial con su posición y civilización dueña.
        /// Las posiciones (x, y) son coordenadas de pantalla aproximadas.
        /// La Vista las usará para colocar cada botón/icono en el mapa.
        private void InicializarTerritorios()
        {
            // Territorio base de Grecia (ya controlado, no atacable)
            Territorios.Add(new TerritorioModel(
                id: 0,
                nombre: "Grecia",
                civ: Civilizacion.Grecia,
                x: 500, y: 300,
                estado: EstadoTerritorio.Base
            ));

            // Territorios enemigos (neutrales al inicio)
            Territorios.Add(new TerritorioModel(
                id: 1,
                nombre: "Persia",
                civ: Civilizacion.Persia,
                x: 700, y: 250
            ));

            Territorios.Add(new TerritorioModel(
                id: 2,
                nombre: "Roma",
                civ: Civilizacion.Roma,
                x: 350, y: 220
            ));

            Territorios.Add(new TerritorioModel(
                id: 3,
                nombre: "Egipto",
                civ: Civilizacion.Egipto,
                x: 550, y: 420
            ));

            Territorios.Add(new TerritorioModel(
                id: 4,
                nombre: "Escandinavia",
                civ: Civilizacion.Vikingos,
                x: 420, y: 100
            ));
        }

        /// Marca un territorio como conquistado después de ganar una batalla.
        public void ConquistarTerritorio(int territorioId)
        {
            var territorio = Territorios.FirstOrDefault(t => t.Id == territorioId);
            if (territorio != null)
            {
                territorio.Estado     = EstadoTerritorio.Conquistado;
                territorio.EsAtacable = false;
            }
        }

        /// Retorna solo los territorios que se pueden atacar:
        public List<TerritorioModel> ObtenerAtacables()
        {
            return Territorios.Where(t => t.EsAtacable).ToList();
        }


        /// Retorna cuántos territorios enemigos quedan por conquistar:
        public int TerritoriosRestantes()
        {
            return Territorios.Count(t => !t.EsBase && !t.EsConquistado); ///comprueba que no sea base y que no esté conquistado
        }
    }
}
