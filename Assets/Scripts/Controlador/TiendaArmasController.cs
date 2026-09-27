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

    [Header("Botones de provisiones")]
    [SerializeField] private Button botonCarne;
    [SerializeField] private Button botonMadera;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoOro;
    [SerializeField] private TMP_Text textoMensaje;
    [SerializeField] private TMP_Text textoArmaEquipada;
    [SerializeField] private Slider barraDurabilidad;

    [Header("Configuracion de la tienda")]
    [SerializeField] private int stockMaximoPorArma = 3;
    [SerializeField] private int msEntreReabastecimientos = 8000;

    [Header("Configuracion de las provisiones")]
    [SerializeField] private int stockMaximoPorLote = 5;
    [SerializeField] private int msEntreReposicionProvisiones = 10000;

    // --- Modelo ---
    private JugadorModel _jugador;
    private InventarioArmasModel _inventario;
    private TiendaArmasModel _tienda;

    // La segunda mitad de la tienda: carne y madera a cambio de oro. Tiene su
    // propio hilo de reposicion, independiente del de las armas.
    private TiendaRecursosModel _provisiones;

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

        // 2) Las dos mitades de la tienda, cada una con SU PROPIO hilo de
        //    reposicion. Son dos hilos mas corriendo a la vez: el herrero
        //    forjando herramientas y el mercader trayendo provisiones.
        _tienda = new TiendaArmasModel(stockMaximoPorArma, msEntreReabastecimientos);
        _tienda.IniciarReabastecimiento();

        _provisiones = new TiendaRecursosModel(stockMaximoPorLote, msEntreReposicionProvisiones);
        _provisiones.IniciarReabastecimiento();

        // 3) Suscribirse al Modelo. OJO: estos handlers pueden ejecutarse en
        //    OTRO hilo, por eso solo encolan trabajo.
        _tienda.StockCambio += AlCambiarStock;
        _provisiones.StockCambio += AlCambiarStockProvisiones;
        _inventario.ArmaEquipadaCambio += AlCambiarArma;

        // El Pawn arranca con las manos vacias, asi que todavia no hay ninguna
        // herramienta a la que engancharse. AlCambiarArma engancha la primera
        // que compre.
        if (_inventario.Equipada != null)
            _inventario.Equipada.DurabilidadCambio += AlCambiarDurabilidad;

        // 4) Cablear botones.
        if (botonHacha != null) botonHacha.onClick.AddListener(() => Comprar(TipoArma.Hacha));
        if (botonPico != null) botonPico.onClick.AddListener(() => Comprar(TipoArma.Pico));
        if (botonCuchillo != null) botonCuchillo.onClick.AddListener(() => Comprar(TipoArma.Cuchillo));
        if (botonMartillo != null) botonMartillo.onClick.AddListener(() => Comprar(TipoArma.Martillo));
        if (botonCerrar != null) botonCerrar.onClick.AddListener(CerrarTienda);

        if (botonCarne != null) botonCarne.onClick.AddListener(() => ComprarProvision(TipoRecurso.Comida));
        if (botonMadera != null) botonMadera.onClick.AddListener(() => ComprarProvision(TipoRecurso.Madera));

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

    private void AlCambiarStockProvisiones(TipoRecurso recurso, int cantidad)
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
            // Puede llegar null si se desequipa todo, y entonces no hay nada
            // que enganchar.
            if (nueva != null)
            {
                nueva.DurabilidadCambio -= AlCambiarDurabilidad;
                nueva.DurabilidadCambio += AlCambiarDurabilidad;
            }

            _refrescarPendiente = true;
        });
    }

    // ------------------------------------------------------------------
    //  Acciones de la UI
    // ------------------------------------------------------------------

    /// <summary>
    /// Le entrega de golpe toda la UI a este controlador. La usa TiendaView,
    /// que construye el panel por codigo en su Awake: como Unity ejecuta
    /// todos los Awake antes de cualquier Start, cuando este script arranca
    /// ya tiene sus referencias puestas.
    ///
    /// Sirve tambien si algun dia se hace el panel a mano en el Inspector:
    /// en ese caso simplemente no se llama y los campos se llenan alli.
    /// </summary>
    /// <summary>
    /// Los dos botones de provisiones. Van aparte de Configurar porque son
    /// opcionales: una tienda que solo venda herramientas sigue funcionando
    /// sin llamar a esto.
    /// </summary>
    public void ConfigurarProvisiones(Button carne, Button madera)
    {
        botonCarne = carne;
        botonMadera = madera;
    }

    public void Configurar(GameObject panel,
                           Button cerrar,
                           Button hacha,
                           Button pico,
                           Button cuchillo,
                           Button martillo,
                           TMP_Text oro,
                           TMP_Text mensaje,
                           TMP_Text armaEquipada,
                           Slider durabilidad)
    {
        panelTienda = panel;
        botonCerrar = cerrar;
        botonHacha = hacha;
        botonPico = pico;
        botonCuchillo = cuchillo;
        botonMartillo = martillo;
        textoOro = oro;
        textoMensaje = mensaje;
        textoArmaEquipada = armaEquipada;
        barraDurabilidad = durabilidad;
    }

    /// <summary>¿Esta abierto el panel ahora mismo?</summary>
    public bool EstaAbierta => panelTienda != null && panelTienda.activeSelf;

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

            ImperiosEnGuerra.Controlador.Bitacora.Anotar(
                "Compra en la Armeria",
                "Compro " + CatalogoArmas.Nombre(tipo) + " por " +
                CatalogoArmas.Precio(tipo) + " de oro (le quedan " +
                _jugador.Oro + ")");
        }

        MostrarMensaje(TiendaArmasModel.Mensaje(resultado, tipo));
        RefrescarUI();
    }

    /// <summary>
    /// Compra un lote de provisiones. El Modelo hace todo el trabajo delicado
    /// (reservar el lote, cobrar y devolverlo si el cobro falla); aqui solo se
    /// muestra el resultado.
    /// </summary>
    private void ComprarProvision(TipoRecurso recurso)
    {
        if (_provisiones == null) return;

        OfertaRecurso oferta = _provisiones.OfertaDe(recurso);
        ResultadoCompraRecurso resultado = _provisiones.Comprar(_jugador, recurso);

        if (resultado == ResultadoCompraRecurso.Exito && oferta != null)
        {
            ImperiosEnGuerra.Controlador.Bitacora.Anotar(
                "Compra en la Armeria",
                "Compro " + oferta.Unidades + " de " + oferta.Nombre +
                " por " + oferta.Precio + " de oro (le quedan " +
                _jugador.Oro + ")");
        }

        MostrarMensaje(TiendaRecursosModel.Mensaje(resultado, oferta));
        RefrescarUI();
    }

    private void MostrarMensaje(string texto)
    {
        if (textoMensaje != null) textoMensaje.text = texto;

        // Tambien en pantalla: el texto del panel solo se ve con la tienda
        // abierta, y algunos avisos importan justo al cerrarla.
        AvisoPantalla.Mostrar(texto);
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

        // Puede no haber ninguna: asi empieza el Pawn y asi sigue hasta que
        // compre su primera herramienta.
        ArmaModel equipada = _inventario.Equipada;

        if (textoArmaEquipada != null)
            textoArmaEquipada.text = equipada != null
                ? "Equipada: " + equipada.Nombre
                : "Sin herramienta";

        if (barraDurabilidad != null)
        {
            barraDurabilidad.gameObject.SetActive(equipada != null);

            if (equipada != null)
                barraDurabilidad.value = equipada.PorcentajeDurabilidad;
        }

        if (textoOro != null && _jugador != null)
            textoOro.text = "Oro: " + _jugador.Oro;

        ActualizarBoton(botonHacha, TipoArma.Hacha);
        ActualizarBoton(botonPico, TipoArma.Pico);
        ActualizarBoton(botonCuchillo, TipoArma.Cuchillo);
        ActualizarBoton(botonMartillo, TipoArma.Martillo);

        ActualizarBotonProvision(botonCarne, TipoRecurso.Comida);
        ActualizarBotonProvision(botonMadera, TipoRecurso.Madera);
    }

    private void ActualizarBotonProvision(Button boton, TipoRecurso recurso)
    {
        if (boton == null || _provisiones == null) return;

        OfertaRecurso oferta = _provisiones.OfertaDe(recurso);
        if (oferta == null) { boton.gameObject.SetActive(false); return; }

        int stock = _provisiones.StockDe(recurso);

        // Solo se apaga por falta de stock. Si lo que falta es oro se deja
        // encendido a proposito: asi al dar clic sale "Te falta oro para
        // Carne", que dice mas que un boton gris sin explicacion.
        boton.interactable = stock > 0;

        TMP_Text etiqueta = boton.GetComponentInChildren<TMP_Text>();
        if (etiqueta != null)
        {
            etiqueta.text = oferta.Nombre + " x" + oferta.Unidades
                          + "  -  " + oferta.Precio + " oro   (quedan " + stock + ")";
        }
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

        // El mercader tiene su propio hilo y hay que apagarlo aparte. Si se
        // olvida, el editor de Unity se queda pegado al salir del Play Mode.
        if (_provisiones != null)
        {
            _provisiones.StockCambio -= AlCambiarStockProvisiones;
            _provisiones.DetenerReabastecimiento();
        }

        if (_inventario != null)
            _inventario.ArmaEquipadaCambio -= AlCambiarArma;
    }
}