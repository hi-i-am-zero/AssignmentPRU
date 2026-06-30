using UnityEngine;

namespace Environment.Weather
{
    /// <summary>
    /// Visual wind streaks – horizontal particles showing wind direction.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class WindVisualEffect : MonoBehaviour
    {
        [SerializeField] Vector2 windDirection = Vector2.right;
        [SerializeField] float emissionRate = 30f;

        ParticleSystem ps;
        ParticleSystem.EmissionModule emission;
        ParticleSystem.VelocityOverLifetimeModule velocity;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            emission = ps.emission;
            velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            ApplyDirection(windDirection);
        }

        public void SetActive(bool active)
        {
            if (active && !ps.isPlaying) ps.Play();
            else if (!active && ps.isPlaying) ps.Stop();
        }

        public void ApplyDirection(Vector2 dir)
        {
            windDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
            if (ps == null) ps = GetComponent<ParticleSystem>();
            if (ps == null) return;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(windDirection.x * 6f);
            vel.y = new ParticleSystem.MinMaxCurve(windDirection.y * 2f);
            emission = ps.emission;
            emission.rateOverTime = emissionRate;
        }
    }
}
