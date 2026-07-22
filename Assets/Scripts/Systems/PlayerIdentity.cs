using UnityEngine;

namespace SkyfallArena.Systems
{
    /// <summary>
    /// ID player runtime (1 hoặc 2) dùng chung giữa các hệ thống.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerIdentity : MonoBehaviour
    {
        [SerializeField, Min(0)] int playerId;

        public int PlayerId => playerId;

        public void Initialize(int assignedPlayerId)
        {
            playerId = Mathf.Max(0, assignedPlayerId);
        }
    }
}
