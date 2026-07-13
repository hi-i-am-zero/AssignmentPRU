using UnityEngine;

public class ComboController : MonoBehaviour
{
    [Header("Combo Settings")]
    [Tooltip("Số đòn tối đa trong chuỗi combo")]
    public int maxCombo = 3;

    [Tooltip("Thời gian cho phép tiếp tục combo")]
    public float comboResetTime = 0.5f;

    private int comboStep;
    private float timer;

    // Combo hiện tại
    public int CurrentCombo => comboStep;

    private void Update()
    {
        // Không có combo nào đang hoạt động
        if (comboStep <= 0)
            return;

        timer += Time.deltaTime;

        // Hết thời gian nối combo
        if (timer >= comboResetTime)
        {
            ResetCombo();
        }
    }

    /// <summary>
    /// Chuyển sang đòn đánh tiếp theo trong chuỗi combo.
    /// Attack1 -> Attack2 -> Attack3
    /// </summary>
    public int NextCombo()
    {
        timer = 0f;

        // Nếu chưa đạt combo tối đa
        if (comboStep < maxCombo)
        {
            comboStep++;
        }

        return comboStep;
    }

    /// <summary>
    /// Kiểm tra combo đã đạt đòn cuối chưa.
    /// </summary>
    public bool IsComboFinished()
    {
        return comboStep >= maxCombo;
    }

    /// <summary>
    /// Đặt lại combo về trạng thái ban đầu.
    /// </summary>
    public void ResetCombo()
    {
        comboStep = 0;
        timer = 0f;
    }
}