using UnityEngine;

public class ComboController : MonoBehaviour
{
    [Header("Combo")]
    public int maxCombo = 3;              // Số đòn tối đa trong combo

    public float comboResetTime = 0.8f;  // Thời gian reset combo

    private int comboStep;               // Đòn hiện tại

    private float timer;                 // Đếm thời gian

    public int CurrentCombo => comboStep; // Lấy combo hiện tại

    void Update()
    {
        // Không có combo thì không cần đếm
        if (comboStep <= 0)
            return;

        timer += Time.deltaTime;

        // Hết thời gian thì reset combo
        if (timer >= comboResetTime)
        {
            ResetCombo();
        }
    }

    public int NextCombo()
    {
        // Reset thời gian đếm
        timer = 0;

        // Tăng combo
        comboStep++;

        // Quay về combo 1 nếu vượt quá giới hạn
        if (comboStep > maxCombo)
            comboStep = 1;

        return comboStep;
    }

    public void ResetCombo()
    {
        // Đưa combo về ban đầu
        comboStep = 0;
        timer = 0;
    }
}