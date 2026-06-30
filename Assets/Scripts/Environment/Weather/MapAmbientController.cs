using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment.Weather
{
    /// <summary>
    /// Tints global lighting when weather changes (darker during rain/storm).
    /// </summary>
    public class MapAmbientController : MonoBehaviour
    {
        [SerializeField] Color clearAmbient = Color.white;
        [SerializeField] Color rainAmbient = new Color(0.7f, 0.75f, 0.85f);
        [SerializeField] Color stormAmbient = new Color(0.55f, 0.58f, 0.68f);
        [SerializeField] Color thunderAmbient = new Color(0.65f, 0.6f, 0.62f);
        [SerializeField] float transitionSpeed = 2f;

        Light2D globalLight;
        Color targetColor;
        float targetIntensity = 1f;

        void Awake()
        {
            globalLight = FindGlobalLight();
            targetColor = clearAmbient;
        }

        void Update()
        {
            if (globalLight == null) return;
            globalLight.color = Color.Lerp(globalLight.color, targetColor, Time.deltaTime * transitionSpeed);
            globalLight.intensity = Mathf.Lerp(globalLight.intensity, targetIntensity, Time.deltaTime * transitionSpeed);
        }

        public void ApplyWeather(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain:
                    targetColor = rainAmbient;
                    targetIntensity = 0.85f;
                    break;
                case WeatherType.Storm:
                    targetColor = stormAmbient;
                    targetIntensity = 0.7f;
                    break;
                case WeatherType.Thunder:
                    targetColor = thunderAmbient;
                    targetIntensity = 0.75f;
                    break;
                default:
                    targetColor = clearAmbient;
                    targetIntensity = 1f;
                    break;
            }
        }

        static Light2D FindGlobalLight()
        {
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            foreach (var l in lights)
                if (l.lightType == Light2D.LightType.Global) return l;
            return null;
        }
    }
}
