using UnityEngine;

// CONTROLADOR: mueve al muñeco del jugador por el mapa usando WASD/flechas,
// activa la animacion de correr, y al iniciar se pinta con el sprite de
// cuerpo que corresponde al personaje elegido en la seleccion.
//
// REQUISITOS EN EL GAMEOBJECT DEL JUGADOR:
// 1) SpriteRenderer
// 2) Rigidbody2D (Dynamic, Gravity Scale 0, Freeze Rotation Z)
// 3) Collider2D
// 4) Animator, con un parametro Bool llamado "EstaCorriendo"

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;

    [Header("Sprites de CUERPO completo, en el MISMO ORDEN que los retratos del carrusel")]
    public Sprite[] spritesCuerpo;

    private Rigidbody2D rb;
    private Vector2 movimiento;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // Debe coincidir EXACTO con el nombre del parametro Bool en el Animator
    private const string PARAM_ESTA_CORRIENDO = "EstaCorriendo";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        AplicarAvatarSeleccionado();
    }

    private void AplicarAvatarSeleccionado()
    {
        if (spriteRenderer == null || spritesCuerpo == null || spritesCuerpo.Length == 0) return;

        int indice = 0;

        if (PlayerSelectionManager.Instance != null)
        {
            indice = PlayerSelectionManager.Instance.AvatarIndex;
        }

        if (indice < 0 || indice >= spritesCuerpo.Length)
        {
            Debug.LogWarning("AvatarIndex (" + indice + ") fuera de rango. Usando el primer sprite de cuerpo.");
            indice = 0;
        }

        spriteRenderer.sprite = spritesCuerpo[indice];
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        movimiento = new Vector2(horizontal, vertical).normalized;

        if (spriteRenderer != null)
        {
            if (horizontal > 0) spriteRenderer.flipX = false;
            else if (horizontal < 0) spriteRenderer.flipX = true;
        }

        // Le avisa al Animator si el jugador se esta moviendo o no
        if (animator != null)
        {
            bool estaCorriendo = movimiento.sqrMagnitude > 0.01f;
            animator.SetBool(PARAM_ESTA_CORRIENDO, estaCorriendo);
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movimiento * moveSpeed * Time.fixedDeltaTime);
    }
}