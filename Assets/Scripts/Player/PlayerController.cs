using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento mínimo del jugador para probar el boss: izquierda/derecha, salto y doble salto.
// Versión para el nuevo Input System (lee el teclado directamente, sin Input Actions).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 7f;
    public float jumpForce = 14f;

    [Header("Sensación de salto")]
    public float gravityScale = 3.5f;       // Gravedad base (más alto = menos flotante)
    public float fallMultiplier = 1.6f;     // Gravedad extra al caer (caída más rápida que la subida)
    public float lowJumpMultiplier = 2.5f;  // Gravedad extra si sueltas el salto pronto (salto variable)
    public float maxFallSpeed = 25f;        // Velocidad máxima de caída

    [Header("Doble salto")]
    public int maxJumps = 2;               // 2 = doble salto

    [Header("Detección de suelo")]
    public Transform groundCheck;          // Objeto hijo colocado en los pies del jugador
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private int jumpsLeft;
    private bool jumpHeld;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        jumpsLeft = maxJumps;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Entrada: flechas o A/D
        moveInput = 0f;
        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) moveInput -= 1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) moveInput += 1f;

        jumpHeld = kb.spaceKey.isPressed;

        // Detectar suelo y recargar saltos
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded && !wasGrounded && rb.linearVelocity.y <= 0.01f)
            jumpsLeft = maxJumps;

        // Salto (barra espaciadora)
        if (kb.spaceKey.wasPressedThisFrame && jumpsLeft > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsLeft--;
        }

        // Girar el sprite según la dirección
        if (moveInput != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveInput);
            transform.localScale = scale;
        }
    }

    void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;
        v.x = moveInput * moveSpeed;

        // Gravedad extra para un salto más "pesado" y con control, estilo metroidvania
        float g = Physics2D.gravity.y * rb.gravityScale;
        if (v.y < 0f)
            v.y += g * (fallMultiplier - 1f) * Time.fixedDeltaTime;        // Cayendo: más rápido
        else if (v.y > 0f && !jumpHeld)
            v.y += g * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;     // Soltó el salto: corta la subida

        v.y = Mathf.Max(v.y, -maxFallSpeed);
        rb.linearVelocity = v;
    }

    // Para ver el área de detección de suelo en la ventana Scene
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
