using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Environment
{
    public class ArenaManager : MonoBehaviour
    {
        [SerializeField] string mapName = "Arena";
        [SerializeField] PlayerSpawnPoint[] spawnPoints;
        [SerializeField] ItemSpawnPoint[] itemSpawns;

        public string MapName => mapName;
        public IReadOnlyList<PlayerSpawnPoint> SpawnPoints => spawnPoints;
        public IReadOnlyList<ItemSpawnPoint> ItemSpawns => itemSpawns;

        void Awake()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                spawnPoints = GetComponentsInChildren<PlayerSpawnPoint>();

            if (itemSpawns == null || itemSpawns.Length == 0)
                itemSpawns = GetComponentsInChildren<ItemSpawnPoint>();
        }

        public Vector2 GetSpawnPosition(int playerIndex)
        {
            var point = spawnPoints.FirstOrDefault(s => s.PlayerIndex == playerIndex);
            return point != null ? point.Position : spawnPoints.FirstOrDefault()?.Position ?? Vector2.zero;
        }
    }
}
