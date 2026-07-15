using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment
{
    /// <summary>Platform đứng được (ground / safe zone).</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Platform : MonoBehaviour
    {
        [SerializeField] bool isSafeZone = true;
        [SerializeField] bool showZoneLight;

        void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer(GameLayers.Ground);

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
