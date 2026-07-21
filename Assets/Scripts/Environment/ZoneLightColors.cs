using UnityEngine;

namespace Environment
{
    /// <summary>Màu đèn vùng: Safe xanh / Hazard đỏ / Item vàng.</summary>
    public static class ZoneLightColors
    {
        // Màu RGBA (mỗi thành phần 0–1)
        public static readonly Color Safe = new Color(0.2f, 1f, 0.3f, 1f);
        public static readonly Color Hazard = new Color(1f, 0.2f, 0.2f, 1f);
        public static readonly Color ItemSpawn = new Color(1f, 0.9f, 0.2f, 1f);

        // intensity: độ sáng Light2D (0 = tắt, 1 = bình thường, >1 = rất sáng)
        public const float SafeIntensity = 0.8f;
        public const float HazardIntensity = 1.0f;
        public const float ItemSpawnIntensity = 0.9f;

        // LightRadius: bán kính ánh sáng point light (world unit ≈ mét)
        public const float LightRadius = 2.5f;
    }
}
