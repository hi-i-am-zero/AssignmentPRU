using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment.Weather
{
    /// <summary>Vùng sét: gây damage định kỳ trong khu vực.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LightningArea : MonoBehaviour
    {
        // Thời gian giữa mỗi lần sét đánh trong vùng
        // strikeInterval: giây
        [SerializeField] float strikeInterval = 3f;

        // Máu mất mỗi lần sét trúng (không phải HP/s — damage một lần)
        // strikeDamage: HP
        [SerializeField] float strikeDamage = 30f;

        // Thời gian đèn vùng sáng mạnh khi sét đánh
        // flashDuration: giây
        [SerializeField] float flashDuration = 0.15f;

        Light2D zoneLight;
        Coroutine strikeRoutine;

        void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Ánh sáng đỏ cảnh báo vùng nguy hiểm
            zoneLight = GetComponentInChildren<Light2D>();
            if (zoneLight != null)
            {
                zoneLight.color = ZoneLightColors.Hazard;
                zoneLight.intensity = 0.4f; // Độ sáng nền (0–1+)
            }
        }

        void OnEnable() => strikeRoutine = StartCoroutine(StrikeLoop());
        void OnDisable()
        {
            if (strikeRoutine != null)
                StopCoroutine(strikeRoutine);
        }

        // Lặp vô hạn: mỗi strikeInterval giây → Strike()
        IEnumerator StrikeLoop()
        {
            var wait = new WaitForSeconds(strikeInterval);
            while (true)
            {
                yield return wait;
                yield return Strike();
            }
        }

        // Quét OverlapBox — damage mọi player còn sống trong vùng (không theo hướng)
        IEnumerator Strike()
        {
            if (zoneLight != null)
                zoneLight.intensity = 2.5f;

            var col = GetComponent<BoxCollider2D>();
            var hits = Physics2D.OverlapBoxAll(
                (Vector2)transform.position + col.offset,
                col.size,
                transform.eulerAngles.z);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag(GameLayers.TagPlayer)) continue;
                if (hit.TryGetComponent<IDamageable>(out var d) && d.IsAlive)
                    d.TakeDamage(strikeDamage);
            }

            yield return new WaitForSeconds(flashDuration);

            if (zoneLight != null)
                zoneLight.intensity = 0.4f;
        }
    }
}
