using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;
using UnityEngine;

/// <summary>
/// CONTROLADOR: el dueño del arsenal del jugador.
/// Va en el GameObject "Jugador" (el mismo que tiene PlayerController).
///
/// DE DONDE SALE EL PERSONAJE:
/// la fuente de verdad es JugadorModel.Rol, que vive en el Modelo. Este script
/// lo lee; y si todavia viene vacio (porque la partida se creo antes de que el
/// Rol se guardara), lo deduce del AvatarIndex del carrusel y lo escribe una
/// sola vez. Asi, de aqui en adelante, todo el juego pregunta por el Rol y no
/// por el indice de un array de sprites.
///
/// Cualquier otro controlador que necesite saber con que arma anda el jugador
/// pide este componente:
///     var armas = other.GetComponent&lt;JugadorArmasController&gt;();
///     ArmaModel equipada = armas.ArmaEquipada;
/// </summary>
public class JugadorArmasController : MonoBehaviour
{
    [Header("Solo para probar la escena Juego sin pasar por la seleccion")]
    [Tooltip("Si no hay partida creada, se usa este personaje.")]
    [SerializeField] private TipoPersonaje personajeDePrueba = TipoPersonaje.Trabajador;

    /// <summary>El arsenal del jugador. Lo leen los demas controladores.</summary>
    public InventarioArmasModel Inventario { get; private set; }

    /// <summary>Que personaje resulto ser, ya resuelto.</summary>
    public TipoPersonaje Personaje { get; private set; }

    private void Awake()
    {
        // Awake y no Start: los nodos de recurso pueden consultarlo en su Start.
        Personaje = ResolverPersonaje();

        // El constructor ya le pone su arma por defecto y le arranca el hilo.
        Inventario = new InventarioArmasModel(Personaje);

        Debug.Log("[Armas] Rol: " + PersonajeInfo.Nombre(Personaje)
                  + " | arma: " + Inventario.Equipada.Nombre
                  + " | puede comprar: " + Inventario.PuedeComprar);
    }

    /// <summary>
    /// Orden de preferencia:
    ///   1. JugadorModel.Rol, si ya viene puesto.
    ///   2. El AvatarIndex del carrusel (y de paso se escribe el Rol).
    ///   3. El personaje de prueba del Inspector.
    /// </summary>
    private TipoPersonaje ResolverPersonaje()
    {
        if (PlayerSelectionManager.Instance == null || PlayerSelectionManager.Instance.Partida == null)
        {
            Debug.LogWarning("[Armas] Sin partida creada. Usando " + personajeDePrueba + " de prueba.");
            return personajeDePrueba;
        }

        JugadorModel jugador = PlayerSelectionManager.Instance.Partida.Jugador;

        // 1) El Rol ya esta guardado en el Modelo: esa es la fuente de verdad.
        if (PersonajeInfo.TryDesdeRol(jugador.Rol, out TipoPersonaje desdeRol))
            return desdeRol;

        // 2) Rol vacio o no reconocido: se deduce del carrusel y se guarda,
        //    para que a partir de ahora el Modelo lo tenga.
        TipoPersonaje desdeIndice = PersonajeInfo.DesdeAvatarIndex(PlayerSelectionManager.Instance.AvatarIndex);
        jugador.Rol = PersonajeInfo.ARol(desdeIndice);

        Debug.Log("[Armas] Rol estaba vacio. Deducido del carrusel y guardado como: " + jugador.Rol);
        return desdeIndice;
    }

    /// <summary>Atajo comodo para los demas scripts.</summary>
    public ArmaModel ArmaEquipada => Inventario?.Equipada;

    // ------------------------------------------------------------------
    //  Apagar los hilos de las armas. Sin esto Unity se congela al salir
    //  del Play Mode.
    // ------------------------------------------------------------------

    private void OnDestroy()
    {
        Inventario?.DetenerTodo();
    }

    private void OnApplicationQuit()
    {
        Inventario?.DetenerTodo();
    }
}