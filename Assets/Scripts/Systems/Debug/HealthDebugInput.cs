using UnityEngine;

namespace SkyfallArena.Systems.DebugTools
{
    /// <summary>
    /// Lightweight helper to simulate damage/heal during play mode testing.
    /// </summary>
    public sealed class HealthDebugInput : MonoBehaviour
    {
        [SerializeField] PlayerHealth playerHealth;
        [SerializeField] KeyCode damageKey = KeyCode.Minus;
        [SerializeField] KeyCode healKey = KeyCode.Equals;
        [SerializeField] KeyCode killKey = KeyCode.Backspace;
        [SerializeField, Min(1f)] float damageAmount = 25f;
        [SerializeField, Min(1f)] float healAmount = 10f;

        void Reset()
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        void Awake()
        {
            if (playerHealth == null)
                playerHealth = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            if (playerHealth == null)
                return;

            if (Input.GetKeyDown(damageKey))
                playerHealth.TakeDamage(damageAmount);

            if (Input.GetKeyDown(healKey))
                playerHealth.Heal(healAmount);

            if (Input.GetKeyDown(killKey))
                playerHealth.Kill();
        }

        [ContextMenu("Debug Damage")]
        void DebugDamage() => playerHealth?.TakeDamage(damageAmount);

        [ContextMenu("Debug Heal")]
        void DebugHeal() => playerHealth?.Heal(healAmount);

        [ContextMenu("Debug Kill")]
        void DebugKill() => playerHealth?.Kill();
    }
}
