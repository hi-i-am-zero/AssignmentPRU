using UnityEngine;

/// <summary>
/// Gán chỉ số theo class: Knight / Ninja / Sorcerer (HP, damage, range, cooldown...).
/// </summary>
public class CharacterInitializer : MonoBehaviour
{
    // Loại nhân vật (Knight, Ninja, Sorcerer)
    private CharacterType characterType;

    // Nơi lưu toàn bộ chỉ số nhân vật
    private CharacterStatus status;

    // Script điều khiển nhân vật
    private PlayerController playerController;
    private ComboController comboController;

    private void Awake()
    {
        characterType = GetComponent<CharacterType>();
        status = GetComponent<CharacterStatus>();
        playerController = GetComponent<PlayerController>();
        comboController = GetComponent<ComboController>();

        if (characterType == null || status == null || playerController == null)
        {
            Debug.LogWarning("CharacterInitializer requires CharacterType, CharacterStatus, and PlayerController.", this);
            return;
        }

        ApplyStats();
    }

    /// <summary>
    /// Re-apply class stats (safe to call if damage/range were still 0 at runtime).
    /// </summary>
    public void ApplyStats()
    {
        if (characterType == null)
            characterType = GetComponent<CharacterType>();
        if (status == null)
            status = GetComponent<CharacterStatus>();
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        if (comboController == null)
            comboController = GetComponent<ComboController>();

        if (characterType == null || status == null || playerController == null)
            return;

        int comboCount = 3;
        switch (characterType.character)
        {
            case CharacterType.Character.Knight:
                status.moveSpeed = 5f;
                status.maxHP = 120;
                status.damage = 20;
                status.attackRange = 1.3f;
                status.attackCooldown = 0.55f;
                status.knockbackForce = 10f;
                comboCount = 3;
                break;

            case CharacterType.Character.Ninja:
                status.moveSpeed = 8f;
                status.maxHP = 90;
                status.damage = 15;
                status.attackRange = 1f;
                status.attackCooldown = 0.25f;
                status.knockbackForce = 5f;
                comboCount = 3;
                break;

            case CharacterType.Character.Sorcerer:
                status.moveSpeed = 4f;
                status.maxHP = 70;
                status.damage = 18;
                status.attackRange = 4f;
                status.attackCooldown = 0.8f;
                status.knockbackForce = 7f;
                comboCount = 4;
                break;
        }

        playerController.moveSpeed = status.moveSpeed;

        if (comboController != null)
            comboController.maxCombo = comboCount;
    }
}