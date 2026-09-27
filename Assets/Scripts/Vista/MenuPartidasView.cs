using System.Collections.Generic;
using ImperiosEnGuerra.Controlador;
using ImperiosEnGuerra.Modelo;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// VISTA de la escena Menu: hace que funcionen los botones OPCIONES y SALIR,
/// y dibuja la lista de partidas guardadas.
///
/// OPCIONES abre un panel con las seis ranuras. Cada una que tenga algo
/// guardado muestra el nombre, el mapa, los recursos y cuantos territorios
/// llevaba conquistados, con un boton para continuarla y otro para borrarla.
/// SALIR cierra el juego.
///
/// COMO ENCUENTRA LOS BOTONES (esto es lo que evita tener que tocar Unity)
/// En la escena los tres botones se llaman "Boton Start", "Boton Start (1)" y
/// "Boton Start (2)", asi que por el nombre no hay forma de saber cual es
/// cual. Pero cada uno tiene dentro un texto que dice JUGAR, OPCIONES o
/// SALIR: se busca por ese texto y se sube al padre. Ademas dos de ellos ni
/// siquiera tenian componente Button (por eso no hacian nada al pulsarlos),
/// asi que si falta se les pone.
///
/// Sirve para Text normal y para TextMeshPro, porque el menu usa el Text
/// viejo y el resto del juego usa TMP.
///
/// NO HAY QUE MONTAR NADA EN UNITY: se crea sola al arrancar el juego.
/// </summary>
public class MenuPartidasView : MonoBehaviour
{
    private static MenuPartidasView _instancia;

    private const string ESCENA_MENU = "Menu";

    private GameObject _panel;
    private Transform _lista;
    private TMP_Text _mensaje;

    // ────────────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearSiNoExiste()
    {
        if (_instancia != null) return;

        var go = new GameObject("MenuPartidas");
        go.AddComponent<MenuPartidasView>();
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
        if (_instancia == this) SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == ESCENA_MENU) Preparar();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // El panel vivia en el Canvas de la escena anterior, que ya no existe.
        _panel = null;
        _lista = null;
        _mensaje = null;

        if (escena.name == ESCENA_MENU) Preparar();
    }

    private void Update()
    {
        if (_panel != null && _panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            _panel.SetActive(false);
    }

    // ────────────────────────────────────────────────────────────────────
    //  Enganchar los botones del menu
    // ────────────────────────────────────────────────────────────────────

    private void Preparar()
    {
        Construir();

        Enganchar("OPCIONES", Abrir);
        Enganchar("SALIR", SalirDelJuego);

        Debug.Log("[Menu] Partidas guardadas en: " + GuardadoController.CarpetaVisible());
    }

    /// <summary>
    /// Busca el boton cuyo texto sea el que se pide y le cuelga la accion.
    /// Si el objeto no tenia componente Button, se lo agrega: dos de los tres
    /// botones del menu solo eran una imagen con un texto encima.
    /// </summary>
    private void Enganchar(string etiqueta, UnityEngine.Events.UnityAction accion)
    {
        GameObject objeto = BuscarPorTexto(etiqueta);

        if (objeto == null)
        {
            Debug.LogWarning("[Menu] No encontre ningun boton que diga \"" + etiqueta +
                             "\". Revisa que el texto del boton diga exactamente eso.");
            return;
        }

        var boton = objeto.GetComponent<Button>();

        if (boton == null)
        {
            boton = objeto.AddComponent<Button>();

            var img = objeto.GetComponent<Image>();
            if (img != null) boton.targetGraphic = img;

            Debug.Log("[Menu] El boton \"" + etiqueta + "\" no tenia componente Button " +
                      "(por eso no hacia nada). Se lo puse.");
        }

        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(accion);
    }

    /// <summary>
    /// Devuelve el objeto que CONTIENE el texto buscado. El texto suele estar
    /// en un hijo del boton, asi que se sube al padre si ese hijo no tiene
    /// imagen propia.
    /// </summary>
    private static GameObject BuscarPorTexto(string etiqueta)
    {
        string buscado = etiqueta.Trim().ToUpperInvariant();

        foreach (Text t in FindObjectsByType<Text>(FindObjectsSortMode.None))
        {
            if (t.text == null) continue;
            if (t.text.Trim().ToUpperInvariant() != buscado) continue;

            return SubirAlBoton(t.gameObject);
        }

        foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
        {
            if (t.text == null) continue;
            if (t.text.Trim().ToUpperInvariant() != buscado) continue;

            return SubirAlBoton(t.gameObject);
        }

        return null;
    }

    private static GameObject SubirAlBoton(GameObject texto)
    {
        // Si el propio texto ya es el boton, se queda.
        if (texto.GetComponent<Button>() != null) return texto;

        Transform padre = texto.transform.parent;

        if (padre == null) return texto;

        // El padre es el boton si tiene una imagen (el recuadro) o ya un Button.
        if (padre.GetComponent<Button>() != null || padre.GetComponent<Image>() != null)
            return padre.gameObject;

        return texto;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Acciones
    // ────────────────────────────────────────────────────────────────────

    public void Abrir()
    {
        if (_panel == null) Construir();
        if (_panel == null) return;

        _panel.SetActive(true);
        Refrescar();
    }

    public void Cerrar()
    {
        if (_panel != null) _panel.SetActive(false);
    }

    private void Continuar(PartidaGuardadaModel datos, int ranura)
    {
        Cerrar();

        // Se le pasa la ranura para que los guardados de esta sesion vuelvan
        // a esta misma, en vez de crear una copia en otra.
        if (!PartidaGuardadaController.Continuar(datos, ranura))
            Mostrar("No se pudo cargar esa partida.");
    }

    private void Borrar(int ranura)
    {
        GuardadoController.Borrar(ranura);
        Mostrar("Ranura " + ranura + " borrada.");
        Refrescar();
    }

    /// <summary>
    /// Application.Quit no hace nada dentro del editor de Unity, por eso
    /// tambien se apaga el modo Play. Sin las dos lineas, probar el boton
    /// desde el editor parece que esta roto cuando no lo esta.
    /// </summary>
    private void SalirDelJuego()
    {
        Debug.Log("[Menu] Saliendo del juego.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    // ────────────────────────────────────────────────────────────────────
    //  Panel
    // ────────────────────────────────────────────────────────────────────

    private void Refrescar()
    {
        if (_lista == null) return;

        for (int i = _lista.childCount - 1; i >= 0; i--)
            Destroy(_lista.GetChild(i).gameObject);

        List<PartidaGuardadaModel> partidas = GuardadoController.Listar();
        int hay = 0;

        for (int i = 0; i < partidas.Count; i++)
        {
            int ranura = i + 1;
            PartidaGuardadaModel datos = partidas[i];

            if (datos == null) { CrearFilaVacia(ranura); continue; }

            hay++;
            CrearFila(ranura, datos);
        }

        Mostrar(hay > 0
            ? hay + " partida(s) guardada(s)."
            : "Todavia no has guardado ninguna partida. Dale a Guardar mientras juegas.");
    }

    private void CrearFila(int ranura, PartidaGuardadaModel datos)
    {
        GameObject fila = CrearFilaVaciaBase();

        CrearTexto(fila.transform, ranura + ".  " + datos.Resumen() + "\n" + datos.Fecha,
                   new Vector2(0f, 0f), new Vector2(0.66f, 1f), 19f,
                   new Color(1f, 0.95f, 0.85f), TextAlignmentOptions.Left);

        int r = ranura;   // copia local: sin esto el clic usaria el ultimo valor

        Button continuar = CrearBoton(fila.transform, "Continuar",
                                      new Vector2(0.67f, 0.12f), new Vector2(0.86f, 0.88f),
                                      new Color(0.20f, 0.35f, 0.22f, 1f));
        continuar.onClick.AddListener(() => Continuar(datos, r));

        Button borrar = CrearBoton(fila.transform, "Borrar",
                                   new Vector2(0.87f, 0.12f), new Vector2(0.99f, 0.88f),
                                   new Color(0.38f, 0.20f, 0.18f, 1f));
        borrar.onClick.AddListener(() => Borrar(r));
    }

    private void CrearFilaVacia(int ranura)
    {
        GameObject fila = CrearFilaVaciaBase();

        CrearTexto(fila.transform, ranura + ".  (vacia)",
                   new Vector2(0f, 0f), new Vector2(1f, 1f), 19f,
                   new Color(0.6f, 0.58f, 0.54f), TextAlignmentOptions.Left);
    }

    private GameObject CrearFilaVaciaBase()
    {
        var go = new GameObject("Fila", typeof(RectTransform));
        go.transform.SetParent(_lista, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.25f);

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 62f;
        le.preferredHeight = 62f;

        return go;
    }

    private void Construir()
    {
        Canvas canvas = BuscarOCrearCanvas();

        _panel = new GameObject("PanelPartidas", typeof(RectTransform));
        _panel.transform.SetParent(canvas.transform, false);

        var rect = _panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(880f, 600f);

        var fondo = _panel.AddComponent<Image>();
        fondo.color = new Color(0.08f, 0.06f, 0.05f, 0.96f);

        CrearTexto(_panel.transform, "PARTIDAS GUARDADAS",
                   new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f), 30f,
                   new Color(1f, 0.95f, 0.85f), TextAlignmentOptions.Center);

        // La lista, una fila debajo de otra.
        var listaGO = new GameObject("Lista", typeof(RectTransform));
        listaGO.transform.SetParent(_panel.transform, false);

        var lr = listaGO.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0.04f, 0.14f);
        lr.anchorMax = new Vector2(0.96f, 0.88f);
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;

        var vertical = listaGO.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = 8f;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandHeight = false;

        _lista = listaGO.transform;

        _mensaje = CrearTexto(_panel.transform, "",
                              new Vector2(0.04f, 0.06f), new Vector2(0.70f, 0.13f), 18f,
                              new Color(0.85f, 0.85f, 0.8f), TextAlignmentOptions.Left);

        Button cerrar = CrearBoton(_panel.transform, "Cerrar",
                                   new Vector2(0.78f, 0.04f), new Vector2(0.96f, 0.12f),
                                   new Color(0.22f, 0.18f, 0.13f, 1f));
        cerrar.onClick.AddListener(Cerrar);

        _panel.SetActive(false);
    }

    private void Mostrar(string texto)
    {
        if (_mensaje != null) _mensaje.text = texto;
    }

    private TMP_Text CrearTexto(Transform padre, string contenido, Vector2 min, Vector2 max,
                                float tamano, Color color, TextAlignmentOptions alineacion)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var r = go.GetComponent<RectTransform>();
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = new Vector2(10f, 4f);
        r.offsetMax = new Vector2(-10f, -4f);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = contenido;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = alineacion;
        t.raycastTarget = false;

        return t;
    }

    private Button CrearBoton(Transform padre, string etiqueta, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject("Boton" + etiqueta, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var r = go.GetComponent<RectTransform>();
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = color;

        var boton = go.AddComponent<Button>();
        boton.targetGraphic = img;

        var textoGO = new GameObject("Texto", typeof(RectTransform));
        textoGO.transform.SetParent(go.transform, false);

        var tr = textoGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(4f, 2f);
        tr.offsetMax = new Vector2(-4f, -2f);

        var t = textoGO.AddComponent<TextMeshProUGUI>();
        t.text = etiqueta;
        t.fontSize = 19f;
        t.color = new Color(1f, 0.96f, 0.88f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        return boton;
    }

    private Canvas BuscarOCrearCanvas()
    {
        GameObject existente = GameObject.Find("Canvas");

        if (existente != null)
        {
            Canvas c = existente.GetComponent<Canvas>();
            if (c != null) return c;
        }

        var go = new GameObject("CanvasPartidas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        return canvas;
    }
}
