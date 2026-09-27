using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;
using UnityEngine;

/// <summary>
/// CONTROLADOR del ataque del jugador. Va en el GameObject Jugador, junto a
/// PlayerController.
///
/// COMO USARLO EN UNITY:
/// 1. Selecciona el Jugador -> Add Component -> AtaqueJugadorController.
/// 2. En su Animator crea un parametro Trigger llamado "Atacar" (o el nombre
///    que uses, y lo cambias en el Inspector).
/// 3. Listo: con la barra espaciadora golpea.
///
/// COMO PEGA:
///   Al presionar la tecla, se reproduce la animacion y se le quita vida a
///   TODOS los enemigos que esten dentro del Radio Ataque. El daño sale de la
///   fuerza del jugador mas la herramienta que lleve equipada, y esa
///   herramienta se desgasta con cada golpe: es el mismo calculo que usa
///   JugadorModel.Atacar, asi que pelear con el hacha rinde mas que con el pico.
/// </summary>
public class AtaqueJugadorController : MonoBehaviour
{
    [Header("Controles")]
    [SerializeField] private KeyCode teclaAtacar = KeyCode.Space;

    [Tooltip("Segundos minimos entre un golpe y el siguiente.")]
    [SerializeField] private float segundosEntreGolpes = 0.6f;

    [Header("Alcance")]
    [Tooltip("A que distancia alcanzas a los enemigos.")]
    [SerializeField] private float radioAtaque = 1.5f;

    [Header("Animator")]
    [SerializeField] private string triggerAtacar = "Atacar";

    private Animator _animator;
    private JugadorArmasController _armas;
    private JugadorModel _jugador;
    private float _proximoGolpe;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _armas = GetComponent<JugadorArmasController>();
    }

    private void Start()
    {
        if (PlayerSelectionManager.Instance != null && PlayerSelectionManager.Instance.Partida != null)
            _jugador = PlayerSelectionManager.Instance.Partida.Jugador;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(teclaAtacar)) return;
        if (Time.time < _proximoGolpe) return;

        _proximoGolpe = Time.time + segundosEntreGolpes;
        Golpear();
    }

    private void Golpear()
    {
        // La animacion se lanza siempre, aunque no le des a nadie: se siente
        // mejor que el muñeco responda a la tecla.
        if (_animator != null && !string.IsNullOrEmpty(triggerAtacar))
            _animator.SetTrigger(triggerAtacar);

        int dano = CalcularDano();
        int alcanzados = 0;

        // Se recorre de atras hacia adelante porque un enemigo que muere se
        // borra de la lista en mitad del recorrido.
        for (int i = EnemigoController.Vivos.Count - 1; i >= 0; i--)
        {
            EnemigoController enemigo = EnemigoController.Vivos[i];
            if (enemigo == null || enemigo.Muerto) continue;

            float distancia = Vector2.Distance(transform.position, enemigo.transform.position);
            if (distancia > radioAtaque) continue;

            enemigo.RecibirDanio(dano);
            alcanzados++;
        }

        if (alcanzados > 0)
        {
            Debug.Log("[Ataque] Golpeaste a " + alcanzados + " enemigo(s) por " + dano + ".");

            ImperiosEnGuerra.Controlador.Bitacora.Anotar(
                "Ataque",
                "Golpeo a " + alcanzados + " enemigo(s) por " + dano + " de daño");
        }
    }

    /// <summary>
    /// Fuerza del jugador mas lo que aporte la herramienta equipada.
    /// Golpear() le baja durabilidad al arma y devuelve 0 si esta rota, asi
    /// que con el arma rota se pega solo con la fuerza base.
    /// </summary>
    private int CalcularDano()
    {
        int dano = _jugador != null ? _jugador.NivelFuerza : 10;

        ArmaModel arma = _armas != null ? _armas.ArmaEquipada : null;
        if (arma != null)
            dano += arma.Golpear();

        return dano;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioAtaque);
    }
}
