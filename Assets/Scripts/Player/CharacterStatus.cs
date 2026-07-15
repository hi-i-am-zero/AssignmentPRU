using UnityEngine;

/// <summary>
/// Chỉ số runtime của nhân vật: tốc độ, HP, damage, range, cooldown, knockback.
/// </summary>
public class CharacterStatus : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;        // Tốc độ di chuyển

    [Header("Health")]
    public int maxHP;              // Máu tối đa

    [Header("Combat")]
    public int damage;             // Sát thương

    public float attackRange;      // Phạm vi tấn công

    public float attackCooldown;   // Thời gian hồi giữa các đòn đánh

    public float knockbackForce;   // Lực hất văng
}