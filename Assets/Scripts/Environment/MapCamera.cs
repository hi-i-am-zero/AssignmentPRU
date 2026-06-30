using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Fixed orthographic camera sized to show the full arena for 2-4 player combat.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MapCamera : MonoBehaviour
    {
        public const float CamSize = 7.5f;
        [SerializeField] Color backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);

        public void SetBackground(Color bg)
        {
            backgroundColor = bg;
            var cam = GetComponent<Camera>();
            if (cam != null) cam.backgroundColor = bg;
        }

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
