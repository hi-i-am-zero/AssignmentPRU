using UnityEngine;

namespace SkyfallArena.Systems.Items
{
    /// <summary>
    /// Runtime payload passed from the item system to buff handlers.
    /// </summary>
    public readonly struct ItemBuffContext
    {
        public readonly int PlayerId;
        public readonly ItemType ItemType;
        public readonly int Level;
        public readonly int PreviousLevel;
        public readonly ItemDefinition Definition;
        public readonly GameObject Collector;

        public ItemBuffContext(
            int playerId,
            ItemType itemType,
            int level,
            int previousLevel,
            ItemDefinition definition,
            GameObject collector)
        {
            PlayerId = playerId;
            ItemType = itemType;
            Level = level;
            PreviousLevel = previousLevel;
            Definition = definition;
            Collector = collector;
        }
    }
}
