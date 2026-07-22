using System.Collections;
using UnityEngine;

namespace Environment.Weather
{
    /// <summary>Đổi hướng gió định kỳ — đồng bộ WindZone (lực) và WindVisualEffect (particle).</summary>
    public class WindDirectionController : MonoBehaviour
    {
        [SerializeField] bool autoChangeDirection = true;

        // Thời gian chờ trước mỗi lần đổi hướng gió (luân phiên trái/phải hoặc random)
        // minInterval/maxInterval: giây (mặc định 6–12 giây/lần)
        [SerializeField] float minInterval = 6f;
        [SerializeField] float maxInterval = 12f;

        // false = luân phiên phải↔trái | true = ngẫu nhiên trái hoặc phải
        [SerializeField] bool useRandomDirection;
        [SerializeField] WindZone[] windZones;
        [SerializeField] WindVisualEffect windVisual;

        // Độ mạnh lực gió ngang — nhân với hướng (±1) khi ApplyDirection
        // horizontalStrength: Newton (Sky=7, WeatherTest=8)
        [SerializeField] float horizontalStrength = 7f;

        // Lực đẩy lên cố định — không đổi khi gió quay trái/phải
        // verticalStrength: Newton
        [SerializeField] float verticalStrength = 0.3f;

        Vector2 currentDirection = Vector2.right;
        Coroutine changeRoutine;
        bool active;

        public Vector2 CurrentDirection => currentDirection;

        // WeatherManager gọi — truyền reference wind zone và visual
        public void Configure(WindZone[] zones, WindVisualEffect visual)
        {
            if (zones != null && zones.Length > 0)
                windZones = zones;
            if (visual != null)
                windVisual = visual;

            SyncStrengthFromZones();
            ApplyDirection(currentDirection);
        }

        // Bật/tắt đổi hướng — map không có gió (Clear/Rain/Thunder) thì tắt
        public void SetActive(bool enabled)
        {
            active = enabled;
            if (!enabled)
            {
                StopChanging();
                return;
            }

            SyncStrengthFromZones();
            ApplyDirection(currentDirection);

            if (autoChangeDirection)
                StartChanging();
        }

        // Đọc cường độ gió ban đầu từ WindZone đầu tiên (do EnvironmentSceneBuilder gán)
        void SyncStrengthFromZones()
        {
            if (windZones == null || windZones.Length == 0) return;

            var force = windZones[0].WindForce;
            if (force.sqrMagnitude < 0.01f) return;

            horizontalStrength = Mathf.Abs(force.x);
            verticalStrength = force.y;
            currentDirection = force.x >= 0f ? Vector2.right : Vector2.left;
        }

        void StartChanging()
        {
            StopChanging();
            changeRoutine = StartCoroutine(ChangeLoop());
        }

        void StopChanging()
        {
            if (changeRoutine != null)
            {
                StopCoroutine(changeRoutine);
                changeRoutine = null;
            }
        }

        // Vòng lặp: chờ ngẫu nhiên → chọn hướng mới → cập nhật WindZone + visual
        IEnumerator ChangeLoop()
        {
            while (active)
            {
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                if (!active) yield break;

                Vector2 next = PickNextDirection();
                ApplyDirection(next);
            }
        }

        // Mặc định luân phiên trái/phải; bật useRandomDirection thì 50/50
        Vector2 PickNextDirection()
        {
            if (useRandomDirection)
                return Random.value > 0.5f ? Vector2.right : Vector2.left;

            return currentDirection.x >= 0f ? Vector2.left : Vector2.right;
        }

        // Cập nhật lực vào tất cả WindZone + hướng particle visual
        void ApplyDirection(Vector2 direction)
        {
            currentDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right;
            var force = new Vector2(currentDirection.x * horizontalStrength, verticalStrength);

            if (windZones != null)
            {
                foreach (var zone in windZones)
                {
                    if (zone != null)
                        zone.SetWindForce(force);
                }
            }

            windVisual?.ApplyDirection(currentDirection);
        }

        void OnDisable() => StopChanging();
    }
}
