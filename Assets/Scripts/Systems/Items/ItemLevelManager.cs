using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SkyfallArena.Systems.Items
{
    /// <summary>
    /// Tracks per-player item levels and notifies UI/gameplay listeners.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemLevelManager : MonoBehaviour
    {
        [Serializable]
        public sealed class ItemLevelChangedUnityEvent : UnityEvent<int, ItemType, int, int> { }

        [Serializable]
        public sealed class ItemBuffContextUnityEvent : UnityEvent<int, ItemType, int, int> { }

        [Header("References")]
        [SerializeField] ItemDefinition[] availableItems;

        [Header("Events")]
        [SerializeField] ItemBuffContextUnityEvent onItemPicked = new ItemBuffContextUnityEvent();
        [SerializeField] ItemLevelChangedUnityEvent onItemLevelChanged = new ItemLevelChangedUnityEvent();

        readonly Dictionary<ItemType, ItemDefinition> definitionByType = new Dictionary<ItemType, ItemDefinition>();
        readonly Dictionary<int, Dictionary<ItemType, int>> levelsByPlayer = new Dictionary<int, Dictionary<ItemType, int>>();

        public event Action<ItemBuffContext> ItemPicked;
        public event Action<ItemBuffContext> ItemLevelChanged;

        void Awake()
        {
            RebuildDefinitionMap();
        }

        public void RebuildDefinitionMap()
        {
            definitionByType.Clear();

            if (availableItems == null)
                return;

            for (int index = 0; index < availableItems.Length; index++)
            {
                var definition = availableItems[index];
                if (definition == null)
                    continue;

                definitionByType[definition.ItemType] = definition;
            }
        }

        public bool TryGetDefinition(ItemType itemType, out ItemDefinition definition)
        {
            if (definitionByType.Count == 0)
                RebuildDefinitionMap();

            return definitionByType.TryGetValue(itemType, out definition) && definition != null;
        }

        public int GetLevel(int playerId, ItemType itemType)
        {
            if (!LocalPlayerRules.IsSupportedPlayerId(playerId))
                return 0;

            if (!levelsByPlayer.TryGetValue(playerId, out var byType))
                return 0;

            return byType.TryGetValue(itemType, out var level) ? level : 0;
        }

        public ItemBuffContext RegisterPickup(int playerId, ItemDefinition definition, GameObject collector = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (!LocalPlayerRules.IsSupportedPlayerId(playerId))
            {
                return new ItemBuffContext(
                    playerId,
                    definition.ItemType,
                    0,
                    0,
                    definition,
                    collector);
            }

            var levels = GetOrCreatePlayerLevels(playerId);

            int previousLevel = levels.TryGetValue(definition.ItemType, out var found) ? found : 0;
            int newLevel = Mathf.Clamp(previousLevel + 1, 1, definition.MaxLevel);
            levels[definition.ItemType] = newLevel;

            var context = new ItemBuffContext(
                playerId,
                definition.ItemType,
                newLevel,
                previousLevel,
                definition,
                collector);

            ItemPicked?.Invoke(context);
            onItemPicked.Invoke(playerId, definition.ItemType, previousLevel, newLevel);

            if (newLevel != previousLevel)
            {
                ItemLevelChanged?.Invoke(context);
                onItemLevelChanged.Invoke(playerId, definition.ItemType, newLevel, definition.MaxLevel);
            }

            return context;
        }

        public void ResetPlayerLevels(int playerId)
        {
            if (!LocalPlayerRules.IsSupportedPlayerId(playerId))
                return;

            levelsByPlayer.Remove(playerId);
        }

        Dictionary<ItemType, int> GetOrCreatePlayerLevels(int playerId)
        {
            if (!levelsByPlayer.TryGetValue(playerId, out var levels))
            {
                levels = new Dictionary<ItemType, int>();
                levelsByPlayer[playerId] = levels;
            }

            return levels;
        }
    }
}
