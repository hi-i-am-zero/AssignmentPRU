using UnityEngine;

public class AttackController : MonoBehaviour
{
    [Header("Attack Settings")]
    public Transform attackPoint;      // Điểm xuất phát đòn đánh
    public LayerMask playerLayer;      // Layer chứa các Player có thể bị đánh

    // Các component cần dùng cho hệ thống combat
    private CharacterStatus stats;
    private PlayerController playerController;
    private ComboController comboController;
    private KnockbackController selfKnockback;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Thời điểm được phép thực hiện đòn đánh tiếp theo
    private float nextAttackTime;

    private void Awake()
    {
        // Lấy các component trên cùng GameObject
        stats = GetComponent<CharacterStatus>();
        playerController = GetComponent<PlayerController>();
        comboController = GetComponent<ComboController>();
        selfKnockback = GetComponent<KnockbackController>();

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Kiểm tra input đánh mỗi frame
        HandleAttackInput();
    }

    /// <summary>
    /// Nhận input tấn công của từng người chơi
    /// Player 1: F
    /// Player 2: Keypad 0
    /// Đồng thời kiểm tra trạng thái knockback và cooldown
    /// </summary>
    void HandleAttackInput()
    {
<<<<<<< HEAD
        if (stats == null || playerController == null)
            return;

        // Không được đánh khi đang bị knockback
=======
        // Không cho đánh khi đang bị hất văng
>>>>>>> origin/vund
        if (selfKnockback != null && selfKnockback.IsKnocked)
            return;

        bool attackPressed = false;

        if (playerController.playerType ==
            PlayerController.PlayerType.Player1)
        {
            attackPressed = Input.GetKeyDown(KeyCode.F);
        }
        else
        {
            attackPressed = Input.GetKeyDown(KeyCode.Keypad0);
        }

        if (!attackPressed)
            return;

        // Kiểm tra thời gian hồi chiêu
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + stats.attackCooldown;

        Attack();
    }

    /// <summary>
    /// Thực hiện đòn đánh:
    /// - Tăng combo
    /// - Chạy animation
    /// - Tìm mục tiêu trong phạm vi đánh
    /// - Áp dụng knockback cho mục tiêu
    /// </summary>
    void Attack()
    {
        if (stats == null)
            return;

<<<<<<< HEAD
        if (attackPoint == null)
            attackPoint = transform;

        // Lấy combo hiện tại
=======
        // Lấy đòn hiện tại trong chuỗi combo
>>>>>>> origin/vund
        int comboIndex = 1;

        if (comboController != null)
        {
            comboIndex = comboController.NextCombo();
        }

        Debug.Log(gameObject.name + " Combo " + comboIndex);

        // Chạy animation đánh
        if (animator != null)
        {
            animator.SetInteger("Combo", comboIndex);
            animator.SetTrigger("Attack");
        }

        // Tìm tất cả mục tiêu trong vùng đánh
        Collider2D[] targets =
            Physics2D.OverlapCircleAll(
                attackPoint.position,
                stats.attackRange,
                playerLayer
            );

        foreach (Collider2D target in targets)
        {
            // Không cho phép tự đánh chính mình
            if (target.gameObject == gameObject)
                continue;

            // Chỉ gây sát thương cho mục tiêu ở phía trước mặt nhân vật
            Vector2 directionToTarget =
                target.transform.position -
                transform.position;

            if (!spriteRenderer.flipX)
            {
                if (directionToTarget.x < 0)
                    continue;
            }
            else
            {
                if (directionToTarget.x > 0)
                    continue;
            }

            Debug.Log(
                gameObject.name +
                " attacked " +
                target.name +
                " | Damage = " +
                stats.damage
            );

            // Áp dụng hiệu ứng hất văng (knockback)
            KnockbackController knockback =
                target.GetComponent<KnockbackController>();

            if (knockback != null)
            {
                knockback.ApplyKnockback(
                    transform.position,
                    stats.knockbackForce
                );
            }

            /*
             * Member 2 sẽ tích hợp hệ thống máu tại đây
             *
             * HealthController health =
             * target.GetComponent<HealthController>();
             *
             * if (health != null)
             * {
             *     health.TakeDamage(stats.damage);
             * }
             */
        }
    }

    /// <summary>
    /// Hiển thị phạm vi tấn công trong Scene View
    /// Chỉ dùng để hỗ trợ debug khi thiết kế game
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;

        float radius = 1f;

        CharacterStatus status =
            GetComponent<CharacterStatus>();

        if (status != null)
        {
            radius = status.attackRange;
        }

        Gizmos.DrawWireSphere(
            attackPoint.position,
            radius
        );
    }
}