using System;
using UnityEngine;

namespace SkyfallArena.Systems.Items
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Skyfall Arena/Items/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class ItemLevelData
        {
            [Min(1)] public int level = 1;
            [TextArea] public string description;
        }

        [SerializeField] ItemType itemType;
        [SerializeField] string displayName = "New Item";
        [SerializeField, Min(1)] int maxLevel = 3;
        [SerializeField] Sprite worldSprite;
        [SerializeField] Color tintColor = Color.white;
        [SerializeField] ItemLevelData[] levelData;
        [SerializeField] ItemPickup pickupPrefabOverride;

        public ItemType ItemType => itemType;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? itemType.ToString() : displayName;
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public Sprite WorldSprite => worldSprite;
        public Color TintColor => tintColor;
        public ItemPickup PickupPrefabOverride => pickupPrefabOverride;

        public ItemLevelData GetLevelData(int level)
        {
            if (levelData == null || levelData.Length == 0)
                return null;

            int clamped = Mathf.Clamp(level, 1, MaxLevel);
            for (int index = 0; index < levelData.Length; index++)
            {
                var entry = levelData[index];
                if (entry != null && entry.level == clamped)
                    return entry;
            }

            return null;
        }
    }
}
