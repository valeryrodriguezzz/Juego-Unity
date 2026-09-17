using System.Collections.Generic;
using System.Threading;

namespace ImperiosEnGuerra.Modelo
{
    public enum EstadoPartida
    {
        MapaMundial, // El jugador está viendo el mapa y eligiendo territorio
        EnBatalla,   // Combatiendo en un territorio
        Terminada    // Ganó o perdió el juego completo
    }

    public class PartidaModel
    {
        // Estado general
        public EstadoPartida Estado { get; set; } = EstadoPartida.MapaMundial;

        // El jugador humano (siempre Grecia)
        public JugadorModel Jugador { get; set; }

        // El mapa mundial con todos los territorios
        public MapaMundialModel MapaMundial { get; set; }

        // Batalla actual:
        public JugadorModel IA { get; set; }
        public MapaModel MapaBatalla { get; set; }
        public TerritorioModel TerritorioEnDisputa { get; set; }

        // Resultado:
        public string NombreGanador { get; set; }

        // Log de acciones: registro escrito de todo lo que pasa en la partida, como una bitácora. Se guarda en un archivo .txt 
        public List<string> LogAcciones { get; private set; } = new List<string>();
        public int NumeroTurno { get; set; } = 1;

        // Hilos de Jugador e IA:
        private Thread _hiloIA;
        private bool _batallaActiva = false;

        public PartidaModel(string nombreJugador)
        {
            Jugador = new JugadorModel(nombreJugador, new CivilizacionModel("Grecia"), esIA: false);
            MapaMundial = new MapaMundialModel();
        }

        public void IniciarBatalla(TerritorioModel territorio)
        {
            TerritorioEnDisputa = territorio;
            Estado = EstadoPartida.EnBatalla;
            MapaBatalla = new MapaModel();
            _batallaActiva = true;

            IA = new JugadorModel(
                nombre: territorio.Imperio,
                civilizacion: territorio.Civilizacion,
                esIA: true
            );

            NumeroTurno = 1;

            // Hilo de la IA — toma decisiones automáticamente:
            _hiloIA = new Thread(TurnoIA);
            _hiloIA.IsBackground = true;
            _hiloIA.Start();
        }

        // Lo que hace la IA en segundo plano durante la batalla:
        private void TurnoIA()
        {
            while (_batallaActiva)
            {
                Thread.Sleep(2000); // La IA actúa cada 2 segundos
                // Aquí el Controlador de IA decidirá qué hacer: mover unidades, atacar, recolectar recursos
            }
        }

        public void TerminarBatalla(bool jugadorGano)
        {
            _batallaActiva = false; // Detiene el hilo de la IA

            if (jugadorGano && TerritorioEnDisputa != null)
            {
                MapaMundial.ConquistarTerritorio(TerritorioEnDisputa);
                NombreGanador = Jugador.Nombre;
            }
            else
            {
                NombreGanador = IA?.Nombre;
            }

            // ¿Ganó el juego completo?
            Estado = MapaMundial.JugadorGanoTodo
                ? EstadoPartida.Terminada   // ganó todo → juego terminado
                : EstadoPartida.MapaMundial; // aún quedan territorios → vuelve al mapa

            IA = null;
            MapaBatalla = null;
            TerritorioEnDisputa = null;
        }

        public void RegistrarAccion(string quienJuega, string accion, string resultado)
        {
            string linea = $"Turno {NumeroTurno} | {quienJuega}\n" +
                           $"Accion: {accion}\n" +
                           $"Resultado: {resultado}\n---";
            LogAcciones.Add(linea);
        }

        public bool VerificarGanadorBatalla()
        {
            if (Jugador.Perdio)
            {
                TerminarBatalla(false);
                return true;
            }

            if (IA != null && IA.Perdio)
            { 
                TerminarBatalla(true);
                return true;
            }
            return false; // nadie perdió todavía → la batalla continúa
        }
    }
}