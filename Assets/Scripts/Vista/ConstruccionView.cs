using System;
using System.Collections.Generic;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;
using ImperiosEnGuerra.Modelo.Edificios;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// VISTA: el panel de construccion. Construye TODA su UI por codigo (el panel,
/// un boton por edificio con su costo, el boton de curar y el mensaje) igual
/// que la tienda y la mochila, asi no hay siete cosas que arrastrar en el
/// Inspector.
///
/// COMO SE CONSTRUYE AHORA
/// Se abre el panel con B, se hace clic en el edificio y el panel se cierra:
/// queda un "fantasma" del edificio pegado al raton, verde donde se puede
/// poner y rojo donde no (encima de un arbol, del mar, de otro edificio, de
/// uno mismo, o demasiado lejos). Clic izquierdo lo coloca, clic derecho o Esc
/// cancela. El oro y la madera se cobran en el clic, no antes.
///
/// El edificio que aparece en el mapa lo dibuja EdificioConstruidoController,
/// que va leyendo el porcentaje que sube el hilo de obra del Modelo.
///
/// AQUI ES DONDE SIRVE EL MARTILLO
/// Para construir hay que llevarlo equipado. Y su MultiplicadorConstruccion
/// (x2) le quita la mitad del tiempo a la obra, ademas de desgastarse con
/// cada construccion. Antes el martillo era la unica de las cuatro
/// herramientas que no hacia nada: se podia comprar pero no servia para nada.
///
/// Los hilos de construccion y de produccion siguen viviendo en EdificioModel:
/// esta clase solo pide construir y muestra mensajes.
///
/// COMO USARLO EN UNITY (un solo paso):
///   En la escena Juego: Create Empty -> "ConstruccionManager" ->
///   Add Component -> ConstruccionView.
///
/// Se abre y se cierra con la tecla B.
/// </summary>
public class ConstruccionView : MonoBehaviour
{
    [Header("Controles")]
    [SerializeField] private KeyCode teclaAbrir = KeyCode.B;

    [Header("Reglas")]
    [Tooltip("Exige llevar el martillo equipado para poder construir.")]
    [SerializeField] private bool exigirMartillo = true;

    [SerializeField] private TipoArma herramientaDeObra = TipoArma.Martillo;

    [Header("Donde se ponen (el dibujo de cada edificio)")]
    [Tooltip("Tiny Swords -> Buildings -> Blue Buildings. Si dejas alguno " +
             "vacio, ese edificio se coloca igual pero como un cuadro de color.")]
    [SerializeField] private Sprite spriteGranja;
    [SerializeField] private Sprite spriteMina;
    [SerializeField] private Sprite spriteAserradero;
    [SerializeField] private Sprite spriteArmeria;
    [SerializeField] private Sprite spriteIglesia;

    [Header("Colocacion")]
    [Tooltip("Que tan lejos del jugador se puede construir. 0 = sin limite.")]
    [SerializeField] private float distanciaMaxima = 14f;

    [SerializeField] private float escalaEdificio = 1f;

    [Tooltip("Nombre del objeto de la escena bajo el que se cuelgan los " +
             "edificios nuevos. Si no existe, se cuelgan sueltos.")]
    [SerializeField] private string carpetaEnLaEscena = "Edificios";

    [Header("Aspecto")]
    [SerializeField] private Sprite fondoPanel;
    [SerializeField] private Vector2 tamanoPanel = new Vector2(600f, 620f);
    [SerializeField] private Color colorPanel = new Color(0.08f, 0.06f, 0.05f, 0.94f);
    [SerializeField] private Color colorBoton = new Color(0.22f, 0.18f, 0.13f, 1f);
    [SerializeField] private Color colorTexto = new Color(1f, 0.95f, 0.85f);

    // Una fila del panel: el edificio que ofrece y su boton.
    private class Oferta
    {
        public string Nombre;
        public Func<EdificioModel> Crear;   // uno nuevo en cada construccion
        public Func<Sprite> Dibujo;         // se lee tarde: el Inspector se
                                            // llena despues de armar el panel
        public Button Boton;
        public TMP_Text Etiqueta;

        // El texto con el costo se calcula UNA vez al armar el panel. Los
        // costos son fijos, asi que recalcularlos en cada frame seria crear
        // cinco edificios por frame para nada.
        public string TextoFijo;
    }

    private readonly List<Oferta> _ofertas = new List<Oferta>();

    private GameObject _panel;
    private TMP_Text _textoMensaje;
    private TMP_Text _textoRecursos;
    private Button _botonCurar;

    private JugadorModel _jugador;
    private JugadorArmasController _armas;
    private IglesiaModel _iglesia;

    // Estado del modo colocacion
    private Oferta _colocando;
    private GameObject _fantasma;
    private SpriteRenderer _fantasmaSprite;
    private Transform _carpeta;
    private Transform _jugadorTransform;
    private int _capaEdificios;
    private int _ordenEdificios;
    private int _capaFantasma;
    private int _ordenFantasma;
    private bool _sitioValido;
    private int _frameEnQueEmpezo;
    private Camera _camara;

    private static readonly Color VERDE = new Color(0.4f, 1f, 0.4f, 0.6f);
    private static readonly Color ROJO = new Color(1f, 0.35f, 0.3f, 0.6f);

    // --------------------------------------------------------------------

    private void Awake()
    {
        Construir();
    }

    private void Start()
    {
        if (PlayerSelectionManager.Instance != null && PlayerSelectionManager.Instance.Partida != null)
            _jugador = PlayerSelectionManager.Instance.Partida.Jugador;

        if (_jugador == null)
        {
            Debug.LogWarning("[Construccion] No hay partida activa. Entra desde el Menu.");
            enabled = false;
            return;
        }

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            _armas = go.GetComponent<JugadorArmasController>();
            _jugadorTransform = go.transform;
        }

        _jugador.OnEdificioConstruido += AlConstruirse;

        PrepararSitio();

        // Orden importante:
        //  1. Si la partida es otra que la de antes, se botan los edificios
        //     apuntados de la anterior.
        //  2. Si se acaba de cargar una partida guardada, se apuntan los
        //     edificios que traia (los de los cinco mapas).
        //  3. Se dibujan los de ESTE mapa.
        EdificioConstruidoController.PrepararEscena(PlayerSelectionManager.Instance.Partida);

        PartidaGuardadaController.RestaurarEdificiosSiHaceFalta(this);

        EdificioConstruidoController.RehacerEscena(SceneManager.GetActiveScene().name, _carpeta);

        Debug.Log("[Construccion] Panel listo. Abre y cierra con " + teclaAbrir +
                  ". Los edificios nuevos van en la capa '" +
                  SortingLayer.IDToName(_capaEdificios) + "' y el fantasma en '" +
                  SortingLayer.IDToName(_capaFantasma) + "'.");
    }

    private void OnDestroy()
    {
        if (_jugador != null)
            _jugador.OnEdificioConstruido -= AlConstruirse;
    }

    private void Update()
    {
        // El aviso que dejo el hilo de obra, ya en el hilo principal. Va aqui
        // arriba y no dentro de Refrescar() porque la obra casi siempre
        // termina con el panel cerrado.
        if (_avisoPendiente != null)
        {
            AvisoPantalla.Mostrar(_avisoPendiente);
            Mostrar(_avisoPendiente);
            _avisoPendiente = null;
        }

        // Mientras se esta colocando un edificio, la tecla del panel cancela.
        if (_colocando != null)
        {
            ActualizarColocacion();
            return;
        }

        if (Input.GetKeyDown(teclaAbrir))
        {
            if (_panel.activeSelf) Cerrar();
            else Abrir();
        }

        if (_panel.activeSelf) Refrescar();
    }

    public void Abrir()
    {
        _panel.SetActive(true);
        Refrescar();
    }

    public void Cerrar()
    {
        _panel.SetActive(false);
    }

    // --------------------------------------------------------------------
    //  MODO COLOCACION
    //
    //  El jugador elige el edificio en el panel, el panel se cierra y queda
    //  un "fantasma" pegado al raton: verde donde se puede construir, rojo
    //  donde no. Clic izquierdo lo pone, clic derecho o Esc cancela.
    // --------------------------------------------------------------------

    private void IniciarColocacion(Oferta oferta)
    {
        // Se comprueba ANTES de entrar en modo colocacion, para no hacerle
        // pasear un fantasma por el mapa y decirle al final que no le alcanza.
        ArmaModel martillo = HerramientaDeObra();

        if (exigirMartillo && martillo == null)
        {
            Mostrar("Necesitas " + CatalogoArmas.Nombre(herramientaDeObra) +
                    " equipado para construir.");
            return;
        }

        if (martillo != null && martillo.EstaRota)
        {
            Mostrar("Tu " + martillo.Nombre + " esta roto. Espera a que se repare.");
            return;
        }

        EdificioModel muestra = oferta.Crear();

        if (_jugador.Oro < muestra.CostoOro || _jugador.Madera < muestra.CostoMadera)
        {
            Mostrar("No alcanzan los recursos para " + muestra.Nombre + ": necesitas " +
                    muestra.CostoOro + " oro y " + muestra.CostoMadera + " madera.");
            return;
        }

        _colocando = oferta;

        // El clic que acaba de pulsar el boton sigue "pulsado" durante este
        // frame. Sin esta marca, el mismo clic que elige el edificio lo
        // colocaria de una vez en mitad de la pantalla.
        _frameEnQueEmpezo = Time.frameCount;

        Cerrar();

        _fantasma = new GameObject("FantasmaConstruccion");
        _fantasmaSprite = _fantasma.AddComponent<SpriteRenderer>();
        _fantasmaSprite.sprite = DibujoDe(oferta);
        _fantasmaSprite.sortingLayerID = _capaFantasma;
        _fantasmaSprite.sortingOrder = _ordenFantasma + 50;
        _fantasma.transform.localScale = new Vector3(escalaEdificio, escalaEdificio, 1f);

        // Se pone ya mismo donde esta el raton: si esperara al siguiente frame
        // se veria aparecer un fotograma en el sitio equivocado.
        _fantasma.transform.position = RatonEnElMundo();

        AvisoPantalla.Mostrar("Clic izquierdo para poner la " + oferta.Nombre +
                              ".  Clic derecho o Esc para cancelar.");

        Debug.Log("[Construccion] Colocando " + oferta.Nombre + ". Fantasma en " +
                  _fantasma.transform.position + ", capa " +
                  SortingLayer.IDToName(_capaFantasma) + ".");
    }

    private void ActualizarColocacion()
    {
        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(teclaAbrir) ||
            Input.GetMouseButtonDown(1))
        {
            CancelarColocacion("Construccion cancelada.");
            return;
        }

        Vector3 destino = RatonEnElMundo();
        _fantasma.transform.position = destino;

        _sitioValido = SitioLibre(destino) && CercaDelJugador(destino) && !RatonSobreLaUI();
        _fantasmaSprite.color = _sitioValido ? VERDE : ROJO;

        if (Time.frameCount == _frameEnQueEmpezo) return;

        if (_sitioValido && Input.GetMouseButtonDown(0))
            Colocar(destino);
    }

    private void Colocar(Vector3 donde)
    {
        Oferta oferta = _colocando;
        EdificioModel edificio = oferta.Crear();

        ArmaModel martillo = HerramientaDeObra();

        // El martillo acorta la obra ANTES de arrancar el hilo.
        float segundos = edificio.TiempoConstruccionSeg;

        if (martillo != null)
            segundos = edificio.AplicarVelocidadDeConstruccion(martillo.MultiplicadorConstruccion);

        // Construir cobra el oro y la madera (todo o nada) y arranca el hilo
        // de obra dentro del Modelo. Se pudo quedar sin recursos entre que
        // eligio el edificio y hizo clic, asi que se vuelve a comprobar aqui:
        // la unica comprobacion que vale es la que cobra.
        if (!_jugador.Construir(edificio))
        {
            CancelarColocacion("No alcanzan los recursos para " + edificio.Nombre + ".");
            return;
        }

        if (martillo != null) martillo.Golpear();

        if (edificio is IglesiaModel iglesia) _iglesia = iglesia;

        EdificioConstruidoController.Crear(
            edificio, DibujoDe(oferta), donde, _carpeta,
            _capaEdificios, _ordenEdificios, escalaEdificio,
            SceneManager.GetActiveScene().name);

        LimpiarFantasma();

        AvisoPantalla.Mostrar(edificio.Nombre + " en obra: " + Mathf.CeilToInt(segundos) + " s.");
        Mostrar(edificio.Nombre + " en obra: " + Mathf.CeilToInt(segundos) + " s.");

        ImperiosEnGuerra.Controlador.Bitacora.Anotar(
            "Construccion",
            "Empezo " + edificio.Nombre + " en " + SceneManager.GetActiveScene().name +
            " por " + edificio.CostoOro + " oro y " + edificio.CostoMadera +
            " madera (" + Mathf.CeilToInt(segundos) + " s de obra)");
    }

    private void CancelarColocacion(string motivo)
    {
        LimpiarFantasma();

        if (!string.IsNullOrEmpty(motivo)) AvisoPantalla.Mostrar(motivo);
    }

    private void LimpiarFantasma()
    {
        _colocando = null;

        if (_fantasma != null) Destroy(_fantasma);

        _fantasma = null;
        _fantasmaSprite = null;
    }

    // --------------------------------------------------------------------
    //  Puente con las partidas guardadas
    // --------------------------------------------------------------------

    /// <summary>
    /// Apunta un edificio que viene de una partida guardada. Lo llama
    /// PartidaGuardadaController, que tiene los datos pero no sabe con que
    /// dibujo ni en que Sorting Layer va cada edificio: eso solo lo sabe este
    /// panel, que es quien los crea normalmente.
    /// </summary>
    public void ApuntarEdificioGuardado(EdificioModel modelo, string clave,
                                        string escena, float x, float y)
    {
        EdificioConstruidoController.Registrar(
            modelo, new Vector3(x, y, 0f), DibujoDeClave(clave),
            _capaEdificios, _ordenEdificios, escalaEdificio, escena);
    }

    private Sprite DibujoDeClave(string clave)
    {
        Sprite s = null;

        switch ((clave ?? "").Trim().ToLowerInvariant())
        {
            case "granja":     s = spriteGranja; break;
            case "mina":       s = spriteMina; break;
            case "aserradero": s = spriteAserradero; break;
            case "armeria":    s = spriteArmeria; break;
            case "iglesia":    s = spriteIglesia; break;
        }

        return s != null ? s : Respaldo();
    }

    /// <summary>
    /// Si a un edificio no se le arrastro su dibujo en el Inspector, se pone
    /// igual pero como un cuadro de madera hecho por codigo. Es feo, pero se
    /// ve y se puede jugar: peor seria colocar un edificio invisible y no
    /// entender por que no pasa nada.
    /// </summary>
    private static Sprite _respaldo;

    private Sprite DibujoDe(Oferta oferta)
    {
        Sprite s = (oferta != null && oferta.Dibujo != null) ? oferta.Dibujo() : null;

        return s != null ? s : Respaldo();
    }

    private static Sprite Respaldo()
    {
        if (_respaldo == null)
        {
            var tex = new Texture2D(64, 64);
            var pixeles = new Color[64 * 64];

            for (int i = 0; i < pixeles.Length; i++)
                pixeles[i] = new Color(0.55f, 0.42f, 0.28f);

            tex.SetPixels(pixeles);
            tex.Apply();

            _respaldo = Sprite.Create(tex, new Rect(0f, 0f, 64f, 64f),
                                      new Vector2(0.5f, 0.5f), 64f);
        }

        return _respaldo;
    }

    /// <summary>
    /// Convierte la posicion del raton en la pantalla a una posicion del mapa.
    ///
    /// OJO CON Camera.main: solo devuelve la camara si el objeto tiene la
    /// etiqueta "MainCamera", y en estas escenas la camara esta Untagged. Con
    /// Camera.main en null el fantasma nacia en la coordenada (0, 0), que en
    /// este mapa esta a 300 unidades de donde juegas: por eso no se veia
    /// nada. Asi que si Camera.main falla se busca la camara a mano.
    /// </summary>
    private Camera Camara()
    {
        if (_camara != null) return _camara;

        _camara = Camera.main;

        if (_camara == null)
        {
            _camara = FindFirstObjectByType<Camera>();

            if (_camara != null)
                Debug.LogWarning("[Construccion] Tu camara no tiene la etiqueta " +
                                 "'MainCamera', asi que use '" + _camara.name + "'. " +
                                 "Ponle la etiqueta cuando puedas: hay mas cosas en " +
                                 "Unity que dependen de ella.");
        }

        return _camara;
    }

    private Vector3 RatonEnElMundo()
    {
        Camera cam = Camara();

        if (cam == null)
        {
            Debug.LogError("[Construccion] No hay ninguna camara en la escena.");
            return Vector3.zero;
        }

        Vector3 p = cam.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }

    /// <summary>
    /// No se puede construir encima de un arbol, una roca, el mar, otro
    /// edificio ni el propio jugador. Se pregunta a la fisica si hay algo
    /// solido en el hueco que ocuparia el edificio.
    ///
    /// useTriggers = false a proposito: las zonas de la "E" de los arboles y
    /// de la tienda son triggers, y si contaran no se podria construir cerca
    /// de nada.
    /// </summary>
    private bool SitioLibre(Vector3 centro)
    {
        Sprite s = _fantasmaSprite != null ? _fantasmaSprite.sprite : null;

        Vector2 tamano = s != null
            ? (Vector2)s.bounds.size * escalaEdificio
            : Vector2.one * escalaEdificio;

        // Solo la base del edificio, igual que su collider definitivo.
        var caja = new Vector2(tamano.x * 0.80f, tamano.y * 0.45f);
        var punto = new Vector2(centro.x, centro.y - tamano.y * 0.25f);

        var filtro = new ContactFilter2D();
        filtro.NoFilter();          // sin capas ni profundidades: todo cuenta
        filtro.useTriggers = false; // ...menos los triggers

        var golpes = new Collider2D[4];
        int n = Physics2D.OverlapBox(punto, caja, 0f, filtro, golpes);

        return n == 0;
    }

    /// <summary>
    /// No se construye a traves de la mochila, el HUD o cualquier otro panel
    /// que este abierto encima del mapa.
    /// </summary>
    private static bool RatonSobreLaUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private bool CercaDelJugador(Vector3 donde)
    {
        if (distanciaMaxima <= 0f) return true;
        if (_jugadorTransform == null) return true;

        return Vector2.Distance(_jugadorTransform.position, donde) <= distanciaMaxima;
    }

    /// <summary>
    /// Busca bajo que objeto colgar los edificios nuevos y de que Sorting
    /// Layer copiarles el dibujo. Sin esto naceria en la capa "Default", que
    /// esta por debajo del mapa, y el edificio quedaria invisible: el mismo
    /// problema que tuvieron los carteles de la "E".
    /// </summary>
    private void PrepararSitio()
    {
        GameObject carpeta = string.IsNullOrEmpty(carpetaEnLaEscena)
            ? null
            : GameObject.Find(carpetaEnLaEscena);

        if (carpeta != null) _carpeta = carpeta.transform;

        SpriteRenderer edificio = null;
        SpriteRenderer jugador = null;

        if (_carpeta != null) edificio = _carpeta.GetComponentInChildren<SpriteRenderer>();

        if (_jugadorTransform != null)
            jugador = _jugadorTransform.GetComponentInChildren<SpriteRenderer>();

        // El edificio nuevo va en la capa de los edificios que ya estan en el
        // mapa, para que se tape con ellos igual de bien.
        SpriteRenderer referencia = edificio != null ? edificio : jugador;

        if (referencia != null)
        {
            _capaEdificios = referencia.sortingLayerID;
            _ordenEdificios = referencia.sortingOrder;
        }
        else
        {
            _capaEdificios = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
            _ordenEdificios = 0;

            Debug.LogWarning("[Construccion] No encontre ningun sprite del que copiar la " +
                             "Sorting Layer. Uso la ultima capa que hay.");
        }

        // El FANTASMA, en cambio, va en la capa del jugador, que es la de mas
        // arriba: mientras se elige el sitio tiene que verse siempre, aunque
        // pase por encima de la montaña o de otro edificio.
        SpriteRenderer arriba = jugador != null ? jugador : referencia;

        _capaFantasma = arriba != null ? arriba.sortingLayerID : _capaEdificios;
        _ordenFantasma = arriba != null ? arriba.sortingOrder : _ordenEdificios;
    }

    // --------------------------------------------------------------------
    //  Construir de verdad
    // --------------------------------------------------------------------

    private void Curar()
    {
        if (_iglesia == null) { Mostrar("Primero construye la Iglesia."); return; }
        if (!_iglesia.Construido) { Mostrar("La Iglesia todavia se esta construyendo."); return; }

        if (_iglesia.Curar(_jugador))
        {
            Mostrar("Te curaste " + _iglesia.CuracionPorUso + " de vida.");
            return;
        }

        double espera = _iglesia.SegundosParaPoderUsar();

        if (espera > 0) Mostrar("La Iglesia estara lista en " + Mathf.CeilToInt((float)espera) + " s.");
        else if (_jugador.Vida >= _jugador.VidaMax) Mostrar("Ya tienes la vida completa.");
        else Mostrar("No alcanza el oro (" + _iglesia.CostoOroPorUso + ").");
    }

    /// <summary>
    /// El evento llega desde el HILO de construccion, que no puede tocar la UI
    /// de Unity. Por eso se guarda el texto y lo escribe Refrescar(), que corre
    /// en el hilo principal.
    /// </summary>
    private volatile string _avisoPendiente;

    private void AlConstruirse(EdificioModel edificio)
    {
        _avisoPendiente = edificio.Nombre + " terminada.";
    }

    private ArmaModel HerramientaDeObra()
    {
        if (_armas == null || _armas.Inventario == null) return null;

        ArmaModel equipada = _armas.Inventario.Equipada;

        return (equipada != null && equipada.Tipo == herramientaDeObra) ? equipada : null;
    }

    // --------------------------------------------------------------------
    //  Refresco
    // --------------------------------------------------------------------

    private void Refrescar()
    {
        if (_jugador == null) return;

        if (_textoRecursos != null)
            _textoRecursos.text = "Oro: " + _jugador.Oro + "     Madera: " + _jugador.Madera;

        ArmaModel martillo = HerramientaDeObra();
        bool puede = !exigirMartillo || martillo != null;

        foreach (Oferta o in _ofertas)
        {
            if (o.Boton == null) continue;

            o.Boton.interactable = puede;
        }

        if (_botonCurar != null)
            _botonCurar.interactable = _iglesia != null && _iglesia.Construido;
    }

    private void Mostrar(string mensaje)
    {
        if (_textoMensaje != null) _textoMensaje.text = mensaje;
    }

    // --------------------------------------------------------------------
    //  Armado del panel
    // --------------------------------------------------------------------

    private void Construir()
    {
        Canvas canvas = BuscarOCrearCanvas();

        _panel = new GameObject("PanelConstruccion", typeof(RectTransform));
        _panel.transform.SetParent(canvas.transform, false);

        var rect = _panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = tamanoPanel;

        var img = _panel.AddComponent<Image>();
        if (fondoPanel != null) { img.sprite = fondoPanel; img.color = Color.white; }
        else img.color = colorPanel;

        Transform p = _panel.transform;
        float ancho = tamanoPanel.x - 90f;

        CrearTexto("Titulo", p, tamanoPanel.x - 60f, 42f, 14f, "CONSTRUCCION", 32f, colorTexto);
        _textoRecursos = CrearTexto("Recursos", p, tamanoPanel.x - 60f, 32f, 58f,
                                    "", 24f, new Color(1f, 0.85f, 0.3f));

        // Los cinco edificios. Cada uno se crea nuevo en cada construccion:
        // un EdificioModel lleva su propio estado y sus hilos, asi que no se
        // puede reutilizar el mismo objeto dos veces.
        Agregar(p, ancho, 100f, "Granja", () => new GranjaModel(), () => spriteGranja);
        Agregar(p, ancho, 168f, "Mina", () => new MinaModel(), () => spriteMina);
        Agregar(p, ancho, 236f, "Aserradero", () => new AserraderoModel(), () => spriteAserradero);
        Agregar(p, ancho, 304f, "Armeria", () => new ArmeriaModel(), () => spriteArmeria);
        Agregar(p, ancho, 372f, "Iglesia", () => new IglesiaModel(), () => spriteIglesia);

        _botonCurar = CrearBoton("BotonCurar", p, ancho, 58f, 448f, "Curarme en la Iglesia");
        _botonCurar.onClick.AddListener(Curar);

        _textoMensaje = CrearTexto("Mensaje", p, ancho, 64f, 516f, "", 21f,
                                   new Color(0.85f, 0.85f, 0.8f));
        _textoMensaje.alignment = TextAlignmentOptions.Top;

        CrearTexto("Ayuda", p, ancho, 24f, 584f,
                   "Elige un edificio y luego haz clic donde lo quieras.  " +
                   "Cierra con " + teclaAbrir, 16f,
                   new Color(0.7f, 0.68f, 0.62f));

        _panel.SetActive(false);
    }

    private void Agregar(Transform padre, float ancho, float y, string nombre,
                         Func<EdificioModel> crear, Func<Sprite> dibujo)
    {
        Button boton = CrearBoton("Boton" + nombre, padre, ancho, 58f, y, nombre);

        // Se crea uno de muestra solo para leerle el costo y el tiempo, que
        // son fijos, y armar la etiqueta de una vez. Este objeto se descarta:
        // no se le arranca ningun hilo porque nunca se llama a Construir.
        EdificioModel muestra = crear();

        var oferta = new Oferta
        {
            Nombre = nombre,
            Crear = crear,
            Dibujo = dibujo,
            Boton = boton,
            Etiqueta = boton.GetComponentInChildren<TMP_Text>(),
            TextoFijo = muestra.Nombre + "   -   " + muestra.CostoOro + " oro, " +
                        muestra.CostoMadera + " madera   (" +
                        Mathf.CeilToInt(muestra.TiempoConstruccionSeg) + " s)"
        };

        if (oferta.Etiqueta != null) oferta.Etiqueta.text = oferta.TextoFijo;

        boton.onClick.AddListener(() => IniciarColocacion(oferta));
        _ofertas.Add(oferta);
    }

    private RectTransform CrearHijo(string nombre, Transform padre, float ancho, float alto, float y)
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

    private TMP_Text CrearTexto(string nombre, Transform padre, float ancho, float alto,
                                float y, string contenido, float tamano, Color color)
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

    private Button CrearBoton(string nombre, Transform padre, float ancho, float alto,
                              float y, string etiqueta)
    {
        RectTransform r = CrearHijo(nombre, padre, ancho, alto, y);

        var img = r.gameObject.AddComponent<Image>();
        img.color = colorBoton;

        var boton = r.gameObject.AddComponent<Button>();
        boton.targetGraphic = img;

        ColorBlock c = boton.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(1f, 0.93f, 0.75f);
        c.pressedColor = new Color(0.8f, 0.72f, 0.55f);
        c.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
        boton.colors = c;

        var textoGO = new GameObject("Texto", typeof(RectTransform));
        textoGO.transform.SetParent(r, false);

        var tr = textoGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12f, 4f);
        tr.offsetMax = new Vector2(-12f, -4f);

        var t = textoGO.AddComponent<TextMeshProUGUI>();
        t.text = etiqueta;
        t.fontSize = 22f;
        t.color = colorTexto;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;   // si no, el texto se come el clic

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

        var go = new GameObject("CanvasConstruccion");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        Debug.LogWarning("[Construccion] No encontre ningun Canvas llamado 'Canvas', " +
                         "asi que cree uno.");
        return canvas;
    }
}
