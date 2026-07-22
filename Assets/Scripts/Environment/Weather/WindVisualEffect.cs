using UnityEngine;

namespace Environment.Weather
{
    /// <summary>Particle gió ngang thể hiện hướng gió.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class WindVisualEffect : MonoBehaviour
    {
        [SerializeField] Vector2 windDirection = Vector2.right;

        // Số hạt gió sinh ra mỗi giây — CHỈ visual, không ảnh hưởng gameplay
        // emissionRate: hạt/s
        [SerializeField] float emissionRate = 30f;

        ParticleSystem ps;
        ParticleSystem.EmissionModule emission;
        ParticleSystem.VelocityOverLifetimeModule velocity;

        // Khởi tạo particle và áp dụng hướng mặc định (phải)
        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            emission = ps.emission;
            velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            ApplyDirection(windDirection);
        }

        // WeatherManager bật/tắt khi map có/không có gió
        public void SetActive(bool active)
        {
            if (ps == null)
                ps = GetComponent<ParticleSystem>();

            if (ps == null)
                return;

            if (active && !ps.isPlaying) ps.Play();
            else if (!active && ps.isPlaying) ps.Stop();
        }

        // Đặt hướng particle — WindDirectionController gọi khi gió đổi
        // Lực thật nằm ở WindZone.windForce, đây chỉ để player nhìn thấy hướng gió
        public void ApplyDirection(Vector2 dir)
        {
            windDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
            if (ps == null) ps = GetComponent<ParticleSystem>();
            if (ps == null) return;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;

            // Hệ số nhân vận tốc hạt (world unit/s) — không liên quan windForce gameplay
            vel.x = new ParticleSystem.MinMaxCurve(windDirection.x * 6f);
            vel.y = new ParticleSystem.MinMaxCurve(windDirection.y * 2f);
            emission = ps.emission;
            emission.rateOverTime = emissionRate;
        }
    }
}
