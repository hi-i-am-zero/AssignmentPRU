using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment
{
    /// <summary>Vùng nguy hiểm: gây damage theo thời gian khi đứng trong.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HazardZone : MonoBehaviour
    {
        // Máu mất mỗi giây khi player đứng trong vùng (map Volcano — dung nham)
        // damagePerSecond: HP/s
        [SerializeField] float damagePerSecond = 25f;
        [SerializeField] bool instantKill;

        Light2D zoneLight;

        // Trigger zone + tag Hazard + ánh sáng đỏ cảnh báo
        void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            gameObject.tag = GameLayers.TagHazard;
            gameObject.layer = LayerMask.NameToLayer(GameLayers.Hazard);

            zoneLight = GetComponentInChildren<Light2D>();
            if (zoneLight != null)
            {
                zoneLight.color = ZoneLightColors.Hazard;
                zoneLight.intensity = ZoneLightColors.HazardIntensity;
            }
        }

        // Khác DeathZone: không chết ngay — mất máu dần mỗi frame player còn trong vùng
        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag(GameLayers.TagPlayer)) return;
            if (!other.TryGetComponent<IDamageable>(out var damageable) || !damageable.IsAlive) return;

            if (instantKill)
                damageable.Kill();
            else
                // Time.deltaTime (giây) × damagePerSecond (HP/s) = HP mất mỗi frame
                damageable.TakeDamage(damagePerSecond * Time.deltaTime);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.35f);
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
                Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
        }
#endif
    }
}
