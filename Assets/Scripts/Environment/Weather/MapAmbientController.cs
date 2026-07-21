using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment.Weather
{
    /// <summary>Đổi màu ánh sáng môi trường theo thời tiết (mưa/bão tối hơn).</summary>
    public class MapAmbientController : MonoBehaviour
    {
        // Màu ánh sáng Global Light mục tiêu theo từng loại thời tiết (RGBA 0–1)
        [SerializeField] Color clearAmbient = Color.white;
        [SerializeField] Color rainAmbient = new Color(0.7f, 0.75f, 0.85f);
        [SerializeField] Color stormAmbient = new Color(0.55f, 0.58f, 0.68f);
        [SerializeField] Color thunderAmbient = new Color(0.65f, 0.6f, 0.62f);

        // Tốc độ chuyển màu/độ sáng mượt — hệ số × Time.deltaTime (càng lớn càng nhanh)
        [SerializeField] float transitionSpeed = 2f;

        Light2D globalLight;
        Color targetColor;

        // Độ sáng Global Light mục tiêu (0 = tối, 1 = sáng bình thường)
        float targetIntensity = 1f;

        void Awake()
        {
            globalLight = FindGlobalLight();
            targetColor = clearAmbient;
        }

        // Lerp màu và intensity mỗi frame — chuyển cảnh mượt, không giật
        void Update()
        {
            if (globalLight == null) return;
            globalLight.color = Color.Lerp(globalLight.color, targetColor, Time.deltaTime * transitionSpeed);
            globalLight.intensity = Mathf.Lerp(globalLight.intensity, targetIntensity, Time.deltaTime * transitionSpeed);
        }

        // WeatherManager gọi — Rain/Storm/Thunder làm tối scene hơn Clear
        public void ApplyWeather(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain:
                    targetColor = rainAmbient;
                    targetIntensity = 0.85f; // 85% độ sáng
                    break;
                case WeatherType.Storm:
                    targetColor = stormAmbient;
                    targetIntensity = 0.7f;  // 70% — tối nhất
                    break;
                case WeatherType.Thunder:
                    targetColor = thunderAmbient;
                    targetIntensity = 0.75f; // 75%
                    break;
                default:
                    targetColor = clearAmbient;
                    targetIntensity = 1f;    // 100%
                    break;
            }
        }

        // Tìm Global Light 2D trong scene (tạo bởi CreateBaseScene)
        static Light2D FindGlobalLight()
        {
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            foreach (var l in lights)
                if (l.lightType == Light2D.LightType.Global) return l;
            return null;
        }
    }
}
