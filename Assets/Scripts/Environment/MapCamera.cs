using UnityEngine;

namespace Environment
{
    /// <summary>Camera orthographic cố định, khung toàn arena.</summary>
    [RequireComponent(typeof(Camera))]
    public class MapCamera : MonoBehaviour
    {
        // Kích thước orthographic — camera KHÔNG follow player, nhìn cả sân đấu
        // orthographicSize = nửa chiều CAO vùng nhìn thấy (world unit ≈ mét)
        // 7.5f → màn 16:9 nhìn thấy khoảng 15 unit cao × 26.7 unit rộng
        public const float CamSize = 7.5f;

        // Màu nền khi chưa load sprite background (RGBA, mỗi thành phần 0–1)
        [SerializeField] Color backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);

        // Gọi từ EnvironmentSceneBuilder — đặt màu nền theo từng map
        public void SetBackground(Color bg)
        {
            backgroundColor = bg;
            var cam = GetComponent<Camera>();
            if (cam != null) cam.backgroundColor = bg;
        }

        // Thiết lập camera 2D cố định khi scene load
        void Awake()
        {
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.backgroundColor = backgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
