using System;
using ImperiosEnGuerra.Modelo;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CONTROLADOR de la batalla de un territorio cuando se pelea EN EL MAPA
/// (el enemigo camina y ataca con su animacion, en vez de por botones).
///
/// Va en cada escena de territorio: Roma, Persia, Egipto y Vikingos.
/// Create Empty -> "CombateTerritorio" -> Add Component -> este script.
///
/// QUE HACE:
///   1. Apaga el ataque automatico del hilo de la IA, porque aqui quien pega
///      es el EnemigoController de cada muñeco. Si no, recibirias doble daño.
///   2. Vigila el final: si mueren todos los enemigos, ganaste el territorio;
///      si te quedas sin vida, perdiste.
///   3. Le avisa a PartidaModel, que es quien marca el territorio como
///      conquistado y dispara el panel de resultado.
///
/// El hilo de la IA sigue vivo aunque no pegue: es el que mantiene el estado
/// de la batalla y el que permite terminarla de forma ordenada.
/// </summary>
public class CombateTerritorioController : MonoBehaviour
{
    [Header("Fin de la batalla")]
    [Tooltip("Segundos de espera antes de declarar la victoria, para que se " +
             "vea caer al ultimo enemigo.")]
    [SerializeField] private float esperaTrasElUltimo = 1.2f;

    [Tooltip("Si no hay ningun enemigo en la escena, no se gana sola: sirve " +
             "para montar la escena sin que se declare victoria al entrar.")]
    [SerializeField] private bool exigirAlMenosUnEnemigo = true;

    private PartidaModel _partida;
    private bool _huboEnemigos;
    private bool _terminada;
    private float _momentoDelUltimo;

    private void Start()
    {
        if (PlayerSelectionManager.Instance != null)
            _partida = PlayerSelectionManager.Instance.Partida;

        if (_partida == null)
        {
            Debug.LogWarning("[Combate] No hay partida activa. Entra desde el mapa.");
            enabled = false;
            return;
        }

        // SIN BATALLA EMPEZADA NO HAY RESULTADO.
        //
        // PartidaModel.TerminarBatalla se sale sin hacer nada si no hay una
        // batalla activa, asi que el panel de victoria o derrota no aparece
        // nunca. Eso pasa al entrar a la escena del territorio sin pasar por el
        // mapa: por ejemplo dandole Play directamente aqui para probar rapido.
        //
        // En vez de dejar que falle en silencio, se abre la batalla del
        // territorio que corresponde a esta escena.
        if (!_partida.BatallaActiva)
            AbrirBatallaDeEstaEscena();

        // Aqui pega el enemigo de la escena, no el hilo.
        _partida.AtaqueAutomaticoIA = false;

        _huboEnemigos = EnemigoController.Vivos.Count > 0;

        Debug.Log("[Combate] Territorio con " + EnemigoController.Vivos.Count +
                  " enemigo(s). Batalla activa: " + _partida.BatallaActiva +
                  ". Golpea con la tecla de ataque.");
    }

    /// <summary>
    /// Busca en el mapa mundial el territorio que se llama como esta escena y
    /// le abre la batalla. Asi la escena funciona igual entrando desde el mapa
    /// que dandole Play directamente.
    /// </summary>
    private void AbrirBatallaDeEstaEscena()
    {
        string escena = SceneManager.GetActiveScene().name;

        if (_partida.MapaMundial == null)
        {
            Debug.LogWarning("[Combate] La partida no tiene mapa mundial.");
            return;
        }

        foreach (TerritorioModel t in _partida.MapaMundial.Territorios)
        {
            bool coincide =
                string.Equals(t.Nombre, escena, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.Imperio, escena, StringComparison.OrdinalIgnoreCase);

            if (!coincide || t.EsBase) continue;

            _partida.IniciarBatalla(t);

            Debug.Log("[Combate] No venias del mapa, asi que abri la batalla de " +
                      t.Nombre + " por mi cuenta. Ahora si puede haber victoria o derrota.");
            return;
        }

        Debug.LogWarning("[Combate] No encontre ningun territorio que se llame '" + escena +
                         "'. Sin batalla abierta no va a salir el panel de resultado. " +
                         "Entra desde el mapa, o revisa que el nombre de la escena coincida " +
                         "con el del imperio en MapaMundialModel.");
    }

    private void Update()
    {
        if (_terminada || _partida == null) return;

        // --- Derrota ---
        if (_partida.Jugador != null && _partida.Jugador.Vida <= 0)
        {
            Terminar(false, _partida.Jugador.Nombre + " cayo en combate");
            return;
        }

        // --- Victoria ---
        if (exigirAlMenosUnEnemigo && !_huboEnemigos)
        {
            // Por si los enemigos se crean despues de arrancar la escena.
            _huboEnemigos = EnemigoController.Vivos.Count > 0;
            return;
        }

        if (EnemigoController.Vivos.Count > 0)
        {
            _momentoDelUltimo = 0f;
            return;
        }

        // Ya no queda ninguno: se espera un momento para que se vea la
        // animacion de muerte antes de sacar el panel.
        if (_momentoDelUltimo <= 0f)
            _momentoDelUltimo = Time.time;

        if (Time.time - _momentoDelUltimo >= esperaTrasElUltimo)
            Terminar(true, "Derrotaste a todos los defensores");
    }

    private void Terminar(bool jugadorGano, string motivo)
    {
        _terminada = true;

        // PartidaModel se encarga del resto: marca el territorio como
        // conquistado si ganaste, guarda el resultado en el archivo de texto
        // y dispara OnBatallaTerminada, que es lo que hace aparecer el panel.
        _partida.TerminarBatalla(jugadorGano, motivo);

        Debug.Log("[Combate] " + (jugadorGano ? "VICTORIA" : "DERROTA") + ": " + motivo);
    }

    private void OnDestroy()
    {
        // Se deja como estaba para no afectar a otras batallas.
        if (_partida != null)
            _partida.AtaqueAutomaticoIA = true;
    }
}
