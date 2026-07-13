using UnityEngine;

public class CharacterInitializer : MonoBehaviour
{
    // Loại nhân vật (Knight, Ninja, Sorcerer)
    private CharacterType characterType;

    // Nơi lưu toàn bộ chỉ số nhân vật
    private CharacterStatus status;

    // Script điều khiển nhân vật
    private PlayerController playerController;

    private void Awake()
    {
        // Lấy các component trên cùng GameObject
        characterType = GetComponent<CharacterType>();
        status = GetComponent<CharacterStatus>();
        playerController = GetComponent<PlayerController>();

<<<<<<< HEAD
        if (characterType == null || status == null || playerController == null)
        {
            Debug.LogWarning("CharacterInitializer requires CharacterType, CharacterStatus, and PlayerController.", this);
            return;
        }

        // Gán chỉ số theo từng nhân vật
=======
        // Gán chỉ số theo từng loại nhân vật
>>>>>>> origin/vund
        switch (characterType.character)
        {
            //--------------------------------------------------
            // KNIGHT
            //--------------------------------------------------
            case CharacterType.Character.Knight:

                // Knight là nhân vật tanker / đấu sĩ
                // Máu cao
                // Damage khá cao
                // Tốc độ trung bình
                // Knockback mạnh

                status.moveSpeed = 5f;
                status.maxHP = 120;
                status.damage = 20;

                // Tầm đánh cận chiến
                status.attackRange = 1.3f;

                // Thời gian chờ giữa 2 lần đánh
                status.attackCooldown = 0.55f;

                // Lực hất văng đối thủ
                status.knockbackForce = 10f;

                break;

            //--------------------------------------------------
            // NINJA
            //--------------------------------------------------
            case CharacterType.Character.Ninja:

                // Ninja là sát thủ
                // Di chuyển nhanh nhất
                // Damage thấp hơn Knight
                // Combo nhanh

                status.moveSpeed = 8f;

                status.maxHP = 90;

                status.damage = 15;

                // Tầm đánh ngắn
                status.attackRange = 1f;

                // Thời gian chờ giữa 2 lần đánh
                status.attackCooldown = 0.25f;

                // Lực hất văng đối thủ
                status.knockbackForce = 5f;

                break;

            //--------------------------------------------------
            // SORCERER
            //--------------------------------------------------
            case CharacterType.Character.Sorcerer:

                // Sorcerer là pháp sư
                // Máu thấp nhất
                // Tốc độ chậm
                // Đánh xa

                status.moveSpeed = 4f;

                status.maxHP = 70;

                status.damage = 18;

                // Tầm đánh xa
                status.attackRange = 4f;

                // Thời gian chờ giữa 2 lần đánh
                status.attackCooldown = 0.8f;

                // Lực hất văng đối thủ
                status.knockbackForce = 7f;

                break;
        }

        if (playerController != null)
        {
            playerController.moveSpeed = status.moveSpeed;
        }
    }
}