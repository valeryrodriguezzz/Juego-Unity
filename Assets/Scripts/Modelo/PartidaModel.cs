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

        // Batalla actual
        public JugadorModel IA { get; set; }
        public MapaModel MapaBatalla { get; set; }
        public TerritorioModel TerritorioEnDisputa { get; set; }

        // Resultado
        public string NombreGanador { get; set; }

        // Log de acciones: guarda lo que pasa en la partida
        public List<string> LogAcciones { get; private set; } = new List<string>();
        public int NumeroTurno { get; set; } = 1;

        // Hilos de Jugador e IA (maquina)
        private Thread _hiloIA;
        private Thread _hiloJugador;
        private bool _batallaActiva = false;
        private readonly object _lock = new object();

        // Señal que activa el hilo del jugador cuando hace una acción
        private ManualResetEventSlim _accionJugador
            = new ManualResetEventSlim(false);

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

            // Hilo del Jugador — espera que el jugador haga clic en "Atacar"
            _hiloJugador = new Thread(TurnoJugador);
            _hiloJugador.IsBackground = true;
            _hiloJugador.Start();

            // Hilo de la IA — ataca automáticamente cada 2 segundos
            _hiloIA = new Thread(TurnoIA);
            _hiloIA.IsBackground = true;
            _hiloIA.Start();
        }

        // Hilo del Jugador:
        // Espera la señal (clic en "Atacar"), ataca y vuelve a esperar
        private void TurnoJugador()
        {
            while (_batallaActiva)
            {
                _accionJugador.Wait();    // Duerme hasta que el jugador haga clic
                _accionJugador.Reset();   // Reinicia la señal para la próxima vez

                if (!_batallaActiva) break;

                lock (_lock)
                {
                    IA.Vida -= Jugador.NivelFuerza;
                    if (IA.Vida < 0) IA.Vida = 0;

                    if (IA.Vida <= 0)
                        TerminarBatalla(jugadorGano: true);
                }
            }
        }

        // Hilo de la IA:
        // Actúa sola cada 2 segundos, sin esperar nada
        private void TurnoIA()
        {
            while (_batallaActiva)
            {
                Thread.Sleep(2000); // La IA ataca cada 2 segundos

                lock (_lock)
                {
                    if (!_batallaActiva) break;

                    Jugador.Vida -= IA.NivelFuerza;
                    if (Jugador.Vida < 0) Jugador.Vida = 0;

                    if (Jugador.Vida <= 0)
                        TerminarBatalla(jugadorGano: false);
                }
            }
        }

        // La Vista llama este método cuando el jugador hace clic en "Atacar"
        // → despierta el hilo del jugador
        public void JugadorAtaca()
        {
            _accionJugador.Set();
        }

        public void TerminarBatalla(bool jugadorGano)
        {
            _batallaActiva = false;      // Detiene ambos hilos
            _accionJugador.Set();        // Despierta el hilo del jugador para que pueda terminar

            if (jugadorGano && TerritorioEnDisputa != null)
            {
                MapaMundial.ConquistarTerritorio(TerritorioEnDisputa);
                NombreGanador = Jugador.Nombre;
            }
            else
            {
                NombreGanador = IA?.Nombre;
            }

            Estado = MapaMundial.JugadorGanoTodo
                ? EstadoPartida.Terminada
                : EstadoPartida.MapaMundial;

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
            if (Jugador.Perdio) { TerminarBatalla(false); return true; }
            if (IA != null && IA.Perdio) { TerminarBatalla(true); return true; }
            return false;
        }
    }
}