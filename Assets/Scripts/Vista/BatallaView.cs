using ImperiosEnGuerra.Modelo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// BatallaView: VISTA de la batalla (escenas Egipto, Persia, Roma, Vikingos).
// Conecta los botones de ataque con PartidaModel y muestra las vidas en pantalla.
//
// COMO USARLO EN UNITY:
// 1. Crea dos botones: "Atacar" y "Atacar centro urbano".
// 2. Crea tres TMP_Text: vida del jugador, vida del enemigo y vida del centro urbano.
// 3. Crea un GameObject vacio "BatallaView" y arrastra este script.
// 4. Conecta los botones y textos en el Inspector.
public class BatallaView : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button botonAtacar;
    [SerializeField] private Button botonAtacarCentroUrbano;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoVidaJugador;
    [SerializeField] private TMP_Text textoVidaEnemigo;
    [SerializeField] private TMP_Text textoCentroUrbano;

    private PartidaModel _partida;

    private void Start()
    {
        if (PlayerSelectionManager.Instance != null)
            _partida = PlayerSelectionManager.Instance.Partida;

        if (_partida == null)
        {
            Debug.LogWarning("[BatallaView] No hay partida activa.");
            return;
        }

        // Los clics llaman al MODELO: el hilo del jugador se encarga del resto
        if (botonAtacar != null)
            botonAtacar.onClick.AddListener(_partida.JugadorAtaca);

        if (botonAtacarCentroUrbano != null)
            botonAtacarCentroUrbano.onClick.AddListener(_partida.JugadorAtacaCentroUrbano);
    }

    // Los hilos del modelo cambian las vidas; la Vista las lee cada frame
    // (los getters del modelo son seguros entre hilos).
    private void Update()
    {
        if (_partida == null) return;

        var jugador = _partida.Jugador;
        var ia = _partida.IA;

        if (textoVidaJugador != null)
            textoVidaJugador.text = jugador.Nombre + ": " + jugador.Vida + "/" + jugador.VidaMax;

        if (ia != null)
        {
            if (textoVidaEnemigo != null)
                textoVidaEnemigo.text = ia.Nombre + ": " + ia.Vida + "/" + ia.VidaMax;

            var centro = ia.EdificioPrincipal;
            if (textoCentroUrbano != null && centro != null)
                textoCentroUrbano.text = centro.Nombre + ": " + centro.Vida + "/" + centro.VidaMax;
        }

        // Al terminar la batalla ya no tiene sentido atacar
        bool enBatalla = _partida.Estado == EstadoPartida.EnBatalla;
        if (botonAtacar != null) botonAtacar.interactable = enBatalla;
        if (botonAtacarCentroUrbano != null) botonAtacarCentroUrbano.interactable = enBatalla;
    }
}
