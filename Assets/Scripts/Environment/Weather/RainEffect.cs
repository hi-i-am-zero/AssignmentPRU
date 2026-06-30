using UnityEngine;

namespace Environment.Weather
{
    public class RainEffect : MonoBehaviour
    {
        [SerializeField] ParticleSystem rainParticles;
        [SerializeField] AudioSource rainAudio;
        [SerializeField] float lightRainRate = 40f;
        [SerializeField] float heavyRainRate = 120f;

        bool isHeavy;

        void Awake()
        {
            if (rainParticles == null)
                rainParticles = GetComponent<ParticleSystem>();
        }

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
