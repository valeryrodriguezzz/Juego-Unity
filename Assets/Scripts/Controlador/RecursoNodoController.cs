using System;
using System.Collections.Generic;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;
using ImperiosEnGuerra.Modelo.Recursos;   // OroModel, MaderaModel, ComidaModel y ArmasModel viven aqui
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

    [Tooltip("Cuanto se regenera por segundo. En -1 usa el ritmo que trae el " +
             "Modelo (4 para madera y oro, 5 para comida). Bajalo a 1 si " +
             "quieres que el tocon se quede un buen rato antes de que el " +
             "arbol vuelva a crecer.")]
    [SerializeField] private int regeneracionPorSegundo = -1;

    [Header("Recoleccion")]
    [Tooltip("Cuanto rinde un golpe ANTES de aplicar el multiplicador del arma.")]
    [SerializeField] private int rendimientoBasePorGolpe = 5;

    [Tooltip("Exige la herramienta adecuada: con el cuchillo no se tala un " +
             "arbol, por mucho que se intente. Si lo desmarcas, cualquier " +
             "herramienta sirve pero las que no son las suyas rinden poquisimo.")]
    [SerializeField] private bool exigirHerramientaCorrecta = true;

    [Tooltip("Que multiplicador tiene que tener el arma sobre este recurso " +
             "para que se acepte el golpe. En 1 solo pasa la herramienta " +
             "especializada (que rinde x2). Bajalo a 0.5 si quieres permitir " +
             "las que sirven a medias, como el hacha cazando ovejas.")]
    [SerializeField] private float multiplicadorMinimo = 1f;

    [Tooltip("Segundos minimos entre un golpe y el siguiente.")]
    [SerializeField] private float segundosEntreGolpes = 0.5f;

    [SerializeField] private KeyCode teclaRecolectar = KeyCode.E;

    [Header("Solo si este nodo es la tienda (Tipo = Armas)")]
    [Tooltip("Arrastra el GameObject TiendaManager. Al presionar E se abre el panel.")]
    [SerializeField] private TiendaArmasController tienda;

    [Header("Cartelito de la tecla")]
    [Tooltip("Si lo dejas vacio se crea uno solo con la letra de la tecla. " +
             "Asi no hay que armarlo a mano en cada arbol.")]
    [SerializeField] private GameObject indicadorPresionaE;

    [SerializeField] private float alturaIndicador = 1.2f;
    [SerializeField] private float tamanoIndicador = 3.5f;
    [SerializeField] private Color colorIndicador = new Color(1f, 0.9f, 0.4f);

    [Tooltip("Fuente del cartelito. Opcional.")]
    [SerializeField] private TMPro.TMP_FontAsset fuenteIndicador;

    [Header("Animacion del nodo")]
    [Tooltip("El Animator del arbol / la oveja, para que se sacuda al golpearlo. " +
             "Dejalo vacio: si el nodo tiene Animator se encuentra solo.")]
    [SerializeField] private Animator animadorNodo;

    [Tooltip("Nombre del Trigger en ESE Animator. OJO: en Tiny Swords el estado " +
             "'Chopped' del arbol NO es una animacion de golpe, es el tocon. " +
             "Dejalo VACIO salvo que hayas creado tu propia animacion de golpe.")]
    [SerializeField] private string triggerNodo = "";

    [Header("Sacudida al golpear")]
    [Tooltip("Tiny Swords no trae animacion de 'me golpearon', asi que se hace " +
             "por codigo: el nodo se mece un momento al recibir el golpe.")]
    [SerializeField] private bool sacudirAlGolpear = true;

    [Tooltip("Cuanto se desplaza, en unidades del mundo. 0.08 se ve bien; " +
             "mas que eso empieza a verse exagerado.")]
    [SerializeField] private float fuerzaSacudida = 0.08f;

    [SerializeField] private float duracionSacudida = 0.18f;

    [Tooltip("Ademas de mecerse, el nodo se tiñe un instante al recibir el " +
             "golpe. Es el clasico destello de daño; en la oveja es lo que la " +
             "pone roja.")]
    [SerializeField] private bool destelloAlGolpear = true;

    [SerializeField] private Color colorDestello = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private float duracionDestello = 0.12f;

    [Header("Cuando se agota")]
    [Tooltip("Esconde el sprite mientras el nodo esta en cero y lo vuelve a " +
             "mostrar cuando se regenera. Util para la oveja: al cazarla " +
             "desaparece y reaparece sola al rato.")]
    [SerializeField] private bool esconderSiSeAgota;

    [Tooltip("Sprite para cuando se agota, por ejemplo un tocon de " +
             "Tiny Swords/Pawn and Resources/Wood/Trees. Si lo pones, se usa " +
             "este en vez de esconderlo.")]
    [SerializeField] private Sprite spriteAgotado;

    [Tooltip("Cuanto tiene que haberse regenerado para que vuelva a verse " +
             "entero. Si fuera en cuanto pase de 0, el tocon duraria un " +
             "segundo y no se alcanzaria a ver. Con 20 y regeneracion de 1 " +
             "por segundo, el tocon se queda unos 20 segundos.")]
    [SerializeField] private int volverACrecerCon = 20;

    [Header("Modo mina: agotarse y reaparecer")]
    [Tooltip("Marcalo para las menas de oro. En vez de irse rellenando poco a " +
             "poco, el nodo se agota a los pocos golpes, desaparece, y al rato " +
             "vuelve LLENO de una vez. Es lo que uno espera de una veta: se " +
             "acaba, no 'crece'.\n\n" +
             "Con esto marcado no se usa Regeneracion Por Segundo ni Volver A " +
             "Crecer Con: manda Segundos Para Reaparecer.")]
    [SerializeField] private bool modoMina;

    [Tooltip("Cuanto tarda en volver a aparecer, en segundos.")]
    [SerializeField] private float segundosParaReaparecer = 30f;

    [Tooltip("Trigger del Animator DEL JUGADOR que se dispara al recolectar. " +
             "Es el mismo que usa el ataque, asi el Override Controller de cada " +
             "herramienta hace que se vea talando con el hacha, picando con el " +
             "pico y construyendo con el martillo. Dejalo vacio para no animarlo.")]
    [SerializeField] private string triggerJugador = "Atacar";

    // --- Modelo ---
    private RecursoModel _nodo;

    // --- Estado de la escena ---
    private JugadorArmasController _jugadorEnZona;
    private Animator _animadorJugador;
    private float _proximoGolpePermitido;

    private SpriteRenderer _sprite;
    private Sprite _spriteNormal;
    private bool _agotado;

    // Solo se le manda el trigger al Animator del nodo si ese parametro existe
    // de verdad. Si no, Unity escribe un warning en CADA golpe y llena la
    // consola de ruido.
    private bool _animadorNodoUsable;

    private Coroutine _sacudida;
    private Vector3 _posicionDelSprite;

    private Coroutine _destello;
    private Color _colorNormal = Color.white;

    // Modo mina: momento en el que vuelve a aparecer.
    private float _reapareceEn;

    /// <summary>
    /// Avisa a la UI (el HUD de recursos) que el jugador acaba de recoger algo.
    /// Se dispara SIEMPRE desde el hilo principal de Unity, asi que el HUD
    /// puede actualizarse directamente sin colas ni nada raro.
    /// </summary>
    public static event Action<TipoRecurso, int> RecursoRecolectado;

    /// <summary>
    /// Este nodo en concreto acaba de recibir un golpe. Lo usa la oveja para
    /// salir corriendo. Es un evento de instancia y no estatico como el de
    /// arriba, porque a cada oveja solo le importan SUS golpes.
    /// </summary>
    public event Action Golpeado;

    /// <summary>Lo que queda en el nodo ahora mismo.</summary>
    public int Cantidad => _nodo != null ? _nodo.Cantidad : 0;

    /// <summary>true mientras esta en cero y todavia no vuelve a crecer.</summary>
    public bool Agotado => _agotado;

    private void Start()
    {
        // 1) Crear el Modelo. Se usa la subclase concreta segun el tipo.
        _nodo = CrearModelo(tipo);
        _nodo.CantidadMaxima = cantidadMaxima;

        // En modo mina el hilo no repone nada: la veta se vacia y punto. Quien
        // la vuelve a llenar es el temporizador de reaparicion, de golpe.
        _nodo.RitmoRegeneracion = modoMina ? 0 : regeneracionPorSegundo;

        // Las subclases nacen en 0, asi que se siembra con lo del Inspector.
        _nodo.Sembrar(cantidadInicial);

        // 2) Arrancar el hilo de regeneracion: el arbol crece solo, siempre.
        //    La tienda (Armas) no regenera nada, asi que no le gastamos un hilo.
        if (tipo != TipoRecurso.Armas)
            _nodo.Recoleccion();

        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();

        if (_sprite != null)
        {
            _spriteNormal = _sprite.sprite;
            _colorNormal = _sprite.color;
        }

        if (indicadorPresionaE == null)
            indicadorPresionaE = CrearIndicador();

        MostrarIndicador(false);

        AvisarSiNoHayTrigger();
        PrepararAnimadorDelNodo();
        Resumen();
    }

    /// <summary>
    /// Una linea en consola por cada nodo al arrancar, con todo lo que suele
    /// estar mal configurado. Vale la pena el ruido: sin esto, un nodo que no
    /// responde se puede deber a cinco cosas distintas y hay que ir probando
    /// a ciegas.
    /// </summary>
    private void Resumen()
    {
        string zona = "SIN TRIGGER";
        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (!c.isTrigger) continue;

            CircleCollider2D circulo = c as CircleCollider2D;
            zona = circulo != null
                ? "trigger circular radio " + circulo.radius.ToString("0.00")
                : "trigger " + c.GetType().Name;
            break;
        }

        string pide = exigirHerramientaCorrecta ? NecesitasTexto() : "cualquier herramienta sirve";

        string ritmo = modoMina
            ? "modo mina, reaparece a los " + segundosParaReaparecer + " s"
            : "regenera " + (regeneracionPorSegundo >= 0 ? regeneracionPorSegundo.ToString() : "lo del Modelo") + "/s";

        Debug.Log("[Recurso] " + gameObject.name + " listo: " + tipo + " " +
                  _nodo.Cantidad + "/" + cantidadMaxima +
                  " | " + pide +
                  " | " + ritmo +
                  " | " + zona +
                  " | sprite: " + (_sprite != null ? _sprite.name : "NINGUNO") +
                  " | cartelito: " + (indicadorPresionaE != null ? teclaRecolectar.ToString() : "NO SE CREO"));
    }

    /// <summary>
    /// Encuentra el Animator del propio nodo y comprueba que tenga el Trigger.
    /// Asi no hay que arrastrar nada en el Inspector, y si al arbol todavia no
    /// le has creado el parametro se avisa UNA vez en vez de en cada golpe.
    /// </summary>
    private void PrepararAnimadorDelNodo()
    {
        if (animadorNodo == null) animadorNodo = GetComponent<Animator>();
        if (animadorNodo == null) animadorNodo = GetComponentInChildren<Animator>();

        _animadorNodoUsable = TieneParametro(animadorNodo, triggerNodo);

        if (animadorNodo != null && !string.IsNullOrEmpty(triggerNodo) && !_animadorNodoUsable)
        {
            Debug.LogWarning("[Recurso] " + gameObject.name + " tiene Animator pero su " +
                             "controller no tiene ningun parametro llamado '" + triggerNodo +
                             "'. No se va a animar el nodo. Crea ese Trigger en el " +
                             "Animator Controller, o borra el nombre del campo Trigger Nodo.");
        }
    }

    /// <summary>
    /// El "me golpearon" hecho a mano: el dibujo se mece de lado a lado y
    /// vuelve a su sitio. Se mueve el transform del SPRITE y no el del nodo,
    /// para no arrastrar con el los colliders (si el sprite esta en el mismo
    /// objeto que el collider, la sacudida es tan chica que no empuja a nadie).
    /// </summary>
    private void Sacudir()
    {
        if (!sacudirAlGolpear || _sprite == null) return;

        if (_sacudida != null)
        {
            // Si vuelve a golpear antes de terminar, se reinicia desde la
            // posicion buena y no desde donde quedo a medio camino.
            StopCoroutine(_sacudida);
            _sprite.transform.localPosition = _posicionDelSprite;
        }

        _sacudida = StartCoroutine(RutinaSacudida());
    }

    private System.Collections.IEnumerator RutinaSacudida()
    {
        Transform t = _sprite.transform;
        _posicionDelSprite = t.localPosition;

        float transcurrido = 0f;

        while (transcurrido < duracionSacudida)
        {
            transcurrido += Time.deltaTime;

            // Una oscilacion que se va apagando: empieza fuerte y termina en
            // cero, asi no se corta de golpe.
            float queda = 1f - (transcurrido / duracionSacudida);
            float desplazamiento = Mathf.Sin(transcurrido * 45f) * fuerzaSacudida * queda;

            t.localPosition = _posicionDelSprite + new Vector3(desplazamiento, 0f, 0f);
            yield return null;
        }

        t.localPosition = _posicionDelSprite;
        _sacudida = null;
    }

    /// <summary>
    /// Destello de daño: el sprite se tiñe un instante y vuelve a su color.
    /// Tiny Swords no trae ninguna animacion para esto, y con el Animator
    /// encima seria imposible: el Animator pisa el sprite en cada frame, pero
    /// NO toca el color, asi que este truco convive con el sin pelearse.
    /// </summary>
    private void Destellar()
    {
        if (!destelloAlGolpear || _sprite == null) return;

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

    // ------------------------------------------------------------------
    //  Que herramienta pide cada recurso
    // ------------------------------------------------------------------

    // Se calcula UNA vez para todo el juego preguntandole a cada arma cuanto
    // rinde en cada recurso. Asi no hay una tabla repetida aqui: si mañana se
    // cambian los multiplicadores en ArmasConcretas, esto se entera solo.
    private static Dictionary<TipoRecurso, TipoArma> _herramientaIdeal;

    private static bool TryHerramientaIdeal(TipoRecurso recurso, out TipoArma herramienta)
    {
        if (_herramientaIdeal == null)
        {
            _herramientaIdeal = new Dictionary<TipoRecurso, TipoArma>();

            foreach (var par in CatalogoArmas.Fichas)
            {
                // Crear un arma no arranca ningun hilo: eso solo pasa al
                // equiparla. Estas son de usar y tirar, solo para consultar.
                ArmaModel muestra = CatalogoArmas.Crear(par.Key);

                foreach (TipoRecurso r in (TipoRecurso[])Enum.GetValues(typeof(TipoRecurso)))
                {
                    if (muestra.MultiplicadorRecoleccion(r) >= 1f &&
                        !_herramientaIdeal.ContainsKey(r))
                    {
                        _herramientaIdeal[r] = par.Key;
                    }
                }
            }
        }

        return _herramientaIdeal.TryGetValue(recurso, out herramienta);
    }

    /// <summary>
    /// A la consola y a la pantalla. El jugador no ve la consola, y sin aviso
    /// visible parece que la tecla no hiciera nada.
    /// </summary>
    private static void Avisar(string mensaje)
    {
        Debug.Log("[Recurso] " + mensaje);
        AvisoPantalla.Mostrar(mensaje);
    }

    private string NecesitasTexto()
    {
        return TryHerramientaIdeal(tipo, out TipoArma cual)
            ? "Necesitas " + CatalogoArmas.Nombre(cual) + "."
            : "Necesitas otra herramienta.";
    }

    private static bool TieneParametro(Animator a, string nombre)
    {
        if (a == null || string.IsNullOrEmpty(nombre)) return false;
        if (a.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter p in a.parameters)
            if (p.name == nombre) return true;

        return false;
    }

    /// <summary>
    /// La zona de recoleccion tiene que ser un collider EN MODO TRIGGER. Si es
    /// solido no detecta nada: solo empuja al jugador, que se queda chocando
    /// contra un muro invisible enorme sin entender por que.
    ///
    /// Aqui solo se avisa, no se corrige: un arbol puede tener a proposito
    /// otro collider solido pequeño en el tronco para que no se lo atraviesen,
    /// y marcarlo como trigger a la fuerza le quitaria eso.
    /// </summary>
    private void AvisarSiNoHayTrigger()
    {
        foreach (Collider2D c in GetComponents<Collider2D>())
            if (c.isTrigger) return;

        Debug.LogWarning("[Recurso] " + gameObject.name + " no tiene ningun Collider2D " +
                         "en modo Is Trigger, asi que no va a detectar al jugador: solo " +
                         "le va a estorbar el paso. Marca Is Trigger en el collider grande.");
    }

    /// <summary>
    /// El cartelito con la tecla, hecho con TextMeshPro 3D (no Canvas). Un
    /// Canvas en World Space obliga a pelear con la escala y termina saliendo
    /// gigante o invisible; esto se porta como cualquier otro sprite.
    /// </summary>
    private GameObject CrearIndicador()
    {
        var go = new GameObject("IndicadorTecla");
        go.transform.SetParent(transform, false);

        // El padre puede estar escalado (los mapas se agrandaron), y el texto
        // heredaria esa escala y saldria gigante o microscopico. Se compensa
        // para que el cartelito mida siempre lo mismo.
        Vector3 escalaPadre = transform.lossyScale;
        float ex = Mathf.Approximately(escalaPadre.x, 0f) ? 1f : 1f / escalaPadre.x;
        float ey = Mathf.Approximately(escalaPadre.y, 0f) ? 1f : 1f / escalaPadre.y;

        go.transform.localScale = new Vector3(ex, ey, 1f);
        go.transform.localPosition = new Vector3(0f, alturaIndicador * ey, 0f);

        var texto = go.AddComponent<TMPro.TextMeshPro>();
        texto.text = teclaRecolectar.ToString();
        texto.fontSize = tamanoIndicador;
        texto.color = colorIndicador;
        texto.alignment = TMPro.TextAlignmentOptions.Center;

        if (fuenteIndicador != null) texto.font = fuenteIndicador;

        // ESTO ES LO QUE LO HACE VISIBLE.
        //
        // Un objeto nuevo nace en la Sorting Layer "Default", que en este
        // proyecto esta por DEBAJO de las capas del mapa. Poniendole
        // sortingOrder alto no basta: el orden solo compite dentro de la misma
        // capa. Asi que se copia la capa del propio nodo y se le suma al orden,
        // que garantiza que se dibuje delante de su arbol y de todo lo que
        // este a su mismo nivel.
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (_sprite != null)
            {
                renderer.sortingLayerID = _sprite.sortingLayerID;
                renderer.sortingOrder = _sprite.sortingOrder + 10;
            }
            else
            {
                renderer.sortingOrder = 100;
            }
        }

        return go;
    }

    /// <summary>
    /// Pone o quita el aspecto de agotado. Se llama despues de cada golpe y
    /// tambien mientras el jugador esta en la zona, para que el arbol vuelva a
    /// aparecer solo cuando el hilo de regeneracion lo rellene.
    /// </summary>
    private void RevisarSiSeAgoto()
    {
        if (_nodo == null || _sprite == null) return;

        // El modo mina siempre esconde la veta al agotarse, aunque no se haya
        // marcado nada: sin eso el temporizador de reaparicion no correria y
        // la mina se quedaria vacia para siempre.
        if (!modoMina && !esconderSiSeAgota && spriteAgotado == null) return;

        int cantidad = _nodo.Cantidad;

        // Dos umbrales distintos a proposito (histeresis): se convierte en
        // tocon al llegar a cero, pero no vuelve a ser arbol hasta que se haya
        // repuesto de verdad. Con un solo umbral el tocon duraria un segundo
        // (lo que tarda el hilo en sumar el primer punto) y ni se veria.
        bool vacio;

        if (modoMina)
        {
            // La veta no se rellena sola: se acaba, desaparece, y cuando pasa
            // el tiempo de reaparicion vuelve llena de una vez.
            if (_agotado)
            {
                if (Time.time < _reapareceEn) return;

                _nodo.Sembrar(cantidadInicial);
                vacio = false;
            }
            else
            {
                vacio = cantidad <= 0;
                if (vacio) _reapareceEn = Time.time + segundosParaReaparecer;
            }
        }
        else
        {
            // Dos umbrales distintos a proposito (histeresis): se convierte en
            // tocon al llegar a cero, pero no vuelve a ser arbol hasta que se
            // haya repuesto de verdad. Con un solo umbral el tocon duraria un
            // segundo (lo que tarda el hilo en sumar el primer punto).
            vacio = _agotado
                ? cantidad < volverACrecerCon   // sigue talado hasta recuperarse
                : cantidad <= 0;                // acaba de agotarse
        }

        if (vacio == _agotado) return;   // no cambio nada

        _agotado = vacio;

        if (spriteAgotado != null)
        {
            // Con tocon: se cambia el dibujo pero el objeto sigue visible.
            _sprite.sprite = vacio ? spriteAgotado : _spriteNormal;

            // El Animator pisa el sprite en cada frame, asi que si hay uno hay
            // que apagarlo mientras se muestra el tocon.
            if (animadorNodo != null) animadorNodo.enabled = !vacio;
        }
        else
        {
            _sprite.enabled = !vacio;
        }
    }

    /// <summary>
    /// Crea la subclase concreta que corresponde.
    ///
    /// Las 4 subclases estan en el namespace ImperiosEnGuerra.Modelo.Recursos
    /// (por eso el using de arriba) y su constructor recibe SOLO las
    /// coordenadas: el tipo y la cantidad inicial las pone la propia clase,
    /// que siempre arranca en 0. La cantidad de arranque se pone despues,
    /// con Sembrar().
    /// </summary>
    private RecursoModel CrearModelo(TipoRecurso t)
    {
        float coords = transform.position.x; // tu Coordenadas es un solo float

        switch (t)
        {
            case TipoRecurso.Oro: return new OroModel(coords);
            case TipoRecurso.Madera: return new MaderaModel(coords);
            case TipoRecurso.Comida: return new ComidaModel(coords);
            case TipoRecurso.Armas: return new ArmasModel(coords);
            default:
                throw new ArgumentOutOfRangeException(nameof(t));
        }
    }

    private void Update()
    {
        // Primero, porque el hilo de regeneracion puede haber rellenado el
        // nodo mientras no habia nadie cerca y el arbol tiene que reaparecer
        // solo. Si no cambio nada, esto no hace practicamente trabajo.
        RevisarSiSeAgoto();

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

        // El Pawn empieza sin herramientas: hay que ir a la tienda primero.
        if (arma == null)
        {
            Avisar("No llevas ninguna herramienta. Compra una en la Armeria.");
            return;
        }

        if (arma.EstaRota)
        {
            Avisar("Tu " + arma.Nombre + " esta roto. Espera a que se repare.");
            return;
        }

        // La herramienta equivocada no hace nada. Antes rendia un 25% y el
        // jugador podia talar un arbol a cuchilladas, lo cual no tiene sentido
        // y ademas le quitaba la gracia a comprar el hacha.
        if (exigirHerramientaCorrecta &&
            arma.MultiplicadorRecoleccion(tipo) < multiplicadorMinimo)
        {
            Avisar("Con " + arma.Nombre + " no puedes con esto. " + NecesitasTexto());
            return;
        }

        // 0) La animacion del jugador. Se lanza antes de mirar si queda algo en
        //    el nodo: aunque el arbol este pelado, el muñeco responde a la tecla
        //    y no parece que el juego se hubiera colgado.
        if (_animadorJugador != null && !string.IsNullOrEmpty(triggerJugador))
            _animadorJugador.SetTrigger(triggerJugador);

        // 1) El ARMA decide cuanto rinde el golpe (hacha x2 en madera, pico x2
        //    en oro, cuchillo x2 en comida, lo demas x0.25) y se desgasta.
        int pedido = arma.CalcularRecoleccion(tipo, rendimientoBasePorGolpe);

        // 2) El NODO entrega lo que realmente tenga. Esto es thread-safe:
        //    entre la lectura y el descuento no se cuela el hilo de regeneracion.
        int obtenido = _nodo.Recolectar(pedido);

        if (obtenido <= 0)
        {
            Avisar("Se agoto. Dale tiempo a que se reponga.");
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

        if (_animadorNodoUsable && animadorNodo.enabled)
            animadorNodo.SetTrigger(triggerNodo);

        Sacudir();
        Destellar();

        RevisarSiSeAgoto();

        Golpeado?.Invoke();
        RecursoRecolectado?.Invoke(tipo, obtenido);

        Debug.Log("[Recurso] +" + obtenido + " de " + tipo
                  + " con " + arma.Nombre
                  + " | queda en el nodo: " + _nodo.Cantidad
                  + " | arma: " + arma.Durabilidad + "/" + arma.DurabilidadMaxima);

        // Y al archivo log_partida.txt, que es lo que pide el enunciado.
        ImperiosEnGuerra.Controlador.Bitacora.Anotar(
            "Recoleccion",
            "+" + obtenido + " de " + RecursoTipoHelper.ATexto(tipo) +
            " con " + arma.Nombre + " (queda " + _nodo.Cantidad +
            " en el nodo, herramienta " + arma.Durabilidad + "/" +
            arma.DurabilidadMaxima + ")");
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

        // Se guarda aqui y no en cada golpe para no andar buscando el
        // componente varias veces por segundo.
        _animadorJugador = other.GetComponent<Animator>();

        MostrarIndicador(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _jugadorEnZona = null;
        _animadorJugador = null;
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