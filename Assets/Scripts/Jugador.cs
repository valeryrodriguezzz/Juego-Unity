using UnityEngine;

public class Jugador : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb2D;
    private Vector2 movementInput;
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        movementInput = movementInput.normalized;
    }
    private void FixedUpdate()
    {
        rb2D.linearVelocity = movementInput * speed;
    }
}
