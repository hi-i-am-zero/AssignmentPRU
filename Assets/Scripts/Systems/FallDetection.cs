using System;
using UnityEngine;
using UnityEngine.Events;
using ArenaEnvironment = Environment;

namespace SkyfallArena.Systems
{
    /// <summary>
    /// Detects when a player falls out of arena bounds and routes to DeathSystem.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class FallDetection : MonoBehaviour
    {
        [Serializable]
        public sealed class PlayerFallUnityEvent : UnityEvent<int, string> { }

        [Header("References")]
        [SerializeField] PlayerHealth playerHealth;
        [SerializeField] DeathSystem deathSystem;

        [Header("Y Threshold Detection")]
        [SerializeField] bool useYThreshold = true;
        [SerializeField] float deathYThreshold = -12f;

        [Header("Trigger Detection")]
        [SerializeField] bool useTriggerDetection = true;
        [SerializeField] string deathZoneTag = ArenaEnvironment.GameLayers.TagDeathZone;
        [SerializeField] string deathZoneLayerName = ArenaEnvironment.GameLayers.DeathZone;

        [Header("Events")]
        [SerializeField] PlayerFallUnityEvent onPlayerFell = new PlayerFallUnityEvent();

        bool hasTriggeredFall;

        public event Action<FallDetection, string> PlayerFell;

        public bool HasTriggeredFall => hasTriggeredFall;

        void Reset()
        {
            playerHealth = GetComponent<PlayerHealth>();
            deathSystem = GetComponent<DeathSystem>();
        }

        void Awake()
        {
            if (playerHealth == null)
                playerHealth = GetComponent<PlayerHealth>();

            if (deathSystem == null)
                deathSystem = GetComponent<DeathSystem>();
        }

        void OnEnable()
        {
            hasTriggeredFall = false;
        }

        void Update()
        {
            if (!useYThreshold || hasTriggeredFall || playerHealth == null || !playerHealth.IsAlive)
                return;

            if (transform.position.y <= deathYThreshold)
                TriggerFall("y_threshold");
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!useTriggerDetection || hasTriggeredFall || other == null || playerHealth == null || !playerHealth.IsAlive)
                return;

            bool matchedTag = !string.IsNullOrEmpty(deathZoneTag) && other.CompareTag(deathZoneTag);
            bool matchedLayer = IsInLayer(other.gameObject.layer, deathZoneLayerName);

            if (matchedTag || matchedLayer)
                TriggerFall("death_zone_trigger");
        }

        void TriggerFall(string reason)
        {
            if (hasTriggeredFall)
                return;

            hasTriggeredFall = true;

            int playerId = playerHealth != null ? playerHealth.PlayerId : 0;
            PlayerFell?.Invoke(this, reason);
            onPlayerFell.Invoke(playerId, reason);

            if (deathSystem != null)
                deathSystem.ForceElimination(reason);
            else if (playerHealth != null)
                playerHealth.Kill();
        }

        static bool IsInLayer(int layer, string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
                return false;

            int expectedLayer = LayerMask.NameToLayer(layerName);
            return expectedLayer >= 0 && layer == expectedLayer;
        }
    }
}
