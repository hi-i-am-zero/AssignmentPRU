using UnityEngine;
using SkyfallArena.Systems.Items;
using SkyfallArena.Systems;
using SkyfallArena.Multiplayer;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Di chuyển + nhảy theo input local (WASD / Arrows). Tôn trọng knockback và block.
/// </summary>
public class PlayerController : MonoBehaviour
{
    private KnockbackController knockback;
    private LocalPlayerInputSource inputSource;
    private PlayerBlockController blockController;

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
    private bool isGrounded;
    private float horizontal;

    private void Awake()
    {
        EnsureCombatAndAnimationSetup();
    }

    private void Start()
    {
        knockback = GetComponent<KnockbackController>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        inputSource = GetComponent<LocalPlayerInputSource>();
        blockController = GetComponent<PlayerBlockController>();
    }

    private void Update()
    {
        CheckGround();
        GetInput();
        Move();
        Jump();
    }

    void GetInput()
    {
        horizontal = 0f;

        // Keyboard-first scheme: read keys by playerType for same-frame reliability.
        if (playerType == PlayerType.Player1)
        {
            if (Input.GetKey(KeyCode.A))
                horizontal = -1f;
            if (Input.GetKey(KeyCode.D))
                horizontal = 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                horizontal = -1f;
            if (Input.GetKey(KeyCode.RightArrow))
                horizontal = 1f;
        }
    }

    void Move()
    {
        if (knockback != null && knockback.IsKnocked)
            return;

        if (blockController != null && blockController.IsBlocking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
        Flip(horizontal);
    }

    void Jump()
    {
        if (!isGrounded)
            return;

        if (blockController != null && blockController.IsBlocking)
            return;

        bool jumpPressed;
        if (playerType == PlayerType.Player1)
            jumpPressed = Input.GetKeyDown(KeyCode.W);
        else
            jumpPressed = Input.GetKeyDown(KeyCode.UpArrow);

        if (!jumpPressed)
            return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void CheckGround()
    {
        if (groundCheck == null)
        {
            isGrounded = true;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );
    }

    void Flip(float direction)
    {
        if (spriteRenderer == null)
            return;

        if (direction > 0f)
            spriteRenderer.flipX = false;
        else if (direction < 0f)
            spriteRenderer.flipX = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    void EnsureCombatAndAnimationSetup()
    {
        int playerLayer = LayerMask.NameToLayer(Environment.GameLayers.Player);
        if (playerLayer >= 0)
        {
            gameObject.layer = playerLayer;
            var transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null)
                    transforms[i].gameObject.layer = playerLayer;
            }
        }

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

        if (GetComponent<PlayerBlockController>() == null)
            gameObject.AddComponent<PlayerBlockController>();

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
