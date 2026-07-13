using UnityEngine;
using SkyfallArena.Systems.Items;
using SkyfallArena.Systems;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerController : MonoBehaviour
{
    // Quản lý trạng thái bị hất văng
    private KnockbackController knockback;

    // Xác định người chơi để gán bộ phím điều khiển phù hợp
    public enum PlayerType
    {
        Player1,
        Player2
    }

    [Header("Player Settings")]
    public PlayerType playerType;

    [Header("Movement Settings")]
    [Tooltip("Tốc độ di chuyển ngang của nhân vật")]
    public float moveSpeed = 6f;

    [Header("Jump Settings")]
    [Tooltip("Lực nhảy của nhân vật")]
    public float jumpForce = 12f;

    [Header("Ground Check")]
    [Tooltip("Điểm dùng để kiểm tra nhân vật có đang đứng trên mặt đất")]
    public Transform groundCheck;

    [Tooltip("Bán kính kiểm tra mặt đất")]
    public float groundRadius = 0.2f;

    [Tooltip("Layer được xem là mặt đất")]
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // Trạng thái nhân vật đang đứng trên mặt đất
    private bool isGrounded;

    // Giá trị di chuyển ngang (-1, 0, 1)
    private float horizontal;

    private void Awake()
    {
        EnsureCombatAndAnimationSetup();
    }

    private void Start()
    {
        // Lấy các component cần sử dụng
        knockback = GetComponent<KnockbackController>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Thứ tự xử lý mỗi frame
        CheckGround();
        GetInput();
        Move();
        Jump();
    }

    /// <summary>
    /// Nhận input từ bàn phím dựa theo PlayerType
    /// Player 1:
    ///     A = Trái
    ///     D = Phải
    ///     W = Nhảy
    ///
    /// Player 2:
    ///     ← = Trái
    ///     → = Phải
    ///     ↑ = Nhảy
    /// </summary>
    void GetInput()
    {
        horizontal = 0;

        if (playerType == PlayerType.Player1)
        {
            if (Input.GetKey(KeyCode.A))
                horizontal = -1;

            if (Input.GetKey(KeyCode.D))
                horizontal = 1;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                horizontal = -1;

            if (Input.GetKey(KeyCode.RightArrow))
                horizontal = 1;
        }
    }

    /// <summary>
    /// Điều khiển di chuyển ngang của nhân vật
    /// Không cho phép di chuyển khi đang bị knockback
    /// </summary>
    void Move()
    {
        if (knockback != null && knockback.IsKnocked)
            return;

        rb.linearVelocity = new Vector2(
            horizontal * moveSpeed,
            rb.linearVelocity.y
        );

        Flip(horizontal);
    }

    /// <summary>
    /// Xử lý nhảy.
    /// Chỉ cho phép nhảy khi đang đứng trên mặt đất.
    /// </summary>
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

    /// <summary>
    /// Kiểm tra nhân vật có đang chạm đất hay không.
    /// Dùng OverlapCircle để phát hiện va chạm với Ground Layer.
    /// </summary>
    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );
    }

    /// <summary>
    /// Lật hướng nhân vật theo hướng di chuyển.
    /// Dùng SpriteRenderer để tránh ảnh hưởng Collider.
    /// </summary>
    void Flip(float direction)
    {
        if (direction > 0)
            spriteRenderer.flipX = false;
        else if (direction < 0)
            spriteRenderer.flipX = true;
    }

    /// <summary>
    /// Hiển thị vùng Ground Check trong Scene View.
    /// Chỉ dùng để debug khi thiết kế map.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundRadius
        );
    }

    void EnsureCombatAndAnimationSetup()
    {
        int playerLayer = LayerMask.NameToLayer(Environment.GameLayers.Player);
        if (playerLayer >= 0)
            gameObject.layer = playerLayer;

        if (GetComponent<CharacterStatus>() == null)
            gameObject.AddComponent<CharacterStatus>();

        if (GetComponent<CharacterInitializer>() == null)
            gameObject.AddComponent<CharacterInitializer>();

        if (GetComponent<ComboController>() == null)
            gameObject.AddComponent<ComboController>();

        if (GetComponent<KnockbackController>() == null)
            gameObject.AddComponent<KnockbackController>();

        if (GetComponent<PlayerHealth>() == null)
            gameObject.AddComponent<PlayerHealth>();

        if (GetComponent<DeathSystem>() == null)
            gameObject.AddComponent<DeathSystem>();

        if (GetComponent<PlayerUpgrade>() == null)
            gameObject.AddComponent<PlayerUpgrade>();

        if (GetComponent<PlayerUpgradeBuffAdapter>() == null)
            gameObject.AddComponent<PlayerUpgradeBuffAdapter>();

        var animator = GetComponent<Animator>();
        if (animator == null)
            animator = gameObject.AddComponent<Animator>();
        animator.applyRootMotion = false;

        TryAssignDefaultAnimatorController(animator);

        if (GetComponent<CharacterAnimationSync>() == null)
            gameObject.AddComponent<CharacterAnimationSync>();

        var attackController = GetComponent<AttackController>();
        if (attackController == null)
            attackController = gameObject.AddComponent<AttackController>();

        if (attackController.attackPoint == null)
            attackController.attackPoint = EnsureAttackPoint();

        if (attackController.playerLayer.value == 0)
        {
            int mask = LayerMask.GetMask(Environment.GameLayers.Player);
            if (mask != 0)
                attackController.playerLayer = mask;
        }
    }

    Transform EnsureAttackPoint()
    {
        var existing = transform.Find("AttackPoint");
        if (existing != null)
            return existing;

        var go = new GameObject("AttackPoint");
        var point = go.transform;
        point.SetParent(transform);
        point.localRotation = Quaternion.identity;
        point.localScale = Vector3.one;
        point.localPosition = new Vector3(0.45f, 0f, 0f);
        return point;
    }

    void TryAssignDefaultAnimatorController(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController != null)
            return;

#if UNITY_EDITOR
        var characterType = GetComponent<CharacterType>();
        string path = "Assets/Animations/Knight/Knight.controller";
        if (characterType != null)
        {
            switch (characterType.character)
            {
                case CharacterType.Character.Ninja:
                    path = "Assets/Animations/Ninja/Ninja.controller";
                    break;
                case CharacterType.Character.Sorcerer:
                    path = "Assets/Animations/Sorcerer/Sorcerer.controller";
                    break;
                default:
                    path = "Assets/Animations/Knight/Knight.controller";
                    break;
            }
        }

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        if (controller != null)
            animator.runtimeAnimatorController = controller;
#endif
    }
}