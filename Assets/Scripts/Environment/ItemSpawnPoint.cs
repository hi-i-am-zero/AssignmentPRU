using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment
{
    /// <summary>Điểm spawn item trên map (ItemSpawner đọc danh sách này).</summary>
    [DisallowMultipleComponent]
    public class ItemSpawnPoint : MonoBehaviour
    {
        // Thời gian chờ trước khi item xuất hiện lại sau khi bị nhặt
        // respawnDelay: giây
        [SerializeField] float respawnDelay = 10f;
        [SerializeField] bool hasItem;

        public float RespawnDelay => respawnDelay;
        public bool HasItem => hasItem;
        public Vector2 Position => transform.position;

        Light2D zoneLight;

        // Bật ánh sáng vàng nhạt đánh dấu vị trí item (chỉ visual)
        void Awake()
        {
            zoneLight = GetComponentInChildren<Light2D>();
            if (zoneLight != null)
            {
                zoneLight.color = ZoneLightColors.ItemSpawn;
                zoneLight.intensity = ZoneLightColors.ItemSpawnIntensity;
            }
        }

        public void MarkItemTaken() => hasItem = false;
        public void MarkItemSpawned() => hasItem = true;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = ZoneLightColors.ItemSpawn;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
        }
#endif
    }
}
