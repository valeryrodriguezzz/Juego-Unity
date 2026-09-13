using System;
using ImperiosEnGuerra.Modelo;

namespace ImperiosEnGuerra.Controlador
{
    /// Controlador del Mapa Mundial.
    /// Recibe acciones del jugador (clic en territorio),
    /// valida si son posibles y actualiza el Modelo.
    /// Notifica a la Vista mediante eventos.
    public class MapaMundialController
    {
        private readonly PartidaModel _partida;

        // ── Eventos que la Vista escucha ───────────────────────────────
        // Se disparan cuando algo cambia en el modelo

        // El jugador hizo clic en un territorio válido
        public event Action<TerritorioModel> OnTerritorioSeleccionado;

        // El jugador confirmó atacar → la Vista debe cargar la escena de batalla
        public event Action<TerritorioModel> OnBatallaIniciada;

        // El jugador intentó atacar un territorio no atacable
        public event Action<string> OnAccionInvalida;

        // El jugador conquistó todos los territorios → ganó el juego
        public event Action OnJuegoGanado;

        public MapaMundialController(PartidaModel partida)
        {
            _partida = partida;
        }

        /// La Vista llama este método cuando el jugador hace clic en un territorio.
        /// Valida si es atacable y lo selecciona.
        public void SeleccionarTerritorio(int territorioId)
        {
            var territorio = _partida.MapaMundial.Territorios
                .Find(t => t.Id == territorioId);

            if (territorio == null)
            {
                OnAccionInvalida?.Invoke("Territorio no encontrado.");
                return;
            }

            if (territorio.EsBase)
            {
                OnAccionInvalida?.Invoke("Ese es tu territorio. Elige uno enemigo.");
                return;
            }

            if (territorio.EsConquistado)
            {
                OnAccionInvalida?.Invoke($"{territorio.Nombre} ya fue conquistado.");
                return;
            }

            // Territorio válido — lo seleccionamos
            _partida.MapaMundial.TerritorioSeleccionado = territorio;
            OnTerritorioSeleccionado?.Invoke(territorio);
        }

        /// La Vista llama este método cuando el jugador confirma el ataque
        /// (por ejemplo, presiona un botón "¡Atacar!").
        public void ConfirmarAtaque()
        {
            var territorio = _partida.MapaMundial.TerritorioSeleccionado;

            if (territorio == null)
            {
                OnAccionInvalida?.Invoke("Primero selecciona un territorio.");
                return;
            }

            // Iniciar la batalla en el modelo
            _partida.IniciarBatalla(territorio);

            // Notificar a la Vista para que cargue la escena de batalla
            OnBatallaIniciada?.Invoke(territorio);
        }

        /// Llamado por PartidaController al terminar una batalla ganada.
        /// Actualiza el mapa y verifica si el jugador ganó todo.
        public void NotificarBatallaGanada(int territorioId)
        {
            _partida.MapaMundial.ConquistarTerritorio(territorioId);

            if (_partida.MapaMundial.JugadorGanoTodo)
                OnJuegoGanado?.Invoke();
        }

        /// Retorna todos los territorios para que la Vista los dibuje.
        public System.Collections.Generic.List<TerritorioModel> ObtenerTerritorios()
        {
            return _partida.MapaMundial.Territorios;
        }
    }
}
