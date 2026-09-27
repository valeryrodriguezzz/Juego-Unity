using System;
using ImperiosEnGuerra.Modelo;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// CONTROLADOR de emergencia para las escenas de territorio.
///
/// QUE PROBLEMA RESUELVE
/// Una batalla de territorio necesita dos piezas en la escena, y las dos
/// vienen dentro del prefab BatallaTerritorio:
///
///   - CombateTerritorioController: apaga el ataque automatico del hilo de la
///     IA (aqui quien pega es el EnemigoController de cada muñeco) y decide
///     cuando se gano o se perdio.
///   - GanadorView: muestra el cartel de victoria o derrota.
///
/// Si a una escena se le olvida ese prefab pasan dos cosas muy raras y nada
/// obvias: la vida baja sola sin que nadie te ataque (es el hilo de la IA
/// pegando desde el Modelo, porque nadie lo apago), y no sale ningun cartel
/// ni al ganar ni al perder (porque nadie llama a TerminarBatalla y nadie
/// escucha el resultado). Eso fue justo lo que paso en Egipto, que se habia
/// quedado con el BatallaView viejo.
///
/// QUE HACE
/// Al cargar cualquier escena que sea un territorio del mapa mundial,
/// comprueba que las dos piezas esten. La que falte, la pone:
///   - el CombateTerritorioController, tal cual;
///   - el cartel de resultado, construido por codigo (feo pero funcional).
///
/// Si la escena ya tiene el prefab bien puesto, este script no hace NADA:
/// no duplica paneles ni controladores.
///
/// NO HAY QUE MONTAR NADA EN UNITY: se crea solo al arrancar el juego.
/// Aun asi, lo suyo es arrastrar el prefab BatallaTerritorio a la escena que
/// lo tenga suelto; esto es una red de seguridad, no el sitio donde deberia
/// vivir la interfaz.
/// </summary>
public class RespaldoBatallaController : MonoBehaviour
{
    private static RespaldoBatallaController _instancia;

    private PartidaModel _partida;
    private bool _suscrito;

    // El evento llega desde el hilo de combate, que no puede tocar la UI de
    // Unity. Asi que el hilo solo levanta esta bandera y el frame la atiende.
    private volatile bool _hayResultadoPendiente;
    private volatile bool _gano;
    private string _nombreGanador;
    private string _duracion;
    private bool _volverAlMapa;

    private GameObject _panel;
    private TMP_Text _titulo, _subtitulo, _detalle, _textoBoton;

    private const string ESCENA_MAPA = "Juego";
    private const string ESCENA_MENU = "Menu";

    // ────────────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearSiNoExiste()
    {
        if (_instancia != null) return;

        var go = new GameObject("RespaldoBatalla");
        go.AddComponent<RespaldoBatallaController>();
    }

    private void Awake()
    {
        if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

        _instancia = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (_instancia != this) return;

        SceneManager.sceneLoaded -= AlCargarEscena;
        Desuscribir();
    }

    private void Start()
    {
        StartCoroutine(RevisarLaEscena());
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // El panel cuelga de este mismo objeto (que sobrevive a los cambios de
        // escena), asi que no se destruye ni se vuelve a construir: solo se
        // esconde. Si se pusiera a null aqui, en la siguiente batalla se
        // crearia un segundo panel encima del primero.
        if (_panel != null) _panel.SetActive(false);

        _hayResultadoPendiente = false;

        Desuscribir();

        StartCoroutine(RevisarLaEscena());
    }

    /// <summary>
    /// Se espera un frame a proposito: los Start de la escena (incluido el del
    /// CombateTerritorioController que venga en el prefab) todavia no han
    /// corrido, y si se mirara ahora pareceria que faltan.
    /// </summary>
    private System.Collections.IEnumerator RevisarLaEscena()
    {
        yield return null;

        _partida = PlayerSelectionManager.Instance != null
            ? PlayerSelectionManager.Instance.Partida
            : null;

        if (_partida == null) yield break;
        if (!EsEscenaDeTerritorio()) yield break;

        // --- Pieza 1: quien apaga al hilo de la IA y decide el resultado ---
        if (FindFirstObjectByType<CombateTerritorioController>() == null)
        {
            var go = new GameObject("CombateTerritorio (automatico)");
            go.AddComponent<CombateTerritorioController>();

            Debug.LogWarning("[Respaldo] A esta escena le falta el prefab " +
                             "BatallaTerritorio, asi que le puse un " +
                             "CombateTerritorioController por codigo. Sin el, la vida " +
                             "te bajaba sola (el hilo de la IA pegando) y no salia " +
                             "ningun cartel de resultado. Arrastra el prefab a la " +
                             "escena cuando puedas.");
        }

        // --- Pieza 2: el cartel de resultado ---
        if (FindFirstObjectByType<GanadorView>() != null) yield break;

        _partida.OnBatallaTerminada += AlTerminarBatalla;
        _suscrito = true;

        Debug.LogWarning("[Respaldo] Esta escena no tiene GanadorView, asi que el " +
                         "cartel de victoria o derrota lo voy a dibujar yo.");
    }

    private bool EsEscenaDeTerritorio()
    {
        string escena = SceneManager.GetActiveScene().name;

        if (_partida.MapaMundial == null) return false;

        foreach (TerritorioModel t in _partida.MapaMundial.Territorios)
        {
            if (t.EsBase) continue;

            if (string.Equals(t.Nombre, escena, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.Imperio, escena, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void Desuscribir()
    {
        if (_partida != null && _suscrito)
            _partida.OnBatallaTerminada -= AlTerminarBatalla;

        _suscrito = false;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Resultado
    // ────────────────────────────────────────────────────────────────────

    private void AlTerminarBatalla(bool jugadorGano)
    {
        // Esto corre en el hilo de combate: aqui no se puede crear ni un solo
        // objeto de Unity. Solo se apuntan los datos y se levanta la bandera.
        _gano = jugadorGano;
        _nombreGanador = _partida.NombreGanador;
        _duracion = _partida.DuracionUltimaBatalla.ToString(@"mm\:ss");
        _volverAlMapa = jugadorGano && _partida.Estado != EstadoPartida.Terminada;

        _hayResultadoPendiente = true;
    }

    private void Update()
    {
        if (!_hayResultadoPendiente) return;

        _hayResultadoPendiente = false;
        Mostrar();
    }

    private void Mostrar()
    {
        if (_panel == null) Construir();
        if (_panel == null) return;

        _panel.SetActive(true);

        _titulo.text = _gano ? "VICTORIA" : "Derrota";
        _titulo.color = _gano ? new Color(1f, 0.87f, 0.35f) : new Color(1f, 0.55f, 0.5f);

        _subtitulo.text = _gano
            ? "Grecia ha conquistado el territorio."
            : "El imperio " + _nombreGanador + " te ha vencido.";

        _detalle.text = "Duracion de la batalla: " + _duracion;
        _textoBoton.text = _volverAlMapa ? "Volver al mapa" : "Volver al menu";

        // Se congela el juego para que dé tiempo a leerlo.
        Time.timeScale = 0f;
    }

    private void Continuar()
    {
        // Sin esto la escena siguiente arranca congelada.
        Time.timeScale = 1f;

        SceneManager.LoadScene(_volverAlMapa ? ESCENA_MAPA : ESCENA_MENU);
    }

    // ────────────────────────────────────────────────────────────────────
    //  El cartel, por codigo
    // ────────────────────────────────────────────────────────────────────

    private void Construir()
    {
        Canvas canvas = BuscarOCrearCanvas();

        _panel = new GameObject("PanelResultado (automatico)", typeof(RectTransform));
        _panel.transform.SetParent(canvas.transform, false);

        var rect = _panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(760f, 420f);

        var fondo = _panel.AddComponent<Image>();
        fondo.color = new Color(0.07f, 0.05f, 0.04f, 0.97f);

        _titulo = Texto("Titulo", new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.92f), 52f);
        _subtitulo = Texto("Subtitulo", new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.66f), 26f);
        _detalle = Texto("Detalle", new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.44f), 20f);

        var botonGO = new GameObject("BotonContinuar", typeof(RectTransform));
        botonGO.transform.SetParent(_panel.transform, false);

        var br = botonGO.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.30f, 0.08f);
        br.anchorMax = new Vector2(0.70f, 0.25f);
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;

        var img = botonGO.AddComponent<Image>();
        img.color = new Color(0.22f, 0.18f, 0.13f, 1f);

        var boton = botonGO.AddComponent<Button>();
        boton.targetGraphic = img;
        boton.onClick.AddListener(Continuar);

        _textoBoton = Texto("Texto", Vector2.zero, Vector2.one, 24f, botonGO.transform);

        _panel.SetActive(false);
    }

    private TMP_Text Texto(string nombre, Vector2 min, Vector2 max, float tamano,
                           Transform padre = null)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre != null ? padre : _panel.transform, false);

        var r = go.GetComponent<RectTransform>();
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = new Vector2(8f, 4f);
        r.offsetMax = new Vector2(-8f, -4f);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = "";
        t.fontSize = tamano;
        t.color = new Color(1f, 0.95f, 0.87f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        return t;
    }

    /// <summary>
    /// Canvas propio y no el de la escena: este cartel tiene que verse por
    /// encima del HUD y de los botones de guardar, y pararse a averiguar el
    /// orden del Canvas de cada mapa seria pedir problemas.
    /// </summary>
    private Canvas BuscarOCrearCanvas()
    {
        var go = new GameObject("CanvasResultado");
        go.transform.SetParent(transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;

        var escalador = go.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);

        go.AddComponent<GraphicRaycaster>();

        return canvas;
    }
}
