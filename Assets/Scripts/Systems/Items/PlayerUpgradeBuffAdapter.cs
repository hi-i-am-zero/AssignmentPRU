using UnityEngine;

namespace SkyfallArena.Systems.Items
{
    /// <summary>
    /// Bridges item buff payloads into legacy PlayerUpgrade methods.
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

            switch (context.ItemType)
            {
                case ItemType.JumpBoost:
                    playerUpgrade.UpgradeJumpBoost();
                    break;
                case ItemType.AttackChain:
                    playerUpgrade.UpgradeAttackChain();
                    break;
                case ItemType.ShieldWall:
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
