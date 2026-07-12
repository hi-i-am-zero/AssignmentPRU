using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ArenaEnvironment = Environment;

namespace SkyfallArena.Systems.Items
{
    [DisallowMultipleComponent]
    public sealed class ItemSpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class ItemSpawnUnityEvent : UnityEvent<ItemType, Vector2> { }

        [Header("References")]
        [SerializeField] ArenaEnvironment.ArenaManager arenaManager;
        [SerializeField] ItemLevelManager itemLevelManager;
        [SerializeField] ItemPickup defaultItemPickupPrefab;
        [SerializeField] ItemDefinition[] itemPool;
        [SerializeField] Transform runtimeItemRoot;

        [Header("Spawn Rules")]
        [SerializeField] bool spawnOnStart = true;
        [SerializeField, Min(0.1f)] float fallbackRespawnDelay = 8f;
        [SerializeField] bool usePooling = true;
        [SerializeField, Min(0)] int prewarmPerDefinition;
        [SerializeField] bool verboseLogs;

        [Header("Events")]
        [SerializeField] ItemSpawnUnityEvent onItemSpawned = new ItemSpawnUnityEvent();
        [SerializeField] ItemSpawnUnityEvent onItemCollected = new ItemSpawnUnityEvent();

        readonly Dictionary<ArenaEnvironment.ItemSpawnPoint, ItemPickup> activePickups =
            new Dictionary<ArenaEnvironment.ItemSpawnPoint, ItemPickup>();

        readonly List<ItemDefinition> validPool = new List<ItemDefinition>();
        readonly Dictionary<ItemPickup, Queue<ItemPickup>> pooledByPrefab = new Dictionary<ItemPickup, Queue<ItemPickup>>();
        readonly Dictionary<ItemPickup, ItemPickup> instancePrefabLookup = new Dictionary<ItemPickup, ItemPickup>();

        public event Action<ItemDefinition, ArenaEnvironment.ItemSpawnPoint> ItemSpawned;
        public event Action<ItemDefinition, ArenaEnvironment.ItemSpawnPoint> ItemCollected;

        void Awake()
        {
            if (arenaManager == null)
                arenaManager = FindFirstObjectByType<ArenaEnvironment.ArenaManager>();

            if (itemLevelManager == null)
                itemLevelManager = FindFirstObjectByType<ItemLevelManager>();

            RebuildPool();
        }

        void Start()
        {
            if (usePooling && prewarmPerDefinition > 0)
                PrewarmPools();

            if (!spawnOnStart)
                return;

            SpawnForAllPoints();
        }

        public void RebuildPool()
        {
            validPool.Clear();

            if (itemPool == null)
                return;

            for (int index = 0; index < itemPool.Length; index++)
            {
                var definition = itemPool[index];
                if (definition != null)
                    validPool.Add(definition);
            }
        }

        public void SpawnForAllPoints()
        {
            var spawnPoints = GetSpawnPoints();
            for (int index = 0; index < spawnPoints.Count; index++)
            {
                TrySpawnAt(spawnPoints[index]);
            }
        }

        public bool TrySpawnAt(ArenaEnvironment.ItemSpawnPoint spawnPoint)
        {
            if (spawnPoint == null || activePickups.ContainsKey(spawnPoint))
                return false;

            if (validPool.Count == 0)
                RebuildPool();

            if (validPool.Count == 0)
            {
                Debug.LogWarning("[ItemSpawner] No item definitions available for spawning.", this);
                return false;
            }

            ItemDefinition definition = validPool[UnityEngine.Random.Range(0, validPool.Count)];
            if (definition == null)
                return false;

            ItemPickup prefab = definition.PickupPrefabOverride != null
                ? definition.PickupPrefabOverride
                : defaultItemPickupPrefab;

            if (prefab == null)
            {
                Debug.LogError($"[ItemSpawner] Missing pickup prefab for {definition.name}.", this);
                return false;
            }

            var instance = AcquirePickup(prefab, spawnPoint.Position);
            instance.name = $"Item_{definition.ItemType}_{spawnPoint.name}";

            int itemLayer = LayerMask.NameToLayer(ArenaEnvironment.GameLayers.Item);
            if (itemLayer >= 0)
                instance.gameObject.layer = itemLayer;

            spawnPoint.MarkItemSpawned();
            instance.Initialize(definition, this, spawnPoint, itemLevelManager);

            activePickups[spawnPoint] = instance;
            ItemSpawned?.Invoke(definition, spawnPoint);
            onItemSpawned.Invoke(definition.ItemType, spawnPoint.Position);

            Log($"Spawned {definition.ItemType} at {spawnPoint.name}.");
            return true;
        }

        public void NotifyCollected(ItemPickup pickup)
        {
            if (pickup == null)
                return;

            var spawnPoint = pickup.OwnerSpawnPoint;
            var definition = pickup.Definition;

            if (spawnPoint != null)
            {
                spawnPoint.MarkItemTaken();
                activePickups.Remove(spawnPoint);
                StartCoroutine(RespawnAfterDelay(spawnPoint));
            }

            if (definition != null && spawnPoint != null)
            {
                ItemCollected?.Invoke(definition, spawnPoint);
                onItemCollected.Invoke(definition.ItemType, spawnPoint.Position);
            }

            RecyclePickup(pickup);
        }

        IEnumerator RespawnAfterDelay(ArenaEnvironment.ItemSpawnPoint spawnPoint)
        {
            float delay = spawnPoint != null
                ? Mathf.Max(0.1f, spawnPoint.RespawnDelay)
                : fallbackRespawnDelay;

            yield return new WaitForSeconds(delay);
            TrySpawnAt(spawnPoint);
        }

        IReadOnlyList<ArenaEnvironment.ItemSpawnPoint> GetSpawnPoints()
        {
            if (arenaManager != null && arenaManager.ItemSpawns != null && arenaManager.ItemSpawns.Count > 0)
                return arenaManager.ItemSpawns;

            var discovered = FindObjectsByType<ArenaEnvironment.ItemSpawnPoint>(FindObjectsSortMode.None);
            return discovered;
        }

        ItemPickup AcquirePickup(ItemPickup prefab, Vector2 position)
        {
            ItemPickup instance = null;

            if (usePooling)
            {
                Queue<ItemPickup> queue = GetPoolQueue(prefab);
                while (queue.Count > 0 && instance == null)
                    instance = queue.Dequeue();
            }

            if (instance == null)
                instance = Instantiate(prefab, position, Quaternion.identity, runtimeItemRoot);
            else
            {
                instance.transform.SetParent(runtimeItemRoot, worldPositionStays: false);
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.identity;
                instance.gameObject.SetActive(true);
            }

            instancePrefabLookup[instance] = prefab;
            return instance;
        }

        void RecyclePickup(ItemPickup pickup)
        {
            if (pickup == null)
                return;

            if (!usePooling)
            {
                instancePrefabLookup.Remove(pickup);
                Destroy(pickup.gameObject);
                return;
            }

            if (!instancePrefabLookup.TryGetValue(pickup, out var prefabKey) || prefabKey == null)
            {
                Destroy(pickup.gameObject);
                return;
            }

            var queue = GetPoolQueue(prefabKey);
            pickup.gameObject.SetActive(false);
            pickup.transform.SetParent(runtimeItemRoot, worldPositionStays: false);
            queue.Enqueue(pickup);
        }

        Queue<ItemPickup> GetPoolQueue(ItemPickup prefab)
        {
            if (!pooledByPrefab.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<ItemPickup>();
                pooledByPrefab[prefab] = queue;
            }

            return queue;
        }

        void PrewarmPools()
        {
            for (int index = 0; index < validPool.Count; index++)
            {
                var definition = validPool[index];
                if (definition == null)
                    continue;

                var prefab = definition.PickupPrefabOverride != null
                    ? definition.PickupPrefabOverride
                    : defaultItemPickupPrefab;

                if (prefab == null)
                    continue;

                var queue = GetPoolQueue(prefab);
                while (queue.Count < prewarmPerDefinition)
                {
                    var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity, runtimeItemRoot);
                    instance.gameObject.SetActive(false);
                    instancePrefabLookup[instance] = prefab;
                    queue.Enqueue(instance);
                }
            }
        }

        void Log(string message)
        {
            if (verboseLogs)
                Debug.Log($"[ItemSpawner] {message}", this);
        }
    }
}
