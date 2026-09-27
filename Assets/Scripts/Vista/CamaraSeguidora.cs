using UnityEngine;

// VISTA: hace que la camara siga al jugador por el mapa.
//
// COMO USARLO EN UNITY:
// 1. Selecciona la Main Camera de la escena Juego.
// 2. Add Component -> CamaraSeguidora.
// 3. Arrastra el GameObject Jugador al campo "Objetivo".
//    (Si lo dejas vacio, lo busca solo por el tag "Player".)
//
// POR QUE LateUpdate Y NO Update:
// el jugador se mueve en FixedUpdate/Update. Si la camara se moviera en
// Update podria leer la posicion ANTES de que el jugador se moviera ese
// frame, y se ve un temblor. LateUpdate corre siempre de ultimo, cuando
// todo el mundo ya termino de moverse.

public class CamaraSeguidora : MonoBehaviour
{
    [Header("A quien sigue")]
    [Tooltip("Arrastra el GameObject Jugador. Si lo dejas vacio se busca por el tag Player.")]
    public Transform objetivo;

    [Header("Suavizado")]
    [Tooltip("0 = la camara va pegada al jugador (rigido). " +
             "0.15 se siente natural. Mas alto = la camara se queda mas atras.")]
    [Range(0f, 1f)]
    public float suavizado = 0.15f;

    [Tooltip("Corrimiento respecto al jugador. La Z debe ser NEGATIVA " +
             "(-10 normalmente) o la camara queda detras del mapa y no se ve nada.")]
    public Vector3 desplazamiento = new Vector3(0f, 0f, -10f);

    [Header("Limites del mapa (opcional)")]
    [Tooltip("Marcalo para que la camara no se salga de la isla y muestre el vacio de afuera.")]
    public bool usarLimites = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;

    private Vector3 _velocidad = Vector3.zero;

    private void Start()
    {
        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");

            if (jugador != null)
            {
                objetivo = jugador.transform;
            }
            else
            {
                Debug.LogWarning("[Camara] No encontre al Jugador. " +
                                 "Arrastralo al campo Objetivo, o ponle el tag Player.");
                enabled = false;
                return;
            }
        }

        // Arranca ya centrada en el jugador, sin el barrido inicial desde
        // donde quedo la camara puesta en el editor.
        transform.position = Posicion(objetivo.position);
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        Vector3 destino = Posicion(objetivo.position);

        transform.position = suavizado <= 0f
            ? destino
            : Vector3.SmoothDamp(transform.position, destino, ref _velocidad, suavizado);
    }

    private Vector3 Posicion(Vector3 posicionObjetivo)
    {
        Vector3 destino = posicionObjetivo + desplazamiento;

        if (usarLimites)
        {
            destino.x = Mathf.Clamp(destino.x, minX, maxX);
            destino.y = Mathf.Clamp(destino.y, minY, maxY);
        }

        return destino;
    }
}
