using UnityEngine;

// CONTROLADOR: mueve al muñeco del jugador por el mapa con WASD/flechas
// y le avisa al Animator cuando esta corriendo.
//
// El jugador siempre es el Pawn, asi que este script ya NO elige sprite ni
// Animator: el GameObject Jugador trae el suyo puesto desde el Inspector y
// aqui no se toca. (Cuando existian 5 personajes seleccionables habia que
// intercambiar el Animator Controller en Start; eso se quito al decidir que
// solo el Pawn tiene animaciones de talar, picar y construir.)
//
// REQUISITOS EN EL GAMEOBJECT DEL JUGADOR:
// 1) SpriteRenderer
// 2) Rigidbody2D (Dynamic, Gravity Scale 0, Freeze Rotation Z)
// 3) Collider2D
// 4) Animator, con un parametro Bool llamado "EstaCorriendo"
// 5) Tag "Player", para que los nodos de recurso lo reconozcan

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;

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

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        movimiento = new Vector2(horizontal, vertical).normalized;

        // El Pawn solo tiene animacion de correr de lado, asi que en vez de
        // sprites para arriba y abajo se voltea el mismo con flipX.
        if (spriteRenderer != null)
        {
            if (horizontal > 0) spriteRenderer.flipX = false;
            else if (horizontal < 0) spriteRenderer.flipX = true;
        }

        if (animator != null && animator.runtimeAnimatorController != null)
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
