using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ImperiosEnGuerra.Modelo.Edificios;

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
        private enum AccionJugador { AtacarIA, AtacarCentroUrbano }

        // Estado general
        public EstadoPartida Estado { get; set; } = EstadoPartida.MapaMundial;

        // El jugador humano (siempre Grecia)
        public JugadorModel Jugador { get; set; }

        // El mapa mundial con todos los territorios
        public MapaMundialModel MapaMundial { get; set; }

        // Batalla actual
        public JugadorModel IA { get; set; }
        public TerritorioModel TerritorioEnDisputa { get; set; }

        // Resultado de la última batalla
        public string NombreGanador { get; set; }
        public bool JugadorGanoUltimaBatalla { get; private set; }
        public string MotivoFinUltimaBatalla { get; private set; }
        public string TerritorioUltimaBatalla { get; private set; }
        public DateTime FechaInicioBatalla { get; private set; }
        public TimeSpan DuracionUltimaBatalla { get; private set; }

        // Se dispara UNA vez por batalla, desde el hilo que la termina (NO es el hilo
        // principal de Unity). Si el suscriptor toca la UI de Unity debe usar
        // MainThreadDispatcher.Encolar(...). El parametro es: ¿gano el jugador?
        public event Action<bool> OnBatallaTerminada;

        // Se dispara en cada acción relevante (también desde hilos de combate).
        // Parametros: turno, quien juega, accion, resultado.
        public event Action<int, string, string, string> OnAccionRegistrada;

        // Log de acciones (lo escriben varios hilos: se accede solo con _lockLog)
        private readonly List<string> _logAcciones = new List<string>();
        private readonly object _lockLog = new object();
        public int NumeroTurno { get; private set; } = 1;

        // Hilos de Jugador e IA (maquina)
        private Thread _hiloIA;
        private Thread _hiloJugador;
        private volatile bool _batallaActiva = false;
        private volatile int _msEntreAtaquesIA = 2000;
        private readonly object _lock = new object();

        // Acciones que el jugador pidio (clics). Cola thread-safe: el hilo de la vista
        // las encola y el hilo del jugador las consume.
        private readonly ConcurrentQueue<AccionJugador> _accionesJugador = new ConcurrentQueue<AccionJugador>();

        // Señal que activa el hilo del jugador cuando hace una acción
        private readonly ManualResetEventSlim _accionJugador = new ManualResetEventSlim(false);

        // Señal para interrumpir la espera de la IA sin esperar el intervalo completo
        private readonly ManualResetEventSlim _senalParar = new ManualResetEventSlim(false);

        public PartidaModel(string nombreJugador)
        {
            Jugador = new JugadorModel(nombreJugador, new CivilizacionModel("Grecia"), esIA: false);
            MapaMundial = new MapaMundialModel();

            // Cada edificio que el jugador termina de construir queda en el log
            Jugador.OnEdificioConstruido += e =>
                RegistrarAccion(Jugador.Nombre, "Construccion", e.Nombre + " terminado");
        }

        // Apaga TODO: hilos de batalla y hilos de los edificios del jugador.
        // Llamar desde OnDestroy / OnApplicationQuit y al reemplazar la partida.
        public void DetenerTodo()
        {
            DetenerBatalla();
            Jugador.DetenerEdificios();
        }

        public void IniciarBatalla(TerritorioModel territorio)
        {
            // Si quedaba una batalla anterior con hilos vivos, se apagan primero
            DetenerBatalla();

            lock (_lock)
            {
                TerritorioEnDisputa = territorio;
                Estado = EstadoPartida.EnBatalla;

                // La dificultad viene del territorio
                IA = new JugadorModel(
                    nombre: territorio.Imperio,
                    civilizacion: territorio.Civilizacion,
                    esIA: true
                );
                IA.VidaMax = territorio.VidaIA;
                IA.Vida = territorio.VidaIA;
                IA.NivelFuerza = territorio.FuerzaIA;
                IA.EdificioPrincipal = new CentroUrbanoModel(
                    "Centro Urbano de " + territorio.Nombre, territorio.VidaCentroUrbano);
                _msEntreAtaquesIA = territorio.MsEntreAtaquesIA;

                NumeroTurno = 1;
                FechaInicioBatalla = DateTime.Now;
                lock (_lockLog) { _logAcciones.Clear(); }

                AccionJugador descartada;
                while (_accionesJugador.TryDequeue(out descartada)) { }
                _accionJugador.Reset();
                _senalParar.Reset();
                _batallaActiva = true;

                _hiloJugador = new Thread(TurnoJugador) { IsBackground = true };
                _hiloJugador.Start();

                _hiloIA = new Thread(TurnoIA) { IsBackground = true };
                _hiloIA.Start();
            }
        }

        // Hilo del Jugador: espera la señal (clic), ejecuta las acciones pedidas y vuelve a esperar
        private void TurnoJugador()
        {
            while (_batallaActiva)
            {
                _accionJugador.Wait();
                _accionJugador.Reset();

                AccionJugador accion;
                while (_accionesJugador.TryDequeue(out accion))
                {
                    lock (_lock)
                    {
                        // Se revisa DENTRO del lock: la IA pudo terminar la batalla justo antes
                        if (!_batallaActiva || IA == null) return;

                        if (accion == AccionJugador.AtacarCentroUrbano)
                            AtacarCentroUrbanoIA();
                        else
                            AtacarVidaIA();
                    }
                }
            }
        }

        // Siempre se llama con _lock tomado
        private void AtacarVidaIA()
        {
            Jugador.Atacar(IA);
            RegistrarAccion(Jugador.Nombre, "Ataque",
                "Impacto - " + IA.Nombre + " queda con " + IA.Vida + " de vida");

            if (IA.Perdio)
                TerminarBatalla(true, IA.Nombre + " se quedo sin vida");
        }

        // Siempre se llama con _lock tomado
        private void AtacarCentroUrbanoIA()
        {
            EdificioModel centro = IA.EdificioPrincipal;
            if (centro == null) { AtacarVidaIA(); return; }

            bool destruido = centro.Destruccion(Jugador.NivelFuerza);
            RegistrarAccion(Jugador.Nombre, "Ataque al centro urbano",
                "Impacto - " + centro.Nombre + " queda con " + centro.Vida + " de vida" +
                (destruido ? " - Centro urbano destruido" : ""));

            if (destruido)
                TerminarBatalla(true, "Centro urbano enemigo destruido");
        }

        // Hilo de la IA: actúa sola con el intervalo definido por la dificultad del territorio
        private void TurnoIA()
        {
            while (_batallaActiva)
            {
                // Espera interrumpible: devuelve true si pidieron parar
                if (_senalParar.Wait(_msEntreAtaquesIA)) break;

                lock (_lock)
                {
                    if (!_batallaActiva || IA == null) break;

                    IA.Atacar(Jugador);
                    RegistrarAccion(IA.Nombre, "Ataque",
                        "Impacto - " + Jugador.Nombre + " queda con " + Jugador.Vida + " de vida");
                    NumeroTurno++;

                    if (Jugador.Perdio)
                        TerminarBatalla(false, Jugador.Nombre + " se quedo sin vida");
                }
            }
        }

        // La Vista llama este método cuando el jugador hace clic en "Atacar"
        public void JugadorAtaca()
        {
            if (!_batallaActiva) return;
            _accionesJugador.Enqueue(AccionJugador.AtacarIA);
            _accionJugador.Set();
        }

        // La Vista llama este método cuando el jugador ataca el centro urbano enemigo
        public void JugadorAtacaCentroUrbano()
        {
            if (!_batallaActiva) return;
            _accionesJugador.Enqueue(AccionJugador.AtacarCentroUrbano);
            _accionJugador.Set();
        }

        public void TerminarBatalla(bool jugadorGano, string motivo = null)
        {
            lock (_lock)
            {
                // Si ya terminó (los dos hilos pueden intentarlo a la vez), no se repite
                if (!_batallaActiva) return;

                _batallaActiva = false;
                _accionJugador.Set();   // despierta al hilo del jugador para que salga
                _senalParar.Set();      // despierta a la IA para que salga

                JugadorGanoUltimaBatalla = jugadorGano;
                MotivoFinUltimaBatalla = motivo ??
                    (jugadorGano ? "El enemigo se quedo sin vida" : "El jugador se quedo sin vida");
                DuracionUltimaBatalla = DateTime.Now - FechaInicioBatalla;
                TerritorioUltimaBatalla = TerritorioEnDisputa != null ? TerritorioEnDisputa.Nombre : null;

                if (jugadorGano && TerritorioEnDisputa != null)
                {
                    MapaMundial.ConquistarTerritorio(TerritorioEnDisputa);
                    NombreGanador = Jugador.Nombre;
                }
                else
                {
                    NombreGanador = IA != null ? IA.Nombre : null;
                }

                // Si pierde, la partida completa termina
                Estado = (!jugadorGano || MapaMundial.JugadorGanoTodo)
                    ? EstadoPartida.Terminada
                    : EstadoPartida.MapaMundial;

                // IA se conserva (con su vida final) para que la Vista pueda mostrarla;
                // se reemplaza en la siguiente IniciarBatalla.
                TerritorioEnDisputa = null;
            }

            // Fuera del lock, para que un suscriptor lento no bloquee a los hilos de combate
            OnBatallaTerminada?.Invoke(jugadorGano);
        }

        // Apaga los hilos de batalla y espera a que salgan.
        // Llamar desde OnDestroy / OnApplicationQuit y antes de iniciar otra batalla.
        public void DetenerBatalla()
        {
            Thread ia, jugador;

            lock (_lock)
            {
                _batallaActiva = false;
                _accionJugador.Set();
                _senalParar.Set();
                ia = _hiloIA;
                jugador = _hiloJugador;
                _hiloIA = null;
                _hiloJugador = null;
            }

            // Join FUERA del lock: los hilos necesitan el lock para poder salir.
            // Y nunca se espera a si mismo.
            Thread actual = Thread.CurrentThread;
            if (ia != null && ia != actual && ia.IsAlive) ia.Join(2500);
            if (jugador != null && jugador != actual && jugador.IsAlive) jugador.Join(2500);
        }

        public void RegistrarAccion(string quienJuega, string accion, string resultado)
        {
            int turno = NumeroTurno;
            string linea = "Turno " + turno + " | " + quienJuega +
                           " | Accion: " + accion +
                           " | Resultado: " + resultado;
            lock (_lockLog) { _logAcciones.Add(linea); }

            OnAccionRegistrada?.Invoke(turno, quienJuega, accion, resultado);
        }

        // Copia del log, segura de recorrer aunque otro hilo siga escribiendo
        public List<string> ObtenerLog()
        {
            lock (_lockLog) { return new List<string>(_logAcciones); }
        }

        public bool VerificarGanadorBatalla()
        {
            if (Jugador.Perdio)
            {
                TerminarBatalla(false, Jugador.Nombre + " se quedo sin vida");
                return true;
            }

            if (IA != null && IA.Perdio)
            {
                TerminarBatalla(true, IA.Nombre + " se quedo sin vida");
                return true;
            }

            if (IA != null && IA.EdificioPrincipal != null && !IA.EdificioPrincipal.EstaEnPie)
            {
                TerminarBatalla(true, "Centro urbano enemigo destruido");
                return true;
            }

            return false;
        }

        // Texto para configuracion.txt: territorios y dificultad con que arranca el juego
        public string ObtenerConfiguracionInicial()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Territorios (dificultad de la IA):");
            lock (_lock)
            {
                foreach (var t in MapaMundial.Territorios)
                {
                    sb.AppendLine(" - " + t.Nombre + " | Estado: " + t.Estado() +
                        " | Vida IA: " + t.VidaIA + " | Fuerza IA: " + t.FuerzaIA +
                        " | Ataque cada " + t.MsEntreAtaquesIA + " ms" +
                        " | Vida centro urbano: " + t.VidaCentroUrbano);
                }
            }
            return sb.ToString().TrimEnd();
        }

        // Texto para resultado_final.txt: motivo, estado final del mapa y vidas finales
        public string ObtenerResumenFinal()
        {
            var sb = new StringBuilder();
            lock (_lock)
            {
                sb.AppendLine("Territorio disputado: " + TerritorioUltimaBatalla);
                sb.AppendLine("Motivo del fin: " + MotivoFinUltimaBatalla);
                sb.AppendLine("Vida final de " + Jugador.Nombre + ": " + Jugador.Vida + "/" + Jugador.VidaMax);
                if (IA != null)
                {
                    sb.AppendLine("Vida final de " + IA.Nombre + ": " + IA.Vida + "/" + IA.VidaMax);
                    if (IA.EdificioPrincipal != null)
                        sb.AppendLine(IA.EdificioPrincipal.Nombre + ": " +
                            IA.EdificioPrincipal.Vida + "/" + IA.EdificioPrincipal.VidaMax);
                }
                sb.AppendLine("Grecia conquisto todos los territorios: " +
                    (MapaMundial.JugadorGanoTodo ? "Si" : "No"));
                sb.AppendLine("Estado final del mapa mundial:");
                foreach (var t in MapaMundial.Territorios)
                    sb.AppendLine(" - " + t.Nombre + ": " + t.Estado());
            }
            return sb.ToString().TrimEnd();
        }
    }
}
