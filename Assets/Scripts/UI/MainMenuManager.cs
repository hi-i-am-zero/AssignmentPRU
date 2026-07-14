using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Giao Diện (Panels)")]
    public GameObject mainPanel;
    public GameObject charSelectPanel;
    public GameObject mapSelectPanel;

    [Header("Văn Bản Hiển Thị")]
    public TextMeshProUGUI p1ChoiceText;
    public TextMeshProUGUI p2ChoiceText;
    public TextMeshProUGUI mapChoiceText;

    private int p1CharacterID = 1;
    private int p2CharacterID = 2;
    private int mapID = 1;

    void Start()
    {
        OpenPanel(mainPanel);
    }

    public void OpenPanel(GameObject panelToOpen)
    {
        mainPanel.SetActive(false);
        charSelectPanel.SetActive(false);
        mapSelectPanel.SetActive(false);

        panelToOpen.SetActive(true);
    }

    public void Button_ChuyenSangChonNhanVat()
    {
        OpenPanel(charSelectPanel);
    }

    public void Button_ChuyenSangChonMap()
    {
        OpenPanel(mapSelectPanel);
    }

    public void SelectPlayer1(int charID)
    {
        p1CharacterID = charID;
        if (p1ChoiceText != null) p1ChoiceText.text = "P1: Nhân vật " + charID;
    }

    public void SelectPlayer2(int charID)
    {
        p2CharacterID = charID;
        if (p2ChoiceText != null) p2ChoiceText.text = "P2: Nhân vật " + charID;
    }

    public void SelectMap(int selectedMapID)
    {
        mapID = selectedMapID;
        if (mapChoiceText != null) mapChoiceText.text = "Map: " + mapID;
    }

    public void StartFight()
    {
        PlayerPrefs.SetInt("P1_Char", p1CharacterID);
        PlayerPrefs.SetInt("P2_Char", p2CharacterID);
        PlayerPrefs.Save();

        // Load Scene dựa trên ID của Map
        SceneManager.LoadScene(mapID);
    }
}