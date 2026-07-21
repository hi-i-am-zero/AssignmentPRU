using UnityEngine;

namespace Environment
{
    /// <summary>Dao động nhẹ theo Y (mây / platform nổi).</summary>
    public class FloatingMotion : MonoBehaviour
    {
        // Biên độ dao động lên/xuống — map Sky, gắn trên platform mây
        // amplitudeY: world unit ≈ mét (0.07f = ±7cm)
        [SerializeField] float amplitudeY = 0.07f;

        // Tốc độ dao động trong hàm Sin — càng lớn mây rung càng nhanh
        // speed: rad/s (hệ số nhân Time.time)
        [SerializeField] float speed = 0.7f;

        // Lệch pha — mỗi mây dao động lệch nhau, không đồng bộ
        // phase: rad
        [SerializeField] float phase;

        Vector3 startWorld;

        void Start() => startWorld = transform.position;

        // Di chuyển transform — chỉ visual/position, collider di chuyển theo
        void Update()
        {
            float y = Mathf.Sin(Time.time * speed + phase) * amplitudeY;
            transform.position = startWorld + new Vector3(0f, y, 0f);
        }
    }
}
