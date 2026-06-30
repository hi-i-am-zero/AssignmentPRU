using UnityEngine;

namespace Environment
{
    [RequireComponent(typeof(PlatformEffector2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class SlipperyPlatform : MonoBehaviour
    {
        [SerializeField] float friction = 0.05f;

        void Awake()
        {
            var effector = GetComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 180f;

            var col = GetComponent<BoxCollider2D>();
            col.usedByEffector = true;

            var material = new PhysicsMaterial2D("Slippery") { friction = friction, bounciness = 0f };
            col.sharedMaterial = material;
        }
    }
}
