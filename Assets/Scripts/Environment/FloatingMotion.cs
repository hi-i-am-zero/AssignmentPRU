using UnityEngine;

namespace Environment
{
    /// <summary>Gentle vertical bob for cloud / floating platforms.</summary>
    public class FloatingMotion : MonoBehaviour
    {
        [SerializeField] float amplitudeY = 0.07f;
        [SerializeField] float speed = 0.7f;
        [SerializeField] float phase;

        Vector3 startWorld;

        void Start() => startWorld = transform.position;

        void Update()
        {
            float y = Mathf.Sin(Time.time * speed + phase) * amplitudeY;
            transform.position = startWorld + new Vector3(0f, y, 0f);
        }
    }
}
