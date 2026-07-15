using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Bắt buộc phải có để load Scene

/// <summary>
/// HUD trận cũ (HP fill + timer). Flow hiện tại dùng PlayerHealthBarsHud + ResultUI.
/// </summary>
public class FightUIManager : MonoBehaviour
{
    [Header("Player 1")]
    public Image p1HealthFill;
    public float p1MaxHealth = 100f;
    private float p1CurrentHealth;

    [Header("Player 2")]
    public Image p2HealthFill;
    public float p2MaxHealth = 100f;
    private float p2CurrentHealth;

    [Header("Timer & UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI announcerText;
    public GameObject endGamePanel;

    private float currentTime = 99f;
    private bool isMatchActive = true;

    void Start()
    {
        // Khởi tạo máu đầy khi mới vào game
        p1CurrentHealth = p1MaxHealth;
        p2CurrentHealth = p2MaxHealth;

        // Ẩn bảng kết quả và chữ K.O đi
        endGamePanel.SetActive(false);
        announcerText.text = "";
    }

    void Update()
    {
        // Đếm ngược thời gian nếu trận đấu đang diễn ra
        if (isMatchActive)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                TimeOut();
            }
            timerText.text = Mathf.Ceil(currentTime).ToString(); // Làm tròn số giây
        }
    }

    // --- HÀM XỬ LÝ NHẬN SÁT THƯƠNG ---
    public void TakeDamage(int playerID, float damage)
    {
        if (!isMatchActive) return; // Nếu game đã kết thúc thì không tính sát thương nữa

        if (playerID == 1)
        {
            p1CurrentHealth -= damage;
            p1HealthFill.fillAmount = p1CurrentHealth / p1MaxHealth;

            if (p1CurrentHealth <= 0) EndMatch("PLAYER 2 THẮNG!");
        }
        else if (playerID == 2)
        {
            p2CurrentHealth -= damage;
            p2HealthFill.fillAmount = p2CurrentHealth / p2MaxHealth;

            if (p2CurrentHealth <= 0) EndMatch("PLAYER 1 THẮNG!");
        }
    }

    // --- CÁC HÀM XỬ LÝ KẾT THÚC TRẬN ĐẤU ---
    private void EndMatch(string resultText)
    {
        isMatchActive = false;
        announcerText.text = "K.O!\n" + resultText;

        // Đợi 2 giây sau đó mới gọi hàm hiện bảng Menu để người chơi kịp đọc chữ K.O
        Invoke("ShowEndPanel", 2f);
    }

    private void TimeOut()
    {
        isMatchActive = false;
        if (p1CurrentHealth > p2CurrentHealth) EndMatch("HẾT GIỜ\nPLAYER 1 THẮNG!");
        else if (p2CurrentHealth > p1CurrentHealth) EndMatch("HẾT GIỜ\nPLAYER 2 THẮNG!");
        else EndMatch("HÒA!");
    }

    private void ShowEndPanel()
    {
        endGamePanel.SetActive(true);
    }

    // --- 2 HÀM DÙNG CHO NÚT BẤM (GIẢ LẬP ĐỂ TEST) ---
    public void Test_HitPlayer1()
    {
        TakeDamage(1, 15f);
    }

    public void Test_HitPlayer2()
    {
        TakeDamage(2, 15f);
    }

    // --- 2 HÀM CHO NÚT BẤM KHI KẾT THÚC GAME ---
    public void Button_VeMenu()
    {
        // Load về MainMenu (nhớ thiết lập MainMenu là số 0 trong Build Profiles)
        SceneManager.LoadScene(0);
    }

    public void Button_ThoatGame()
    {
        Debug.Log("Đang thoát game...");

        // Nếu đang mở phần mềm Unity -> Tắt nút Play
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Nếu đã xuất ra game thật -> Tắt hoàn toàn game
        Application.Quit(); 
#endif
    }
}