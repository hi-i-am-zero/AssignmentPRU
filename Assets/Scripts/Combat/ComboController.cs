using UnityEngine;

public class ComboController : MonoBehaviour
{
    [Header("Combo Settings")]
    [Tooltip("Số đòn tối đa trong chuỗi combo")]
    public int maxCombo = 3;

    [Tooltip("Thời gian giữ trạng thái đòn vừa dùng trước khi reset")]
    public float comboResetTime = 0.5f;

    private int comboStep;
    private float timer;

    public int CurrentCombo => comboStep;

    private void Update()
    {
        if (comboStep <= 0)
            return;

        timer += Time.deltaTime;
        if (timer >= comboResetTime)
            ResetCombo();
    }

    /// <summary>
    /// Đặt đòn đánh theo phím riêng (Attack_1..Attack_4).
    /// </summary>
    public void SetComboStep(int step)
    {
        int safeMax = Mathf.Max(1, maxCombo);
        comboStep = Mathf.Clamp(step, 1, safeMax);
        timer = 0f;
    }

    public void ResetCombo()
    {
        comboStep = 0;
        timer = 0f;
    }
}
