using UnityEngine;

namespace Environment
{
    /// <summary>Cuộn sprite lava tạo hiệu ứng chảy.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class LavaFlowEffect : MonoBehaviour
    {
        [SerializeField] float scrollSpeed = 1.2f;
        [SerializeField] Vector2 scrollDirection = Vector2.right;

        Material runtimeMat;
        Vector2 offset;

        void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            runtimeMat = sr.material;
        }

        void Update()
        {
            if (runtimeMat == null) return;
            offset += scrollDirection.normalized * (scrollSpeed * Time.deltaTime);
            runtimeMat.mainTextureOffset = offset;
        }
    }
}
