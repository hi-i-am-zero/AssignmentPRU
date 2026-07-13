using UnityEngine;

public class ItemType : MonoBehaviour
{
    // Các loại item nâng cấp trong game
    public enum Item
    {
        JumpBoost,
        AttackChain,
        ShieldWall
    }

    // Loại item hiện tại của object
    public Item itemType;
}