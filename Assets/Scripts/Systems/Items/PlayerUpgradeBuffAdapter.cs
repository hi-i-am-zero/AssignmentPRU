using UnityEngine;

namespace SkyfallArena.Systems.Items
{
    /// <summary>
    /// Cầu nối item → PlayerUpgrade: nhảy / sát thương / giảm damage.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerUpgradeBuffAdapter : MonoBehaviour, IBuffable
    {
        [SerializeField] PlayerUpgrade playerUpgrade;
        [SerializeField] bool verboseLogs;

        void Reset()
        {
            playerUpgrade = GetComponent<PlayerUpgrade>();
        }

        void Awake()
        {
            if (playerUpgrade == null)
                playerUpgrade = GetComponent<PlayerUpgrade>();
        }

        public bool TryApplyBuff(ItemBuffContext context)
        {
            if (playerUpgrade == null)
                return false;

            // Pickup layer already blocks maxed skills; keep a hard guard here too.
            switch (context.ItemType)
            {
                case ItemType.JumpBoost:
                    if (playerUpgrade.jumpBoostLevel >= 3)
                        return false;
                    playerUpgrade.UpgradeJumpBoost();
                    break;
                case ItemType.AttackChain:
                    if (playerUpgrade.attackChainLevel >= 3)
                        return false;
                    playerUpgrade.UpgradeAttackChain();
                    break;
                case ItemType.ShieldWall:
                    if (playerUpgrade.shieldWallLevel >= 3)
                        return false;
                    playerUpgrade.UpgradeShieldWall();
                    break;
                default:
                    return false;
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[PlayerUpgradeBuffAdapter] P{context.PlayerId} applied {context.ItemType} -> Lv{context.Level}.",
                    this);
            }

            return true;
        }
    }
}
