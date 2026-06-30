using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Visual Communication System – color-coded zone lighting.
    /// Safe = Green, Hazard = Red, Item Spawn = Yellow.
    /// </summary>
    public static class ZoneLightColors
    {
        public static readonly Color Safe = new Color(0.2f, 1f, 0.3f, 1f);
        public static readonly Color Hazard = new Color(1f, 0.2f, 0.2f, 1f);
        public static readonly Color ItemSpawn = new Color(1f, 0.9f, 0.2f, 1f);

        public const float SafeIntensity = 0.8f;
        public const float HazardIntensity = 1.0f;
        public const float ItemSpawnIntensity = 0.9f;
        public const float LightRadius = 2.5f;
    }
}
