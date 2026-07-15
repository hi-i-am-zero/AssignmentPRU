using UnityEngine;

public class PlayerUpgrade : MonoBehaviour
{
    [Header("Upgrade Levels")]
    [Tooltip("Cấp độ Jump Boost hiện tại (Lv1 - Lv3)")]
    public int jumpBoostLevel;

    [Tooltip("Cấp độ Attack Chain hiện tại (Lv1 - Lv3)")]
    public int attackChainLevel;

    [Tooltip("Cấp độ Shield Wall hiện tại (Lv1 - Lv3)")]
    public int shieldWallLevel;

    [Header("Shield Wall")]
    [Tooltip("Tỷ lệ giảm sát thương nhận vào")]
    public float damageReduction;

    private CharacterStatus status;
    private PlayerController playerController;

    private float baseJumpForce;
    private int baseDamage;
    private bool isInitialized;

    private void Start()
    {
        EnsureInitialized();
    }

    public void UpgradeJumpBoost()
    {
        if (!EnsureInitialized())
            return;

        if (jumpBoostLevel >= 3)
            return;

        jumpBoostLevel++;
        ApplyJumpBoost();
    }

    /// <summary>
    /// Attack Chain chỉ tăng damage. Combo/đòn đánh đã mở sẵn theo phím riêng.
    /// </summary>
    public void UpgradeAttackChain()
    {
        if (!EnsureInitialized())
            return;

        if (attackChainLevel >= 3)
            return;

        attackChainLevel++;
        ApplyAttackChain();
    }

    public void UpgradeShieldWall()
    {
        if (!EnsureInitialized())
            return;

        if (shieldWallLevel >= 3)
            return;

        shieldWallLevel++;
        ApplyShieldWall();
    }

    private void ApplyJumpBoost()
    {
        switch (jumpBoostLevel)
        {
            case 1:
                playerController.jumpForce = baseJumpForce + 2f;
                break;
            case 2:
                playerController.jumpForce = baseJumpForce + 4f;
                break;
            case 3:
                playerController.jumpForce = baseJumpForce + 6f;
                break;
        }
    }

    private void ApplyAttackChain()
    {
        switch (attackChainLevel)
        {
            default:
            case 0:
                status.damage = baseDamage;
                break;
            case 1:
                status.damage = baseDamage + 5;
                break;
            case 2:
                status.damage = baseDamage + 10;
                break;
            case 3:
                status.damage = baseDamage + 15;
                break;
        }
    }

    private void ApplyShieldWall()
    {
        switch (shieldWallLevel)
        {
            case 1:
                damageReduction = 0.1f;
                break;
            case 2:
                damageReduction = 0.2f;
                break;
            case 3:
                damageReduction = 0.3f;
                break;
        }
    }

    private bool EnsureInitialized()
    {
        if (isInitialized)
            return true;

        if (status == null)
            status = GetComponent<CharacterStatus>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (status == null || playerController == null)
        {
            Debug.LogWarning("PlayerUpgrade requires CharacterStatus and PlayerController.", this);
            return false;
        }

        baseJumpForce = playerController.jumpForce;
        baseDamage = status.damage;
        ApplyAttackChain();
        isInitialized = true;
        return true;
    }
}
