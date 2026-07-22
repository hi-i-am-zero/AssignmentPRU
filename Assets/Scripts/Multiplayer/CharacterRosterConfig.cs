using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// ScriptableObject: map Character → prefab + thứ tự chọn nhân vật.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterRosterConfig", menuName = "Skyfall Arena/Multiplayer/Character Roster")]
    public sealed class CharacterRosterConfig : ScriptableObject
    {
        [Serializable]
        struct CharacterPrefabEntry
        {
            public CharacterType.Character character;
            public GameObject prefab;
        }

        [SerializeField] CharacterType.Character[] characterSelectionOrder =
        {
            CharacterType.Character.Knight,
            CharacterType.Character.Ninja,
            CharacterType.Character.Sorcerer
        };

        [SerializeField] CharacterPrefabEntry[] entries;

        readonly Dictionary<CharacterType.Character, GameObject> prefabLookup = new Dictionary<CharacterType.Character, GameObject>();

        public IReadOnlyList<CharacterType.Character> SelectionOrder => characterSelectionOrder;

        void OnEnable()
        {
            RebuildLookup();
        }

        public bool TryGetPrefab(CharacterType.Character character, out GameObject prefab)
        {
            if (prefabLookup.Count == 0)
                RebuildLookup();

            return prefabLookup.TryGetValue(character, out prefab) && prefab != null;
        }

        void RebuildLookup()
        {
            prefabLookup.Clear();

            if (entries == null)
                return;

            for (int index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                if (entry.prefab == null)
                    continue;

                prefabLookup[entry.character] = entry.prefab;
            }
        }
    }
}
