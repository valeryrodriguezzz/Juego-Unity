using System;
using System.Collections.Generic;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// VISTA: la mochila del jugador, en casillas con icono y cantidad, estilo
// Minecraft. Se abre y se cierra con Tab.
//
// Muestra una casilla por cada recurso (oro, madera, comida, armas) con su
// cantidad, y una casilla por cada arma que lleva, con su durabilidad. La
// que esta equipada se marca con un borde de color.
//
// LOS CLICS:
//  - Clic en un arma: la equipa.
//  - Clic en la casilla de Comida: se come una unidad. La casilla sale verde
//    cuando hay carne y gris cuando no, para saber antes de hacer clic si va
//    a pasar algo. La tecla F sigue funcionando como atajo.
//
// COMO USARLO EN UNITY:
// 1. En el Canvas: clic derecho -> UI -> Image. Renombralo PanelMochila,
//    hazlo de unos 500x550, centralo y ponle de fondo un pergamino de
//    Tiny Swords/UI Elements/Papers. DEJALO DESACTIVADO.
// 2. Create Empty -> Mochila -> Add Component -> MochilaView.
// 3. Arrastra PanelMochila al campo Panel.
// 4. Llena los iconos (ver abajo). Es lo unico que toma un rato.
//
// LOS ICONOS:
// En Iconos Recursos y Iconos Armas pones el Size y en cada elemento eliges
// de que es y le arrastras el sprite. Los de recursos estan en
// Tiny Swords/UI Elements/Icons. Los que no llenes salen como una casilla
// con las tres primeras letras del nombre, asi que puedes ir poniendolos
// de a poco sin que nada se rompa.
//
// Las casillas se crean por codigo: no hay que hacer prefabs, ni Grid
// Layout Group a mano, ni colocar nada. Si cambias el tamaño o el numero
// de columnas en el Inspector, se reacomodan solas al abrir.

public class MochilaView : MonoBehaviour
{
    [Serializable]
    public class IconoDeRecurso
    {
        public TipoRecurso recurso;
        public Sprite icono;
    }

    [Serializable]
    public class IconoDeArma
    {
        public TipoArma arma;
        public Sprite icono;
    }

    [Header("UI")]
    [Tooltip("El panel que se prende y se apaga. Dejalo desactivado en la escena.")]
    [SerializeField] private GameObject panel;

    [Tooltip("Sprite del recuadro de cada casilla. Opcional: si lo dejas vacio " +
             "se usa un cuadro gris.")]
    [SerializeField] private Sprite fondoDeCasilla;

    [Header("Distribucion")]
    [SerializeField] private float tamanoCasilla = 90f;
    [SerializeField] private int columnas = 4;
    [SerializeField] private float separacion = 10f;

    [Tooltip("Margen desde el borde del panel, para que las casillas no " +
             "queden encima del marco del pergamino.")]
    [SerializeField] private int margen = 40;

    [Header("Que recursos mostrar")]
    [Tooltip("El recurso Armas es el armamento que fabrica la Armeria, NO tus " +
             "herramientas. Mientras no se use para nada, dejalo sin marcar y " +
             "la mochila muestra solo Oro, Madera y Comida.")]
    [SerializeField] private bool mostrarRecursoArmas;

    [Header("Iconos")]
    [SerializeField] private IconoDeRecurso[] iconosRecursos;
    [SerializeField] private IconoDeArma[] iconosArmas;

    [Header("Controles")]
    [SerializeField] private KeyCode teclaAbrir = KeyCode.Tab;

    [Tooltip("Hacer clic en un arma la equipa.")]
    [SerializeField] private bool clicParaEquipar = true;

    [Tooltip("Hacer clic en la casilla de Comida se come una unidad.")]
    [SerializeField] private bool clicParaComer = true;

    [Header("Colores")]
    [Tooltip("Borde del arma que lleva puesta.")]
    [SerializeField] private Color colorEquipada = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color colorNormal = Color.white;
    [Tooltip("Color del arma rota.")]
    [SerializeField] private Color colorRota = new Color(0.6f, 0.3f, 0.3f);

    [Tooltip("Casilla de Comida cuando si hay carne para comer.")]
    [SerializeField] private Color colorComestible = new Color(0.65f, 1f, 0.6f);

    [Tooltip("Casilla de Comida cuando la mochila esta vacia.")]
    [SerializeField] private Color colorSinComida = new Color(0.45f, 0.45f, 0.45f);

    // Una casilla ya creada. Se guardan las referencias para poder
    // actualizar solo el numero en cada frame, sin rehacer la cuadricula.
    private class Casilla
    {
        public GameObject Objeto;
        public Image Fondo;
        public Image Icono;
        public TMP_Text Cantidad;
        public RectTransform BarraDurabilidad;
        public TipoArma Arma;
        public TipoRecurso Recurso;
        public bool EsArma;
    }

    private readonly List<Casilla> _casillas = new List<Casilla>();
    private RectTransform _contenedor;

    private JugadorArmasController _armas;
    private JugadorModel _jugador;
    private HambreController _hambre;
    private bool _abierta;

    private void Start()
    {
        // Primer mensaje antes de nada: si este no sale en la consola, el
        // script no se esta ejecutando (el componente no esta puesto, o su
        // casilla o la del GameObject estan desmarcadas).
        Debug.Log("[Mochila] Arrancando. Abre y cierra con " + teclaAbrir + ".");

        if (panel == null)
        {
            Debug.LogWarning("[Mochila] El campo Panel estaba vacio, asi que cree uno " +
                             "automatico. Si quieres el del pergamino, arrastra tu " +
                             "PanelMochila al campo Panel del Inspector.");
            panel = CrearPanelAutomatico();
        }

        if (PlayerSelectionManager.Instance != null && PlayerSelectionManager.Instance.Partida != null)
            _jugador = PlayerSelectionManager.Instance.Partida.Jugador;

        GameObject jugadorGO = GameObject.FindGameObjectWithTag("Player");
        if (jugadorGO != null)
        {
            _armas = jugadorGO.GetComponent<JugadorArmasController>();
            _hambre = jugadorGO.GetComponent<HambreController>();
        }

        if (_hambre == null && clicParaComer)
            Debug.LogWarning("[Mochila] No encontre el HambreController, asi que la " +
                             "casilla de Comida no va a servir para comer. Revisa que " +
                             "el Jugador lo tenga puesto.");

        if (_armas == null)
            Debug.LogWarning("[Mochila] No encontre el JugadorArmasController. " +
                             "Revisa que el Jugador lo tenga y que su Tag sea Player.");

        Cerrar();
    }

    /// <summary>
    /// Crea un panel oscuro centrado, por si no se asigno ninguno. Feo pero
    /// funcional: permite comprobar que la mochila abre antes de ponerse a
    /// hacerla bonita.
    /// </summary>
    private GameObject CrearPanelAutomatico()
    {
        Canvas canvas = BuscarOCrearCanvas();

        var go = new GameObject("PanelMochila (automatico)", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var fondo = go.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.85f);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(520f, 560f);

        return go;
    }

    private Canvas BuscarOCrearCanvas()
    {
        // Se busca por nombre en vez de con FindObjectOfType, que quedo
        // obsoleto en las versiones nuevas de Unity.
        GameObject existente = GameObject.Find("Canvas");

        if (existente != null)
        {
            Canvas c = existente.GetComponent<Canvas>();
            if (c != null) return c;
        }

        var go = new GameObject("CanvasMochila");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        Debug.LogWarning("[Mochila] No encontre ningun Canvas llamado 'Canvas', " +
                         "asi que cree uno nuevo para la mochila.");
        return canvas;
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaAbrir))
        {
            if (_abierta) Cerrar();
            else Abrir();
        }

        // Con la mochila abierta se refrescan solo los numeros. La cuadricula
        // no se rehace: seria tirar y volver a crear objetos 60 veces por
        // segundo para nada.
        if (_abierta) ActualizarCantidades();
    }

    public void Abrir()
    {
        _abierta = true;
        if (panel != null) panel.SetActive(true);

        Reconstruir();

        Debug.Log("[Mochila] Abierta con " + _casillas.Count + " casillas.");
    }

    public void Cerrar()
    {
        _abierta = false;
        if (panel != null) panel.SetActive(false);
    }

    // ------------------------------------------------------------------
    //  Armado de la cuadricula
    // ------------------------------------------------------------------

    private void Reconstruir()
    {
        if (panel == null) return;

        PrepararContenedor();

        foreach (Casilla c in _casillas)
            if (c.Objeto != null) Destroy(c.Objeto);

        _casillas.Clear();

        // Los recursos, aunque esten en cero. El armamento solo si se pidio:
        // hoy no se gasta en nada, y una casilla de "Armas" al lado de las
        // herramientas se presta a confusion.
        foreach (TipoRecurso recurso in new[]
                 { TipoRecurso.Oro, TipoRecurso.Madera, TipoRecurso.Comida, TipoRecurso.Armas })
        {
            if (recurso == TipoRecurso.Armas && !mostrarRecursoArmas)
                continue;

            CrearCasillaRecurso(recurso);
        }

        // Despues las armas que lleva encima.
        if (_armas != null && _armas.Inventario != null)
        {
            List<ArmaModel> lista = _armas.Inventario.Listar();

            // Ordenadas por tipo para que no bailen de posicion entre aperturas.
            lista.Sort((a, b) => a.Tipo.CompareTo(b.Tipo));

            foreach (ArmaModel arma in lista)
                CrearCasillaArma(arma);
        }

        ActualizarCantidades();
    }

    /// <summary>
    /// Crea (una sola vez) el objeto que ordena las casillas en cuadricula.
    /// El GridLayoutGroup es el que hace todo el trabajo de acomodarlas.
    /// </summary>
    private void PrepararContenedor()
    {
        if (_contenedor != null)
        {
            // Si por lo que sea el contenedor quedo colgando de otro objeto
            // (por ejemplo del panel automatico de un Play anterior), se
            // devuelve a su sitio en vez de dejar las casillas sueltas.
            if (_contenedor.parent != panel.transform)
            {
                _contenedor.SetParent(panel.transform, false);
                _contenedor.anchorMin = Vector2.zero;
                _contenedor.anchorMax = Vector2.one;
                _contenedor.offsetMin = new Vector2(margen, margen);
                _contenedor.offsetMax = new Vector2(-margen, -margen);
            }

            AjustarGrid(_contenedor.GetComponent<GridLayoutGroup>());
            return;
        }

        var go = new GameObject("Casillas", typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);

        _contenedor = go.GetComponent<RectTransform>();

        // Que ocupe todo el panel menos el margen.
        _contenedor.anchorMin = Vector2.zero;
        _contenedor.anchorMax = Vector2.one;
        _contenedor.offsetMin = new Vector2(margen, margen);
        _contenedor.offsetMax = new Vector2(-margen, -margen);

        AjustarGrid(go.AddComponent<GridLayoutGroup>());
    }

    private void AjustarGrid(GridLayoutGroup grid)
    {
        if (grid == null) return;

        grid.cellSize = new Vector2(tamanoCasilla, tamanoCasilla);
        grid.spacing = new Vector2(separacion, separacion);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columnas);
        // MiddleCenter y no UpperLeft: asi las casillas quedan en el medio del
        // panel sea cual sea su tamaño. Con UpperLeft, un panel grande manda
        // las casillas a su esquina superior izquierda, que puede caer fuera
        // de la parte visible del pergamino.
        grid.childAlignment = TextAnchor.MiddleCenter;
    }

    private void CrearCasillaRecurso(TipoRecurso recurso)
    {
        Casilla c = CrearCasillaVacia(RecursoTipoHelper.ATexto(recurso));
        c.EsArma = false;
        c.Recurso = recurso;

        Sprite icono = BuscarIconoRecurso(recurso);

        if (icono != null)
        {
            c.Icono.sprite = icono;
            c.Icono.enabled = true;
        }
        else
        {
            // Sin icono asignado: se deja el nombre cortito como pista.
            PonerTextoDeRespaldo(c, RecursoTipoHelper.ATexto(recurso));
        }

        // La carne se come haciendo clic en su casilla, que es lo natural en
        // un inventario. La tecla F sigue funcionando como atajo.
        if (recurso == TipoRecurso.Comida && clicParaComer)
        {
            var boton = c.Objeto.AddComponent<Button>();
            boton.onClick.AddListener(Comer);
        }

        _casillas.Add(c);
    }

    /// <summary>
    /// Todas las comprobaciones (que haya comida, que no este ya lleno) las
    /// hace HambreModel.Comer dentro de su lock, y de un solo golpe: sin eso,
    /// entre "¿tengo comida?" y "descontar una" el hilo del hambre podria
    /// haber cambiado el numero. Aqui solo se pide comer.
    /// </summary>
    private void Comer()
    {
        if (_hambre == null)
        {
            AvisoPantalla.Mostrar("No puedo comer: falta el HambreController en el Jugador.");
            return;
        }

        _hambre.Comer();
    }

    private void CrearCasillaArma(ArmaModel arma)
    {
        Casilla c = CrearCasillaVacia(arma.Nombre);
        c.EsArma = true;
        c.Arma = arma.Tipo;

        if (c.BarraDurabilidad != null)
            c.BarraDurabilidad.gameObject.SetActive(true);

        Sprite icono = BuscarIconoArma(arma.Tipo);

        if (icono != null)
        {
            c.Icono.sprite = icono;
            c.Icono.enabled = true;
        }
        else
        {
            PonerTextoDeRespaldo(c, arma.Nombre);
        }

        if (clicParaEquipar)
        {
            var boton = c.Objeto.AddComponent<Button>();
            TipoArma tipo = arma.Tipo; // copia local: sin esto el clic usaria
                                       // el valor de la ultima vuelta del bucle
            boton.onClick.AddListener(() => Equipar(tipo));
        }

        _casillas.Add(c);
    }

    /// <summary>
    /// Arma la estructura de una casilla:
    ///   Casilla (Image de fondo)
    ///   +-- Icono    (Image)
    ///   +-- Cantidad (texto abajo a la derecha)
    /// </summary>
    private Casilla CrearCasillaVacia(string nombre)
    {
        var go = new GameObject("Casilla_" + nombre, typeof(RectTransform));
        go.transform.SetParent(_contenedor, false);

        var fondo = go.AddComponent<Image>();
        if (fondoDeCasilla != null) fondo.sprite = fondoDeCasilla;
        else fondo.color = new Color(0f, 0f, 0f, 0.35f);

        // --- icono ---
        var iconoGO = new GameObject("Icono", typeof(RectTransform));
        iconoGO.transform.SetParent(go.transform, false);

        var iconoRect = iconoGO.GetComponent<RectTransform>();
        iconoRect.anchorMin = Vector2.zero;
        iconoRect.anchorMax = Vector2.one;
        // Un poco metido hacia adentro para que no tape el marco.
        iconoRect.offsetMin = new Vector2(10f, 10f);
        iconoRect.offsetMax = new Vector2(-10f, -10f);

        var icono = iconoGO.AddComponent<Image>();
        icono.preserveAspect = true;
        icono.raycastTarget = false;
        icono.enabled = false; // se prende solo si hay sprite

        // --- cantidad ---
        var textoGO = new GameObject("Cantidad", typeof(RectTransform));
        textoGO.transform.SetParent(go.transform, false);

        var textoRect = textoGO.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(4f, 2f);
        textoRect.offsetMax = new Vector2(-6f, -4f);

        var texto = textoGO.AddComponent<TextMeshProUGUI>();
        texto.alignment = TextAlignmentOptions.BottomRight;
        texto.fontSize = tamanoCasilla * 0.26f;
        texto.color = Color.white;
        texto.raycastTarget = false;

        // --- barrita de durabilidad (solo se ve en las herramientas) ---
        // Va como en Minecraft: una barra abajo en vez de un numero suelto,
        // que se confunde con la cantidad de un recurso.
        var barraGO = new GameObject("Durabilidad", typeof(RectTransform));
        barraGO.transform.SetParent(go.transform, false);

        var barraRect = barraGO.GetComponent<RectTransform>();
        barraRect.anchorMin = new Vector2(0.12f, 0.08f);
        barraRect.anchorMax = new Vector2(0.88f, 0.18f);
        barraRect.offsetMin = Vector2.zero;
        barraRect.offsetMax = Vector2.zero;

        var barraImg = barraGO.AddComponent<Image>();
        barraImg.raycastTarget = false;
        barraGO.SetActive(false); // los recursos no la usan

        return new Casilla
        {
            Objeto = go,
            Fondo = fondo,
            Icono = icono,
            Cantidad = texto,
            BarraDurabilidad = barraRect
        };
    }

    /// <summary>Cuando no hay icono asignado, al menos que se lea que es.</summary>
    private void PonerTextoDeRespaldo(Casilla c, string nombre)
    {
        var etiquetaGO = new GameObject("Etiqueta", typeof(RectTransform));
        etiquetaGO.transform.SetParent(c.Objeto.transform, false);

        var rect = etiquetaGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var texto = etiquetaGO.AddComponent<TextMeshProUGUI>();
        texto.text = nombre.Length > 3 ? nombre.Substring(0, 3) : nombre;
        texto.alignment = TextAlignmentOptions.Center;
        texto.fontSize = tamanoCasilla * 0.3f;
        texto.color = new Color(1f, 1f, 1f, 0.7f);
        texto.raycastTarget = false;
    }

    // ------------------------------------------------------------------
    //  Refresco de numeros (cada frame, barato)
    // ------------------------------------------------------------------

    private void ActualizarCantidades()
    {
        ArmaModel equipada = (_armas != null && _armas.Inventario != null)
            ? _armas.Inventario.Equipada
            : null;

        foreach (Casilla c in _casillas)
        {
            if (c.Objeto == null) continue;

            if (!c.EsArma)
            {
                int cantidad = _jugador != null ? CantidadDeRecurso(c) : 0;
                c.Cantidad.text = cantidad.ToString();

                // La casilla de comida se pinta verde cuando hay algo que
                // comer y gris cuando no: asi se ve de un vistazo que ese
                // clic va a hacer algo, sin tener que probarlo.
                if (c.Recurso == TipoRecurso.Comida && clicParaComer)
                    c.Fondo.color = cantidad > 0 ? colorComestible : colorSinComida;

                continue;
            }

            ArmaModel arma = BuscarArma(c.Arma);
            if (arma == null) continue;

            // Sin numero: en una herramienta el numero se lee como "cantidad"
            // y en realidad es su desgaste. Va en la barrita de abajo.
            c.Cantidad.text = "";

            float vidaUtil = arma.DurabilidadMaxima > 0
                ? (float)arma.Durabilidad / arma.DurabilidadMaxima
                : 0f;

            if (c.BarraDurabilidad != null)
            {
                // La barra se acorta moviendo su borde derecho.
                c.BarraDurabilidad.anchorMax = new Vector2(
                    0.12f + (0.88f - 0.12f) * vidaUtil, 0.18f);

                var img = c.BarraDurabilidad.GetComponent<Image>();
                if (img != null)
                    img.color = vidaUtil > 0.5f ? new Color(0.35f, 0.8f, 0.3f)
                              : vidaUtil > 0.2f ? new Color(0.95f, 0.8f, 0.2f)
                              : new Color(0.9f, 0.25f, 0.2f);
            }

            bool esLaEquipada = equipada != null && equipada.Tipo == c.Arma;

            c.Fondo.color = arma.EstaRota ? colorRota
                          : esLaEquipada ? colorEquipada
                          : colorNormal;
        }
    }

    private int CantidadDeRecurso(Casilla c)
    {
        // Se leen las propiedades del JugadorModel, que ya son thread-safe
        // (cada una toma el lock por dentro), asi que no hay problema en
        // consultarlas mientras los hilos del juego las estan cambiando.
        switch (c.Recurso)
        {
            case TipoRecurso.Oro:    return _jugador.Oro;
            case TipoRecurso.Madera: return _jugador.Madera;
            case TipoRecurso.Comida: return _jugador.Comida;
            case TipoRecurso.Armas:  return _jugador.Armas;
            default:                 return 0;
        }
    }

    private ArmaModel BuscarArma(TipoArma tipo)
    {
        if (_armas == null || _armas.Inventario == null) return null;

        foreach (ArmaModel a in _armas.Inventario.Listar())
            if (a.Tipo == tipo) return a;

        return null;
    }

    private void Equipar(TipoArma tipo)
    {
        if (_armas == null || _armas.Inventario == null) return;

        if (_armas.Inventario.Equipar(tipo))
            Debug.Log("[Mochila] Equipaste: " + CatalogoArmas.Nombre(tipo));
    }

    // ------------------------------------------------------------------
    //  Busqueda de iconos
    // ------------------------------------------------------------------

    private Sprite BuscarIconoRecurso(TipoRecurso recurso)
    {
        if (iconosRecursos == null) return null;

        foreach (IconoDeRecurso i in iconosRecursos)
            if (i != null && i.recurso == recurso) return i.icono;

        return null;
    }

    private Sprite BuscarIconoArma(TipoArma arma)
    {
        if (iconosArmas == null) return null;

        foreach (IconoDeArma i in iconosArmas)
            if (i != null && i.arma == arma) return i.icono;

        return null;
    }
}
