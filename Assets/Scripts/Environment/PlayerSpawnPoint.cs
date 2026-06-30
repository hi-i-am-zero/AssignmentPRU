using UnityEngine;

namespace Environment
{
    [DisallowMultipleComponent]
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] int playerIndex;
        [SerializeField] Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.8f);

        public int PlayerIndex => playerIndex;
        public Vector2 Position => transform.position;

        public void SetPlayerIndex(int index) => playerIndex = index;

#if UNITY_EDITOR
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
