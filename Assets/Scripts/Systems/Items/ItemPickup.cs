using System;
using UnityEngine;
using UnityEngine.Events;
using ArenaEnvironment = Environment;
using SkyfallArena.Multiplayer;
using SkyfallArena.Systems;

namespace SkyfallArena.Systems.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [Serializable]
        public sealed class ItemCollectedUnityEvent : UnityEvent<int, ItemType, int, int> { }

        [Header("Setup")]
        [SerializeField] ItemDefinition itemDefinition;
        [SerializeField] ItemLevelManager itemLevelManager;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Collider2D pickupCollider;

        [Header("Events")]
        [SerializeField] ItemCollectedUnityEvent onItemCollected = new ItemCollectedUnityEvent();

        ItemSpawner ownerSpawner;
        ArenaEnvironment.ItemSpawnPoint ownerSpawnPoint;
        bool isCollected;

        public event Action<ItemBuffContext> ItemCollected;

        public ItemDefinition Definition => itemDefinition;
        public ArenaEnvironment.ItemSpawnPoint OwnerSpawnPoint => ownerSpawnPoint;
        public bool IsCollected => isCollected;

        void Reset()
        {
            pickupCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (pickupCollider != null)
                pickupCollider.isTrigger = true;
        }

        void Awake()
        {
            if (pickupCollider == null)
                pickupCollider = GetComponent<Collider2D>();

            if (pickupCollider != null)
                pickupCollider.isTrigger = true;

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (itemLevelManager == null)
                itemLevelManager = FindFirstObjectByType<ItemLevelManager>();

            ApplyDefinitionVisuals();
        }

        public void Initialize(
            ItemDefinition definition,
            ItemSpawner spawner,
            ArenaEnvironment.ItemSpawnPoint spawnPoint,
            ItemLevelManager levelManager)
        {
            itemDefinition = definition;
            ownerSpawner = spawner;
            ownerSpawnPoint = spawnPoint;
            itemLevelManager = levelManager != null ? levelManager : itemLevelManager;
            isCollected = false;

            if (pickupCollider != null)
                pickupCollider.enabled = true;

            ApplyDefinitionVisuals();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected || other == null)
                return;

            if (!other.CompareTag(ArenaEnvironment.GameLayers.TagPlayer))
                return;

            if (itemDefinition == null || itemLevelManager == null)
                return;

            var collector = ResolveCollectorObject(other);
            int playerId = ResolvePlayerId(collector);
            if (!LocalPlayerRules.IsSupportedPlayerId(playerId))
                return;

            // Already maxed this skill: leave the item for the other player / future use.
            if (itemLevelManager.IsMaxLevel(playerId, itemDefinition))
                return;

            if (!itemLevelManager.TryRegisterPickup(playerId, itemDefinition, collector, out var context))
                return;

            isCollected = true;

            ApplyBuffToCollector(collector, context);

            ItemCollected?.Invoke(context);
            onItemCollected.Invoke(playerId, itemDefinition.ItemType, context.PreviousLevel, context.Level);

            if (pickupCollider != null)
                pickupCollider.enabled = false;

            if (ownerSpawner != null)
                ownerSpawner.NotifyCollected(this);
            else
                Destroy(gameObject);
        }

        void ApplyBuffToCollector(GameObject collector, ItemBuffContext context)
        {
            if (collector == null)
                return;

            var behaviours = collector.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            bool anyApplied = false;

            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IBuffable buffable)
                {
                    try
                    {
                        buffable.TryApplyBuff(context);
                        anyApplied = true;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, collector);
                    }
                }
            }

            if (!anyApplied)
            {
                var parentBehaviours = collector.GetComponentsInParent<MonoBehaviour>(includeInactive: true);
                for (int index = 0; index < parentBehaviours.Length; index++)
                {
                    if (parentBehaviours[index] is IBuffable buffable)
                    {
                        try
                        {
                            buffable.TryApplyBuff(context);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception, collector);
                        }
                    }
                }
            }
        }

        static GameObject ResolveCollectorObject(Collider2D other)
        {
            if (other.attachedRigidbody != null)
                return other.attachedRigidbody.gameObject;

            return other.transform.root != null ? other.transform.root.gameObject : other.gameObject;
        }

        static int ResolvePlayerId(GameObject collector)
        {
            if (collector == null)
                return 0;

            if (collector.TryGetComponent<PlayerIdentity>(out var identity) && identity.PlayerId > 0)
                return identity.PlayerId;

            if (collector.TryGetComponent<PlayerHealth>(out var health) && health.PlayerId > 0)
                return health.PlayerId;

            if (collector.TryGetComponent<LocalPlayerInputSource>(out var inputSource) && inputSource.PlayerId > 0)
                return inputSource.PlayerId;

            if (collector.TryGetComponent<PlayerController>(out var playerController))
            {
                return playerController.playerType == PlayerController.PlayerType.Player1 ? 1 : 2;
            }

            return 0;
        }

        void ApplyDefinitionVisuals()
        {
            if (itemDefinition == null || spriteRenderer == null)
                return;

            if (itemDefinition.WorldSprite != null)
                spriteRenderer.sprite = itemDefinition.WorldSprite;

            spriteRenderer.color = itemDefinition.TintColor;
        }
    }
}
