using UnityEngine;

namespace Environment
{
    public enum AmbientParticleStyle { None, Stars, Snow, Embers }

    /// <summary>Lightweight ambient particles (stars, snow, embers).</summary>
    public class AmbientParticles : MonoBehaviour
    {
        [SerializeField] AmbientParticleStyle style = AmbientParticleStyle.None;
        [SerializeField] float areaWidth = 22f;
        [SerializeField] float areaHeight = 12f;

        ParticleSystem ps;

        void Start()
        {
            if (style == AmbientParticleStyle.None) return;
            ps = GetComponent<ParticleSystem>();
            if (ps == null) ps = gameObject.AddComponent<ParticleSystem>();
            Configure(style);
            ps.Play();
        }

        void Configure(AmbientParticleStyle s)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = s == AmbientParticleStyle.Stars ? 120 : 200;
            main.startLifetime = s == AmbientParticleStyle.Embers ? 2.5f : 4f;
            main.startSpeed = s == AmbientParticleStyle.Snow ? 1.2f : 0.15f;
            main.gravityModifier = s == AmbientParticleStyle.Snow ? 0.08f : 0f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = s switch
            {
                AmbientParticleStyle.Stars => 18f,
                AmbientParticleStyle.Snow => 45f,
                AmbientParticleStyle.Embers => 25f,
                _ => 0f
            };

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(areaWidth, areaHeight, 1f);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            switch (s)
            {
                case AmbientParticleStyle.Stars:
                    main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
                    main.startColor = new Color(1f, 1f, 0.9f, 0.85f);
                    grad.SetKeys(
                        new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
                    break;
                case AmbientParticleStyle.Snow:
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
                    main.startColor = new Color(0.95f, 0.97f, 1f, 0.75f);
                    grad.SetKeys(
                        new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
                    break;
                case AmbientParticleStyle.Embers:
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
                    main.startColor = new Color(1f, 0.55f, 0.15f, 0.9f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
                    main.gravityModifier = -0.05f;
                    grad.SetKeys(
                        new[] { new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.2f, 0.05f), 1f) },
                        new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                    break;
            }
            colorOverLife.color = grad;

            var renderer = GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.material = new Material(Shader.Find("Sprites/Default"));
        }
    }
}
