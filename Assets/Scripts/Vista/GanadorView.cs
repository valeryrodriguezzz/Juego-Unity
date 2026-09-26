using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ImperiosEnGuerra.Modelo;

// GanadorView: muestra el resultado final (victoria o derrota) al terminar la partida.
//
// COMO USARLO EN UNITY:
// 1. En la escena Juego, crea un Panel de UI y desactivalo (SetActive false).
// 2. Dentro del panel agrega:
//    - Un TMP_Text para el mensaje principal ("Ganaste!" o "Perdiste!")
//    - Un TMP_Text para el nombre del ganador
//    - Un Button "Volver al Menu"
// 3. Crea un GameObject vacio llamado "GanadorManager" y arrastra este script.
// 4. Conecta el panel y los componentes en el Inspector.
// 5. Llama MostrarResultado() desde el Controlador cuando termina la partida.
public class GanadorView : MonoBehaviour
{
    [Header("Panel completo de resultado")]
    [SerializeField] private GameObject panelResultado;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoTitulo;       // "!Ganaste!" o "Perdiste..."
    [SerializeField] private TMP_Text textoGanador;      // "Grecia conquisto el mundo"
    [SerializeField] private TMP_Text textoDetalle;      // Detalle adicional opcional

    [Header("Botones")]
    [SerializeField] private Button botonVolverMenu;

    [Header("Nombre de la escena del menu")]
    [SerializeField] private string escenaMenu = "Menu";

    private void Start()
    {
        // El panel empieza oculto
        if (panelResultado != null)
            panelResultado.SetActive(false);

        if (botonVolverMenu != null)
            botonVolverMenu.onClick.AddListener(VolverAlMenu);

        // Anuncia el resultado apenas termina la batalla (el evento llega desde otro hilo)
        if (PlayerSelectionManager.Instance != null)
        {
            _partida = PlayerSelectionManager.Instance.Partida;
            if (_partida != null)
                _partida.OnBatallaTerminada += AlTerminarBatalla;
        }
    }

    private PartidaModel _partida;

    private void AlTerminarBatalla(bool jugadorGano)
    {
        string ganador = _partida.NombreGanador;
        string duracion = _partida.DuracionUltimaBatalla.ToString(@"mm\:ss");
        MainThreadDispatcher.Encolar(() => MostrarResultado(ganador, jugadorGano, duracion));
    }

    private void OnDestroy()
    {
        if (_partida != null)
            _partida.OnBatallaTerminada -= AlTerminarBatalla;
    }

    // Llamar este metodo desde el Controlador cuando la partida termina.
    public void MostrarResultado(string nombreGanador, bool jugadorGano, string duracion = "")
    {
        if (panelResultado != null)
            panelResultado.SetActive(true);

        if (textoTitulo != null)
            textoTitulo.text = jugadorGano ? "VICTORIA!" : "Derrota...";

        if (textoGanador != null)
            textoGanador.text = jugadorGano
                ? "Grecia ha conquistado el territorio!"
                : "El Imperio " + nombreGanador + " ha ganado.";

        if (textoDetalle != null && !string.IsNullOrEmpty(duracion))
            textoDetalle.text = "Duracion: " + duracion;

        // El archivo resultado_final.txt lo escribe PlayerSelectionManager al terminar la batalla.

        // Pausar el juego para que el jugador pueda leer el resultado
        Time.timeScale = 0f;
    }

    private void VolverAlMenu()
    {
        Time.timeScale = 1f;  // Restaurar el tiempo antes de cambiar escena
        SceneManager.LoadScene(escenaMenu);
    }
}
