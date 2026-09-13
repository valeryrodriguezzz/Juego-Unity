using System.Collections.Generic;

namespace ImperiosEnGuerra.Modelo
{
    public enum EstadoPartida
    {
        MapaMundial,  // El jugador está viendo el mapa y eligiendo territorio
        EnBatalla,    // Combatiendo en un territorio (mapa 15x15)
        Terminada     // Ganó o perdió el juego completo
    }

    /// Estado global de la partida completa.
    ///
    /// Contiene el mapa mundial (con todos los territorios)
    /// y el estado de la batalla actual (cuando hay una activa).
    public class PartidaModel
    {
        // Estado general
        public EstadoPartida Estado { get; set; } = EstadoPartida.MapaMundial;

        // El jugador humano (siempre Grecia)
        public JugadorModel Jugador { get; set; }

        // El mapa mundial con todos los territorios
        public MapaMundialModel MapaMundial { get; set; }

        // ── Batalla actual ─────────────────────────────────────────────
        // Se llena cuando el jugador selecciona un territorio para atacar
        public JugadorModel IA { get; set; }              // Oponente en la batalla actual
        public MapaModel MapaBatalla { get; set; }        // Mapa 15x15 de la batalla
        public TerritorioModel TerritorioEnDisputa { get; set; } // Qué territorio se está disputando

        // Resultado
        public string NombreGanador { get; set; }

        // Log de acciones (para log_partida.txt)
        public List<string> LogAcciones { get; private set; } = new List<string>();
        public int NumeroTurno { get; set; } = 1;

        public PartidaModel(string nombreJugador)
        {
            // El jugador siempre es Grecia
            Jugador     = new JugadorModel(nombreJugador, Civilizacion.Grecia, esIA: false);
            MapaMundial = new MapaMundialModel();
        }

        /// Inicia una batalla contra el territorio seleccionado.
        /// Crea al oponente IA con la civilización de ese territorio.
        public void IniciarBatalla(TerritorioModel territorio)
        {
            TerritorioEnDisputa = territorio;
            Estado              = EstadoPartida.EnBatalla;
            MapaBatalla         = new MapaModel();

            // El oponente es la civilización dueña del territorio
            string nombreIA = CivilizacionInfo.NombreEdificioPrincipal(territorio.CivilizacionDuena);
            IA = new JugadorModel(
                nombre: territorio.Nombre,
                civilizacion: territorio.CivilizacionDuena,
                esIA: true
            );

            NumeroTurno = 1;
        }

        /// Termina la batalla actual. Si ganó el jugador, conquista el territorio.
        public void TerminarBatalla(bool jugadorGano)
        {
            if (jugadorGano && TerritorioEnDisputa != null)
            {
                MapaMundial.ConquistarTerritorio(TerritorioEnDisputa.Id);
                NombreGanador = Jugador.Nombre;
            }
            else
            {
                NombreGanador = IA?.Nombre;
            }

            // ¿Ganó el juego completo?
            Estado = MapaMundial.JugadorGanoTodo
                ? EstadoPartida.Terminada
                : EstadoPartida.MapaMundial; // Vuelve al mapa mundial

            // Limpiar batalla
            IA                  = null;
            MapaBatalla         = null;
            TerritorioEnDisputa = null;
        }

        /// Registra una acción en el log (para log_partida.txt).
        public void RegistrarAccion(string quienJuega, string accion, string resultado)
        {
            string linea = $"Turno {NumeroTurno} | {quienJuega}\n" +
                           $"Accion: {accion}\n" +
                           $"Resultado: {resultado}\n---";
            LogAcciones.Add(linea);
        }

        /// Verifica si hay ganador en la batalla actual.
        public bool VerificarGanadorBatalla()
        {
            if (Jugador.Perdio)      { TerminarBatalla(jugadorGano: false); return true; }
            if (IA != null && IA.Perdio) { TerminarBatalla(jugadorGano: true);  return true; }
            return false;
        }
    }
}
