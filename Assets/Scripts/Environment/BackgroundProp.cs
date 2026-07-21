using UnityEngine;

namespace Environment
{
    /// <summary>Decor nền: trôi chậm, không va chạm.</summary>
    public class BackgroundProp : MonoBehaviour
    {
        // Tốc độ trôi decor (cây, chim, mây nhỏ) — không có collider
        // driftX/driftY: hệ số nhân Time.time (không phải m/s)
        [SerializeField] float driftX = 0.08f;
        [SerializeField] float driftY = 0.05f;

        // Lệch pha — mỗi prop trôi khác nhau
        // phase: rad
        [SerializeField] float phase;

        Vector3 start;

        void Start() => start = transform.position;

        void Update()
        {
            float t = Time.time + phase;
            // Biên độ dao động: 0.15f ngang, 0.08f dọc (world unit ≈ mét)
            transform.position = start + new Vector3(
                Mathf.Sin(t * driftX) * 0.15f,
                Mathf.Cos(t * driftY) * 0.08f,
                0f);
        }
    }
}
