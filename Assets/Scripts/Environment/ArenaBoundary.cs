using UnityEngine;

namespace Environment
{
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
