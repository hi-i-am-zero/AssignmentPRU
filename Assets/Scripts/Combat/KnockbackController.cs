using UnityEngine;
using System.Collections;

public class KnockbackController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Knockback")]
    public float knockbackDuration = 0.2f; // Thời gian bị hất văng

    private bool isKnocked; // Trạng thái knockback

    public bool IsKnocked => isKnocked; // Kiểm tra có đang bị knockback

    private void Awake()
    {
        // Lấy Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(Vector2 attackPosition, float force)
    {
        // Đang bị knockback thì bỏ qua
        if (isKnocked)
            return;

        // Tính hướng bị đẩy
        Vector2 direction =
            ((Vector2)transform.position - attackPosition).normalized;

        // Dừng vận tốc hiện tại
        rb.linearVelocity = Vector2.zero;

        // Đẩy nhân vật ra xa điểm tấn công
        rb.AddForce(direction * force, ForceMode2D.Impulse);

        // Bắt đầu thời gian knockback
        StartCoroutine(KnockbackRoutine());
    }

    IEnumerator KnockbackRoutine()
    {
        // Đánh dấu đang bị knockback
        isKnocked = true;

        yield return new WaitForSeconds(knockbackDuration);

        // Kết thúc knockback
        isKnocked = false;
    }
}