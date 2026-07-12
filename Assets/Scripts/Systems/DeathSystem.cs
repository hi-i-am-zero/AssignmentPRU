using System;
using UnityEngine;
using UnityEngine.Events;

namespace SkyfallArena.Systems
{
    /// <summary>
    /// Applies Ring-Out Elimination behavior when a player dies.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class DeathSystem : MonoBehaviour
    {
        [Serializable]
        public sealed class PlayerDeathUnityEvent : UnityEvent<int, GameObject, string> { }

        [Header("References")]
        [SerializeField] PlayerHealth playerHealth;
        [SerializeField] bool restrictToFixedPlayers = true;

        [Header("Elimination Behavior")]
        [SerializeField] bool disablePlayerController = true;
        [SerializeField] bool disableAttackController = true;
        [SerializeField] bool disableComboController = true;
        [SerializeField] bool disableKnockbackController = true;
        [SerializeField] bool freezeRigidbodyOnDeath = true;
        [SerializeField] bool disableAllCollidersOnDeath;
        [SerializeField] Behaviour[] additionalBehavioursToDisable;

        [Header("Events")]
        [SerializeField] PlayerDeathUnityEvent onPlayerDeath = new PlayerDeathUnityEvent();
        [SerializeField] PlayerDeathUnityEvent onPlayerEliminated = new PlayerDeathUnityEvent();

        bool isEliminated;
        string eliminationReason = string.Empty;

        public event Action<DeathSystem, string> PlayerDied;
        public event Action<DeathSystem, string> PlayerEliminated;

        public bool IsEliminated => isEliminated;
        public string EliminationReason => eliminationReason;
        public int PlayerId => playerHealth != null ? playerHealth.PlayerId : 0;

        void Reset()
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        void Awake()
        {
            if (playerHealth == null)
                playerHealth = GetComponent<PlayerHealth>();
        }

        void OnEnable()
        {
            if (restrictToFixedPlayers && !LocalPlayerRules.IsSupportedPlayerId(PlayerId))
            {
                enabled = false;
                return;
            }

            if (playerHealth != null)
                playerHealth.HealthDepleted += HandleHealthDepleted;
        }

        void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.HealthDepleted -= HandleHealthDepleted;
        }

        public void ForceElimination(string reason)
        {
            if (isEliminated)
                return;

            ApplyElimination(string.IsNullOrWhiteSpace(reason) ? "forced_elimination" : reason);

            if (playerHealth != null && playerHealth.IsAlive)
                playerHealth.Kill();
        }

        void HandleHealthDepleted(PlayerHealth _, bool killedDirectly)
        {
            string reason = killedDirectly ? "killed" : "hp_depleted";
            ApplyElimination(reason);
        }

        void ApplyElimination(string reason)
        {
            if (isEliminated)
                return;

            isEliminated = true;
            eliminationReason = reason;

            DisableGameplayBehaviours();
            FreezePhysicsBodyIfNeeded();
            DisableCollidersIfNeeded();

            PlayerDied?.Invoke(this, reason);
            PlayerEliminated?.Invoke(this, reason);
            onPlayerDeath.Invoke(PlayerId, gameObject, reason);
            onPlayerEliminated.Invoke(PlayerId, gameObject, reason);
        }

        void DisableGameplayBehaviours()
        {
            if (disablePlayerController)
            {
                var movement = GetComponent<PlayerController>();
                if (movement != null)
                    movement.enabled = false;
            }

            if (disableAttackController)
            {
                var attack = GetComponent<AttackController>();
                if (attack != null)
                    attack.enabled = false;
            }

            if (disableComboController)
            {
                var combo = GetComponent<ComboController>();
                if (combo != null)
                    combo.enabled = false;
            }

            if (disableKnockbackController)
            {
                var knockback = GetComponent<KnockbackController>();
                if (knockback != null)
                    knockback.enabled = false;
            }

            if (additionalBehavioursToDisable == null)
                return;

            for (int index = 0; index < additionalBehavioursToDisable.Length; index++)
            {
                var behaviour = additionalBehavioursToDisable[index];
                if (behaviour != null)
                    behaviour.enabled = false;
            }
        }

        void FreezePhysicsBodyIfNeeded()
        {
            if (!freezeRigidbodyOnDeath)
                return;

            var body = GetComponent<Rigidbody2D>();
            if (body == null)
                return;

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        void DisableCollidersIfNeeded()
        {
            if (!disableAllCollidersOnDeath)
                return;

            var colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
            for (int index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null)
                    colliders[index].enabled = false;
            }
        }
    }
}
