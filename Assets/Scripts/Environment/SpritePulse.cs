using UnityEngine;

namespace Environment
{
    /// <summary>Pulse sprite tint/alpha – lava glow, item markers, etc.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpritePulse : MonoBehaviour
    {
        [SerializeField] Color tintA = Color.white;
        [SerializeField] Color tintB = new Color(1f, 0.7f, 0.4f, 1f);
        [SerializeField] float speed = 1.8f;
        [SerializeField] float phase;

        SpriteRenderer sr;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            float t = (Mathf.Sin(Time.time * speed + phase) + 1f) * 0.5f;
            sr.color = Color.Lerp(tintA, tintB, t);
        }
    }
}
