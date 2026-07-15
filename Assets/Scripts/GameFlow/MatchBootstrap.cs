using SkyfallArena.Multiplayer;
using UnityEngine;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Starts a local match from GameSession selections (or editor fallback).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchBootstrap : MonoBehaviour
    {
        [SerializeField] LocalMultiplayerManager multiplayerManager;
        [SerializeField] bool useEditorFallbackWhenNoSession = true;
        [SerializeField] CharacterType.Character fallbackP1 = CharacterType.Character.Knight;
        [SerializeField] CharacterType.Character fallbackP2 = CharacterType.Character.Ninja;

        void Awake()
        {
            if (multiplayerManager == null)
                multiplayerManager = FindFirstObjectByType<LocalMultiplayerManager>();

            if (GetComponent<ResultUI>() == null)
                gameObject.AddComponent<ResultUI>();

            if (GetComponent<PlayerHealthBarsHud>() == null)
                gameObject.AddComponent<PlayerHealthBarsHud>();
        }

        void Start()
        {
            if (multiplayerManager == null)
            {
                Debug.LogError("[MatchBootstrap] LocalMultiplayerManager not found.");
                return;
            }

            CharacterType.Character p1 = fallbackP1;
            CharacterType.Character p2 = fallbackP2;

            var session = GameSession.Instance;
            if (session != null && session.HasMatchSelection)
            {
                p1 = session.Player1Character;
                p2 = session.Player2Character;
            }
            else if (!useEditorFallbackWhenNoSession)
            {
                Debug.LogWarning("[MatchBootstrap] No match selection. Waiting for session.");
                return;
            }
            else
            {
                Debug.Log("[MatchBootstrap] No session selection — using editor fallback characters.");
            }

            multiplayerManager.StartMatchFromSession(p1, p2);
        }
    }
}
