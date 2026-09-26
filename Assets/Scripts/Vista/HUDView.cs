using ImperiosEnGuerra.Modelo;
using TMPro;
using UnityEngine;

// HUDView: muestra los recursos del jugador (Oro, Madera, Comida) en pantalla.
//
// COMO USARLO EN UNITY:
// 1. Crea un Canvas en la escena Juego con textos de TMP para Oro, Madera y Comida.
// 2. Crea un GameObject vacio llamado "HUD" y arrastra este script.
// 3. En el Inspector conecta los tres TMP_Text.
//
// ACTUALIZACION:
// Cuando el jugador recolecta un recurso, RecursoNodoController dispara el evento
// RecursoRecolectado desde el hilo PRINCIPAL de Unity, asi que el HUD puede
// actualizar el texto directamente sin necesitar MainThreadDispatcher.
public class HUDView : MonoBehaviour
{
    [Header("Textos de recursos")]
    [SerializeField] private TMP_Text textoOro;
    [SerializeField] private TMP_Text textoMadera;
    [SerializeField] private TMP_Text textoComida;
    [SerializeField] private TMP_Text textoArmas;

    [Header("Texto de vida del jugador (opcional)")]
    [SerializeField] private TMP_Text textoVida;

    private JugadorModel _jugador;

    private void Start()
    {
        if (PlayerSelectionManager.Instance != null && PlayerSelectionManager.Instance.Partida != null)
        {
            _jugador = PlayerSelectionManager.Instance.Partida.Jugador;
        }
        else
        {
            Debug.LogWarning("[HUD] No hay partida activa. El HUD no mostrara datos.");
            return;
        }

        // Suscribirse al evento de recoleccion para actualizar en tiempo real
        RecursoNodoController.RecursoRecolectado += AlRecolectar;

        Actualizar();
    }

    // Se llama cuando el jugador recolecta un recurso (evento del nodo)
    private void AlRecolectar(TipoRecurso tipo, int cantidad)
    {
        Actualizar();
    }

    // Actualiza todos los textos con los valores actuales del jugador
    private void Actualizar()
    {
        if (_jugador == null) return;

        if (textoOro    != null) textoOro.text    = "Oro: "    + _jugador.Oro;
        if (textoMadera != null) textoMadera.text = "Madera: " + _jugador.Madera;
        if (textoComida != null) textoComida.text = "Comida: " + _jugador.Comida;
        if (textoArmas  != null) textoArmas.text  = "Armas: "  + _jugador.Armas;
        if (textoVida   != null) textoVida.text   = "Vida: "   + _jugador.Vida + "/" + _jugador.VidaMax;
    }

    // Update: refresca cada frame para capturar cambios de hilos
    // (por ejemplo el hilo de la IA quitando vida al jugador)
    private void Update()
    {
        Actualizar();
    }

    private void OnDestroy()
    {
        RecursoNodoController.RecursoRecolectado -= AlRecolectar;
    }
}
