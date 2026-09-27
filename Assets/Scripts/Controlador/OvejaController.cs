using UnityEngine;

/// <summary>
/// CONTROLADOR de la oveja: la parte viva del animal. Va en el MISMO objeto
/// que el RecursoNodoController, que es el que maneja la carne, los golpes y
/// la regeneracion.
///
/// Reparto de trabajo:
///   RecursoNodoController  -> cuanta carne queda, el golpe, el hilo que la
///                             repone, el destello rojo y la sacudida.
///   OvejaController (este) -> que la oveja pastee, se quede quieta, y salga
///                             corriendo cuando la golpean.
///
/// POR QUE NO USA PARAMETROS DEL ANIMATOR
/// El Sheep.controller de Tiny Swords trae los tres estados (Sheep_Idle,
/// Sheep_Grass y Sheep_Run) pero NINGUN parametro ni transicion. En vez de
/// obligar a crear todo eso a mano en la ventana Animator, este script llama
/// directo al estado que quiere con Animator.Play(). El resultado es el mismo
/// y no hay nada que configurar.
///
/// COMO USARLO EN UNITY:
/// 1. La oveja ya debe tener su SpriteRenderer, su Animator con el
///    Sheep.controller y el RecursoNodoController configurado como Comida.
/// 2. Add Component -> OvejaController.
/// 3. Listo. Si tus estados se llaman distinto, cambia los nombres abajo.
/// </summary>
[RequireComponent(typeof(RecursoNodoController))]
public class OvejaController : MonoBehaviour
{
    [Header("Nombres de los estados del Animator")]
    [SerializeField] private string estadoIdle = "Sheep_Idle";
    [SerializeField] private string estadoComiendo = "Sheep_Grass";
    [SerializeField] private string estadoCorriendo = "Sheep_Run";

    [Header("Vida tranquila")]
    [Tooltip("Cada cuanto se replantea si pastar o descansar (segundos, min y max).")]
    [SerializeField] private float pausaMinima = 3f;
    [SerializeField] private float pausaMaxima = 7f;

    [Tooltip("Probabilidad de ponerse a comer pasto en vez de quedarse quieta.")]
    [Range(0f, 1f)]
    [SerializeField] private float ganasDeComer = 0.6f;

    [Header("Huida")]
    [Tooltip("Segundos que corre despues de recibir un golpe.")]
    [SerializeField] private float duracionHuida = 1.4f;

    [SerializeField] private float velocidadHuida = 2.5f;

    [Tooltip("Hasta donde se puede alejar de donde nacio. Sin esto la oveja " +
             "se termina yendo del mapa a fuerza de golpes.")]
    [SerializeField] private float distanciaMaximaDesdeCasa = 6f;

    [Tooltip("Radio del cuerpo de la oveja para comprobar si cabe donde va a " +
             "pisar. Si se sigue metiendo en las paredes, subelo un poco.")]
    [SerializeField] private float radioCuerpo = 0.35f;

    [Tooltip("Velocidad a la que vuelve caminando a su sitio cuando quedo " +
             "lejos de casa.")]
    [SerializeField] private float velocidadRegreso = 1.2f;

    private RecursoNodoController _nodo;
    private Animator _animator;
    private SpriteRenderer _sprite;
    private Transform _jugador;

    private Vector3 _casa;
    private float _proximoCambio;
    private float _huyendoHasta;
    private Vector2 _direccionHuida;
    private string _estadoActual = "";

    // Para comprobar si el sitio al que va esta libre. useTriggers = false es
    // lo importante: las zonas de recoleccion son triggers y no deben contar
    // como pared, solo la roca y los edificios.
    private ContactFilter2D _filtro;
    private readonly Collider2D[] _cache = new Collider2D[8];

    private void Awake()
    {
        _nodo = GetComponent<RecursoNodoController>();
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();

        _casa = transform.position;

        _filtro = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = Physics2D.AllLayers
        };
    }

    private void OnEnable()
    {
        // Se engancha al golpe de SU nodo: cada oveja huye por lo suyo.
        if (_nodo != null) _nodo.Golpeado += AlRecibirGolpe;
    }

    private void OnDisable()
    {
        if (_nodo != null) _nodo.Golpeado -= AlRecibirGolpe;
    }

    private void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _jugador = go.transform;

        ProgramarProximoCambio();
        Reproducir(estadoIdle);
    }

    private void Update()
    {
        // Mientras esta agotada (cazada) no hace nada: el RecursoNodoController
        // la tiene escondida hasta que se repone.
        if (_nodo.Agotado) return;

        if (Time.time < _huyendoHasta)
        {
            Huir();
            return;
        }

        // Antes de ponerse a pastar, comprueba que este donde debe.
        if (RegresarSiSeAlejo()) return;

        Pastar();
    }

    // ------------------------------------------------------------------

    private void AlRecibirGolpe()
    {
        _huyendoHasta = Time.time + duracionHuida;

        // Se aleja del jugador. Si por lo que sea no se encontro al jugador,
        // arranca para cualquier lado, que es mejor que quedarse tiesa.
        if (_jugador != null)
        {
            Vector2 lejos = transform.position - _jugador.position;
            _direccionHuida = lejos.sqrMagnitude > 0.01f
                ? lejos.normalized
                : Random.insideUnitCircle.normalized;
        }
        else
        {
            _direccionHuida = Random.insideUnitCircle.normalized;
        }

        Reproducir(estadoCorriendo);
    }

    private void Huir()
    {
        // No se aleja infinitamente de donde nacio: al llegar al limite, la
        // direccion se invierte hacia casa.
        if (Vector2.Distance(transform.position, _casa) > distanciaMaximaDesdeCasa)
            _direccionHuida = HaciaCasa();

        // Si corriendo asustada choca contra la montaña, prueba a volver hacia
        // casa; y si tampoco puede, deja de huir. Sin esto la oveja atraviesa
        // la pared de roca y queda encerrada donde el jugador no la alcanza.
        if (!Avanzar(_direccionHuida, velocidadHuida))
        {
            _direccionHuida = HaciaCasa();

            if (!Avanzar(_direccionHuida, velocidadHuida))
                _huyendoHasta = 0f;
        }
    }

    /// <summary>
    /// Cuando no esta huyendo y quedo lejos de su sitio, se devuelve caminando.
    /// Es la red de seguridad: aunque algo raro la saque de lugar, siempre
    /// termina volviendo a donde el jugador puede cazarla.
    /// </summary>
    private bool RegresarSiSeAlejo()
    {
        if (Vector2.Distance(transform.position, _casa) <= distanciaMaximaDesdeCasa * 0.5f)
            return false;

        Reproducir(estadoCorriendo);
        Avanzar(HaciaCasa(), velocidadRegreso);
        return true;
    }

    private Vector2 HaciaCasa()
    {
        Vector2 diferencia = _casa - transform.position;

        return diferencia.sqrMagnitude > 0.0001f
            ? diferencia.normalized
            : Random.insideUnitCircle.normalized;
    }

    /// <summary>
    /// Intenta dar un paso en esa direccion. Devuelve false si el sitio esta
    /// ocupado por algo solido, y entonces no se mueve.
    /// </summary>
    private bool Avanzar(Vector2 direccion, float velocidad)
    {
        Vector3 destino = transform.position + (Vector3)direccion * velocidad * Time.deltaTime;

        if (!EstaLibre(destino)) return false;

        transform.position = destino;

        if (_sprite != null && Mathf.Abs(direccion.x) > 0.01f)
            _sprite.flipX = direccion.x < 0f;

        return true;
    }

    private bool EstaLibre(Vector3 destino)
    {
        int cuantos = Physics2D.OverlapCircle(destino, radioCuerpo, _filtro, _cache);

        for (int i = 0; i < cuantos; i++)
        {
            Collider2D c = _cache[i];
            if (c == null) continue;

            // Ni ella misma ni el jugador cuentan como pared.
            if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
            if (c.CompareTag("Player")) continue;

            return false;
        }

        return true;
    }

    /// <summary>
    /// La vida normal de la oveja: cada cierto rato decide si ponerse a comer
    /// pasto o quedarse quieta. Nada de esto afecta al Modelo, es solo para
    /// que el mapa no se vea muerto.
    /// </summary>
    private void Pastar()
    {
        if (Time.time < _proximoCambio) return;

        ProgramarProximoCambio();

        bool come = Random.value < ganasDeComer;
        Reproducir(come ? estadoComiendo : estadoIdle);
    }

    private void ProgramarProximoCambio()
    {
        _proximoCambio = Time.time + Random.Range(pausaMinima, pausaMaxima);
    }

    /// <summary>
    /// Animator.Play va directo al estado por su nombre, sin necesitar
    /// parametros ni transiciones. Se comprueba cual esta sonando para no
    /// reiniciar la animacion en cada frame.
    /// </summary>
    private void Reproducir(string estado)
    {
        if (_animator == null || string.IsNullOrEmpty(estado)) return;
        if (_estadoActual == estado) return;

        _estadoActual = estado;
        _animator.Play(estado, 0, 0f);
    }
}
