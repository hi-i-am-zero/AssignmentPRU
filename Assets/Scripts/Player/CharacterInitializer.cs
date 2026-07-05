using UnityEngine;

public class CharacterInitializer : MonoBehaviour
{
    private CharacterType characterType;
    private CharacterStatus status;
    private PlayerController playerController;

    private void Awake()
    {
        // Lấy các component
        characterType = GetComponent<CharacterType>();
        status = GetComponent<CharacterStatus>();
        playerController = GetComponent<PlayerController>();

        // Gán chỉ số theo từng nhân vật
        switch (characterType.character)
        {
            case CharacterType.Character.Knight:

                status.moveSpeed = 5f;
                status.maxHP = 120;
                status.damage = 25;
                status.attackRange = 1.3f;
                status.attackCooldown = 0.7f;
                status.knockbackForce = 8f;

                break;

            case CharacterType.Character.Ninja:

                status.moveSpeed = 8f;
                status.maxHP = 90;
                status.damage = 12;
                status.attackRange = 1f;
                status.attackCooldown = 0.3f;
                status.knockbackForce = 5f;
                break;

            case CharacterType.Character.Sorcerer:

                status.moveSpeed = 4f;
                status.maxHP = 70;
                status.damage = 15;
                status.attackRange = 4f;
                status.attackCooldown = 0.9f;
                status.knockbackForce = 6f;

                break;
        }

        // Cập nhật tốc độ di chuyển cho Player
        playerController.moveSpeed = status.moveSpeed;
    }
}