using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Đánh dấu vị trí spawn player trên map.
    /// Được đặt bởi EnvironmentSceneBuilder qua hàm StandOn().
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] int playerIndex; // 1 = P1, 2 = P2
        [SerializeField] Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.8f);

        public int PlayerIndex => playerIndex;
        public Vector2 Position => transform.position;

        public void SetPlayerIndex(int index) => playerIndex = index;

#if UNITY_EDITOR
        // Hiển thị vị trí spawn trong Scene view khi chỉnh map
        void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.8f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"P{playerIndex}");
        }
#endif
    }
}
