using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment.Weather
{
    /// <summary>Hiệu ứng sấm (flash + có thể kèm SFX).</summary>
    public class ThunderEffect : MonoBehaviour
    {
        // Khoảng thời gian giữa mỗi tiếng sấm (ngẫu nhiên trong khoảng này)
        // minInterval/maxInterval: giây
        [SerializeField] float minInterval = 4f;
        [SerializeField] float maxInterval = 10f;

        [SerializeField] AudioSource thunderAudio;
        [SerializeField] Light2D flashLight;

        // Độ sáng Light2D khi sấm — KHÔNG gây damage (damage do LightningArea)
        // flashIntensity: 0–2+ (2 = rất sáng)
        [SerializeField] float flashIntensity = 2f;

        // Thời gian giữ đèn sáng mỗi lần sấm
        // flashDuration: giây
        [SerializeField] float flashDuration = 0.12f;

        Coroutine thunderRoutine;

        void Awake()
        {
            if (flashLight == null)
                flashLight = GetComponent<Light2D>();
        }

        void OnEnable() => thunderRoutine = StartCoroutine(ThunderLoop());
        void OnDisable()
        {
            if (thunderRoutine != null)
                StopCoroutine(thunderRoutine);
        }

        // Chờ 1–3s ban đầu, sau đó lặp: chờ → flash toàn map
        IEnumerator ThunderLoop()
        {
            yield return new WaitForSeconds(Random.Range(1f, 3f));
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                yield return Flash();
            }
        }

        // Bật Point Light 2D sáng rồi tắt + phát SFX
        IEnumerator Flash()
        {
            if (flashLight != null)
            {
                flashLight.lightType = Light2D.LightType.Point;
                flashLight.pointLightOuterRadius = 18f; // Bán kính ánh sáng (world unit)
                flashLight.intensity = flashIntensity;
                flashLight.enabled = true;
            }

            if (thunderAudio != null && thunderAudio.clip != null)
                thunderAudio.PlayOneShot(thunderAudio.clip);

            yield return new WaitForSeconds(flashDuration);

            if (flashLight != null)
            {
                flashLight.intensity = 0f;
                flashLight.enabled = false;
            }
        }
    }
}
