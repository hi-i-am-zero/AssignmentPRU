using UnityEngine;
using SkyfallArena.Systems;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// Hook HUD trong trận (đã bỏ gợi ý phím; UI chính ở Title/Select/Result).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] LocalMultiplayerManager multiplayerManager;
        [SerializeField] MatchResultSystem matchResultSystem;

        void Awake()
        {
            if (multiplayerManager == null)
                multiplayerManager = FindFirstObjectByType<LocalMultiplayerManager>();
            if (matchResultSystem == null)
                matchResultSystem = FindFirstObjectByType<MatchResultSystem>();
        }
    }
}
