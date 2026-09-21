using System;
using System.Collections.Concurrent;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CONTROLADOR (MVC): une la UI de la tienda con el Modelo de armas.
/// El Modelo no sabe que existe Unity; este script es el traductor.
///
/// El inventario NO se crea aqui: pertenece al jugador y vive en
/// JugadorArmasController. La tienda solo se lo pide prestado.
///
/// REGLA DE ORO DE UNITY + HILOS:
/// los hilos del Modelo (mantenimiento del arma, reabastecimiento de la tienda)
/// NO pueden tocar la API de Unity: nada de text.text ni Debug.Log desde ellos,
/// o Unity lanza excepciones o se cuelga. Por eso los eventos que llegan de
/// otros hilos se meten en una cola (ConcurrentQueue) y se procesan en Update(),
/// que si corre en el hilo principal.
///
/// Colocalo en un GameObject vacio llamado "TiendaManager" en la escena Juego.
/// </summary>
public class TiendaArmasController : MonoBehaviour
{
    [Header("Jugador")]
    [Tooltip("Arrastra el GameObject Jugador. Si lo dejas vacio se busca por el tag Player.")]
    [SerializeField] private JugadorArmasController jugadorArmas;

    [Header("Panel de la tienda")]
    [SerializeField] private GameObject panelTienda;
    [SerializeField] private Button botonCerrar;

    [Header("Botones de compra (uno por arma comprable)")]
    [SerializeField] private Button botonHacha;
    [SerializeField] private Button botonPico;
    [SerializeField] private Button botonCuchillo;
    [SerializeField] private Button botonMartillo;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoOro;
    [SerializeField] private TMP_Text textoMensaje;
    [SerializeField] private TMP_Text textoArmaEquipada;
    [SerializeField] private Slider barraDurabilidad;

    [Header("Configuracion de la tienda")]
    [SerializeField] private int stockMaximoPorArma = 3;
    [SerializeField] private int msEntreReabastecimientos = 8000;

    // --- Modelo ---
    private JugadorModel _jugador;
    private InventarioArmasModel _inventario;
    private TiendaArmasModel _tienda;

    // Puente entre los hilos del Modelo y el hilo principal de Unity.
    private readonly ConcurrentQueue<Action> _pendientesUI = new ConcurrentQueue<Action>();
    private bool _refrescarPendiente;

    private void Start()
    {
        // 1) El jugador y su arsenal.
        if (jugadorArmas == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) jugadorArmas = go.GetComponent<JugadorArmasController>();
        }

        if (jugadorArmas == null)
        {
            Debug.LogError("[Tienda] No encontre el JugadorArmasController. Arrastralo en el Inspector.");
            enabled = false;
            return;
        }

        _inventario = jugadorArmas.Inventario;

        if (PlayerSelectionManager.Instance == null || PlayerSelectionManager.Instance.Partida == null)
        {
            Debug.LogWarning("[Tienda] No hay partida. Probando la escena Juego directamente?");
            enabled = false;
            return;
        }

        _jugador = PlayerSelectionManager.Instance.Partida.Jugador;

        // 2) La tienda, con su hilo de reabastecimiento.
        _tienda = new TiendaArmasModel(stockMaximoPorArma, msEntreReabastecimientos);
        _tienda.IniciarReabastecimiento();

        // 3) Suscribirse al Modelo. OJO: estos handlers pueden ejecutarse en
        //    OTRO hilo, por eso solo encolan trabajo.
        _tienda.StockCambio += AlCambiarStock;
        _inventario.ArmaEquipadaCambio += AlCambiarArma;
        _inventario.Equipada.DurabilidadCambio += AlCambiarDurabilidad;

        // 4) Cablear botones.
        if (botonHacha != null) botonHacha.onClick.AddListener(() => Comprar(TipoArma.Hacha));
        if (botonPico != null) botonPico.onClick.AddListener(() => Comprar(TipoArma.Pico));
        if (botonCuchillo != null) botonCuchillo.onClick.AddListener(() => Comprar(TipoArma.Cuchillo));
        if (botonMartillo != null) botonMartillo.onClick.AddListener(() => Comprar(TipoArma.Martillo));
        if (botonCerrar != null) botonCerrar.onClick.AddListener(CerrarTienda);

        // 5) Si el personaje no es el comprador, la tienda ni se le ofrece.
        if (!_inventario.PuedeComprar)
            DeshabilitarBotonesDeCompra();

        CerrarTienda();
        RefrescarUI();
    }

    private void Update()
    {
        // Drenar lo que dejaron los hilos del Modelo. Esto corre en el hilo
        // principal, asi que aqui SI se puede tocar la UI.
        while (_pendientesUI.TryDequeue(out Action accion))
            accion();

        if (_refrescarPendiente)
        {
            _refrescarPendiente = false;
            RefrescarUI();
        }
    }

    // ------------------------------------------------------------------
    //  Eventos que llegan desde otros hilos -> solo encolan
    // ------------------------------------------------------------------

    private void AlCambiarStock(TipoArma tipo, int cantidad)
    {
        _pendientesUI.Enqueue(() => _refrescarPendiente = true);
    }

    private void AlCambiarDurabilidad(ArmaModel arma)
    {
        _pendientesUI.Enqueue(() => _refrescarPendiente = true);
    }

    private void AlCambiarArma(ArmaModel nueva)
    {
        _pendientesUI.Enqueue(() =>
        {
            // Reenganchar el evento de durabilidad a la nueva arma equipada.
            nueva.DurabilidadCambio -= AlCambiarDurabilidad;
            nueva.DurabilidadCambio += AlCambiarDurabilidad;
            _refrescarPendiente = true;
        });
    }

    // ------------------------------------------------------------------
    //  Acciones de la UI
    // ------------------------------------------------------------------

    /// <summary>Llamalo desde el trigger del edificio tienda en el mapa.</summary>
    public void AbrirTienda()
    {
        if (panelTienda != null) panelTienda.SetActive(true);
        RefrescarUI();
    }

    public void CerrarTienda()
    {
        if (panelTienda != null) panelTienda.SetActive(false);
    }

    private void Comprar(TipoArma tipo)
    {
        ResultadoCompra resultado = _tienda.Comprar(_jugador, _inventario, tipo);

        if (resultado == ResultadoCompra.Exito)
        {
            // Al comprarla se equipa de una vez: esto apaga el hilo del arma
            // anterior y prende el de la nueva.
            _inventario.Equipar(tipo);
        }

        MostrarMensaje(TiendaArmasModel.Mensaje(resultado, tipo));
        RefrescarUI();
    }

    private void MostrarMensaje(string texto)
    {
        if (textoMensaje != null) textoMensaje.text = texto;
    }

    private void DeshabilitarBotonesDeCompra()
    {
        if (botonHacha != null) botonHacha.interactable = false;
        if (botonPico != null) botonPico.interactable = false;
        if (botonCuchillo != null) botonCuchillo.interactable = false;
        if (botonMartillo != null) botonMartillo.interactable = false;

        MostrarMensaje("Tu personaje ya tiene su arma y no puede cambiarla.");
    }

    private void RefrescarUI()
    {
        if (_inventario == null) return;

        ArmaModel equipada = _inventario.Equipada;

        if (textoArmaEquipada != null)
            textoArmaEquipada.text = equipada.Nombre;

        if (barraDurabilidad != null)
            barraDurabilidad.value = equipada.PorcentajeDurabilidad;

        if (textoOro != null && _jugador != null)
            textoOro.text = _jugador.Oro.ToString(); // ajusta si tu propiedad se llama distinto

        ActualizarBoton(botonHacha, TipoArma.Hacha);
        ActualizarBoton(botonPico, TipoArma.Pico);
        ActualizarBoton(botonCuchillo, TipoArma.Cuchillo);
        ActualizarBoton(botonMartillo, TipoArma.Martillo);
    }

    private void ActualizarBoton(Button boton, TipoArma tipo)
    {
        if (boton == null) return;

        boton.interactable = _inventario.PuedeAdquirir(tipo) && _tienda.StockDe(tipo) > 0;

        TMP_Text etiqueta = boton.GetComponentInChildren<TMP_Text>();
        if (etiqueta != null)
        {
            etiqueta.text = _inventario.Posee(tipo)
                ? CatalogoArmas.Nombre(tipo) + " (ya la tienes)"
                : CatalogoArmas.Nombre(tipo) + " - " + CatalogoArmas.Precio(tipo) + " oro  x" + _tienda.StockDe(tipo);
        }
    }

    // ------------------------------------------------------------------
    //  Apagar el hilo de la tienda. Los hilos de las armas los apaga
    //  JugadorArmasController, que es su dueño.
    // ------------------------------------------------------------------

    private void OnDestroy()
    {
        ApagarHilos();
    }

    private void OnApplicationQuit()
    {
        ApagarHilos();
    }

    private void ApagarHilos()
    {
        if (_tienda != null)
        {
            _tienda.StockCambio -= AlCambiarStock;
            _tienda.DetenerReabastecimiento();
        }

        if (_inventario != null)
            _inventario.ArmaEquipadaCambio -= AlCambiarArma;
    }
}