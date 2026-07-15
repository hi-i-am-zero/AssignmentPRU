using UnityEngine;

namespace Environment
{
    /// <summary>Biên arena (collider) giới hạn khu vực chơi.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ArenaBoundary : MonoBehaviour
    {
        void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = false;
            gameObject.layer = LayerMask.NameToLayer(GameLayers.Ground);
        }
    }
}
