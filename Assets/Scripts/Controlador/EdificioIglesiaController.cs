using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Edificios;
using TMPro;
using UnityEngine;

/// <summary>
/// CONTROLADOR de una iglesia del mapa: el jugador se le acerca, presiona E y
/// se cura a cambio de oro.
///
/// Es la hermana de EdificioTiendaController y funciona igual: un Collider2D
/// en modo trigger, un cartelito con la tecla, y una tecla para usarla.
///
/// CADA IGLESIA ES SUYA
/// Cada objeto con este script tiene su PROPIO IglesiaModel, con su propio
/// tiempo de espera. Asi el jugador no puede curarse dos veces seguidas en la
/// misma iglesia, pero si puede ir corriendo a otra y usar esa. Eso da juego:
/// conocer donde estan las iglesias de cada territorio pasa a ser util.
///
/// La que el jugador construye con el panel (tecla B) sigue funcionando por su
/// boton "Curarme"; esta es para las iglesias que ya estan dibujadas en el mapa.
///
/// COMO USARLO EN UNITY:
/// 1. Pon el sprite de la iglesia en la escena. En Tiny Swords sirve el
///    Monastery de Buildings/&lt;color&gt; Buildings.
/// 2. Add Component -> Box Collider 2D -> marca "Is Trigger" y agrandalo un
///    poco mas que el edificio: esa es la zona desde la que se puede rezar.
/// 3. Add Component -> EdificioIglesiaController.
/// 4. Listo. Si quieres que esta iglesia cure distinto, cambia los numeros del
///    Inspector.
///
/// Si el edificio ya tenia otro Collider2D solido para que no lo atraviesen,
/// dejalo y agrega este segundo como trigger: un objeto puede tener varios.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EdificioIglesiaController : MonoBehaviour
{
    [Header("Interaccion")]
    [SerializeField] private KeyCode tecla = KeyCode.E;

    [Header("Cuanto cura esta iglesia")]
    [SerializeField] private int curacionPorUso = 30;
    [SerializeField] private int costoOroPorUso = 20;

    [Tooltip("Segundos de espera antes de poder volver a usar ESTA iglesia.")]
    [SerializeField] private int esperaSegundos = 10;

    [Header("Cartelito de la tecla")]
    [Tooltip("Si lo dejas vacio se crea uno solo con la letra de la tecla.")]
    [SerializeField] private GameObject indicador;

    [SerializeField] private float alturaIndicador = 1.6f;
    [SerializeField] private float tamanoIndicador = 4f;
    [SerializeField] private Color colorIndicador = new Color(0.6f, 0.9f, 1f);
    [SerializeField] private TMP_FontAsset fuente;

    private IglesiaModel _iglesia;
    private JugadorModel _jugador;
    private bool _jugadorCerca;

    // --------------------------------------------------------------------

    private void Awake()
    {
        AsegurarTrigger();
    }

    /// <summary>
    /// La usa EdificioConstruidoController cuando el jugador construye una
    /// iglesia con el panel: esa iglesia ya tiene su IglesiaModel (el que
    /// pago y el que tiene el hilo de obra), asi que hay que usar ese y no
    /// crear otro. Se llama justo despues del AddComponent, antes de Start.
    /// </summary>
    public void UsarModelo(IglesiaModel modelo)
    {
        if (modelo == null) return;
        _iglesia = modelo;
    }

    private void Start()
    {
        // Si nadie le paso un modelo, esta iglesia es una de las que ya
        // estaban dibujadas en el mapa: se le crea el suyo y se marca como
        // terminada, porque no hay que construirla.
        if (_iglesia == null)
        {
            _iglesia = new IglesiaModel();
            _iglesia.Configurar(curacionPorUso, costoOroPorUso, esperaSegundos);
            _iglesia.Construccion();
        }

        if (PlayerSelectionManager.Instance != null && PlayerSelectionManager.Instance.Partida != null)
            _jugador = PlayerSelectionManager.Instance.Partida.Jugador;

        if (_jugador == null)
            Debug.LogWarning("[Iglesia] No hay partida activa. Entra desde el Menu.");

        if (indicador == null) indicador = CrearIndicador();

        MostrarIndicador(false);

        Debug.Log("[Iglesia] " + gameObject.name + " lista: cura " + curacionPorUso +
                  " por " + costoOroPorUso + " de oro, cada " + esperaSegundos + " s.");
    }

    private void Update()
    {
        if (!_jugadorCerca || _jugador == null) return;
        if (!Input.GetKeyDown(tecla)) return;

        Curar();
    }

    /// <summary>
    /// Todo el trabajo delicado lo hace el Modelo: comprobar la espera, que no
    /// estes ya al maximo de vida y cobrar el oro, las tres cosas dentro de su
    /// lock. Aqui solo se decide que mensaje mostrar cuando dice que no.
    /// </summary>
    private void Curar()
    {
        if (!_iglesia.Construido)
        {
            Avisar("Esta iglesia todavia se esta construyendo (" +
                   Mathf.FloorToInt(_iglesia.PorcentajeConstruccion) + "%).");
            return;
        }

        if (_iglesia.Curar(_jugador))
        {
            Avisar("Te curaste " + _iglesia.CuracionPorUso + " de vida (-" +
                   _iglesia.CostoOroPorUso + " de oro)");

            ImperiosEnGuerra.Controlador.Bitacora.Anotar(
                "Curacion en la Iglesia",
                "+" + _iglesia.CuracionPorUso + " de vida por " +
                _iglesia.CostoOroPorUso + " de oro (queda en " +
                _jugador.Vida + "/" + _jugador.VidaMax + ")");
            return;
        }

        double espera = _iglesia.SegundosParaPoderUsar();

        if (espera > 0)
            Avisar("Esta iglesia se esta recuperando: " + Mathf.CeilToInt((float)espera) + " s.");
        else if (_jugador.Vida >= _jugador.VidaMax)
            Avisar("Ya tienes la vida completa.");
        else
            Avisar("Te faltan " + _iglesia.CostoOroPorUso + " de oro para curarte.");
    }

    private static void Avisar(string mensaje)
    {
        Debug.Log("[Iglesia] " + mensaje);
        AvisoPantalla.Mostrar(mensaje);
    }

    // --------------------------------------------------------------------
    //  Zona de la iglesia
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
    }

    private void MostrarIndicador(bool visible)
    {
        if (indicador != null) indicador.SetActive(visible);
    }

    // --------------------------------------------------------------------
    //  Piezas
    // --------------------------------------------------------------------

    /// <summary>
    /// Sin un collider en modo trigger no se detecta nunca al jugador, y el
    /// error es dificil de ver. Se arregla y se avisa.
    /// </summary>
    private void AsegurarTrigger()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();

        foreach (Collider2D c in colliders)
            if (c.isTrigger) return;

        if (colliders.Length > 0)
        {
            colliders[colliders.Length - 1].isTrigger = true;
            Debug.LogWarning("[Iglesia] Ningun Collider2D estaba en Is Trigger. Le marque " +
                             "la casilla al ultimo para que detecte al jugador.");
        }
    }

    /// <summary>
    /// El cartelito con TextMeshPro 3D. Copia la Sorting Layer del propio
    /// edificio: si naciera en la capa Default quedaria escondido detras del
    /// mapa, que es lo que pasaba antes con los arboles.
    /// </summary>
    private GameObject CrearIndicador()
    {
        var go = new GameObject("IndicadorIglesia");
        go.transform.SetParent(transform, false);

        Vector3 escalaPadre = transform.lossyScale;
        float ex = Mathf.Approximately(escalaPadre.x, 0f) ? 1f : 1f / escalaPadre.x;
        float ey = Mathf.Approximately(escalaPadre.y, 0f) ? 1f : 1f / escalaPadre.y;

        go.transform.localScale = new Vector3(ex, ey, 1f);
        go.transform.localPosition = new Vector3(0f, alturaIndicador * ey, 0f);

        var texto = go.AddComponent<TextMeshPro>();
        texto.text = tecla.ToString();
        texto.fontSize = tamanoIndicador;
        texto.color = colorIndicador;
        texto.alignment = TextAlignmentOptions.Center;

        if (fuente != null) texto.font = fuente;

        var renderer = go.GetComponent<MeshRenderer>();
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        if (renderer != null)
        {
            if (sprite != null)
            {
                renderer.sortingLayerID = sprite.sortingLayerID;
                renderer.sortingOrder = sprite.sortingOrder + 10;
            }
            else
            {
                renderer.sortingOrder = 100;
            }
        }

        return go;
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}
