using ImperiosEnGuerra.Modelo;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// VISTA: los dos botones de la esquina de abajo a la derecha mientras se
/// juega.
///
///   GUARDAR       -> escribe la partida en la primera ranura libre.
///   SALIR AL MENU -> guarda y vuelve al menu principal.
///
/// Se construye entera por codigo y se crea sola al arrancar el juego, igual
/// que AvisoPantalla: no hay nada que montar ni que arrastrar en Unity, y
/// funciona en los cinco mapas sin tener que acordarse de ponerla en cada uno.
///
/// Solo aparece cuando de verdad hay una partida en curso: en el Menu y en la
/// pantalla de seleccion de nombre se esconde sola.
/// </summary>
public class BotonesPartidaView : MonoBehaviour
{
    private static BotonesPartidaView _instancia;

    /// <summary>Escenas donde NO se muestran los botones.</summary>
    private static readonly string[] ESCENAS_SIN_JUEGO = { "Menu", "SeleccionJugador", "jugador" };

    private const string ESCENA_MENU = "Menu";

    private GameObject _barra;
    private TMP_Text _textoGuardar;
    private float _ocultarTextoEn;

    // ────────────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearSiNoExiste()
    {
        if (_instancia != null) return;

        var go = new GameObject("BotonesPartida");
        go.AddComponent<BotonesPartidaView>();
    }

    private void Awake()
    {
        if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

        _instancia = this;
        DontDestroyOnLoad(gameObject);

        Construir();

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (_instancia == this) SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void Start()
    {
        Revisar();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        Revisar();
    }

    /// <summary>
    /// Los botones aparecen solo si hay partida y estamos en un mapa. Se mira
    /// en cada frame y no solo al cambiar de escena porque la partida se crea
    /// despues de que la escena cargue.
    /// </summary>
    private void Update()
    {
        Revisar();

        if (_textoGuardar != null && _ocultarTextoEn > 0f &&
            Time.unscaledTime > _ocultarTextoEn)
        {
            _textoGuardar.text = "Guardar";
            _ocultarTextoEn = 0f;
        }
    }

    private void Revisar()
    {
        if (_barra == null) return;

        bool debeVerse = HayPartida() && EsEscenaDeJuego();

        if (_barra.activeSelf != debeVerse) _barra.SetActive(debeVerse);
    }

    private static bool HayPartida()
    {
        return PlayerSelectionManager.Instance != null &&
               PlayerSelectionManager.Instance.Partida != null;
    }

    private static bool EsEscenaDeJuego()
    {
        string actual = SceneManager.GetActiveScene().name;

        foreach (string s in ESCENAS_SIN_JUEGO)
            if (actual == s) return false;

        return true;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Los dos botones
    // ────────────────────────────────────────────────────────────────────

    private void Guardar()
    {
        int ranura = PartidaGuardadaController.Guardar();

        if (_textoGuardar != null)
        {
            _textoGuardar.text = ranura > 0 ? "Guardado" : "No se pudo";
            _ocultarTextoEn = Time.unscaledTime + 2f;
        }
    }

    /// <summary>
    /// Guarda antes de salir. Es lo que espera cualquiera que le da a "salir"
    /// en un juego, y evita el clasico de perder media hora por un clic.
    /// </summary>
    private void SalirAlMenu()
    {
        PartidaGuardadaController.Guardar();

        // Sin esto la partida se queda en marcha: el tiempo vuelve a correr
        // solo si alguien lo devuelve, y el panel de derrota lo deja en 0.
        Time.timeScale = 1f;

        SceneManager.LoadScene(ESCENA_MENU);
    }

    // ────────────────────────────────────────────────────────────────────
    //  Armado
    // ────────────────────────────────────────────────────────────────────

    private void Construir()
    {
        // Canvas propio, por debajo de los avisos (500) pero por encima del
        // HUD, para que los botones no queden tapados por nada.
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        var escalador = gameObject.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);

        // Este SI necesita raycaster: son botones, hay que poder pulsarlos.
        gameObject.AddComponent<GraphicRaycaster>();

        _barra = new GameObject("Barra", typeof(RectTransform));
        _barra.transform.SetParent(transform, false);

        // Abajo a la derecha. Arriba estorbaban: ahi es donde el HUD pone el
        // oro y la madera, y los botones los tapaban.
        var rect = _barra.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 24f);
        rect.sizeDelta = new Vector2(320f, 56f);

        Button guardar = CrearBoton("BotonGuardar", _barra.transform, "Guardar",
                                    new Vector2(-170f, 0f), 140f,
                                    new Color(0.20f, 0.35f, 0.22f, 0.95f));
        guardar.onClick.AddListener(Guardar);
        _textoGuardar = guardar.GetComponentInChildren<TMP_Text>();

        Button salir = CrearBoton("BotonSalir", _barra.transform, "Salir al menu",
                                  new Vector2(0f, 0f), 165f,
                                  new Color(0.38f, 0.20f, 0.18f, 0.95f));
        salir.onClick.AddListener(SalirAlMenu);

        _barra.SetActive(false);
    }

    private Button CrearBoton(string nombre, Transform padre, string etiqueta,
                              Vector2 posicion, float ancho, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(1f, 0.5f);
        r.anchorMax = new Vector2(1f, 0.5f);
        r.pivot = new Vector2(1f, 0.5f);
        r.sizeDelta = new Vector2(ancho, 48f);
        r.anchoredPosition = posicion;

        var img = go.AddComponent<Image>();
        img.color = color;

        var boton = go.AddComponent<Button>();
        boton.targetGraphic = img;

        ColorBlock c = boton.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(1f, 0.95f, 0.8f);
        c.pressedColor = new Color(0.75f, 0.7f, 0.6f);
        boton.colors = c;

        var textoGO = new GameObject("Texto", typeof(RectTransform));
        textoGO.transform.SetParent(go.transform, false);

        var tr = textoGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(6f, 3f);
        tr.offsetMax = new Vector2(-6f, -3f);

        var t = textoGO.AddComponent<TextMeshProUGUI>();
        t.text = etiqueta;
        t.fontSize = 22f;
        t.color = new Color(1f, 0.96f, 0.88f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;   // si no, el texto se come el clic

        return boton;
    }
}
