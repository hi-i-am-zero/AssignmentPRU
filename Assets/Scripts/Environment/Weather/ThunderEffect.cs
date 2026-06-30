using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment.Weather
{
    public class ThunderEffect : MonoBehaviour
    {
        [SerializeField] float minInterval = 4f;
        [SerializeField] float maxInterval = 10f;
        [SerializeField] AudioSource thunderAudio;
        [SerializeField] Light2D flashLight;
        [SerializeField] float flashIntensity = 2f;
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

        IEnumerator ThunderLoop()
        {
            yield return new WaitForSeconds(Random.Range(1f, 3f));
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                yield return Flash();
            }
        }

        IEnumerator Flash()
        {
            if (flashLight != null)
            {
                flashLight.lightType = Light2D.LightType.Point;
                flashLight.pointLightOuterRadius = 18f;
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
