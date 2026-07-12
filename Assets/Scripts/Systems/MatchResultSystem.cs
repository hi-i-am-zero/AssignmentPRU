using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SkyfallArena.Multiplayer;

namespace SkyfallArena.Systems
{
    /// <summary>
    /// Determines match winner based on remaining non-eliminated players.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchResultSystem : MonoBehaviour
    {
        const int FixedPlayers = LocalPlayerRules.FixedPlayerCount;

        [Serializable]
        public sealed class MatchEndedUnityEvent : UnityEvent<int, int> { }

        [Serializable]
        public sealed class PlayerEliminatedUnityEvent : UnityEvent<int, string> { }

        [Header("References")]
        [SerializeField] LocalMultiplayerManager localMultiplayerManager;

        [Header("Rules")]
        [SerializeField] bool autoStartTrackingOnMatchStart = true;
        [SerializeField] bool evaluateAsSoonAsPlayerEliminated = true;
        [SerializeField] bool endMatchWhenOneOrZeroAlive = true;
        [SerializeField, Min(0.1f)] float discoveryInterval = 0.5f;
        [SerializeField] bool verboseLogs;

        [Header("Events")]
        [SerializeField] MatchEndedUnityEvent onMatchEnded = new MatchEndedUnityEvent();
        [SerializeField] PlayerEliminatedUnityEvent onPlayerEliminated = new PlayerEliminatedUnityEvent();

        readonly Dictionary<int, DeathSystem> deathSystemsByPlayer = new Dictionary<int, DeathSystem>();

        float nextDiscoveryTime;
        bool isTracking;
        bool matchEnded;

        public event Action<int, int> MatchEnded;
        public event Action<int, string> PlayerEliminated;

        public bool IsTracking => isTracking;
        public bool MatchHasEnded => matchEnded;

        void Awake()
        {
            if (localMultiplayerManager == null)
                localMultiplayerManager = FindFirstObjectByType<LocalMultiplayerManager>();
        }

        void OnEnable()
        {
            if (localMultiplayerManager != null)
                localMultiplayerManager.MatchStarted += HandleMatchStarted;
        }

        void OnDisable()
        {
            if (localMultiplayerManager != null)
                localMultiplayerManager.MatchStarted -= HandleMatchStarted;

            ClearTrackedSystems();
        }

        void Update()
        {
            if (!isTracking || matchEnded)
                return;

            if (Time.time < nextDiscoveryTime)
                return;

            nextDiscoveryTime = Time.time + discoveryInterval;
            DiscoverAndTrackPlayers();
        }

        public void BeginTracking()
        {
            if (isTracking)
                return;

            isTracking = true;
            matchEnded = false;
            nextDiscoveryTime = Time.time;
            DiscoverAndTrackPlayers();
            EvaluateOutcome();
        }

        public void StopTracking()
        {
            isTracking = false;
            ClearTrackedSystems();
        }

        public void ForceEvaluate()
        {
            if (!isTracking)
                BeginTracking();
            else
                EvaluateOutcome();
        }

        void HandleMatchStarted()
        {
            if (!autoStartTrackingOnMatchStart)
                return;

            BeginTracking();
        }

        void DiscoverAndTrackPlayers()
        {
            var discovered = FindObjectsByType<DeathSystem>(FindObjectsSortMode.None);
            for (int index = 0; index < discovered.Length; index++)
            {
                var deathSystem = discovered[index];
                if (deathSystem == null || deathSystem.PlayerId <= 0)
                    continue;

                if (!LocalPlayerRules.IsSupportedPlayerId(deathSystem.PlayerId))
                    continue;

                if (deathSystemsByPlayer.ContainsKey(deathSystem.PlayerId))
                    continue;

                deathSystemsByPlayer[deathSystem.PlayerId] = deathSystem;
                deathSystem.PlayerEliminated += HandlePlayerEliminated;
                Log($"Tracking P{deathSystem.PlayerId}.");
            }
        }

        void HandlePlayerEliminated(DeathSystem deathSystem, string reason)
        {
            if (deathSystem == null)
                return;

            int playerId = deathSystem.PlayerId;
            PlayerEliminated?.Invoke(playerId, reason);
            onPlayerEliminated.Invoke(playerId, reason);

            if (evaluateAsSoonAsPlayerEliminated)
                EvaluateOutcome();
        }

        void EvaluateOutcome()
        {
            if (matchEnded || !endMatchWhenOneOrZeroAlive)
                return;

            if (deathSystemsByPlayer.Count < FixedPlayers)
                return;

            int aliveCount = 0;
            int winnerPlayerId = 0;

            foreach (var pair in deathSystemsByPlayer)
            {
                var deathSystem = pair.Value;
                if (deathSystem == null)
                    continue;

                if (!deathSystem.IsEliminated)
                {
                    aliveCount++;
                    winnerPlayerId = pair.Key;
                }
            }

            if (deathSystemsByPlayer.Count == 0)
                return;

            if (aliveCount > 1)
                return;

            matchEnded = true;
            MatchEnded?.Invoke(winnerPlayerId, aliveCount);
            onMatchEnded.Invoke(winnerPlayerId, aliveCount);
            Log(aliveCount == 1
                ? $"Match ended. Winner is P{winnerPlayerId}."
                : "Match ended with no remaining alive players.");
        }

        void ClearTrackedSystems()
        {
            foreach (var pair in deathSystemsByPlayer)
            {
                if (pair.Value != null)
                    pair.Value.PlayerEliminated -= HandlePlayerEliminated;
            }

            deathSystemsByPlayer.Clear();
        }

        void Log(string message)
        {
            if (verboseLogs)
                Debug.Log($"[MatchResultSystem] {message}", this);
        }
    }
}
