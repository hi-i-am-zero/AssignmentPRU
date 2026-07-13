using UnityEngine;
using System.Collections;

public class KnockbackController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Knockback Settings")]

    [Tooltip("Lực hất lên")]
    public float upwardForce = 0.4f;

    [Tooltip("Thời gian bị knockback")]
    public float knockbackDuration = 0.15f;

    // Đang bị knockback
    private bool isKnocked;

    // Cho script khác kiểm tra trạng thái
    public bool IsKnocked => isKnocked;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Áp dụng lực knockback
    /// </summary>
    public void ApplyKnockback(Vector2 attackPosition, float force)
    {
        // Tránh nhận nhiều knockback cùng lúc
        if (isKnocked)
            return;

        // Hướng bị đẩy
        Vector2 direction =
            ((Vector2)transform.position - attackPosition).normalized;

        // Hất nhẹ lên trên
        direction.y = upwardForce;

        direction.Normalize();

        // Reset vận tốc hiện tại
        rb.linearVelocity = Vector2.zero;

        // Thêm lực đẩy
        rb.AddForce(
            direction * force,
            ForceMode2D.Impulse
        );

        StartCoroutine(KnockbackRoutine());
    }

    /// <summary>
    /// Khóa điều khiển trong thời gian knockback
    /// </summary>
    IEnumerator KnockbackRoutine()
    {
        isKnocked = true;

        yield return new WaitForSeconds(knockbackDuration);

        isKnocked = false;
    }

    /// <summary>
    /// Reset trạng thái knockback
    /// </summary>
    public void ResetKnockback()
    {
        StopAllCoroutines();

        isKnocked = false;

        rb.linearVelocity = Vector2.zero;
    }
}