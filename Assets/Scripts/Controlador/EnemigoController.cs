using System.Collections.Generic;
using ImperiosEnGuerra.Modelo;
using UnityEngine;

/// <summary>
/// CONTROLADOR de un enemigo del territorio: te ve, te persigue y te ataca
/// con su animacion. Va en CADA muñeco enemigo de las escenas Roma, Persia,
/// Egipto y Vikingos.
///
/// COMO USARLO EN UNITY:
/// 1. Pon el sprite del enemigo en la escena (arquero, caballero, lancero...).
/// 2. Dale un SpriteRenderer, un Animator y un Collider2D (para que no se
///    atraviese con el jugador; con Rigidbody2D Dynamic y Gravity Scale 0).
/// 3. Add Component -> EnemigoController.
/// 4. En el Animator crea los parametros:
///       EstaCorriendo  (Bool)    - para la animacion de caminar
///       Atacar         (Trigger) - para la animacion de ataque
///       Morir          (Trigger) - opcional, para la de muerte
///    Si tus parametros se llaman distinto, cambia los nombres en el Inspector.
///
/// COMO PELEA:
///   - Si el jugador entra en su Radio Deteccion, se le acerca.
///   - Si esta a Radio Ataque, para y ataca cada Segundos Entre Ataques.
///   - El daño se lo aplica al JugadorModel, que es thread-safe: el hambre
///     puede estar quitando vida al mismo tiempo desde su hilo y no se pierde
///     ningun golpe.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemigoController : MonoBehaviour
{
    /// <summary>
    /// Todos los enemigos vivos de la escena. El ataque del jugador la recorre
    /// para saber a quien golpea, y CombateTerritorioController la usa para
    /// saber cuando se acabaron.
    /// </summary>
    public static readonly List<EnemigoController> Vivos = new List<EnemigoController>();

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 50;

    [Header("Ataque")]
    [SerializeField] private int danoPorGolpe = 8;

    [Tooltip("Cada cuantos segundos ataca mientras te tenga al alcance.")]
    [SerializeField] private float segundosEntreAtaques = 2f;

    [Tooltip("Distancia a la que empieza a perseguirte.")]
    [SerializeField] private float radioDeteccion = 6f;

    [Tooltip("Distancia a la que se detiene y ataca.")]
    [SerializeField] private float radioAtaque = 1.2f;

    [Header("Movimiento")]
    [SerializeField] private float velocidad = 2f;

    [Tooltip("Radio del cuerpo para comprobar si cabe donde va a pisar. Sin " +
             "esto el enemigo atraviesa la montaña y los edificios. Solo se " +
             "usa cuando el enemigo NO tiene Rigidbody2D.")]
    [SerializeField] private float radioCuerpo = 0.35f;

    [Tooltip("Masa del cuerpo. Alta a proposito: con la masa por defecto el " +
             "jugador empuja al enemigo por todo el mapa a punta de choques.")]
    [SerializeField] private float masa = 50f;

    [Header("Animator por PARAMETROS (si tu controller los tiene)")]
    [Tooltip("Si tu Animator no tiene alguno de estos, no pasa nada: el script " +
             "comprueba que exista antes de usarlo, asi que no te llena la " +
             "consola de avisos, y se pasa al modo de abajo.")]
    [SerializeField] private string paramCorriendo = "EstaCorriendo";
    [SerializeField] private string triggerAtacar = "Atacar";
    [SerializeField] private string triggerMorir = "Morir";

    [Header("Animator por NOMBRE DE ESTADO (para los controllers de Tiny Swords)")]
    [Tooltip("Los controllers que trae Tiny Swords (Warrior, Archer, Lancer...) " +
             "tienen los estados pero NINGUN parametro ni transicion. En vez de " +
             "obligarte a crearlos a mano, el script llama al estado por su " +
             "nombre con Animator.Play(). Se usa solo si arriba no encontro " +
             "parametros.\n\n" +
             "Para el guerrero rojo: Warrior_Idle_Red, Warrior_Run_Red, " +
             "Warrior_Attack1_Red.")]
    [SerializeField] private string estadoIdle = "Warrior_Idle_Red";
    [SerializeField] private string estadoCorriendo = "Warrior_Run_Red";
    [SerializeField] private string estadoAtacar = "Warrior_Attack1_Red";

    [Tooltip("Cuanto dura la animacion de ataque. Durante ese tiempo no se le " +
             "cambia el estado, para que no se corte a la mitad.")]
    [SerializeField] private float duracionAnimacionAtaque = 0.6f;

    [Header("Muerte")]
    [Tooltip("Tiny Swords no trae animacion de muerte. Con esto el enemigo se " +
             "desvanece y se encoge mientras cae, que es mejor que desaparecer " +
             "de golpe.")]
    [SerializeField] private bool desvanecerAlMorir = true;

    [Header("Al recibir un golpe")]
    [Tooltip("El enemigo se tiñe un instante, como la oveja. Sin esto no se " +
             "nota que le estas pegando y parece que tu ataque no sirve.")]
    [SerializeField] private bool destelloAlRecibir = true;

    [SerializeField] private Color colorDestello = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private float duracionDestello = 0.12f;

    [Header("Al morir")]
    [Tooltip("Segundos que se queda en pantalla para que se vea la animacion.")]
    [SerializeField] private float segundosAntesDeDesaparecer = 1f;

    private Transform _jugador;
    private JugadorModel _jugadorModel;
    private Animator _animator;
    private SpriteRenderer _sprite;

    private int _vida;
    private float _proximoAtaque;
    private bool _muerto;

    // Se comprueba una vez si el Animator tiene cada parametro. Si no lo
    // tiene, no se le manda nada: Unity escupe un warning por CADA llamada y
    // con varios enemigos eso vuelve la consola inservible.
    private bool _tieneCorriendo, _tieneAtacar, _tieneMorir;

    // Cuando el controller no trae parametros se maneja por nombre de estado.
    private bool _usaEstados;
    private string _estadoActual = "";
    private float _finAnimAtaque;

    private Coroutine _destello;
    private Color _colorNormal = Color.white;

    private ContactFilter2D _filtro;
    private readonly Collider2D[] _cache = new Collider2D[8];

    // Si el enemigo tiene Rigidbody2D, el movimiento se hace con la fisica y
    // no tocando el transform. Ver el comentario de FixedUpdate.
    private Rigidbody2D _rb;
    private Vector2 _direccionDeseada;

    public bool Muerto => _muerto;
    public int Vida => _vida;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();
        if (_sprite != null) _colorNormal = _sprite.color;

        _vida = vidaMaxima;

        // Se configura el Rigidbody por codigo para que no dependa de que la
        // casilla quedara marcada en el Inspector. Sin FreezeRotation, en
        // cuanto el jugador lo empuja el enemigo sale rodando como una peonza.
        _rb = GetComponent<Rigidbody2D>();

        if (_rb != null)
        {
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.mass = masa;
        }

        // Por si el prefab quedo girado de antes.
        transform.rotation = Quaternion.identity;

        // useTriggers = false: las zonas de recoleccion y las de la tienda son
        // triggers y no deben frenar al enemigo, solo la roca y los edificios.
        _filtro = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = Physics2D.AllLayers
        };
    }

    private void OnEnable()
    {
        if (!Vivos.Contains(this)) Vivos.Add(this);
    }

    private void OnDisable()
    {
        Vivos.Remove(this);
    }

    private void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");

        if (go != null)
            _jugador = go.transform;
        else
            Debug.LogWarning("[Enemigo] No encontre al jugador. Revisa que tenga el tag Player.");

        if (PlayerSelectionManager.Instance != null && PlayerSelectionManager.Instance.Partida != null)
            _jugadorModel = PlayerSelectionManager.Instance.Partida.Jugador;

        _tieneCorriendo = TieneParametro(paramCorriendo);
        _tieneAtacar = TieneParametro(triggerAtacar);
        _tieneMorir = TieneParametro(triggerMorir);

        // Sin parametros se cae al modo de estados por nombre.
        _usaEstados = _animator != null && !_tieneCorriendo && !_tieneAtacar;

        if (_usaEstados) ResolverEstados();

        string modo = (_tieneCorriendo || _tieneAtacar)
            ? "parametros (" + paramCorriendo + "=" + (_tieneCorriendo ? "si" : "NO") +
              ", " + triggerAtacar + "=" + (_tieneAtacar ? "si" : "NO") + ")"
            : (_usaEstados ? "estados por nombre" : "SIN ANIMACION");

        Debug.Log("[Enemigo] " + gameObject.name + " listo: vida " + vidaMaxima +
                  ", daño " + danoPorGolpe +
                  " | te ve a " + radioDeteccion + ", pega a " + radioAtaque +
                  " | animator: " + modo +
                  " | muerte: " + (_tieneMorir ? triggerMorir : (desvanecerAlMorir ? "se desvanece" : "desaparece")) +
                  " | enemigos vivos: " + Vivos.Count);

        if (_usaEstados) PonerEstado(estadoIdle);

        // Esto merece un aviso amarillo, no una linea mas de log: es el motivo
        // numero uno de "no hace ninguna animacion".
        if (_animator == null)
        {
            Debug.LogWarning("[Enemigo] " + gameObject.name + " NO TIENE componente " +
                             "Animator. Es un sprite quieto, asi que no va a animarse " +
                             "nunca. Add Component -> Animator, y arrastrale el " +
                             "Warrior.controller al campo Controller.");
        }
        else if (_animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[Enemigo] " + gameObject.name + " tiene Animator pero con " +
                             "el campo Controller VACIO. Arrastrale ahi el " +
                             "Warrior.controller de su carpeta de Units.");
        }
    }

    /// <summary>
    /// Comprueba que los nombres de estado escritos en el Inspector existan de
    /// verdad en el Animator, y si alguno esta vacio o mal escrito lo busca
    /// solo entre los clips del controller.
    ///
    /// Esto existe porque Animator.Play con un nombre que no existe NO da
    /// error: simplemente no hace nada. Es el peor tipo de fallo, porque uno
    /// se queda mirando al muñeco quieto sin ninguna pista de por que.
    /// </summary>
    private void ResolverEstados()
    {
        estadoIdle      = ResolverUno(estadoIdle,      "idle");
        estadoCorriendo = ResolverUno(estadoCorriendo, "run", "walk", "corr");
        estadoAtacar    = ResolverUno(estadoAtacar,    "attack", "atac");

        Debug.Log("[Enemigo] " + gameObject.name + " estados resueltos -> idle: " +
                  Mostrar(estadoIdle) + " | correr: " + Mostrar(estadoCorriendo) +
                  " | atacar: " + Mostrar(estadoAtacar));
    }

    private static string Mostrar(string s)
    {
        return string.IsNullOrEmpty(s) ? "NO ENCONTRADO" : s;
    }

    /// <summary>
    /// Devuelve el nombre puesto a mano si ese estado existe; si no, busca
    /// entre los clips del Animator uno cuyo nombre contenga alguna de las
    /// palabras clave.
    /// </summary>
    private string ResolverUno(string puestoAMano, params string[] palabras)
    {
        if (ExisteEstado(puestoAMano)) return puestoAMano;

        if (!string.IsNullOrEmpty(puestoAMano))
        {
            Debug.LogWarning("[Enemigo] " + gameObject.name + ": el Animator no tiene " +
                             "ningun estado llamado '" + puestoAMano + "'. Buscando uno parecido.");
        }

        if (_animator == null || _animator.runtimeAnimatorController == null) return "";

        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip == null) continue;

            string minus = clip.name.ToLowerInvariant();

            foreach (string palabra in palabras)
            {
                // En Tiny Swords el estado se llama igual que su clip, asi que
                // el nombre del clip sirve para Play(). Aun asi se comprueba.
                if (minus.Contains(palabra) && ExisteEstado(clip.name))
                    return clip.name;
            }
        }

        return "";
    }

    private bool ExisteEstado(string nombre)
    {
        if (_animator == null || string.IsNullOrEmpty(nombre)) return false;

        return _animator.HasState(0, Animator.StringToHash(nombre));
    }

    /// <summary>
    /// Animator.Play va directo al estado por su nombre, sin parametros ni
    /// transiciones. Se recuerda cual esta sonando para no reiniciarlo en cada
    /// frame, que dejaria al enemigo congelado en el primer fotograma.
    /// </summary>
    private void PonerEstado(string estado)
    {
        if (_animator == null || string.IsNullOrEmpty(estado)) return;
        if (_estadoActual == estado) return;

        _estadoActual = estado;
        _animator.Play(estado, 0, 0f);
    }

    private bool TieneParametro(string nombre)
    {
        if (_animator == null || string.IsNullOrEmpty(nombre)) return false;
        if (_animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter p in _animator.parameters)
            if (p.name == nombre) return true;

        return false;
    }

    private void Update()
    {
        if (_muerto || _jugador == null) return;

        float distancia = Vector2.Distance(transform.position, _jugador.position);

        // Mirar hacia el jugador
        if (_sprite != null)
            _sprite.flipX = _jugador.position.x < transform.position.x;

        bool corriendo = false;
        _direccionDeseada = Vector2.zero;   // quieto salvo que Perseguir diga otra cosa

        if (distancia <= radioAtaque)
        {
            Atacar();
        }
        else if (distancia <= radioDeteccion)
        {
            corriendo = true;
            Perseguir();
        }

        Correr(corriendo);

        // Modo estados: mientras dura la animacion de ataque no se le cambia,
        // o se cortaria a la mitad en cuanto el jugador se aleje un pelo.
        if (_usaEstados && Time.time >= _finAnimAtaque)
        {
            string quiere = corriendo ? estadoCorriendo : estadoIdle;

            // Si no hay estado de correr, al menos que no se quede tieso con
            // la pose de ataque: se vuelve al idle.
            if (string.IsNullOrEmpty(quiere)) quiere = estadoIdle;

            PonerEstado(quiere);
        }
    }

    /// <summary>
    /// El movimiento con fisica va aqui y no en Update.
    ///
    /// ESTE ERA EL BUG DE LA PEONZA: yo movia al enemigo con
    /// transform.position aunque tuviera un Rigidbody2D Dynamic. Eso es pelear
    /// contra el motor de fisica: el cuerpo sigue acumulando la velocidad de
    /// los empujones mientras el transform lo teletransporta, y el resultado
    /// es que sale disparado y girando en cuanto lo tocas.
    ///
    /// MovePosition mueve el cuerpo POR la fisica, respetando colisiones, y
    /// cada FixedUpdate vuelve a mandar la posicion, asi que los empujones del
    /// jugador dejan de acumularse.
    /// </summary>
    private void FixedUpdate()
    {
        if (_rb == null || _muerto) return;

        if (_direccionDeseada == Vector2.zero)
        {
            // Quieto de verdad. Sin esto el jugador lo va arrastrando por el
            // mapa a punta de choques, porque los dos son cuerpos Dynamic y se
            // empujan entre ellos.
            _rb.MovePosition(_rb.position);
            return;
        }

        _rb.MovePosition(_rb.position + _direccionDeseada * velocidad * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Va hacia el jugador, pero sin atravesar paredes. Si el camino recto
    /// esta bloqueado prueba a rodear por los lados, que es lo minimo para que
    /// no se quede pegado contra una esquina eternamente.
    /// </summary>
    private void Perseguir()
    {
        Vector2 recta = ((Vector2)(_jugador.position - transform.position)).normalized;

        // Con Rigidbody se deja que la fisica resuelva los choques: aqui solo
        // se apunta la direccion y FixedUpdate hace el resto.
        if (_rb != null)
        {
            _direccionDeseada = recta;
            return;
        }

        if (Avanzar(recta)) return;

        // Bloqueado de frente: se prueba en diagonal, primero a un lado y
        // luego al otro. No es un buscador de caminos, pero saca al enemigo de
        // la mayoria de los atascos sin complicar el codigo.
        Vector2 costado = new Vector2(-recta.y, recta.x);

        if (Avanzar((recta + costado).normalized)) return;
        Avanzar((recta - costado).normalized);
    }

    private bool Avanzar(Vector2 direccion)
    {
        Vector3 destino = transform.position + (Vector3)direccion * velocidad * Time.deltaTime;

        if (!EstaLibre(destino)) return false;

        transform.position = destino;
        return true;
    }

    private bool EstaLibre(Vector3 destino)
    {
        int cuantos = Physics2D.OverlapCircle(destino, radioCuerpo, _filtro, _cache);

        for (int i = 0; i < cuantos; i++)
        {
            Collider2D c = _cache[i];
            if (c == null) continue;

            // Ni el mismo, ni el jugador (si no, nunca llegaria a pegarle),
            // ni los otros enemigos (se estorbarian entre ellos y se
            // quedarian todos quietos en fila).
            if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
            if (c.CompareTag("Player")) continue;
            if (c.GetComponent<EnemigoController>() != null) continue;

            return false;
        }

        return true;
    }

    private void Atacar()
    {
        if (Time.time < _proximoAtaque) return;

        _proximoAtaque = Time.time + segundosEntreAtaques;

        if (_tieneAtacar)
        {
            _animator.SetTrigger(triggerAtacar);
        }
        else if (_usaEstados && !string.IsNullOrEmpty(estadoAtacar))
        {
            // Se fuerza el replay aunque ya estuviera en ese estado: si no, el
            // segundo golpe no se veria.
            _estadoActual = "";
            PonerEstado(estadoAtacar);
            _finAnimAtaque = Time.time + duracionAnimacionAtaque;
        }

        if (_jugadorModel == null) return;

        // RecibirDanio hace leer, restar y guardar dentro del mismo lock, asi
        // que este golpe no se pierde aunque el hambre este pegando a la vez.
        _jugadorModel.RecibirDanio(danoPorGolpe);

        ImperiosEnGuerra.Controlador.Bitacora.Anotar(
            gameObject.name,
            "Ataque al jugador",
            "-" + danoPorGolpe + " de vida (le queda " + _jugadorModel.Vida +
            "/" + _jugadorModel.VidaMax + ")");
    }

    private void Correr(bool corriendo)
    {
        if (_tieneCorriendo) _animator.SetBool(paramCorriendo, corriendo);
    }

    /// <summary>
    /// Destello de daño. El Animator pisa el sprite en cada frame pero NO toca
    /// el color, asi que esto convive con la animacion sin pelearse.
    /// </summary>
    private void Destellar()
    {
        if (!destelloAlRecibir || _sprite == null) return;

        if (_destello != null)
        {
            StopCoroutine(_destello);
            _sprite.color = _colorNormal;
        }

        _destello = StartCoroutine(RutinaDestello());
    }

    private System.Collections.IEnumerator RutinaDestello()
    {
        _sprite.color = colorDestello;
        yield return new WaitForSeconds(duracionDestello);

        _sprite.color = _colorNormal;
        _destello = null;
    }

    /// <summary>
    /// Lo llama el jugador al golpearlo. Devuelve true si con este golpe murio.
    /// </summary>
    public bool RecibirDanio(int cantidad)
    {
        if (_muerto) return false;

        _vida -= cantidad;

        Destellar();

        Debug.Log("[Enemigo] " + gameObject.name + " recibe " + cantidad +
                  ". Le queda " + Mathf.Max(0, _vida) + "/" + vidaMaxima + ".");

        if (_vida > 0)
            return false;

        _vida = 0;
        Morir();
        return true;
    }

    private void Morir()
    {
        _muerto = true;
        Vivos.Remove(this);

        if (_tieneMorir)
        {
            _animator.SetTrigger(triggerMorir);
        }
        else if (desvanecerAlMorir)
        {
            // Tiny Swords no trae animacion de muerte para las unidades, asi
            // que se hace por codigo: el Animator se apaga (si no, seguiria
            // pisando el sprite) y el muñeco se desvanece encogiendose.
            if (_animator != null) _animator.enabled = false;
            StartCoroutine(RutinaDesvanecer());
        }

        // Se le quitan TODOS los colliders para que el jugador pueda pasar por
        // encima del cadaver mientras se ve la animacion de muerte.
        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        Debug.Log("[Enemigo] " + gameObject.name + " ha caido. Quedan " + Vivos.Count + ".");

        ImperiosEnGuerra.Controlador.Bitacora.Anotar(
            "Combate",
            "Derroto a " + gameObject.name + " (quedan " + Vivos.Count + " enemigos)");

        Destroy(gameObject, segundosAntesDeDesaparecer);
    }

    private System.Collections.IEnumerator RutinaDesvanecer()
    {
        if (_sprite == null) yield break;

        Color desde = _sprite.color;
        Vector3 escalaInicial = transform.localScale;

        float t = 0f;
        float duracion = Mathf.Max(0.1f, segundosAntesDeDesaparecer);

        while (t < duracion)
        {
            t += Time.deltaTime;
            float avance = t / duracion;

            _sprite.color = new Color(desde.r, desde.g, desde.b, 1f - avance);
            transform.localScale = escalaInicial * (1f - avance * 0.3f);

            yield return null;
        }
    }

    // Dibuja los dos radios en la vista Scene para poder ajustarlos a ojo.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioAtaque);
    }
}
