using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum PlayerType
    {
        Player1,
        Player2
    }

    [Header("Player")]
    public PlayerType playerType;

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Jump")]
    public float jumpForce = 12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;

    private float horizontal;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        CheckGround();
        GetInput();
        Move();
        Jump();
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

    // Di chuyển nhân vật theo phương ngang
    void Move()
    {
        rb.linearVelocity = new Vector2(
            horizontal * moveSpeed,
            rb.linearVelocity.y
        );

        Flip(horizontal);
    }

    // Chỉ cho phép nhảy khi đang đứng trên mặt đất
    void Jump()
    {
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

    // Kiểm tra nhân vật có đang chạm đất hay không
    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );
    }

    // Lật hướng nhân vật theo hướng di chuyển
    void Flip(float direction)
    {
        if (direction > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (direction < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    // Hiển thị vùng kiểm tra mặt đất trong Scene
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}