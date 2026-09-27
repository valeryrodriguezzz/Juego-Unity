using System.Collections.Generic;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Edificios;
using TMPro;
using UnityEngine;

/// <summary>
/// CONTROLADOR del edificio que el jugador acaba de mandar a construir: el que
/// se ve en el mapa, en el sitio exacto donde lo puso.
///
/// QUE PROBLEMA RESUELVE
/// Antes el panel de construccion cobraba el oro, arrancaba el hilo de obra y
/// avisaba "Granja terminada", pero no aparecia nada en ninguna parte: el
/// edificio existia solo como objeto del Modelo. Esta clase es la Vista de ese
/// EdificioModel.
///
/// COMO SE REPARTE EL TRABAJO (esto es lo que hay que contar en la sustentacion)
///  - El MODELO (EdificioModel) tiene el hilo de obra: el que va subiendo
///    PorcentajeConstruccion, y el de produccion, que le suma el recurso al
///    jugador cada segundo.
///  - Este CONTROLADOR no calcula nada de eso. Cada frame le pregunta al Modelo
///    en que porcentaje va y pinta el edificio en consecuencia: translucido
///    mientras se levanta, solido cuando termina.
///
/// Es la unica forma correcta de hacerlo: el hilo de obra NO puede tocar la
/// pantalla (Unity solo deja hacerlo desde el hilo principal), asi que el hilo
/// escribe un numero y el frame lo lee. Escribir y leer un float protegido por
/// el lock del Modelo es exactamente lo que hace PorcentajeConstruccion.
///
/// NO HAY QUE MONTAR NADA EN UNITY: lo crea ConstruccionView cuando el jugador
/// hace clic para colocar el edificio.
/// </summary>
public class EdificioConstruidoController : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────────
    //  Memoria de lo construido en cada mapa
    //
    //  Los edificios son del jugador, no de la escena: si construye una
    //  granja en Grecia, se va a pelear a Roma y vuelve, la granja tiene que
    //  seguir ahi. Los GameObjects se destruyen al cambiar de escena, pero
    //  los EdificioModel (y sus hilos de produccion) no, asi que aqui se
    //  guarda donde estaba cada uno para volver a dibujarlo al regresar.
    // ────────────────────────────────────────────────────────────────────

    private class Registro
    {
        public EdificioModel Modelo;
        public Vector3 Posicion;
        public Sprite Sprite;
        public int Capa;
        public int Orden;
        public float Escala;
    }

    private static readonly Dictionary<string, List<Registro>> _porEscena =
        new Dictionary<string, List<Registro>>();

    // De que partida son los registros guardados. Si empieza una partida
    // nueva se botan, para no heredar los edificios de la anterior.
    private static object _duenoDelRegistro;

    public static void OlvidarTodo()
    {
        _porEscena.Clear();
        _duenoDelRegistro = null;
    }

    /// <summary>
    /// Se llama al entrar a cada mapa, antes de nada. Si la partida es otra
    /// (el jugador empezo una nueva o cargo una guardada), se botan los
    /// edificios apuntados de la anterior.
    /// </summary>
    public static void PrepararEscena(object partida)
    {
        if (ReferenceEquals(_duenoDelRegistro, partida)) return;

        OlvidarTodo();
        _duenoDelRegistro = partida;
    }

    /// <summary>
    /// Apunta un edificio sin ponerlo todavia en el mapa. La usa la carga de
    /// partidas: hay que registrar los edificios de LOS CINCO mapas, pero
    /// solo se dibujan los del mapa en el que se esta.
    /// </summary>
    public static void Registrar(EdificioModel modelo, Vector3 posicion, Sprite sprite,
                                 int capa, int orden, float escala, string escena)
    {
        if (modelo == null || string.IsNullOrEmpty(escena)) return;

        Apuntar(new Registro
        {
            Modelo = modelo,
            Posicion = posicion,
            Sprite = sprite,
            Capa = capa,
            Orden = orden,
            Escala = escala
        }, escena);
    }

    private static void Apuntar(Registro r, string escena)
    {
        if (!_porEscena.TryGetValue(escena, out List<Registro> lista))
        {
            lista = new List<Registro>();
            _porEscena[escena] = lista;
        }

        lista.Add(r);
    }

    /// <summary>
    /// Vuelve a levantar en esta escena los edificios que el jugador ya habia
    /// construido aqui. La llama ConstruccionView al arrancar cada mapa.
    /// </summary>
    public static void RehacerEscena(string escena, Transform padre)
    {
        if (!_porEscena.TryGetValue(escena, out List<Registro> lista)) return;

        foreach (Registro r in lista)
            Levantar(r, padre, false);

        if (lista.Count > 0)
            Debug.Log("[Edificio] Volvi a poner " + lista.Count +
                      " edificio(s) que ya tenias en " + escena + ".");
    }

    /// <summary>Un edificio apuntado, tal como lo necesita el guardado.</summary>
    public struct Apunte
    {
        public string Escena;
        public EdificioModel Modelo;
        public float X;
        public float Y;
    }

    /// <summary>Todo lo construido, para escribirlo en la partida guardada.</summary>
    public static List<Apunte> Exportar()
    {
        var todos = new List<Apunte>();

        foreach (KeyValuePair<string, List<Registro>> par in _porEscena)
            foreach (Registro r in par.Value)
                todos.Add(new Apunte
                {
                    Escena = par.Key,
                    Modelo = r.Modelo,
                    X = r.Posicion.x,
                    Y = r.Posicion.y
                });

        return todos;
    }

    /// <summary>
    /// Crea el edificio en el mapa y lo apunta para poder rehacerlo despues.
    /// </summary>
    public static EdificioConstruidoController Crear(EdificioModel modelo, Sprite sprite,
                                                     Vector3 posicion, Transform padre,
                                                     int capa, int orden, float escala,
                                                     string escena)
    {
        var r = new Registro
        {
            Modelo = modelo,
            Posicion = posicion,
            Sprite = sprite,
            Capa = capa,
            Orden = orden,
            Escala = escala
        };

        if (!string.IsNullOrEmpty(escena)) Apuntar(r, escena);

        return Levantar(r, padre, true);
    }

    private static EdificioConstruidoController Levantar(Registro r, Transform padre, bool esNuevo)
    {
        var go = new GameObject(r.Modelo.Nombre);
        go.transform.SetParent(padre, true);
        go.transform.position = r.Posicion;
        go.transform.localScale = new Vector3(r.Escala, r.Escala, 1f);

        var ctrl = go.AddComponent<EdificioConstruidoController>();
        ctrl.Preparar(r, esNuevo);

        return ctrl;
    }

    // ────────────────────────────────────────────────────────────────────

    private EdificioModel _modelo;
    private SpriteRenderer _sprite;
    private BoxCollider2D _cuerpo;
    private TextMeshPro _cartel;
    private bool _yaTermino;

    public EdificioModel Modelo => _modelo;

    private void Preparar(Registro r, bool esNuevo)
    {
        _modelo = r.Modelo;

        _sprite = gameObject.AddComponent<SpriteRenderer>();
        _sprite.sprite = r.Sprite;
        _sprite.sortingLayerID = r.Capa;
        _sprite.sortingOrder = r.Orden;

        // El collider solido cubre solo la parte de abajo del sprite: asi el
        // jugador puede pasar por detras del tejado, que es lo que se espera
        // en una vista cenital, y solo choca con la base del edificio.
        Vector2 tamano = r.Sprite != null ? (Vector2)r.Sprite.bounds.size : Vector2.one;

        _cuerpo = gameObject.AddComponent<BoxCollider2D>();
        _cuerpo.size = new Vector2(tamano.x * 0.80f, tamano.y * 0.45f);
        _cuerpo.offset = new Vector2(0f, -tamano.y * 0.25f);

        _cartel = CrearCartel();

        // Si es una iglesia o una armeria, el edificio construido tiene que
        // servir para lo mismo que los que ya estaban puestos en el mapa. Se
        // le enchufa el mismo Controlador que usan aquellos.
        PonerleSuOficio(tamano);

        Pintar(_modelo.Construido ? 100f : _modelo.PorcentajeConstruccion);

        if (esNuevo)
            Debug.Log("[Edificio] " + _modelo.Nombre + " colocada en " +
                      transform.position + ".");
    }

    private void Update()
    {
        if (_modelo == null) return;
        if (_yaTermino) return;

        float pct = _modelo.Construido ? 100f : _modelo.PorcentajeConstruccion;
        Pintar(pct);
    }

    /// <summary>
    /// Lo unico que hace este Controlador: traducir el numero que va subiendo
    /// el hilo de obra a algo que se vea.
    /// </summary>
    private void Pintar(float pct)
    {
        bool listo = pct >= 100f || _modelo.Construido;

        if (listo)
        {
            _yaTermino = true;
            _sprite.color = Color.white;

            if (_cartel != null) _cartel.gameObject.SetActive(false);
            return;
        }

        // Mientras se levanta: translucido y grisaceo, y se va aclarando.
        float t = Mathf.Clamp01(pct / 100f);
        float a = Mathf.Lerp(0.35f, 0.95f, t);
        float gris = Mathf.Lerp(0.55f, 1f, t);

        _sprite.color = new Color(gris, gris, gris, a);

        if (_cartel != null)
            _cartel.text = _modelo.Nombre + "\n" + Mathf.FloorToInt(pct) + "%";
    }

    // ────────────────────────────────────────────────────────────────────
    //  Piezas
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// La iglesia que el jugador construye tiene que curar igual que los
    /// monasterios que ya estan en el mapa, y la armeria que construye tiene
    /// que abrir la tienda igual que la que ya esta. En vez de reescribir esa
    /// logica aqui se le agrega el mismo Controlador, con un collider en modo
    /// trigger aparte para la zona de interaccion.
    ///
    /// El trigger se agrega ANTES que el Controlador a proposito: los dos
    /// tienen un AsegurarTrigger() que, si no encuentra ninguno, convierte el
    /// ultimo collider en trigger. Sin este, convertiria el collider solido y
    /// el jugador podria atravesar el edificio.
    /// </summary>
    private void PonerleSuOficio(Vector2 tamano)
    {
        bool esIglesia = _modelo is IglesiaModel;
        bool esArmeria = _modelo is ArmeriaModel;

        if (!esIglesia && !esArmeria) return;

        var zona = gameObject.AddComponent<BoxCollider2D>();
        zona.isTrigger = true;
        zona.size = new Vector2(tamano.x * 1.15f, tamano.y * 0.9f);
        zona.offset = new Vector2(0f, -tamano.y * 0.1f);

        if (esIglesia)
        {
            var iglesia = gameObject.AddComponent<EdificioIglesiaController>();

            // Que use el modelo que el jugador acaba de pagar, no uno nuevo.
            iglesia.UsarModelo((IglesiaModel)_modelo);
            return;
        }

        gameObject.AddComponent<EdificioTiendaController>();
    }

    private TextMeshPro CrearCartel()
    {
        var go = new GameObject("Progreso");
        go.transform.SetParent(transform, false);

        Vector3 escala = transform.lossyScale;
        float ex = Mathf.Approximately(escala.x, 0f) ? 1f : 1f / escala.x;
        float ey = Mathf.Approximately(escala.y, 0f) ? 1f : 1f / escala.y;

        go.transform.localScale = new Vector3(ex, ey, 1f);
        go.transform.localPosition = new Vector3(0f, 1.3f * ey, 0f);

        var t = go.AddComponent<TextMeshPro>();
        t.text = "";
        t.fontSize = 3.2f;
        t.color = new Color(1f, 0.93f, 0.6f);
        t.alignment = TextAlignmentOptions.Center;

        // Misma capa que el edificio: si naciera en "Default" quedaria
        // escondido detras del mapa.
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && _sprite != null)
        {
            mr.sortingLayerID = _sprite.sortingLayerID;
            mr.sortingOrder = _sprite.sortingOrder + 10;
        }

        return t;
    }
}
