using UnityEngine;

namespace Environment
{
    /// <summary>Slow drift for background decor layers.</summary>
    public class ParallaxDrift : MonoBehaviour
    {
        [SerializeField] Vector2 amplitude = new Vector2(0.2f, 0.1f);
        [SerializeField] Vector2 speed = new Vector2(0.18f, 0.12f);
        [SerializeField] float phase;

        Vector3 startLocal;

        void Start() => startLocal = transform.localPosition;

        void Update()
        {
            float t = Time.time + phase;
            transform.localPosition = startLocal + new Vector3(
                Mathf.Sin(t * speed.x) * amplitude.x,
                Mathf.Cos(t * speed.y) * amplitude.y,
                0f);
        }
    }
}
