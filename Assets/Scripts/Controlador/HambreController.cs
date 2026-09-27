using ImperiosEnGuerra.Modelo;
using UnityEngine;

/// <summary>
/// CONTROLADOR del hambre: crea el HambreModel, le arranca su hilo y le pasa
/// la tecla de comer. La UI (HUDView) le pregunta a este por el nivel.
///
/// COMO USARLO EN UNITY:
/// 1. Selecciona el GameObject Jugador.
/// 2. Add Component -> HambreController.
/// 3. Los valores del Inspector se pueden ajustar mientras juegas para
///    encontrar el ritmo que se sienta bien.
///
/// Con la tecla F (configurable) el jugador se come una unidad de Comida
/// de las que ha recolectado y recupera hambre.
/// </summary>
public class HambreController : MonoBehaviour
{
    [Header("Ritmo del hambre")]
    [Tooltip("Cada cuantos segundos baja el hambre un punto.")]
    [SerializeField] private float segundosPorTick = 3f;

    [Tooltip("Cuanto baja el hambre en cada tick.")]
    [SerializeField] private int puntosPorTick = 1;

    [Tooltip("Nivel maximo. Con 100 y un punto cada 3 segundos, " +
             "el jugador aguanta 5 minutos sin comer.")]
    [SerializeField] private int nivelMaximo = 100;

    [Header("Castigo por no comer")]
    [Tooltip("Vida que se pierde en cada tick mientras el hambre este en cero.")]
    [SerializeField] private int danioPorTickSinComer = 2;

    [Header("Comer")]
    [SerializeField] private KeyCode teclaComer = KeyCode.F;

    [Tooltip("Cuanto hambre recupera cada unidad de Comida.")]
    [SerializeField] private int puntosPorComida = 10;

    /// <summary>El Modelo. Lo lee el HUD para pintar la barra.</summary>
    public HambreModel Hambre { get; private set; }

    private JugadorModel _jugador;

    private void Start()
    {
        if (PlayerSelectionManager.Instance == null || PlayerSelectionManager.Instance.Partida == null)
        {
            Debug.LogWarning("[Hambre] No hay partida activa. El hambre no se activa.");
            enabled = false;
            return;
        }

        _jugador = PlayerSelectionManager.Instance.Partida.Jugador;

        Hambre = new HambreModel(_jugador, nivelMaximo)
        {
            MsPorTick = Mathf.Max(100, (int)(segundosPorTick * 1000f)),
            PuntosPorTick = puntosPorTick,
            DanioPorTickSinComer = danioPorTickSinComer,
            PuntosPorComida = puntosPorComida
        };

        // Estos eventos llegan desde el hilo del hambre, asi que el trabajo
        // que toque Unity se encola en el MainThreadDispatcher.
        Hambre.DanioPorHambre += AlPerderVidaPorHambre;
        Hambre.MurioDeHambre += AlMorirDeHambre;

        Hambre.Iniciar();

        Debug.Log("[Hambre] Activada: baja " + puntosPorTick + " cada " +
                  segundosPorTick + "s. Presiona " + teclaComer + " para comer.");
    }

    private void Update()
    {
        if (Hambre == null) return;

        if (Input.GetKeyDown(teclaComer))
            Comer();
    }

    /// <summary>Tambien se puede llamar desde un boton de la UI.</summary>
    public void Comer()
    {
        if (Hambre == null) return;

        int recuperado = Hambre.Comer(1);

        // El mensaje va a la consola Y a la pantalla: el que juega no ve la
        // consola, y presionar una tecla sin que pase nada visible se siente
        // como que el juego esta roto.
        string aviso;

        if (recuperado > 0)
            aviso = "Comiste. +" + recuperado + " de hambre (te queda " + _jugador.Comida + " de comida)";
        else if (_jugador.Comida <= 0)
            aviso = "No tienes comida. Ve a cazar ovejas con el cuchillo.";
        else
            aviso = "Ya estas lleno.";

        Debug.Log("[Hambre] " + aviso);
        AvisoPantalla.Mostrar(aviso);

        if (recuperado > 0)
            ImperiosEnGuerra.Controlador.Bitacora.Anotar(
                "Comer",
                "Se comio 1 de comida: +" + recuperado + " de hambre (nivel " +
                Hambre.Nivel + "/" + nivelMaximo + ", le queda " +
                _jugador.Comida + " de comida)");
    }

    // Llega desde el hilo del hambre: nada de tocar Unity aqui directamente.
    private void AlPerderVidaPorHambre(int danio)
    {
        MainThreadDispatcher.Encolar(() =>
            Debug.Log("[Hambre] Estas pasando hambre: -" + danio + " de vida."));
    }

    private void AlMorirDeHambre()
    {
        MainThreadDispatcher.Encolar(() =>
            Debug.Log("[Hambre] Te moriste de hambre."));
    }

    // ------------------------------------------------------------------
    //  Apagar el hilo del hambre
    // ------------------------------------------------------------------

    private void OnDestroy()
    {
        Apagar();
    }

    private void OnApplicationQuit()
    {
        Apagar();
    }

    private void Apagar()
    {
        if (Hambre == null) return;

        Hambre.DanioPorHambre -= AlPerderVidaPorHambre;
        Hambre.MurioDeHambre -= AlMorirDeHambre;
        Hambre.Detener();
    }
}
