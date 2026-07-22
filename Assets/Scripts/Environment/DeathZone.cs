using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Vùng chết ở đáy map (y ≈ -8.2). Player chạm vào → loại khỏi trận ngay.
    /// FallDetection cũng kill player nếu y <= -12 (dự phòng).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone : MonoBehaviour
    {
        [SerializeField] bool instantKill = true;

        // Thiết lập trigger + tag/layer để FallDetection và script khác nhận diện
        void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            gameObject.tag = GameLayers.TagDeathZone;
            gameObject.layer = LayerMask.NameToLayer(GameLayers.DeathZone);
        }

        // Chỉ xử lý object tag Player — gọi Kill() hoặc damage lớn
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(GameLayers.TagPlayer)) return;

            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                if (instantKill)
                    damageable.Kill();
                else
                    damageable.TakeDamage(999f);
            }
        }

#if UNITY_EDITOR
        // Vẽ hộp đỏ mờ trong Scene view
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
                Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
        }
#endif
    }
}
