using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SkyfallArena.Systems.Items
{
    /// <summary>
    /// Theo dõi cấp skill từng player (max Lv3). Full rồi không nhặt thêm cùng loại.
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

        public bool IsMaxLevel(int playerId, ItemDefinition definition)
        {
            if (definition == null || !LocalPlayerRules.IsSupportedPlayerId(playerId))
                return false;

            return GetLevel(playerId, definition.ItemType) >= definition.MaxLevel;
        }

        public bool IsMaxLevel(int playerId, ItemType itemType)
        {
            if (!LocalPlayerRules.IsSupportedPlayerId(playerId))
                return false;

            if (!TryGetDefinition(itemType, out var definition) || definition == null)
                return GetLevel(playerId, itemType) >= 3;

            return GetLevel(playerId, itemType) >= definition.MaxLevel;
        }

        /// <summary>
        /// Returns false when the player already has this item at max level (no pickup).
        /// </summary>
        public bool TryRegisterPickup(
            int playerId,
            ItemDefinition definition,
            GameObject collector,
            out ItemBuffContext context)
        {
            context = default;

            if (definition == null)
                return false;

            if (!LocalPlayerRules.IsSupportedPlayerId(playerId))
                return false;

            int previousLevel = GetLevel(playerId, definition.ItemType);
            if (previousLevel >= definition.MaxLevel)
                return false;

            var levels = GetOrCreatePlayerLevels(playerId);
            int newLevel = Mathf.Clamp(previousLevel + 1, 1, definition.MaxLevel);
            levels[definition.ItemType] = newLevel;

            context = new ItemBuffContext(
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

            return true;
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
