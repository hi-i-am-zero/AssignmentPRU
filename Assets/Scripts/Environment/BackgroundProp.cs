using UnityEngine;

namespace Environment
{
    /// <summary>Decorative background element – slow drift, no collision.</summary>
    public class BackgroundProp : MonoBehaviour
    {
        [SerializeField] float driftX = 0.08f;
        [SerializeField] float driftY = 0.05f;
        [SerializeField] float phase;

        Vector3 start;

        void Start() => start = transform.position;

        void Update()
        {
            float t = Time.time + phase;
            transform.position = start + new Vector3(
                Mathf.Sin(t * driftX) * 0.15f,
                Mathf.Cos(t * driftY) * 0.08f,
                0f);
        }
    }
}
