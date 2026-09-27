using System;
using System.Collections.Generic;
using ImperiosEnGuerra.Controlador;
using ImperiosEnGuerra.Modelo;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// MapaMundialView: VISTA del mapa mundial (escena "Juego").
//
// COMO FUNCIONA:
//   En pantalla solo se ve un boton "Atacar". Al presionarlo se abre un panel
//   (el pergamino) con los cuatro territorios. Al elegir uno, aparece el texto
//   "Territorio seleccionado: Roma" y se habilita el boton de confirmar, que
//   es el que carga la escena de ese territorio.
//
// COMO USARLO EN UNITY (escena Juego):
// 1. Boton Atacar suelto en el Canvas -> campo "Boton Abrir Mapa".
// 2. Un panel (Image con el pergamino) DESACTIVADO -> campo "Panel Territorios".
//    Dentro del panel van:
//      - los cuatro botones de territorio
//      - un TMP_Text para el mensaje (dejalo VACIO en el editor, sin "New Text")
//      - un boton de confirmar        -> campo "Boton Atacar"
//      - opcionalmente un boton cerrar -> campo "Boton Cerrar Mapa"
// 3. Create Empty "MapaMundialView" con este script y conecta todo.
// 4. En "Territorios" pon Size 4 y escribe los nombres EXACTOS
//    (Roma, Persia, Egipto, Vikingos) arrastrando cada boton.
//    Cada nombre debe coincidir con el de su escena en Build Settings.
public class MapaMundialView : MonoBehaviour
{
    [Serializable]
    public class BotonTerritorio
    {
        public string imperio;
        public Button boton;
    }

    [Header("Abrir y cerrar el mapa")]
    [Tooltip("El boton que se ve en pantalla y abre el panel de territorios.")]
    [SerializeField] private Button botonAbrirMapa;

    [Tooltip("El panel con los territorios. Dejalo DESACTIVADO en la escena.")]
    [SerializeField] private GameObject panelTerritorios;

    [Tooltip("Opcional: un boton dentro del panel para cerrarlo sin atacar.")]
    [SerializeField] private Button botonCerrarMapa;

    [Header("Dentro del panel")]
    [SerializeField] private List<BotonTerritorio> territorios = new List<BotonTerritorio>();

    [Tooltip("El boton que confirma el ataque y carga la escena.")]
    [SerializeField] private Button botonAtacar;

    [Tooltip("Donde se escribe 'Territorio seleccionado: ...'. Dejalo vacio en el editor.")]
    [SerializeField] private TMP_Text textoMensaje;

    private MapaMundialController _controlador;
    private PartidaModel _partida;
    private bool _hayTerritorioElegido;

    private void Start()
    {
        if (PlayerSelectionManager.Instance != null)
            _partida = PlayerSelectionManager.Instance.Partida;

        if (_partida == null)
        {
            Debug.LogWarning("[MapaMundialView] No hay partida activa.");
            return;
        }

        _controlador = new MapaMundialController(_partida);

        _controlador.OnTerritorioSeleccionado += t =>
        {
            _hayTerritorioElegido = true;
            Mostrar("Territorio seleccionado: " + t.Nombre);
            RefrescarBotonAtacar();
        };

        _controlador.OnAccionInvalida += Mostrar;

        // La escena de cada territorio se llama igual que el territorio.
        _controlador.OnBatallaIniciada += t => SceneManager.LoadScene(t.Nombre);

        _controlador.OnJuegoGanado += () => Mostrar("Grecia conquisto todos los territorios!");

        foreach (var entrada in territorios)
        {
            if (entrada.boton == null) continue;
            string imperio = entrada.imperio; // copia local: sin esto todos los
                                              // botones usarian el ultimo nombre
            entrada.boton.onClick.AddListener(() => _controlador.SeleccionarTerritorio(imperio));
        }

        if (botonAtacar != null)
            botonAtacar.onClick.AddListener(_controlador.ConfirmarAtaque);

        if (botonAbrirMapa != null)
            botonAbrirMapa.onClick.AddListener(AbrirMapa);

        if (botonCerrarMapa != null)
            botonCerrarMapa.onClick.AddListener(CerrarMapa);

        CerrarMapa();
    }

    /// <summary>Abre el panel de territorios.</summary>
    public void AbrirMapa()
    {
        if (panelTerritorios != null)
            panelTerritorios.SetActive(true);

        // Se empieza limpio cada vez que se abre: sin territorio elegido,
        // sin mensaje viejo, y con los ya conquistados deshabilitados.
        _hayTerritorioElegido = false;
        Mostrar("");
        ActualizarBotones();
        RefrescarBotonAtacar();
    }

    public void CerrarMapa()
    {
        if (panelTerritorios != null)
            panelTerritorios.SetActive(false);
    }

    /// <summary>
    /// El boton de confirmar solo se puede pulsar si ya se eligio territorio.
    /// Asi el jugador no le da a "atacar" y recibe un regaño.
    /// </summary>
    private void RefrescarBotonAtacar()
    {
        if (botonAtacar != null)
            botonAtacar.interactable = _hayTerritorioElegido;
    }

    /// <summary>
    /// Apaga los territorios que ya se conquistaron. Se llama al abrir el
    /// panel, asi que al volver de una batalla ganada aparece actualizado.
    /// </summary>
    private void ActualizarBotones()
    {
        if (_controlador == null) return;

        var lista = _controlador.ObtenerTerritorios();

        foreach (var entrada in territorios)
        {
            if (entrada.boton == null) continue;

            var t = lista.Find(x => x.Nombre == entrada.imperio);
            entrada.boton.interactable = t != null && !t.EsBase && !t.EsConquistado;
        }
    }

    private void Mostrar(string mensaje)
    {
        if (textoMensaje != null) textoMensaje.text = mensaje;

        if (!string.IsNullOrEmpty(mensaje))
            Debug.Log("[Mapa] " + mensaje);
    }
}
