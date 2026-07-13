using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    private ItemType itemType;

    private void Awake()
    {
        // Lấy loại item được gắn trên object
        itemType = GetComponent<ItemType>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra đối tượng có thể nhận nâng cấp hay không
        PlayerUpgrade upgrade =
            other.GetComponent<PlayerUpgrade>();

        if (upgrade == null)
            return;

        // Thực hiện nâng cấp theo loại item
        switch (itemType.itemType)
        {
            case ItemType.Item.JumpBoost:
                upgrade.UpgradeJumpBoost();
                break;

            case ItemType.Item.AttackChain:
                upgrade.UpgradeAttackChain();
                break;

            case ItemType.Item.ShieldWall:
                upgrade.UpgradeShieldWall();
                break;
        }

        // Xóa item sau khi nhặt
        Destroy(gameObject);
    }
}