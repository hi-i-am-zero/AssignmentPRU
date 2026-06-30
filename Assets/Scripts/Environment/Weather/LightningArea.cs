using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment.Weather
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class LightningArea : MonoBehaviour
    {
        [SerializeField] float strikeInterval = 3f;
        [SerializeField] float strikeDamage = 30f;
        [SerializeField] float flashDuration = 0.15f;

        Light2D zoneLight;
        Coroutine strikeRoutine;

        void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;

            zoneLight = GetComponentInChildren<Light2D>();
            if (zoneLight != null)
            {
                zoneLight.color = ZoneLightColors.Hazard;
                zoneLight.intensity = 0.4f;
            }
        }

        void OnEnable() => strikeRoutine = StartCoroutine(StrikeLoop());
        void OnDisable()
        {
            if (strikeRoutine != null)
                StopCoroutine(strikeRoutine);
        }

        IEnumerator StrikeLoop()
        {
            var wait = new WaitForSeconds(strikeInterval);
            while (true)
            {
                yield return wait;
                yield return Strike();
            }
        }

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
