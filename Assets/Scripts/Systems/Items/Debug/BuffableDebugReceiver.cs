using UnityEngine;

namespace SkyfallArena.Systems.Items.DebugTools
{
    /// <summary>
    /// Temporary helper to verify that pickup correctly forwards buff payloads.
    /// Remove after Member 1 provides real buff handlers.
    /// </summary>
    public sealed class BuffableDebugReceiver : MonoBehaviour, IBuffable
    {
        [SerializeField] bool verboseLogs = true;

        public bool TryApplyBuff(ItemBuffContext context)
        {
            if (verboseLogs)
            {
                Debug.Log(
                    $"[BuffableDebugReceiver] P{context.PlayerId} -> {context.ItemType} Lv{context.Level} (prev Lv{context.PreviousLevel})",
                    this);
            }

            return true;
        }
    }
}
