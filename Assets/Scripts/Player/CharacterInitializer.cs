using UnityEngine;

/// <summary>
/// Gán chỉ số theo class: Knight / Ninja / Sorcerer (HP, damage, range, cooldown...).
/// </summary>
public class CharacterInitializer : MonoBehaviour
{
    private CharacterType characterType;
    private CharacterStatus status;
    private PlayerController playerController;

    private void Awake()
    {
        characterType = GetComponent<CharacterType>();
        status = GetComponent<CharacterStatus>();
        playerController = GetComponent<PlayerController>();

        if (characterType == null || status == null || playerController == null)
        {
            Debug.LogWarning("CharacterInitializer requires CharacterType, CharacterStatus, and PlayerController.", this);
            return;
        }

        ApplyStats();
    }

    /// <summary>
    /// Gán lại chỉ số theo class (gọi khi damage/range vẫn 0 lúc runtime).
    /// </summary>
    public void ApplyStats()
    {
        if (characterType == null)
            characterType = GetComponent<CharacterType>();
        if (status == null)
            status = GetComponent<CharacterStatus>();
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (characterType == null || status == null || playerController == null)
            return;

        switch (characterType.character)
        {
            case CharacterType.Character.Knight:
                status.moveSpeed = 5.5f;
                status.maxHP = 110;
                status.damage = 16;
                status.attackRange = 1.25f;
                status.attackCooldown = 0.5f;
                status.knockbackForce = 8f;
                break;

            case CharacterType.Character.Ninja:
                status.moveSpeed = 8f;
                status.maxHP = 85;
                status.damage = 14;
                status.attackRange = 1.05f;
                status.attackCooldown = 0.32f;
                status.knockbackForce = 5f;
                break;

            case CharacterType.Character.Sorcerer:
                status.moveSpeed = 4.5f;
                status.maxHP = 80;
                status.damage = 17;
                // Tầm đạn thực tế = 85% chiều rộng map (AttackController).
                status.attackRange = 0.85f;
                status.attackCooldown = 0.65f;
                status.knockbackForce = 6.5f;
                break;
        }

        playerController.moveSpeed = status.moveSpeed;
    }
}
