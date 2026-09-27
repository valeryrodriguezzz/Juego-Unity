using TMPro;
using UnityEngine;

/// <summary>
/// CONTROLADOR del edificio que hace de tienda (la Armeria). Cuando el jugador
/// se le acerca aparece una "E" flotando encima; al presionarla se abre el
/// mercado, y al presionarla otra vez se cierra.
///
/// Es el mismo patron que RecursoNodoController usa con los arboles y las
/// menas: un Collider2D en modo trigger que detecta al jugador y una tecla
/// para interactuar. Se hace en un script aparte y no dentro de aquel porque
/// una tienda no es un nodo de recurso: no tiene cantidad, no se agota y no
/// se regenera, asi que meterla ahi seria heredar un hilo de regeneracion
/// que no hace nada.
///
/// COMO USARLO EN UNITY:
/// 1. Pon el sprite de la Armeria en la escena Juego (Tiny Swords tiene varios
///    edificios en Buildings). Si ya lo tienes puesto, usalo.
/// 2. Add Component -> Box Collider 2D -> marca la casilla "Is Trigger" y
///    agrandalo un poco mas que el edificio, que es la zona desde la que se
///    puede comprar. (Si se te olvida marcar Is Trigger, este script te lo
///    marca solo y te avisa en la consola.)
/// 3. Add Component -> EdificioTiendaController.
/// 4. Listo. El campo Tienda se llena solo si tu objeto de la tienda se llama
///    "TiendaManager"; si no, arrastralo a mano.
///
/// OJO: si el edificio ya tenia OTRO Collider2D solido para que el jugador no
/// lo atraviese, dejalo y agrega este segundo collider como trigger. Un objeto
/// puede tener varios colliders.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EdificioTiendaController : MonoBehaviour
{
    [Header("Tienda")]
    [Tooltip("El objeto TiendaManager. Si lo dejas vacio se busca solo.")]
    [SerializeField] private TiendaArmasController tienda;

    [Header("Interaccion")]
    [SerializeField] private KeyCode tecla = KeyCode.E;

    [Header("Cartelito de la E")]
    [Tooltip("Si lo dejas vacio se crea uno por codigo con la letra de la tecla.")]
    [SerializeField] private GameObject indicador;

    [Tooltip("A que altura sobre el edificio flota el cartelito.")]
    [SerializeField] private float alturaIndicador = 1.6f;

    [SerializeField] private float tamanoIndicador = 4f;
    [SerializeField] private Color colorIndicador = new Color(1f, 0.9f, 0.4f);

    [Tooltip("Fuente del cartelito. Opcional: sin ella se usa la de por defecto.")]
    [SerializeField] private TMP_FontAsset fuente;

    private bool _jugadorCerca;

    // --------------------------------------------------------------------

    private void Awake()
    {
        AsegurarTrigger();
    }

    private void Start()
    {
        if (tienda == null)
            tienda = BuscarTienda();

        if (tienda == null)
            Debug.LogWarning("[Armeria] No encontre la tienda. Crea un objeto vacio " +
                             "llamado TiendaManager con el componente TiendaView, o " +
                             "arrastra el tuyo al campo Tienda de este edificio.");

        if (indicador == null)
            indicador = CrearIndicador();

        MostrarIndicador(false);
    }

    private void Update()
    {
        if (!_jugadorCerca || tienda == null) return;
        if (!Input.GetKeyDown(tecla)) return;

        if (tienda.EstaAbierta) tienda.CerrarTienda();
        else tienda.AbrirTienda();
    }

    // --------------------------------------------------------------------
    //  Zona de la tienda
    // --------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _jugadorCerca = true;
        MostrarIndicador(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _jugadorCerca = false;
        MostrarIndicador(false);

        // Si se aleja con la tienda abierta, se le cierra: no tiene sentido
        // seguir comprando desde el otro lado del mapa.
        if (tienda != null && tienda.EstaAbierta)
            tienda.CerrarTienda();
    }

    private void MostrarIndicador(bool visible)
    {
        if (indicador != null) indicador.SetActive(visible);
    }

    // --------------------------------------------------------------------
    //  Piezas
    // --------------------------------------------------------------------

    /// <summary>
    /// Si ninguno de los colliders del edificio es trigger, no se detectaria
    /// nunca al jugador y el error seria dificil de encontrar. Asi que se
    /// arregla y se avisa.
    /// </summary>
    private void AsegurarTrigger()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();

        foreach (Collider2D c in colliders)
            if (c.isTrigger) return;

        if (colliders.Length > 0)
        {
            colliders[colliders.Length - 1].isTrigger = true;
            Debug.LogWarning("[Armeria] Ningun Collider2D estaba en Is Trigger. " +
                             "Le marque la casilla al ultimo para que la tienda " +
                             "detecte al jugador. Revisalo en el Inspector.");
        }
    }

    private TiendaArmasController BuscarTienda()
    {
        // Primero por nombre, que es lo mas barato.
        GameObject go = GameObject.Find("TiendaManager");
        if (go != null)
        {
            var t = go.GetComponent<TiendaArmasController>();
            if (t != null) return t;
        }

        // Y si no, se busca en toda la escena. FindFirstObjectByType y no
        // FindObjectOfType, que quedo obsoleto en las versiones nuevas.
        return FindFirstObjectByType<TiendaArmasController>();
    }

    /// <summary>
    /// Crea la "E" flotante con TextMeshPro 3D (no Canvas). Es el mismo
    /// enfoque del nombre sobre el jugador: un Canvas en World Space obliga a
    /// pelear con la escala y termina invisible o gigante.
    /// </summary>
    private GameObject CrearIndicador()
    {
        var go = new GameObject("IndicadorTienda");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, alturaIndicador, 0f);

        var texto = go.AddComponent<TextMeshPro>();
        texto.text = tecla.ToString();
        texto.fontSize = tamanoIndicador;
        texto.color = colorIndicador;
        texto.alignment = TextAlignmentOptions.Center;

        if (fuente != null) texto.font = fuente;

        // Que se dibuje por encima del mapa y de los edificios.
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 100;
        }

        return go;
    }

    // Dibuja la zona en la vista Scene para poder ajustarla a ojo.
    private void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
        Bounds b = c.bounds;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
