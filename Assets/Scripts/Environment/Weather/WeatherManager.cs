using UnityEngine;

namespace Environment.Weather
{
    /// <summary>Loại thời tiết trên map.</summary>
    public enum WeatherType { Clear, Rain, Wind, Thunder, Storm }

    /// <summary>Bật/tắt hiệu ứng thời tiết (mưa, gió, sấm...).</summary>
    public class WeatherManager : MonoBehaviour
    {
        [SerializeField] WeatherType currentWeather = WeatherType.Clear;
        [SerializeField] RainEffect rainEffect;
        [SerializeField] ThunderEffect thunderEffect;
        [SerializeField] WindVisualEffect windVisual;
        [SerializeField] Environment.Weather.WindZone[] windZones;
        [SerializeField] WindDirectionController windDirectionController;
        [SerializeField] MapAmbientController ambientController;

        public WeatherType CurrentWeather => currentWeather;

        // Tự tìm hoặc tạo WindDirectionController nếu scene cũ chưa có
        void Awake()
        {
            if (ambientController == null)
                ambientController = GetComponent<MapAmbientController>();

            if (windDirectionController == null)
                windDirectionController = GetComponent<WindDirectionController>();
            if (windDirectionController == null)
                windDirectionController = gameObject.AddComponent<WindDirectionController>();
        }

        // Scene load xong → áp dụng thời tiết đã gán sẵn trong Inspector
        void Start() => ApplyWeather(currentWeather);

        // API đổi thời tiết runtime (hiện chưa có script nào gọi)
        public void SetWeather(WeatherType weather)
        {
            currentWeather = weather;
            ApplyWeather(weather);
        }

        // Bật/tắt từng hiệu ứng theo loại thời tiết:
        // Rain/Storm → mưa | Thunder/Storm → sấm | Wind/Storm → gió
        void ApplyWeather(WeatherType weather)
        {
            bool rain = weather == WeatherType.Rain || weather == WeatherType.Storm;
            bool thunder = weather == WeatherType.Thunder || weather == WeatherType.Storm;
            bool wind = weather == WeatherType.Wind || weather == WeatherType.Storm;

            if (rainEffect != null)
                rainEffect.SetActive(rain, weather == WeatherType.Storm);

            if (thunderEffect != null)
                thunderEffect.enabled = thunder;

            if (windVisual != null)
                windVisual.SetActive(wind);

            // Bật/tắt vùng gió gameplay (WindZone)
            if (windZones != null)
            {
                foreach (var zone in windZones)
                {
                    if (zone != null)
                        zone.gameObject.SetActive(wind);
                }
            }

            // Cấu hình và bật/tắt đổi hướng gió tự động
            if (windDirectionController != null)
            {
                windDirectionController.Configure(windZones, windVisual);
                windDirectionController.SetActive(wind);
            }

            // Làm tối/sáng ánh sáng global theo thời tiết
            ambientController?.ApplyWeather(weather);
        }
    }
}
