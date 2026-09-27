using UnityEngine;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;
using ImperiosEnGuerra.Controlador;

// Puente que sobrevive entre escenas (Menu -> Seleccion -> Juego).
// Guarda la PartidaModel completa, que es lo unico que tiene que viajar:
// el nombre que escribio el jugador y su Rol (siempre Pawn/Trabajador).
//
// COMO USARLO:
// 1) En tu escena de Menu, crea un GameObject vacio llamado "GameManager".
// 2) Arrastra este script sobre ese GameObject.

public class PlayerSelectionManager : MonoBehaviour
{
    public static PlayerSelectionManager Instance { get; private set; }

    // La partida completa: Jugador (siempre Grecia) + MapaMundial
    public PartidaModel Partida { get; private set; }


    [Header("Desarrollo")]
    [Tooltip("Si le das Play directamente a la escena Juego (sin pasar por el Menu), " +
             "crea una partida de prueba para que el HUD, el hambre y las armas funcionen igual. " +
             "Solo actua dentro del editor de Unity: en el juego compilado nunca se activa.")]
    [SerializeField] private bool crearPartidaDePruebaSiNoHay = true;

    [SerializeField] private string nombreDePrueba = "Jugador de prueba";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Ya hay uno vivo desde el Menu (con la partida de verdad adentro):
            // este es un duplicado de esta escena y sobra.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        // Llegamos aqui sin partida = le dieron Play a una escena suelta.
        // Se arma una de prueba para poder trabajar sin repetir todo el flujo.
        // Awake corre antes que cualquier Start, asi que cuando el HUD, el
        // hambre o las armas pregunten por la Partida, ya va a estar lista.
        if (Partida == null && crearPartidaDePruebaSiNoHay)
        {
            CrearPartida(nombreDePrueba);
            Debug.LogWarning("[GameManager] No venias del Menu, asi que cree una partida de " +
                             "prueba con el nombre \"" + nombreDePrueba + "\". " +
                             "Para jugar de verdad, dale Play desde la escena Menu.");
        }
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this) ApagarPartida();
    }

    private void OnApplicationQuit()
    {
        ApagarPartida();
    }

    /// <summary>
    /// Apaga los hilos de la partida Y los de las armas. El arsenal vive en
    /// JugadorModel.Armamento para poder viajar entre escenas, asi que ya no
    /// lo apaga el JugadorArmasController al destruirse: hay que hacerlo aqui,
    /// que es donde termina de verdad la partida.
    /// </summary>
    private void ApagarPartida()
    {
        if (Partida == null) return;

        Partida.Jugador?.Armamento?.DetenerTodo();
        Partida.DetenerTodo();
    }

    // Se llama desde la escena de seleccion de personaje al confirmar
    public void CrearPartida(string nombreJugador)
    {
        // Si se empieza otra partida, la anterior se apaga entera (incluidas
        // las armas) para no dejar hilos sueltos dando vueltas.
        ApagarPartida();

        // Y se bota lo que hubiera quedado a medias de una carga anterior.
        PartidaGuardadaController.Olvidar();

        Partida = new PartidaModel(nombreJugador);

        // El jugador siempre es el Pawn (Trabajador): es el unico personaje de
        // Tiny Swords con animaciones de talar, picar y construir. Por eso ya
        // no se elige personaje, solo el nombre.
        Partida.Jugador.Rol = PersonajeInfo.ARol(TipoPersonaje.Trabajador);

        // Archivos de texto: configuracion al iniciar, log en cada accion, resultado al terminar.
        // ArchivoController es thread-safe, asi que los hilos de combate pueden llamarlo directo.
        ArchivoController.LimpiarLog();
        ArchivoController.GuardarConfiguracion(
            Partida.Jugador.Nombre,
            Partida.Jugador.Civilizacion.Imperio,
            Partida.ObtenerConfiguracionInicial());
        Partida.OnAccionRegistrada += ArchivoController.RegistrarAccion;
        Partida.OnBatallaTerminada += AlTerminarBatalla;

        Debug.Log("Partida creada. Jugador: " + Partida.Jugador.Nombre +
                   " (Civilizacion: " + Partida.Jugador.Civilizacion.Imperio +
                   ", Rol: " + Partida.Jugador.Rol + ")");
    }

    // Se ejecuta en el hilo de combate que termino la batalla (no en el principal).
    private void AlTerminarBatalla(bool jugadorGano)
    {
        PartidaModel p = Partida;
        if (p == null) return;

        ArchivoController.GuardarResultado(
            p.NombreGanador,
            p.DuracionUltimaBatalla.ToString(@"mm\:ss"),
            jugadorGano,
            p.ObtenerResumenFinal());
    }

}