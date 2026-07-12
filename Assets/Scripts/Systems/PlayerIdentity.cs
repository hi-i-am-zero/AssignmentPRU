using UnityEngine;

namespace SkyfallArena.Systems
{
    /// <summary>
    /// Stable runtime player id shared across systems.
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
