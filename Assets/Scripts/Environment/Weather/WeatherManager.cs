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
        [SerializeField] MapAmbientController ambientController;

        public WeatherType CurrentWeather => currentWeather;

        void Awake()
        {
            if (ambientController == null)
                ambientController = GetComponent<MapAmbientController>();
        }

        void Start() => ApplyWeather(currentWeather);

        public void SetWeather(WeatherType weather)
        {
            currentWeather = weather;
            ApplyWeather(weather);
        }

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

            if (windZones != null)
            {
                foreach (var zone in windZones)
                {
                    if (zone != null)
                        zone.gameObject.SetActive(wind);
                }
            }

            ambientController?.ApplyWeather(weather);
        }
    }
}
