using UnityEngine;

namespace Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone : MonoBehaviour
    {
        [SerializeField] bool instantKill = true;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            gameObject.tag = GameLayers.TagDeathZone;
            gameObject.layer = LayerMask.NameToLayer(GameLayers.DeathZone);
        }

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
