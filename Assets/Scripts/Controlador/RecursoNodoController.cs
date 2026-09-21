using System;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;
using UnityEngine;

/// <summary>
/// CONTROLADOR de UN nodo de recurso del mapa: un arbol, una roca, una oveja.
///
/// Este script es el punto 1 de tus pendientes: conecta el sprite que ya esta
/// en la escena Juego con su RecursoModel y con el arma del jugador.
///
/// COMO SE USA EN UNITY
///   1. Selecciona un arbol (o roca, u oveja) del Hierarchy.
///   2. Agregale un Collider2D (Circle o Box) y marca "Is Trigger".
///      Hazlo mas grande que el sprite: es la zona donde el jugador puede talar.
///   3. Agregale este script y elige el Tipo en el Inspector.
///   4. El GameObject "Jugador" debe tener el tag "Player" y el componente
///      JugadorArmasController.
///
/// COMO FUNCIONA
///   - En Start arranca el hilo de regeneracion del nodo (el arbol crece solo).
///     Corre siempre, este o no el jugador cerca.
///   - Al entrar el jugador al trigger, se guarda su referencia.
///   - Con la tecla E, el jugador da un golpe: el arma equipada decide cuanto
///     rinde ese golpe y el nodo entrega lo que realmente tenga.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RecursoNodoController : MonoBehaviour
{
    [Header("Que recurso es este nodo")]
    [SerializeField] private TipoRecurso tipo = TipoRecurso.Madera;
    [SerializeField] private int cantidadInicial = 50;
    [SerializeField] private int cantidadMaxima = 100;

    [Header("Recoleccion")]
    [Tooltip("Cuanto rinde un golpe ANTES de aplicar el multiplicador del arma.")]
    [SerializeField] private int rendimientoBasePorGolpe = 5;

    [Tooltip("Segundos minimos entre un golpe y el siguiente.")]
    [SerializeField] private float segundosEntreGolpes = 0.5f;

    [SerializeField] private KeyCode teclaRecolectar = KeyCode.E;

    [Header("Solo si este nodo es la tienda (Tipo = Armas)")]
    [Tooltip("Arrastra el GameObject TiendaManager. Al presionar E se abre el panel.")]
    [SerializeField] private TiendaArmasController tienda;

    [Header("Feedback opcional")]
    [SerializeField] private GameObject indicadorPresionaE; // un cartelito "E"
    [SerializeField] private Animator animadorNodo;         // para sacudir el arbol

    // --- Modelo ---
    private RecursoModel _nodo;

    // --- Estado de la escena ---
    private JugadorArmasController _jugadorEnZona;
    private float _proximoGolpePermitido;

    /// <summary>
    /// Avisa a la UI (el HUD de recursos) que el jugador acaba de recoger algo.
    /// Se dispara SIEMPRE desde el hilo principal de Unity, asi que el HUD
    /// puede actualizarse directamente sin colas ni nada raro.
    /// </summary>
    public static event Action<TipoRecurso, int> RecursoRecolectado;

    private void Start()
    {
        // 1) Crear el Modelo. Se usa la subclase concreta segun el tipo.
        _nodo = CrearModelo(tipo);
        _nodo.CantidadMaxima = cantidadMaxima;

        // 2) Arrancar el hilo de regeneracion: el arbol crece solo, siempre.
        //    La tienda (Armas) no regenera nada, asi que no le gastamos un hilo.
        if (tipo != TipoRecurso.Armas)
            _nodo.Recoleccion();

        MostrarIndicador(false);
    }

    /// <summary>
    /// Crea la subclase concreta que corresponde. Usa el constructor corto
    /// (cantidad, coordenadas): el string del tipo lo pone la propia clase,
    /// asi no hay forma de crear un OroModel que por dentro diga "Madera".
    /// </summary>
    private RecursoModel CrearModelo(TipoRecurso t)
    {
        float coords = transform.position.x; // tu Coordenadas es un solo float

        switch (t)
        {
            case TipoRecurso.Oro: return new OroModel(cantidadInicial, coords);
            case TipoRecurso.Madera: return new MaderaModel(cantidadInicial, coords);
            case TipoRecurso.Comida: return new ComidaModel(cantidadInicial, coords);
            case TipoRecurso.Armas: return new ArmasModel(cantidadInicial, coords);
            default:
                throw new ArgumentOutOfRangeException(nameof(t));
        }
    }

    private void Update()
    {
        if (_jugadorEnZona == null) return;
        if (!Input.GetKeyDown(teclaRecolectar)) return;
        if (Time.time < _proximoGolpePermitido) return;

        _proximoGolpePermitido = Time.time + segundosEntreGolpes;

        // La tienda no se golpea: se entra a comprar.
        if (tipo == TipoRecurso.Armas)
        {
            if (tienda != null) tienda.AbrirTienda();
            else Debug.LogWarning("[Tienda] Arrastra el TiendaArmasController en el Inspector de este nodo.");
            return;
        }

        Golpear();
    }

    private void Golpear()
    {
        ArmaModel arma = _jugadorEnZona.ArmaEquipada;

        if (arma == null) return;

        if (arma.EstaRota)
        {
            Debug.Log("[Recurso] Tu " + arma.Nombre + " esta rota. Espera a que se repare.");
            return;
        }

        // 1) El ARMA decide cuanto rinde el golpe (hacha x2 en madera, pico x2
        //    en oro, cuchillo x2 en comida, lo demas x0.25) y se desgasta.
        int pedido = arma.CalcularRecoleccion(tipo, rendimientoBasePorGolpe);

        // 2) El NODO entrega lo que realmente tenga. Esto es thread-safe:
        //    entre la lectura y el descuento no se cuela el hilo de regeneracion.
        int obtenido = _nodo.Recolectar(pedido);

        if (obtenido <= 0)
        {
            Debug.Log("[Recurso] " + tipo + " agotado. Dale tiempo a que se regenere.");
            return;
        }

        // 3) Al jugador. Conseguir_Recursos es el metodo que ya existe en
        //    JugadorModel; RecursoTipoHelper.ATexto evita escribir "Madera"
        //    a mano y equivocarse en una letra.
        if (PlayerSelectionManager.Instance != null && PlayerSelectionManager.Instance.Partida != null)
        {
            PlayerSelectionManager.Instance.Partida.Jugador
                .Conseguir_Recursos(RecursoTipoHelper.ATexto(tipo), obtenido);
        }

        if (animadorNodo != null)
            animadorNodo.SetTrigger("Golpe");

        RecursoRecolectado?.Invoke(tipo, obtenido);

        Debug.Log("[Recurso] +" + obtenido + " de " + tipo
                  + " con " + arma.Nombre
                  + " | queda en el nodo: " + _nodo.Cantidad
                  + " | arma: " + arma.Durabilidad + "/" + arma.DurabilidadMaxima);
    }

    // ------------------------------------------------------------------
    //  Zona de recoleccion
    // ------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var armas = other.GetComponent<JugadorArmasController>();
        if (armas == null) return;

        _jugadorEnZona = armas;
        MostrarIndicador(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _jugadorEnZona = null;
        MostrarIndicador(false);
    }

    private void MostrarIndicador(bool visible)
    {
        if (indicadorPresionaE != null)
            indicadorPresionaE.SetActive(visible);
    }

    /// <summary>Para que la UI pueda pintar cuanto le queda al nodo.</summary>
    public int CantidadActual => _nodo != null ? _nodo.Cantidad : 0;

    // ------------------------------------------------------------------
    //  Apagar el hilo del nodo
    // ------------------------------------------------------------------

    private void OnDestroy()
    {
        _nodo?.DetenerRecoleccion();
    }

    private void OnApplicationQuit()
    {
        _nodo?.DetenerRecoleccion();
    }
}