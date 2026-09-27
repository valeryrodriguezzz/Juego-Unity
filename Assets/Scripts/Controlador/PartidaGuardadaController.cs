using System.Collections.Generic;
using ImperiosEnGuerra.Controlador;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Edificios;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CONTROLADOR que une las dos mitades del guardado.
///
/// El Modelo (PartidaGuardadaModel) sabe que datos hay que guardar y como se
/// escriben. GuardadoController sabe en que archivo van. Pero ninguno de los
/// dos puede saber DONDE esta parado el muñequito ni cuanta hambre tiene:
/// eso solo se sabe desde la escena. Esa es la parte que hace esta clase.
///
/// AL GUARDAR
///   - le pide al Modelo la foto de la partida,
///   - le agrega la escena, la posicion del jugador, el hambre y los
///     edificios que hay puestos en los mapas,
///   - y se la pasa a GuardadoController para que la escriba.
///
/// AL CARGAR
///   La partida no se puede aplicar de una: hay que cambiar de escena
///   primero, y la escena tarda un frame en existir. Asi que se deja apuntada
///   como "pendiente", se pide la escena guardada y, cuando esa escena
///   termina de cargar, se aplica. Eso es lo que hace AlCargarEscena.
///
/// NO HAY QUE MONTAR NADA EN UNITY: se crea solo al arrancar el juego, igual
/// que AvisoPantalla.
/// </summary>
public class PartidaGuardadaController : MonoBehaviour
{
    private static PartidaGuardadaController _instancia;

    /// <summary>Lo que se cargo y todavia no se ha aplicado a la escena.</summary>
    private static PartidaGuardadaModel _pendiente;

    /// <summary>
    /// Los edificios de la partida cargada, ya recreados como Modelo pero
    /// todavia sin dibujo en el mapa. Se vacia en cuanto ConstruccionView los
    /// apunta, para no volver a ponerlos cada vez que se cambia de escena.
    /// </summary>
    private static List<PendienteEdificio> _edificiosPendientes;

    private class PendienteEdificio
    {
        public EdificioModel Modelo;
        public string Clave;
        public string Escena;
        public float X;
        public float Y;
    }

    /// <summary>
    /// La ranura que ESTA partida ya esta ocupando. 0 = todavia no se ha
    /// guardado nunca.
    ///
    /// Es lo que evita que una misma partida se multiplique por el archivo.
    /// Antes cada guardado buscaba la primera ranura libre, asi que dar a
    /// Guardar y despues salir al menu (que tambien guarda) dejaba la misma
    /// partida escrita dos veces, en dos ranuras distintas. Ahora la primera
    /// vez se elige ranura y a partir de ahi siempre se pisa esa.
    /// </summary>
    private static int _ranuraActual;

    public static PartidaGuardadaController Instancia => _instancia;

    // ────────────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearSiNoExiste()
    {
        if (_instancia != null) return;

        var go = new GameObject("PartidaGuardadaController");
        go.AddComponent<PartidaGuardadaController>();
    }

    private void Awake()
    {
        if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

        _instancia = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (_instancia == this) SceneManager.sceneLoaded -= AlCargarEscena;
    }

    // ────────────────────────────────────────────────────────────────────
    //  GUARDAR
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Guarda la partida en curso y devuelve la ranura usada, o -1 si no se
    /// pudo.
    ///
    /// La primera vez busca un hueco libre; a partir de ahi vuelve a escribir
    /// SIEMPRE en esa misma ranura. Una partida ocupa un solo sitio, se
    /// guarde una vez o veinte.
    /// </summary>
    public static int Guardar(int ranura = 0)
    {
        PartidaModel partida = PartidaActual();

        if (partida == null)
        {
            AvisoPantalla.Mostrar("No hay ninguna partida que guardar.");
            return -1;
        }

        PartidaGuardadaModel datos = PartidaGuardadaModel.Capturar(partida);

        AgregarLoDeLaEscena(datos);

        // Prioridad: la ranura que pidan > la que ya usa esta partida > una libre.
        int destino = ranura > 0 ? ranura : _ranuraActual;

        int usada = destino > 0
            ? (GuardadoController.Guardar(destino, datos) ? destino : -1)
            : GuardadoController.GuardarEnLibre(datos);

        if (usada > 0)
        {
            _ranuraActual = usada;

            Bitacora.Anotar("Guardar partida",
                "Ranura " + usada + " en " + datos.Escena + " (" + datos.Oro +
                " oro, " + datos.Madera + " madera, vida " + datos.Vida + ")");
        }

        AvisoPantalla.Mostrar(usada > 0
            ? "Partida guardada en la ranura " + usada + "."
            : "No se pudo guardar la partida.");

        return usada;
    }

    /// <summary>
    /// La parte que solo se puede saber mirando la escena: donde esta el
    /// jugador, en que mapa, cuanta hambre lleva y que edificios ha puesto.
    /// </summary>
    private static void AgregarLoDeLaEscena(PartidaGuardadaModel datos)
    {
        datos.Escena = SceneManager.GetActiveScene().name;

        GameObject jugador = GameObject.FindGameObjectWithTag("Player");

        if (jugador != null)
        {
            datos.X = jugador.transform.position.x;
            datos.Y = jugador.transform.position.y;

            var hambre = jugador.GetComponent<HambreController>();

            if (hambre != null && hambre.Hambre != null)
                datos.Hambre = hambre.Hambre.Nivel;
        }

        foreach (EdificioConstruidoController.Apunte a in EdificioConstruidoController.Exportar())
        {
            if (a.Modelo == null) continue;

            string clave = FabricaEdificios.Clave(a.Modelo);
            if (string.IsNullOrEmpty(clave)) continue;

            datos.Edificios.Add(new PartidaGuardadaModel.EdificioGuardado
            {
                Escena = a.Escena,
                Tipo = clave,
                X = a.X,
                Y = a.Y,
                Construido = a.Modelo.Construido
            });
        }
    }

    // ────────────────────────────────────────────────────────────────────
    //  CARGAR
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Arranca una partida a partir de lo guardado. Crea la partida, le
    /// vuelca los datos del Modelo, la deja apuntada como pendiente y pide la
    /// escena donde estaba el jugador. Lo demas se termina en AlCargarEscena.
    /// </summary>
    /// <param name="ranura">
    /// De que ranura salio. Se recuerda para que, al volver a guardar, esta
    /// partida siga en su sitio en vez de aparecer duplicada en otro.
    /// </param>
    public static bool Continuar(PartidaGuardadaModel datos, int ranura = 0)
    {
        if (datos == null) return false;

        if (PlayerSelectionManager.Instance == null)
        {
            Debug.LogWarning("[Guardado] No hay GameManager en la escena, " +
                             "no puedo cargar la partida.");
            return false;
        }

        PlayerSelectionManager.Instance.CrearPartida(datos.Nombre);

        PartidaModel partida = PlayerSelectionManager.Instance.Partida;

        datos.Aplicar(partida);

        // Los edificios se recrean YA (asi sus hilos de produccion empiezan a
        // correr de una vez), pero se quedan esperando a que ConstruccionView
        // los dibuje, que es quien sabe con que sprite van.
        List<EdificioModel> modelos = datos.RecrearEdificios(partida);

        _edificiosPendientes = new List<PendienteEdificio>();

        for (int i = 0; i < datos.Edificios.Count && i < modelos.Count; i++)
        {
            if (modelos[i] == null) continue;

            _edificiosPendientes.Add(new PendienteEdificio
            {
                Modelo = modelos[i],
                Clave = datos.Edificios[i].Tipo,
                Escena = datos.Edificios[i].Escena,
                X = datos.Edificios[i].X,
                Y = datos.Edificios[i].Y
            });
        }

        _pendiente = datos;

        // CrearPartida llamo a Olvidar() y puso la ranura en 0, asi que esto
        // tiene que ir despues.
        _ranuraActual = ranura;

        Debug.Log("[Guardado] Cargando la partida de " + datos.Nombre +
                  " en " + datos.Escena + ".");

        // CrearPartida limpio el log, asi que esta es la primera linea del
        // nuevo log_partida.txt y deja claro que la partida viene de un
        // archivo y no empezo de cero.
        Bitacora.Anotar("Cargar partida",
            "Continuando desde la ranura " + ranura + " en " + datos.Escena +
            " (guardada el " + datos.Fecha + ")");

        SceneManager.LoadScene(datos.Escena);
        return true;
    }

    /// <summary>
    /// Ya existe la escena: ahora si se puede mover al jugador y ponerle el
    /// hambre. Se hace al final del frame porque los Start de los demas
    /// scripts (el hambre entre ellos) todavia no han corrido.
    /// </summary>
    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (_pendiente == null) return;

        StartCoroutine(AplicarAlFinalDelFrame());
    }

    private System.Collections.IEnumerator AplicarAlFinalDelFrame()
    {
        yield return null;   // deja correr todos los Start de la escena

        PartidaGuardadaModel datos = _pendiente;
        _pendiente = null;

        if (datos == null) yield break;

        GameObject jugador = GameObject.FindGameObjectWithTag("Player");

        if (jugador != null)
        {
            // Con Rigidbody2D hay que mover el cuerpo, no solo el Transform:
            // si no, la fisica lo devuelve a donde estaba en el siguiente
            // FixedUpdate.
            var rb = jugador.GetComponent<Rigidbody2D>();
            var donde = new Vector2(datos.X, datos.Y);

            jugador.transform.position = new Vector3(datos.X, datos.Y, jugador.transform.position.z);
            if (rb != null) rb.position = donde;

            var hambre = jugador.GetComponent<HambreController>();

            if (hambre != null && hambre.Hambre != null && datos.Hambre >= 0)
                hambre.Hambre.RestaurarNivel(datos.Hambre);
        }
        else
        {
            Debug.LogWarning("[Guardado] No encontre al jugador en " + datos.Escena +
                             ", asi que no lo pude poner donde estaba.");
        }

        AvisoPantalla.Mostrar("Partida de " + datos.Nombre + " cargada.");
    }

    /// <summary>
    /// La llama ConstruccionView al arrancar cada mapa. Le pasa los edificios
    /// de la partida cargada para que los apunte con su dibujo; solo la
    /// primera vez, porque a partir de ahi ya viven en el registro.
    /// </summary>
    public static void RestaurarEdificiosSiHaceFalta(ConstruccionView panel)
    {
        if (_edificiosPendientes == null || panel == null) return;

        foreach (PendienteEdificio e in _edificiosPendientes)
            panel.ApuntarEdificioGuardado(e.Modelo, e.Clave, e.Escena, e.X, e.Y);

        Debug.Log("[Guardado] Recupere " + _edificiosPendientes.Count +
                  " edificio(s) de la partida guardada.");

        _edificiosPendientes = null;
    }

    /// <summary>
    /// Al empezar una partida nueva se olvida lo que quedara pendiente, y
    /// sobre todo la ranura: la partida nueva todavia no ocupa ninguna, y si
    /// heredara la de la anterior la pisaria al primer guardado.
    /// </summary>
    public static void Olvidar()
    {
        _pendiente = null;
        _edificiosPendientes = null;
        _ranuraActual = 0;
    }

    public static PartidaModel PartidaActual()
    {
        return PlayerSelectionManager.Instance != null
            ? PlayerSelectionManager.Instance.Partida
            : null;
    }
}
