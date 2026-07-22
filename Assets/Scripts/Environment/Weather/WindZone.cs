using UnityEngine;

namespace Environment.Weather
{
    /// <summary>Vùng gió: đẩy Rigidbody ngang khi đứng trong.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class WindZone : MonoBehaviour
    {
        // Lực gió gameplay — cộng dồn với velocity player set mỗi frame
        // windForce.x: lực ngang (Newton, qua AddForce — KHÔNG phải m/s). Sky=7, WeatherTest=8
        // windForce.y: lực đẩy lên nhẹ (Newton). Sky=0.3, WeatherTest=0.35
        // WindDirectionController cập nhật khi gió đổi hướng (±horizontalStrength)
        [SerializeField] Vector2 windForce = new Vector2(8f, 0f);

        [SerializeField] bool showGizmo = true;

        public Vector2 WindForce => windForce;

        BoxCollider2D zoneCollider;

        // WindDirectionController gọi khi đổi hướng gió trái/phải
        public void SetWindForce(Vector2 force) => windForce = force;

        // Collider trigger — phát hiện player trong vùng, không va chạm cứng
        void Awake()
        {
            zoneCollider = GetComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
        }

        // Mỗi physics step: chỉ đẩy object tag Player có Rigidbody2D
        // Item/projectile KHÔNG bị ảnh hưởng
        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag(GameLayers.TagPlayer)) return;
            if (!other.TryGetComponent<Rigidbody2D>(out var rb)) return;

            // ForceMode2D.Force: lực tính theo khối lượng, áp dụng mỗi FixedUpdate
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
                // 0.3f: hệ số scale mũi tên gizmo (chỉ Editor, không gameplay)
                Gizmos.DrawRay(transform.position, (Vector3)windForce * 0.3f);
            }
        }
#endif
    }
}
