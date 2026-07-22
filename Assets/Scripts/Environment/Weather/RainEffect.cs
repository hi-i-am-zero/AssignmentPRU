using UnityEngine;

namespace Environment.Weather
{
    /// <summary>Hiệu ứng mưa (particle / visual).</summary>
    public class RainEffect : MonoBehaviour
    {
        [SerializeField] ParticleSystem rainParticles;
        [SerializeField] AudioSource rainAudio;

        // Mưa nhẹ — map Mountain (Rain)
        // lightRainRate: hạt mưa/giây (hạt/s)
        [SerializeField] float lightRainRate = 40f;

        // Mưa nặng — map WeatherTest (Storm)
        // heavyRainRate: hạt mưa/giây (hạt/s)
        [SerializeField] float heavyRainRate = 120f;

        bool isHeavy;

        void Awake()
        {
            if (rainParticles == null)
                rainParticles = GetComponent<ParticleSystem>();
        }

        // WeatherManager gọi: active=true bật mưa, heavy=true = Storm
        // Mưa KHÔNG đẩy player — chỉ particle + audio
        public void SetActive(bool active, bool heavy = false)
        {
            isHeavy = heavy;
            if (!active)
            {
                if (rainParticles != null && rainParticles.isPlaying)
                    rainParticles.Stop();
                if (rainAudio != null && rainAudio.isPlaying)
                    rainAudio.Stop();
                return;
            }

            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                emission.rateOverTime = heavy ? heavyRainRate : lightRainRate;
                if (!rainParticles.isPlaying) rainParticles.Play();
            }

            if (rainAudio != null && !rainAudio.isPlaying)
                rainAudio.Play();
        }

        public void SetActive(bool active) => SetActive(active, isHeavy);
    }
}
