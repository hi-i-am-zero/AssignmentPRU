using UnityEngine;

namespace Environment.Weather
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class WindZone : MonoBehaviour
    {
        [SerializeField] Vector2 windForce = new Vector2(8f, 0f);
        [SerializeField] bool showGizmo = true;

        BoxCollider2D zoneCollider;

        void Awake()
        {
            zoneCollider = GetComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag(GameLayers.TagPlayer)) return;
            if (!other.TryGetComponent<Rigidbody2D>(out var rb)) return;

            rb.AddForce(windForce, ForceMode2D.Force);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!showGizmo) return;
            Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.3f);
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, (Vector3)windForce * 0.3f);
            }
        }
#endif
    }
}
