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

    // Thông tin chỉ số nhân vật
    private CharacterStatus status;

    // Điều khiển nhân vật
    private PlayerController playerController;
    private ComboController comboController;

    // Lưu chỉ số gốc để làm mốc nâng cấp
    private float baseJumpForce;
    private int baseDamage;
    private int baseMaxCombo;
    private bool isInitialized;

    private void Start()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// Nâng cấp Jump Boost.
    /// Tăng khả năng nhảy tối đa đến Lv3.
    /// </summary>
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
    /// Nâng cấp Attack Chain.
    /// Mở thêm số đòn combo và tăng sát thương tối đa đến Lv3.
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

    /// <summary>
    /// Nâng cấp Shield Wall.
    /// Tăng khả năng giảm sát thương tối đa đến Lv3.
    /// </summary>
    public void UpgradeShieldWall()
    {
        if (!EnsureInitialized())
            return;

        if (shieldWallLevel >= 3)
            return;

        shieldWallLevel++;
        ApplyShieldWall();
    }

    /// <summary>
    /// Áp dụng hiệu ứng Jump Boost theo cấp độ.
    /// Lv1: +2 Jump Force
    /// Lv2: +4 Jump Force
    /// Lv3: +6 Jump Force
    /// </summary>
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

    /// <summary>
    /// Áp dụng hiệu ứng Attack Chain theo cấp độ.
    /// Lv1: +5 Damage
    /// Lv2: +10 Damage
    /// Lv3: +15 Damage
    /// </summary>
    private void ApplyAttackChain()
    {
        // Mở rộng số đòn combo theo cấp Attack Chain dựa trên mốc mặc định từng nhân vật.
        // Lv0: 1 đòn, Lv1: 2 đòn, Lv2: 3 đòn, Lv3: 4 đòn (nếu class có đủ combo gốc).
        if (comboController != null)
        {
            int unlockedCombo = Mathf.Clamp(
                1 + attackChainLevel,
                1,
                Mathf.Max(1, baseMaxCombo));
            comboController.maxCombo = unlockedCombo;
        }

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

    /// <summary>
    /// Áp dụng hiệu ứng Shield Wall theo cấp độ.
    /// Lv1: Giảm 10% sát thương
    /// Lv2: Giảm 20% sát thương
    /// Lv3: Giảm 30% sát thương
    /// </summary>
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

        Debug.Log(
            gameObject.name +
            " upgraded Shield Wall to Lv" +
            shieldWallLevel +
            " (" +
            damageReduction * 100 +
            "% Damage Reduction)"
        );
    }

    private bool EnsureInitialized()
    {
        if (isInitialized)
            return true;

        if (status == null)
            status = GetComponent<CharacterStatus>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (comboController == null)
            comboController = GetComponent<ComboController>();

        if (status == null || playerController == null)
        {
            Debug.LogWarning("PlayerUpgrade requires CharacterStatus and PlayerController.", this);
            return false;
        }

        // Lưu giá trị ban đầu của nhân vật
        baseJumpForce = playerController.jumpForce;
        baseDamage = status.damage;
        baseMaxCombo = comboController != null ? Mathf.Max(1, comboController.maxCombo) : 1;

        // Đồng bộ trạng thái mặc định khi chưa nhặt item.
        ApplyAttackChain();

        isInitialized = true;
        return true;
    }
}