using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private KnockbackController knockback;

    public enum PlayerType
    {
        Player1,
        Player2
    }

    [Header("Player")]
    public PlayerType playerType; // Loại người chơi

    [Header("Movement")]
    public float moveSpeed = 6f; // Tốc độ di chuyển

    [Header("Jump")]
    public float jumpForce = 12f; // Lực nhảy

    [Header("Ground Check")]
    public Transform groundCheck;      // Điểm kiểm tra mặt đất
    public float groundRadius = 0.2f;  // Bán kính kiểm tra
    public LayerMask groundLayer;      // Layer mặt đất

    private Rigidbody2D rb;
    private bool isGrounded;           // Trạng thái chạm đất
    private SpriteRenderer spriteRenderer;

    private float horizontal;          // Hướng di chuyển

    void Start()
    {
        // Lấy các component
        knockback = GetComponent<KnockbackController>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        CheckGround(); // Kiểm tra chạm đất
        GetInput();    // Nhận input
        Move();        // Di chuyển
        Jump();        // Nhảy
    }

    void GetInput()
    {
        if (playerType == PlayerType.Player1)
        {
            // Player 1: A D W
            horizontal = 0;

            if (Input.GetKey(KeyCode.A))
                horizontal = -1;

            if (Input.GetKey(KeyCode.D))
                horizontal = 1;
        }
        else
        {
            // Player 2: ← → ↑
            horizontal = 0;

            if (Input.GetKey(KeyCode.LeftArrow))
                horizontal = -1;

            if (Input.GetKey(KeyCode.RightArrow))
                horizontal = 1;
        }
    }

    // Di chuyển nhân vật
    void Move()
    {
        // Không di chuyển khi bị knockback
        if (knockback != null && knockback.IsKnocked)
            return;

        rb.linearVelocity = new Vector2(
            horizontal * moveSpeed,
            rb.linearVelocity.y
        );

        // Đổi hướng nhân vật
        Flip(horizontal);
    }

    // Xử lý nhảy
    void Jump()
    {
        // Chỉ được nhảy khi đứng trên mặt đất
        if (!isGrounded)
            return;

        if (playerType == PlayerType.Player1)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    jumpForce
                );
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    jumpForce
                );
            }
        }
    }

    // Kiểm tra có đang đứng trên mặt đất
    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );
    }

    // Lật hướng nhân vật
    void Flip(float direction)
    {
        if (direction > 0)
            spriteRenderer.flipX = false;
        else if (direction < 0)
            spriteRenderer.flipX = true;
    }

    // Vẽ vùng kiểm tra mặt đất
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}