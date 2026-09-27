using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// VISTA: los avisos cortos que el jugador tiene que ver mientras juega.
/// "No tienes comida", "Necesitas el hacha", "Compraste el pico".
///
/// POR QUE EXISTE
/// Todos esos mensajes existian ya, pero solo en Debug.Log, o sea que solo los
/// veia quien tuviera la consola de Unity abierta. Para el que juega, presionar
/// una tecla y que no pase nada es indistinguible de que el juego este roto.
///
/// NO HAY QUE MONTAR NADA EN UNITY
/// Se crea solo al arrancar el juego, con su propio Canvas por encima de todo
/// y DontDestroyOnLoad, asi que funciona en las cinco escenas sin configurar
/// nada. Cualquier script lo usa asi:
///
///     AvisoPantalla.Mostrar("No tienes comida.");
///
/// Si no existe todavia, la llamada no falla: simplemente no se ve.
/// </summary>
public class AvisoPantalla : MonoBehaviour
{
    private static AvisoPantalla _instancia;

    [SerializeField] private float segundosVisible = 2.5f;
    [SerializeField] private int maximoALaVez = 4;

    private readonly List<TMP_Text> _lineas = new List<TMP_Text>();
    private readonly List<float> _expiran = new List<float>();

    private RectTransform _contenedor;

    /// <summary>
    /// Muestra un aviso. Es estatico para que cualquier script lo llame sin
    /// tener que arrastrar referencias.
    /// </summary>
    public static void Mostrar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return;
        if (_instancia == null) return;

        _instancia.Agregar(texto);
    }

    /// <summary>
    /// Se crea solo, antes de que arranque cualquier escena. Asi no hay un
    /// objeto que se pueda olvidar en un mapa y funcione en los otros.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearSiNoExiste()
    {
        if (_instancia != null) return;

        var go = new GameObject("AvisoPantalla");
        go.AddComponent<AvisoPantalla>();
    }

    private void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        _instancia = this;
        DontDestroyOnLoad(gameObject);

        Construir();
    }

    private void Construir()
    {
        // Canvas propio, con sortingOrder alto para quedar por encima del HUD,
        // de la tienda y del panel de resultado.
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var escalador = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        escalador.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);

        // Sin GraphicRaycaster a proposito: estos avisos no se tocan, y si
        // tuviera uno se comerian los clics de la tienda que hay debajo.

        var cont = new GameObject("Avisos", typeof(RectTransform));
        cont.transform.SetParent(transform, false);

        _contenedor = cont.GetComponent<RectTransform>();
        _contenedor.anchorMin = new Vector2(0.5f, 1f);
        _contenedor.anchorMax = new Vector2(0.5f, 1f);
        _contenedor.pivot = new Vector2(0.5f, 1f);
        _contenedor.anchoredPosition = new Vector2(0f, -120f);
        _contenedor.sizeDelta = new Vector2(1000f, 200f);

        var grupo = cont.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        grupo.childAlignment = TextAnchor.UpperCenter;
        grupo.spacing = 4f;
        grupo.childForceExpandHeight = false;
        grupo.childControlHeight = true;
        grupo.childControlWidth = true;
    }

    private void Agregar(string texto)
    {
        // Si ya hay demasiados, se va el mas viejo.
        while (_lineas.Count >= maximoALaVez)
            Quitar(0);

        var go = new GameObject("Aviso", typeof(RectTransform));
        go.transform.SetParent(_contenedor, false);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = 30f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1f, 0.95f, 0.8f);
        t.raycastTarget = false;

        // Contorno para que se lea sobre el pasto claro y sobre la roca oscura.
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(0, 0, 0, 255);

        _lineas.Add(t);
        _expiran.Add(Time.unscaledTime + segundosVisible);
    }

    private void Update()
    {
        // unscaledTime y no time: cuando sale el panel de resultado el juego se
        // pausa con timeScale = 0, y con Time.time los avisos se quedarian
        // congelados en pantalla para siempre.
        for (int i = _lineas.Count - 1; i >= 0; i--)
        {
            if (_lineas[i] == null) { Quitar(i); continue; }

            float queda = _expiran[i] - Time.unscaledTime;

            if (queda <= 0f) { Quitar(i); continue; }

            // Medio segundo final desvaneciendose.
            if (queda < 0.5f)
            {
                Color c = _lineas[i].color;
                _lineas[i].color = new Color(c.r, c.g, c.b, queda / 0.5f);
            }
        }
    }

    private void Quitar(int i)
    {
        if (i < 0 || i >= _lineas.Count) return;

        if (_lineas[i] != null) Destroy(_lineas[i].gameObject);

        _lineas.RemoveAt(i);
        _expiran.RemoveAt(i);
    }
}
