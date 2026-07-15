using UnityEngine;
using SkyfallArena.Systems;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// Match HUD hook (control hints removed — Title/Select/Result handle UI flow).
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
