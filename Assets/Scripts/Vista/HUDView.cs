using ImperiosEnGuerra.Modelo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUDView: muestra en pantalla los recursos, la vida y el hambre del jugador.
//
// COMO USARLO EN UNITY:
// 1. En la escena Juego, Canvas (Screen Space - Overlay) con los textos y barras.
// 2. GameObject vacio llamado "HUD" con este script.
// 3. Conecta en el Inspector solo lo que tengas: TODOS los campos son
//    opcionales, lo que dejes vacio simplemente no se pinta.
//
// COMO HACER UNA BARRA:
//    Clic derecho en el Canvas -> UI -> Slider.
//    Al Slider: quitale el hijo "Handle Slide Area" (no es para arrastrar),
//    Interactable DESMARCADO, Min Value 0, Max Value 1.
//    El color de la barra se cambia en Fill Area > Fill > Image > Color:
//    rojo para la vida, naranja para el hambre.
//
// ACTUALIZACION:
// Se refresca en Update() porque los valores los cambian HILOS del Modelo
// (el hambre bajando, la IA atacando, los nodos regenerando). Leer cada frame
// es la forma mas simple y segura: las propiedades del Modelo ya son
// thread-safe, asi que aqui nunca se lee un valor a medio escribir.
public class HUDView : MonoBehaviour
{
    [Header("Textos de recursos")]
    [SerializeField] private TMP_Text textoOro;
    [SerializeField] private TMP_Text textoMadera;
    [SerializeField] private TMP_Text textoComida;
    [SerializeField] private TMP_Text textoArmas;

    [Header("Vida")]
    [SerializeField] private Slider barraVida;
    [SerializeField] private TMP_Text textoVida;

    [Header("Hambre")]
    [Tooltip("Arrastra el GameObject Jugador (el que tiene HambreController). " +
             "Si lo dejas vacio se busca por el tag Player.")]
    [SerializeField] private HambreController hambre;
    [SerializeField] private Slider barraHambre;
    [SerializeField] private TMP_Text textoHambre;

    [Tooltip("La barra se pone de este color cuando el hambre esta por acabarse.")]
    [SerializeField] private Color colorHambreCritica = Color.red;
    [SerializeField] private Color colorHambreNormal = new Color(1f, 0.6f, 0.1f);

    [Tooltip("Por debajo de este porcentaje la barra cambia de color.")]
    [Range(0f, 1f)]
    [SerializeField] private float umbralHambreCritica = 0.25f;

    private JugadorModel _jugador;
    private Image _rellenoHambre;

    private void Start()
    {
        if (PlayerSelectionManager.Instance != null && PlayerSelectionManager.Instance.Partida != null)
        {
            _jugador = PlayerSelectionManager.Instance.Partida.Jugador;
        }
        else
        {
            Debug.LogWarning("[HUD] No hay partida activa. El HUD no mostrara datos.");
            return;
        }

        // Si no lo arrastraron, se busca el HambreController en el jugador.
        if (hambre == null)
        {
            GameObject jugadorGO = GameObject.FindGameObjectWithTag("Player");
            if (jugadorGO != null) hambre = jugadorGO.GetComponent<HambreController>();
        }

        // Se guarda el Image del relleno para poder cambiarle el color.
        if (barraHambre != null && barraHambre.fillRect != null)
            _rellenoHambre = barraHambre.fillRect.GetComponent<Image>();

        PrepararBarra(barraVida);
        PrepararBarra(barraHambre);

        Actualizar();
    }

    // Los Sliders se usan como barras de progreso, no como controles.
    private void PrepararBarra(Slider barra)
    {
        if (barra == null) return;

        barra.minValue = 0f;
        barra.maxValue = 1f;
        barra.interactable = false;
    }

    private void Update()
    {
        Actualizar();
    }

    private void Actualizar()
    {
        if (_jugador == null) return;

        // --- Recursos ---
        if (textoOro != null) textoOro.text = "Oro: " + _jugador.Oro;
        if (textoMadera != null) textoMadera.text = "Madera: " + _jugador.Madera;
        if (textoComida != null) textoComida.text = "Comida: " + _jugador.Comida;
        if (textoArmas != null) textoArmas.text = "Armas: " + _jugador.Armas;

        // --- Vida ---
        int vida = _jugador.Vida;
        int vidaMax = _jugador.VidaMax;

        if (textoVida != null)
            textoVida.text = vida + "/" + vidaMax;

        if (barraVida != null)
            barraVida.value = vidaMax > 0 ? (float)vida / vidaMax : 0f;

        // --- Hambre ---
        if (hambre == null || hambre.Hambre == null) return;

        float porcentaje = hambre.Hambre.Porcentaje;

        if (barraHambre != null)
            barraHambre.value = porcentaje;

        if (textoHambre != null)
            textoHambre.text = hambre.Hambre.Nivel + "/" + hambre.Hambre.NivelMaximo;

        if (_rellenoHambre != null)
            _rellenoHambre.color = porcentaje <= umbralHambreCritica
                ? colorHambreCritica
                : colorHambreNormal;
    }
}
