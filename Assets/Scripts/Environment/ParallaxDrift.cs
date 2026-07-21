using UnityEngine;

namespace Environment
{
    /// <summary>Trôi chậm lớp nền (parallax decor).</summary>
    public class ParallaxDrift : MonoBehaviour
    {
        // Biên độ dao động nền — tạo cảm giác chiều sâu, không ảnh hưởng gameplay
        // amplitude: world unit (0.2f ngang = lắc tối đa ±20cm)
        [SerializeField] Vector2 amplitude = new Vector2(0.2f, 0.1f);

        // Tốc độ dao động trục X/Y — hệ số nhân Time.time (không phải m/s)
        [SerializeField] Vector2 speed = new Vector2(0.18f, 0.12f);

        // Lệch pha ban đầu — tránh mọi layer nền dao động cùng nhịp
        // phase: rad
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
