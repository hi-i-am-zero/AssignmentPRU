using System;
using UnityEngine;
using UnityEngine.Events;
using ArenaEnvironment = Environment;
using SkyfallArena.Multiplayer;

namespace SkyfallArena.Systems
{
    /// <summary>
    /// Stores and updates HP for one player. Exposes events for UI/VFX systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour, ArenaEnvironment.IDamageable
    {
        [Serializable]
        public sealed class HealthChangedUnityEvent : UnityEvent<int, float, float, float> { }

        [Serializable]
        public sealed class HealthValueUnityEvent : UnityEvent<int, float, float> { }

        [Serializable]
        public sealed class PlayerStateUnityEvent : UnityEvent<int> { }

        [Header("Health")]
        [SerializeField, Min(1f)] float maxHealth = 100f;
        [SerializeField, Min(0f)] float currentHealth = 100f;
        [SerializeField] bool startAtMaxHealth = true;
        [SerializeField] bool useCharacterStatusMaxHealth = true;
        [SerializeField] bool restrictToFixedPlayers = true;
        [SerializeField, Min(0)] int fallbackPlayerId;

        [Header("Events")]
        [SerializeField] HealthChangedUnityEvent onHealthChanged = new HealthChangedUnityEvent();
        [SerializeField] HealthValueUnityEvent onDamaged = new HealthValueUnityEvent();
        [SerializeField] HealthValueUnityEvent onHealed = new HealthValueUnityEvent();
        [SerializeField] PlayerStateUnityEvent onHealthDepleted = new PlayerStateUnityEvent();
        [SerializeField] PlayerStateUnityEvent onKilled = new PlayerStateUnityEvent();

        CharacterStatus characterStatus;
        PlayerUpgrade playerUpgrade;
        PlayerIdentity playerIdentity;
        LocalPlayerInputSource localInputSource;
        PlayerController playerController;

        bool isAlive = true;
        bool initialized;

        public event Action<PlayerHealth, float, float> HealthChanged;
        public event Action<PlayerHealth, float> Damaged;
        public event Action<PlayerHealth, float> Healed;
        public event Action<PlayerHealth, bool> HealthDepleted;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
        public bool IsAlive => isAlive;

        public int PlayerId
        {
            get
            {
                if (playerIdentity != null && playerIdentity.PlayerId > 0)
                    return playerIdentity.PlayerId;

                if (localInputSource == null)
                    localInputSource = GetComponent<LocalPlayerInputSource>();

                if (localInputSource != null && localInputSource.PlayerId > 0)
                    return localInputSource.PlayerId;

                if (playerController != null)
                {
                    if (playerController.playerType == PlayerController.PlayerType.Player1)
                        return 1;
                    if (playerController.playerType == PlayerController.PlayerType.Player2)
                        return 2;
                }

                int fallback = Mathf.Max(0, fallbackPlayerId);
                if (restrictToFixedPlayers && !LocalPlayerRules.IsSupportedPlayerId(fallback))
                    return 0;

                return fallback;
            }
        }

        void Awake()
        {
            characterStatus = GetComponent<CharacterStatus>();
            playerUpgrade = GetComponent<PlayerUpgrade>();
            playerIdentity = GetComponent<PlayerIdentity>();
            localInputSource = GetComponent<LocalPlayerInputSource>();
            playerController = GetComponent<PlayerController>();
        }

        void Start()
        {
            if (restrictToFixedPlayers && !LocalPlayerRules.IsSupportedPlayerId(PlayerId))
            {
                enabled = false;
                return;
            }

            InitializeHealth();
            BroadcastHealthChanged();
        }

        public void SetMaxHealth(float newMaxHealth, bool clampCurrent = true)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);

            if (clampCurrent)
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            if (!isAlive && currentHealth > 0f)
                isAlive = true;

            BroadcastHealthChanged();
        }

        public void RestoreToFullHealth()
        {
            currentHealth = maxHealth;
            isAlive = true;
            BroadcastHealthChanged();
        }

        public void TakeDamage(float amount)
        {
            // Keep damage working even if this component was disabled by id checks.
            if (!initialized)
                InitializeHealth();

            if (!isAlive || amount <= 0f)
                return;

            if (playerUpgrade != null && playerUpgrade.damageReduction > 0f)
            {
                float reduction = Mathf.Clamp01(playerUpgrade.damageReduction);
                amount *= 1f - reduction;
            }

            var blockController = GetComponent<PlayerBlockController>();
            if (blockController != null && blockController.IsBlocking)
                amount *= Mathf.Clamp01(blockController.DamageTakenWhileBlocking);

            if (amount <= 0f)
                return;

            float previous = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - amount);

            Damaged?.Invoke(this, amount);
            onDamaged.Invoke(PlayerId, amount, currentHealth);

            if (!Mathf.Approximately(previous, currentHealth))
                BroadcastHealthChanged();

            if (currentHealth <= 0f)
                HandleHealthDepleted(false);
        }

        public void Heal(float amount)
        {
            if (!isAlive || amount <= 0f)
                return;

            float previous = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            float applied = currentHealth - previous;
            if (applied <= 0f)
                return;

            Healed?.Invoke(this, applied);
            onHealed.Invoke(PlayerId, applied, currentHealth);
            BroadcastHealthChanged();
        }

        public void Kill()
        {
            if (!isAlive)
                return;

            currentHealth = 0f;
            BroadcastHealthChanged();
            HandleHealthDepleted(true);
        }

        void InitializeHealth()
        {
            if (initialized)
                return;

            initialized = true;

            if (useCharacterStatusMaxHealth && characterStatus != null && characterStatus.maxHP > 0)
            {
                maxHealth = characterStatus.maxHP;
            }

            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = startAtMaxHealth ? maxHealth : Mathf.Clamp(currentHealth, 0f, maxHealth);
            isAlive = currentHealth > 0f;
        }

        void BroadcastHealthChanged()
        {
            float normalized = HealthNormalized;
            HealthChanged?.Invoke(this, currentHealth, maxHealth);
            onHealthChanged.Invoke(PlayerId, currentHealth, maxHealth, normalized);
        }

        void HandleHealthDepleted(bool killedDirectly)
        {
            if (!isAlive)
                return;

            isAlive = false;
            HealthDepleted?.Invoke(this, killedDirectly);
            onHealthDepleted.Invoke(PlayerId);

            if (killedDirectly)
                onKilled.Invoke(PlayerId);
        }
    }
}
