using UnityEngine;

namespace Environment
{
    /// <summary>Platform trơn: giảm ma sát / trượt khi đứng.</summary>
    [RequireComponent(typeof(PlatformEffector2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class SlipperyPlatform : MonoBehaviour
    {
        // Ma sát vật lý — càng thấp càng trơn (WeatherTest: băng + gió = trượt mạnh)
        // friction: hệ số PhysicsMaterial2D (0 = trơn tuyệt đối, 1 = bám chặt)
        [SerializeField] float friction = 0.05f;

        // One-way platform + vật liệu trơn (không nảy)
        void Awake()
        {
            var effector = GetComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 180f; // Góc mặt platform hướng lên (độ)

            var col = GetComponent<BoxCollider2D>();
            col.usedByEffector = true;

            var material = new PhysicsMaterial2D("Slippery") { friction = friction, bounciness = 0f };
            col.sharedMaterial = material;
        }
    }
}
