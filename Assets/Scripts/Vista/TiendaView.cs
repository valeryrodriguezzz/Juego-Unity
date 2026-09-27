using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VISTA: la tienda de herramientas. Construye TODO el panel por codigo (fondo,
/// titulo, los cuatro botones de compra, el oro, la barra de durabilidad y el
/// mensaje) y se lo entrega al TiendaArmasController, que es quien tiene la
/// logica y habla con el Modelo.
///
/// Se hace asi, y no arrastrando cosas en el Inspector, por lo mismo que la
/// mochila: son doce objetos de UI con sus anclas y sus tamaños, y montarlos a
/// mano es media hora de clics en la que cualquier campo que se quede sin
/// asignar sale luego como un NullReference en mitad de la partida.
///
/// COMO USARLO EN UNITY (un solo paso):
///   En la escena Juego: clic derecho en la Hierarchy -> Create Empty ->
///   llamalo "TiendaManager" -> Add Component -> TiendaView.
///   Unity le agrega solo el TiendaArmasController porque este script se lo
///   exige. No hay que arrastrar NADA.
///
/// Se abre y se cierra con la tecla T. Si mas adelante quieres que se abra al
/// entrar a la Armeria en vez de con una tecla, ese edificio solo tiene que
/// llamar a TiendaArmasController.AbrirTienda().
/// </summary>
[RequireComponent(typeof(TiendaArmasController))]
public class TiendaView : MonoBehaviour
{
    [Header("Controles")]
    [Tooltip("Tecla para abrir la tienda desde CUALQUIER parte del mapa. " +
             "Normalmente va en None, porque la tienda se abre con E parado " +
             "en la Armeria (eso lo hace EdificioTiendaController). Ponle T " +
             "solo si quieres probarla sin tener que caminar hasta el edificio.")]
    [SerializeField] private KeyCode teclaAbrir = KeyCode.None;

    [Header("Aspecto")]
    [Tooltip("Opcional: un pergamino de Tiny Swords/UI Elements/Papers. " +
             "Si lo dejas vacio se usa un panel oscuro.")]
    [SerializeField] private Sprite fondoPanel;

    [Tooltip("Opcional: el recuadro de cada boton.")]
    [SerializeField] private Sprite fondoBoton;

    [SerializeField] private Vector2 tamanoPanel = new Vector2(600f, 750f);

    // Colores. Se dejan en el Inspector por si quiere ajustarlos sin tocar codigo.
    [SerializeField] private Color colorPanel = new Color(0.08f, 0.06f, 0.05f, 0.94f);
    [SerializeField] private Color colorBoton = new Color(0.22f, 0.18f, 0.13f, 1f);
    [SerializeField] private Color colorTexto = new Color(1f, 0.95f, 0.85f);
    [SerializeField] private Color colorOro = new Color(1f, 0.85f, 0.3f);

    private TiendaArmasController _tienda;
    private GameObject _panel;

    // --------------------------------------------------------------------
    //  Construccion
    // --------------------------------------------------------------------

    private void Awake()
    {
        _tienda = GetComponent<TiendaArmasController>();

        Construir();

        Debug.Log("[Tienda] Panel construido. Abre y cierra con la tecla " + teclaAbrir + ".");
    }

    private void Update()
    {
        // Con KeyCode.None esto nunca se cumple, que es lo normal: la tienda
        // se abre desde la Armeria. Es solo un atajo para probar.
        if (teclaAbrir == KeyCode.None) return;
        if (!Input.GetKeyDown(teclaAbrir)) return;

        Alternar();
    }

    /// <summary>Abre si esta cerrada y cierra si esta abierta.</summary>
    public void Alternar()
    {
        if (_tienda.EstaAbierta) _tienda.CerrarTienda();
        else _tienda.AbrirTienda();
    }

    private void Construir()
    {
        Canvas canvas = BuscarOCrearCanvas();

        // ---- panel ----
        _panel = new GameObject("PanelTienda", typeof(RectTransform));
        _panel.transform.SetParent(canvas.transform, false);

        var rectPanel = _panel.GetComponent<RectTransform>();
        rectPanel.anchorMin = new Vector2(0.5f, 0.5f);
        rectPanel.anchorMax = new Vector2(0.5f, 0.5f);
        rectPanel.pivot = new Vector2(0.5f, 0.5f);
        rectPanel.anchoredPosition = Vector2.zero;
        rectPanel.sizeDelta = tamanoPanel;

        var imgPanel = _panel.AddComponent<Image>();
        if (fondoPanel != null) { imgPanel.sprite = fondoPanel; imgPanel.color = Color.white; }
        else imgPanel.color = colorPanel;

        Transform p = _panel.transform;

        float ancho = tamanoPanel.x - 90f;
        const float alto = 60f;   // alto de cada boton
        const float salto = 70f;  // de un boton al siguiente

        // ---- titulo y oro ----
        CrearTexto("Titulo", p, tamanoPanel.x - 60f, 42f, 14f,
                   "MERCADO", 32f, colorTexto);

        TMP_Text textoOro = CrearTexto("TextoOro", p, tamanoPanel.x - 60f, 34f, 58f,
                                       "Oro: 0", 26f, colorOro);

        // ---- seccion 1: herramientas ----
        // El texto de cada boton lo escribe el Controlador en RefrescarUI: ahi
        // pone nombre, precio y stock, que cambian solos mientras juegas.
        CrearSeccion("TituloHerramientas", p, ancho, 96f, "HERRAMIENTAS");

        Button bHacha    = CrearBoton("BotonHacha",    p, ancho, alto, 126f + salto * 0f, "Hacha");
        Button bPico     = CrearBoton("BotonPico",     p, ancho, alto, 126f + salto * 1f, "Pico");
        Button bCuchillo = CrearBoton("BotonCuchillo", p, ancho, alto, 126f + salto * 2f, "Cuchillo");
        Button bMartillo = CrearBoton("BotonMartillo", p, ancho, alto, 126f + salto * 3f, "Martillo");

        // ---- seccion 2: provisiones ----
        CrearSeccion("TituloProvisiones", p, ancho, 408f, "PROVISIONES");

        Button bCarne  = CrearBoton("BotonCarne",  p, ancho, alto, 438f, "Carne");
        Button bMadera = CrearBoton("BotonMadera", p, ancho, alto, 508f, "Madera");

        // ---- estado del arma equipada ----
        TMP_Text textoEquipada = CrearTexto("TextoArmaEquipada", p, ancho, 30f, 578f,
                                            "Sin herramienta", 23f, colorTexto);

        Slider barra = CrearBarra("BarraDurabilidad", p, ancho - 80f, 18f, 612f);

        // ---- mensajes de la tienda ----
        TMP_Text textoMensaje = CrearTexto("TextoMensaje", p, ancho, 64f, 638f,
                                           "", 21f, new Color(0.85f, 0.85f, 0.8f));
        textoMensaje.alignment = TextAlignmentOptions.Top;

        // Pista, para que no se le olvide en la sustentacion.
        CrearTexto("Ayuda", p, ancho, 24f, 708f,
                   "Cierra con E o con la X", 17f,
                   new Color(0.7f, 0.68f, 0.62f));

        // ---- boton de cerrar ----
        Button bCerrar = CrearBotonCerrar(p);

        // ---- y se le entrega todo al Controlador ----
        // Esto pasa en Awake, asi que cuando el Controlador ejecute su Start
        // ya tiene sus referencias. El es quien cablea los onClick.
        _tienda.Configurar(_panel, bCerrar, bHacha, bPico, bCuchillo, bMartillo,
                           textoOro, textoMensaje, textoEquipada, barra);

        _tienda.ConfigurarProvisiones(bCarne, bMadera);

        _panel.SetActive(false);
    }

    // --------------------------------------------------------------------
    //  Piezas sueltas
    // --------------------------------------------------------------------

    /// <summary>
    /// Crea un hijo anclado ARRIBA-CENTRO del panel y lo baja "y" pixeles.
    /// Colocar todo desde arriba hace que el panel se lea como una lista y que
    /// cambiar un tamaño no descuadre lo de abajo.
    /// </summary>
    private RectTransform CrearHijo(string nombre, Transform padre,
                                    float ancho, float alto, float y)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 1f);
        r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(ancho, alto);
        r.anchoredPosition = new Vector2(0f, -y);

        return r;
    }

    /// <summary>
    /// Encabezado de seccion: el texto mas una linea debajo, para que se vea
    /// que los cuatro botones de arriba y los dos de abajo son cosas distintas.
    /// </summary>
    private void CrearSeccion(string nombre, Transform padre, float ancho, float y, string titulo)
    {
        TMP_Text t = CrearTexto(nombre, padre, ancho, 26f, y, titulo, 20f,
                                new Color(0.75f, 0.70f, 0.55f));
        t.alignment = TextAlignmentOptions.Left;
        t.characterSpacing = 6f;

        RectTransform linea = CrearHijo(nombre + "_Linea", padre, ancho, 2f, y + 26f);
        var img = linea.gameObject.AddComponent<Image>();
        img.color = new Color(0.75f, 0.70f, 0.55f, 0.35f);
        img.raycastTarget = false;
    }

    private TMP_Text CrearTexto(string nombre, Transform padre,
                                float ancho, float alto, float y,
                                string contenido, float tamano, Color color)
    {
        RectTransform r = CrearHijo(nombre, padre, ancho, alto, y);

        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = contenido;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        return t;
    }

    private Button CrearBoton(string nombre, Transform padre,
                              float ancho, float alto, float y, string etiqueta)
    {
        RectTransform r = CrearHijo(nombre, padre, ancho, alto, y);

        var img = r.gameObject.AddComponent<Image>();
        if (fondoBoton != null) { img.sprite = fondoBoton; img.color = Color.white; }
        else img.color = colorBoton;

        var boton = r.gameObject.AddComponent<Button>();
        boton.targetGraphic = img;

        // Gris translucido cuando no se puede comprar (sin oro, sin stock o
        // porque ya la tienes). El Controlador lo maneja con .interactable.
        ColorBlock colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1f, 0.93f, 0.75f);
        colores.pressedColor = new Color(0.8f, 0.72f, 0.55f);
        colores.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
        boton.colors = colores;

        // La etiqueta va estirada dentro del boton. El Controlador la busca con
        // GetComponentInChildren<TMP_Text>() y le escribe precio y stock.
        var textoGO = new GameObject("Texto", typeof(RectTransform));
        textoGO.transform.SetParent(r, false);

        var tr = textoGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12f, 4f);
        tr.offsetMax = new Vector2(-12f, -4f);

        var t = textoGO.AddComponent<TextMeshProUGUI>();
        t.text = etiqueta;
        t.fontSize = 24f;
        t.color = colorTexto;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false; // si no, el texto se come el clic del boton

        return boton;
    }

    /// <summary>
    /// Un Slider armado a mano. Necesita Fill Area -> Fill y que el Slider
    /// sepa cual es su fillRect; sin eso la barra no se mueve.
    /// </summary>
    private Slider CrearBarra(string nombre, Transform padre,
                              float ancho, float alto, float y)
    {
        RectTransform r = CrearHijo(nombre, padre, ancho, alto, y);

        var slider = r.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;      // es un indicador, no un control
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;             // PorcentajeDurabilidad va de 0 a 1
        slider.value = 1f;

        // fondo
        var fondoGO = new GameObject("Fondo", typeof(RectTransform));
        fondoGO.transform.SetParent(r, false);
        Estirar(fondoGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var fondoImg = fondoGO.AddComponent<Image>();
        fondoImg.color = new Color(0f, 0f, 0f, 0.55f);
        fondoImg.raycastTarget = false;

        // area de relleno
        var areaGO = new GameObject("Fill Area", typeof(RectTransform));
        areaGO.transform.SetParent(r, false);
        Estirar(areaGO.GetComponent<RectTransform>(), new Vector2(2f, 2f), new Vector2(-2f, -2f));

        // relleno
        var fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(areaGO.transform, false);
        RectTransform fill = fillGO.GetComponent<RectTransform>();
        Estirar(fill, Vector2.zero, Vector2.zero);

        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.35f, 0.8f, 0.3f);
        fillImg.raycastTarget = false;

        slider.fillRect = fill;
        slider.targetGraphic = fillImg;

        return slider;
    }

    private Button CrearBotonCerrar(Transform padre)
    {
        var go = new GameObject("BotonCerrar", typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(1f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(1f, 1f);
        r.sizeDelta = new Vector2(42f, 42f);
        r.anchoredPosition = new Vector2(-14f, -14f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.55f, 0.2f, 0.18f, 0.95f);

        var boton = go.AddComponent<Button>();
        boton.targetGraphic = img;

        var textoGO = new GameObject("X", typeof(RectTransform));
        textoGO.transform.SetParent(go.transform, false);
        Estirar(textoGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var t = textoGO.AddComponent<TextMeshProUGUI>();
        t.text = "X";
        t.fontSize = 24f;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        return boton;
    }

    private static void Estirar(RectTransform r, Vector2 desdeAbajo, Vector2 desdeArriba)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = desdeAbajo;
        r.offsetMax = desdeArriba;
    }

    private Canvas BuscarOCrearCanvas()
    {
        // Igual que en la mochila: se busca por nombre y no con
        // FindObjectOfType, que quedo obsoleto en las versiones nuevas.
        GameObject existente = GameObject.Find("Canvas");

        if (existente != null)
        {
            Canvas c = existente.GetComponent<Canvas>();
            if (c != null) return c;
        }

        var go = new GameObject("CanvasTienda");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        Debug.LogWarning("[Tienda] No encontre ningun Canvas llamado 'Canvas', " +
                         "asi que cree uno para la tienda.");
        return canvas;
    }
}
