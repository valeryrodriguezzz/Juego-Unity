using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ImperiosEnGuerra.Modelo;

// GanadorView: muestra el resultado al terminar una batalla y devuelve al
// jugador a donde corresponda.
//
// DONDE VA: en CADA escena de batalla (Egipto, Persia, Roma, Vikingos).
// No en la escena Juego: la batalla termina mientras esa escena no esta
// cargada, asi que alli nadie escucharia el evento.
//
// COMO USARLO EN UNITY (en cada escena de territorio):
// 1. En el Canvas crea un Panel y DESACTIVALO.
// 2. Dentro del panel: un TMP_Text para el titulo, otro para el ganador,
//    otro para el detalle, y un Button para continuar.
// 3. Crea un GameObject vacio "GanadorManager" y arrastrale este script.
// 4. Conecta el panel, los textos y el boton en el Inspector.
//
// A DONDE VUELVE EL BOTON:
//   - Ganaste la batalla y quedan territorios  -> al mapa (escena Juego)
//   - Ganaste el ultimo territorio, o perdiste -> al menu
// PartidaModel ya marco el territorio como conquistado al terminar, asi que
// al volver al mapa aparece bloqueado sin que nadie tenga que hacer nada.
public class GanadorView : MonoBehaviour
{
    [Header("Panel completo de resultado")]
    [SerializeField] private GameObject panelResultado;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoTitulo;       // "!Ganaste!" o "Perdiste..."
    [SerializeField] private TMP_Text textoGanador;      // "Grecia conquisto el territorio"
    [SerializeField] private TMP_Text textoDetalle;      // Duracion de la batalla

    [Header("Boton de continuar")]
    [SerializeField] private Button botonContinuar;

    [Tooltip("Texto del boton. Se cambia solo segun a donde vaya a llevar.")]
    [SerializeField] private TMP_Text textoBotonContinuar;

    [Header("Nombres de las escenas")]
    [SerializeField] private string escenaMapa = "Juego";
    [SerializeField] private string escenaMenu = "Menu";

    private PartidaModel _partida;
    private bool _volverAlMapa;

    private void Start()
    {
        if (panelResultado != null)
            panelResultado.SetActive(false);

        if (botonContinuar != null)
            botonContinuar.onClick.AddListener(Continuar);

        if (PlayerSelectionManager.Instance != null)
        {
            _partida = PlayerSelectionManager.Instance.Partida;

            if (_partida != null)
                _partida.OnBatallaTerminada += AlTerminarBatalla;
            else
                Debug.LogWarning("[Ganador] No hay partida activa.");
        }
    }

    // OJO: este evento llega desde el HILO de combate que termino la batalla,
    // no desde el hilo principal de Unity. Por eso el trabajo que toca la UI
    // se encola en el MainThreadDispatcher en vez de hacerse aqui mismo.
    private void AlTerminarBatalla(bool jugadorGano)
    {
        string ganador = _partida.NombreGanador;
        string duracion = _partida.DuracionUltimaBatalla.ToString(@"mm\:ss");

        // Si la partida sigue viva, el jugador vuelve al mapa a por el
        // siguiente territorio. Si perdio o ya conquisto todo, se acabo.
        bool sigueLaPartida = _partida.Estado != EstadoPartida.Terminada;

        MainThreadDispatcher.Encolar(() =>
        {
            _volverAlMapa = jugadorGano && sigueLaPartida;
            MostrarResultado(ganador, jugadorGano, duracion);
        });
    }

    private void OnDestroy()
    {
        if (_partida != null)
            _partida.OnBatallaTerminada -= AlTerminarBatalla;

        // Por si se sale de la escena con el juego pausado.
        Time.timeScale = 1f;
    }

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

        if (textoBotonContinuar != null)
            textoBotonContinuar.text = _volverAlMapa ? "Volver al mapa" : "Volver al menu";

        // Se pausa para que alcance a leerse el resultado.
        Time.timeScale = 0f;
    }

    private void Continuar()
    {
        Time.timeScale = 1f; // sin esto la escena siguiente arranca congelada

        SceneManager.LoadScene(_volverAlMapa ? escenaMapa : escenaMenu);
    }
}
