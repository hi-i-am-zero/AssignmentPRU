using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Tường biên arena (trái, phải, trên) — collider cứng, player không đi ra ngoài.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ArenaBoundary : MonoBehaviour
    {
        // Collider không phải trigger → va chạm vật lý thật, layer Ground
        void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = false;
            gameObject.layer = LayerMask.NameToLayer(GameLayers.Ground);
        }
    }
}
