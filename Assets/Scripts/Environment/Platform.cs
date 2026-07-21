using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment
{
    /// <summary>
    /// Platform đứng được — nền tảng chính trên mọi map.
    /// PlayerController dùng layer Ground để kiểm tra isGrounded.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Platform : MonoBehaviour
    {
        [SerializeField] bool isSafeZone = true;
        [SerializeField] bool showZoneLight;

        // Gán layer Ground để Physics2D.OverlapCircle nhận diện mặt đất
        void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer(GameLayers.Ground);

            // Tuỳ chọn: bật ánh sáng xanh an toàn trên platform
            if (!isSafeZone || !showZoneLight) return;

            var light = GetComponentInChildren<Light2D>();
            if (light != null)
            {
                light.color = ZoneLightColors.Safe;
                light.intensity = ZoneLightColors.SafeIntensity;
            }
        }
    }
}
