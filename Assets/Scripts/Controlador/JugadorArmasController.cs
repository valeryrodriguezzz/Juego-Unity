using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;
using UnityEngine;

/// <summary>
/// CONTROLADOR: el dueño del arsenal del jugador.
/// Va en el GameObject "Jugador" (el mismo que tiene PlayerController).
///
/// EL JUGADOR SIEMPRE ES EL PAWN (TipoPersonaje.Trabajador), porque es el
/// unico personaje de Tiny Swords con animaciones de talar, picar y construir.
/// Por eso ya no hay que deducir nada de un carrusel: se fija aqui y se deja
/// escrito en JugadorModel.Rol, que es lo que lee el resto del juego.
///
/// El enum TipoPersonaje y la tabla de CatalogoArmas se conservan completos a
/// proposito: los otros cuatro (guerrero, arquero, lancero, monje) tienen idle,
/// caminar y atacar, que es justo lo que necesita una UNIDAD entrenable. Cuando
/// se haga el cuartel, cada unidad nacera con su arma fija usando esa misma tabla.
///
/// Cualquier otro controlador que necesite saber con que arma anda el jugador
/// pide este componente:
///     var armas = other.GetComponent&lt;JugadorArmasController&gt;();
///     ArmaModel equipada = armas.ArmaEquipada;
/// </summary>
public class JugadorArmasController : MonoBehaviour
{
    /// <summary>El personaje del jugador. Siempre el Pawn.</summary>
    public const TipoPersonaje PERSONAJE_JUGADOR = TipoPersonaje.Trabajador;

    /// <summary>El arsenal del jugador. Lo leen los demas controladores.</summary>
    public InventarioArmasModel Inventario { get; private set; }

    /// <summary>Se conserva por comodidad para los demas scripts.</summary>
    public TipoPersonaje Personaje => PERSONAJE_JUGADOR;

    private void Awake()
    {
        // Awake y no Start: los nodos de recurso pueden consultarlo en su Start.

        JugadorModel jugador = BuscarJugadorDelModelo();

        if (jugador == null)
        {
            // Sin partida (por ejemplo probando una escena suelta): se trabaja
            // con un inventario local que no sobrevive al cambio de escena.
            Inventario = new InventarioArmasModel(PERSONAJE_JUGADOR);
            Debug.LogWarning("[Armas] Sin partida creada. Probando una escena directamente? " +
                             "El inventario funciona, pero no se guarda al cambiar de mapa.");
            Informar("temporal");
            return;
        }

        jugador.Rol = PersonajeInfo.ARol(PERSONAJE_JUGADOR);

        // AQUI ESTA LA CLAVE DE QUE LAS ARMAS NO SE PIERDAN.
        //
        // El GameObject Jugador se crea de nuevo en cada escena (Juego, Roma,
        // Persia...), asi que si el inventario naciera aqui, al viajar a un
        // territorio empezarias otra vez con las manos vacias aunque acabaras
        // de comprar el cuchillo.
        //
        // Lo que SI sobrevive es el JugadorModel, que vive dentro de la
        // Partida y esa es DontDestroyOnLoad. Por eso el arsenal se guarda
        // alli: este componente ya no es el dueño del inventario, solo el que
        // lo conecta con la escena de turno.
        if (jugador.Armamento != null)
        {
            Inventario = jugador.Armamento;

            // Los hilos de mantenimiento pudieron quedar apagados al destruir
            // la escena anterior. Se vuelve a prender el de la herramienta que
            // lleva puesta; IniciarMantenimiento no hace nada si ya corria.
            Inventario.Equipada?.IniciarMantenimiento();

            Informar("recuperado de la partida");
            return;
        }

        // Primera escena de la partida: el Pawn arranca SIN herramientas. El
        // inventario nace vacio y se va llenando con lo que compre en la
        // tienda; la primera compra se equipa sola.
        Inventario = new InventarioArmasModel(PERSONAJE_JUGADOR);

        // Se le presta el arsenal al Modelo para que el combate pueda usar la
        // herramienta equipada, y de paso para que viaje entre escenas.
        jugador.Armamento = Inventario;

        Informar("nuevo");
    }

    private static JugadorModel BuscarJugadorDelModelo()
    {
        if (PlayerSelectionManager.Instance == null) return null;
        if (PlayerSelectionManager.Instance.Partida == null) return null;

        return PlayerSelectionManager.Instance.Partida.Jugador;
    }

    private void Informar(string origen)
    {
        // Ojo: Equipada es null hasta la primera compra, asi que no se le
        // puede pedir el Nombre de una.
        string equipada = Inventario.Equipada != null
            ? Inventario.Equipada.Nombre
            : "ninguna (manos vacias)";

        Debug.Log("[Armas] Rol: " + PersonajeInfo.Nombre(PERSONAJE_JUGADOR)
                  + " | inventario: " + origen
                  + " (" + Inventario.Listar().Count + " herramienta(s))"
                  + " | equipada: " + equipada);
    }

    /// <summary>Atajo comodo para los demas scripts.</summary>
    public ArmaModel ArmaEquipada => Inventario?.Equipada;

    // ------------------------------------------------------------------
    //  Apagar los hilos de las armas. Sin esto Unity se congela al salir
    //  del Play Mode.
    // ------------------------------------------------------------------

    /// <summary>
    /// OJO: aqui NO se apagan los hilos. Este objeto se destruye cada vez que
    /// se cambia de escena, y el inventario ya no le pertenece: es de la
    /// Partida y tiene que seguir vivo en el siguiente mapa. Si se apagara
    /// aqui, al llegar a Roma la herramienta equipada no se repararia nunca.
    ///
    /// Los hilos son IsBackground, asi que no impiden cerrar el juego, y
    /// OnApplicationQuit los detiene de forma ordenada.
    /// </summary>
    private void OnDestroy()
    {
        if (PlayerSelectionManager.Instance == null ||
            PlayerSelectionManager.Instance.Partida == null)
        {
            // Inventario local de una escena suelta: ese si es nuestro y hay
            // que apagarlo, o Unity se queda pegado al salir del Play Mode.
            Inventario?.DetenerTodo();
        }
    }

    private void OnApplicationQuit()
    {
        Inventario?.DetenerTodo();
    }
}
