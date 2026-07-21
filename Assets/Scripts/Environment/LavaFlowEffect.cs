using UnityEngine;

namespace Environment
{
    /// <summary>Cuộn sprite lava tạo hiệu ứng chảy.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class LavaFlowEffect : MonoBehaviour
    {
        // Tốc độ cuộn texture — CHỈ visual; damage thật do HazardZone xử lý
        // scrollSpeed: UV offset/giây (không phải world unit/s)
        [SerializeField] float scrollSpeed = 1.2f;

        // Hướng cuộn texture lava
        [SerializeField] Vector2 scrollDirection = Vector2.right;

        Material runtimeMat;
        Vector2 offset;

        void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            runtimeMat = sr.material;
        }

        // Dịch mainTextureOffset mỗi frame → lava trông như đang chảy
        void Update()
        {
            if (runtimeMat == null) return;
            offset += scrollDirection.normalized * (scrollSpeed * Time.deltaTime);
            runtimeMat.mainTextureOffset = offset;
        }
    }
}
