using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Quản lý metadata map: tên arena, vị trí spawn player và item.
    /// LocalMultiplayerManager gọi GetSpawnPosition() khi spawn nhân vật.
    /// </summary>
    public class ArenaManager : MonoBehaviour
    {
        [SerializeField] string mapName = "Arena";
        [SerializeField] PlayerSpawnPoint[] spawnPoints;
        [SerializeField] ItemSpawnPoint[] itemSpawns;

        public string MapName => mapName;
        public IReadOnlyList<PlayerSpawnPoint> SpawnPoints => spawnPoints;
        public IReadOnlyList<ItemSpawnPoint> ItemSpawns => itemSpawns;

        // Tự tìm spawn point trong con nếu chưa gán thủ công trong Inspector
        void Awake()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                spawnPoints = GetComponentsInChildren<PlayerSpawnPoint>();

            if (itemSpawns == null || itemSpawns.Length == 0)
                itemSpawns = GetComponentsInChildren<ItemSpawnPoint>();
        }

        // Trả về vị trí spawn theo playerIndex (1 = P1, 2 = P2)
        // Fallback: spawn point đầu tiên hoặc (0,0) nếu không tìm thấy
        public Vector2 GetSpawnPosition(int playerIndex)
        {
            var point = spawnPoints.FirstOrDefault(s => s.PlayerIndex == playerIndex);
            return point != null ? point.Position : spawnPoints.FirstOrDefault()?.Position ?? Vector2.zero;
        }
    }
}
