using UnityEngine;

public class AttackController : MonoBehaviour
{
    [Header("Attack Settings")]
    public Transform attackPoint;      // Vị trí đánh
    public LayerMask playerLayer;      // Layer của Player

    private CharacterStatus stats;             // Thông số nhân vật
    private PlayerController playerController; // Điều khiển Player
    private Animator animator;                 // Animation
    private ComboController comboController;   // Combo đánh
    private KnockbackController selfKnockback; // Kiểm tra bị knockback

    private float nextAttackTime; // Thời gian đánh tiếp theo

    private void Awake()
    {
        // Lấy các component
        stats = GetComponent<CharacterStatus>();
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        comboController = GetComponent<ComboController>();
        selfKnockback = GetComponent<KnockbackController>();
    }

    private void Update()
    {
        HandleAttackInput(); // Kiểm tra input đánh
    }

    void HandleAttackInput()
    {
        // Không được đánh khi đang bị knockback
        if (selfKnockback != null && selfKnockback.IsKnocked)
            return;

        bool attackPressed = false;

        // Phím đánh của từng Player
        if (playerController.playerType == PlayerController.PlayerType.Player1)
            attackPressed = Input.GetKeyDown(KeyCode.F);
        else
            attackPressed = Input.GetKeyDown(KeyCode.Keypad0);

        if (!attackPressed)
            return;

        // Kiểm tra cooldown
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + stats.attackCooldown;

        Attack();
    }

    void Attack()
    {
        if (attackPoint == null)
            return;

        // Lấy combo hiện tại
        int comboIndex = 1;

        if (comboController != null)
            comboIndex = comboController.NextCombo();

        Debug.Log("Combo: " + comboIndex);

        // Chạy animation đánh
        if (animator != null)
        {
            animator.SetInteger("Combo", comboIndex);
            animator.SetTrigger("Attack");
        }

        // Tìm Player trong vùng đánh
        Collider2D[] targets = Physics2D.OverlapCircleAll(
            attackPoint.position,
            stats.attackRange,
            playerLayer
        );

        foreach (Collider2D target in targets)
        {
            // Bỏ qua chính mình
            if (target.gameObject == gameObject)
                continue;

            Debug.Log(gameObject.name + " attacked " + target.name);

            // Gây knockback
            KnockbackController knockback =
                target.GetComponent<KnockbackController>();

            if (knockback != null)
            {
                knockback.ApplyKnockback(
                    transform.position,
                    stats.knockbackForce
                );
            }

            // ==========================
            // HỆ THỐNG MÁU (Member 2)
            //
            // HealthController health =
            // target.GetComponent<HealthController>();
            //
            // if (health != null)
            // {
            //     health.TakeDamage(stats.damage);
            // }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;

        float radius = 1f;

        CharacterStatus status = GetComponent<CharacterStatus>();

        if (status != null)
            radius = status.attackRange;

        // Vẽ phạm vi đánh
        Gizmos.DrawWireSphere(attackPoint.position, radius);
    }
}