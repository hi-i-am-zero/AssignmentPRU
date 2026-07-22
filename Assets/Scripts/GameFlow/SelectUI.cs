using UnityEngine;
using UnityEngine.UI;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Màn Select: P1 / Map / P2. UI gắn sẵn trong scene (Inspector).
    /// Thứ tự nút: Knight, Ninja, Sorcerer — Map: Mountain, Volcano, Sky, Weather, TestArena.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SelectUI : MonoBehaviour
    {
        static readonly CharacterType.Character[] Characters =
        {
            CharacterType.Character.Knight,
            CharacterType.Character.Ninja,
            CharacterType.Character.Sorcerer
        };

        static readonly string[] MapSceneNames =
        {
            "Mountain",
            "Volcano",
            "Sky",
            "Weather",
            "TestArena"
        };

        static readonly Color NormalColor = new Color(0.2f, 0.2f, 0.25f, 0.95f);
        static readonly Color SelectedColor = new Color(0.85f, 0.55f, 0.15f, 1f);
        static readonly Color FightReadyColor = new Color(0.75f, 0.2f, 0.2f, 1f);
        static readonly Color FightDisabledColor = new Color(0.35f, 0.35f, 0.38f, 1f);

        [Header("Actions")]
        [SerializeField] Button backButton;
        [SerializeField] Button fightButton;

        [Header("Player 1 (Knight / Ninja / Sorcerer)")]
        [SerializeField] Button[] p1CharacterButtons;

        [Header("Player 2 (Knight / Ninja / Sorcerer)")]
        [SerializeField] Button[] p2CharacterButtons;

        [Header("Maps")]
        [SerializeField] Button[] mapButtons;

        CharacterType.Character? p1Choice;
        CharacterType.Character? p2Choice;
        string mapChoice;

        void Awake()
        {
            WireButtons();
        }

        void Start()
        {
            RefreshHighlights();
            RefreshFightButton();
        }

        void WireButtons()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => GameSession.Instance.LoadTitle());
            }

            if (fightButton != null)
            {
                fightButton.onClick.RemoveAllListeners();
                fightButton.onClick.AddListener(OnFightPressed);
            }

            WireCharacterButtons(p1CharacterButtons, true);
            WireCharacterButtons(p2CharacterButtons, false);
            WireMapButtons();
        }

        void WireCharacterButtons(Button[] buttons, bool isP1)
        {
            if (buttons == null)
                return;

            int count = Mathf.Min(buttons.Length, Characters.Length);
            for (int i = 0; i < count; i++)
            {
                if (buttons[i] == null)
                    continue;

                int index = i;
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() =>
                {
                    if (isP1)
                        p1Choice = Characters[index];
                    else
                        p2Choice = Characters[index];

                    RefreshHighlights();
                    RefreshFightButton();
                });
            }
        }

        void WireMapButtons()
        {
            if (mapButtons == null)
                return;

            int count = Mathf.Min(mapButtons.Length, MapSceneNames.Length);
            for (int i = 0; i < count; i++)
            {
                if (mapButtons[i] == null)
                    continue;

                string mapName = MapSceneNames[i];
                mapButtons[i].onClick.RemoveAllListeners();
                mapButtons[i].onClick.AddListener(() =>
                {
                    mapChoice = mapName;
                    RefreshHighlights();
                    RefreshFightButton();
                });
            }
        }

        void RefreshHighlights()
        {
            for (int i = 0; i < Characters.Length; i++)
            {
                SetHighlight(GetButton(p1CharacterButtons, i), p1Choice.HasValue && p1Choice.Value == Characters[i]);
                SetHighlight(GetButton(p2CharacterButtons, i), p2Choice.HasValue && p2Choice.Value == Characters[i]);
            }

            for (int i = 0; i < MapSceneNames.Length; i++)
                SetHighlight(GetButton(mapButtons, i), mapChoice == MapSceneNames[i]);
        }

        void RefreshFightButton()
        {
            bool ready = p1Choice.HasValue && p2Choice.HasValue && !string.IsNullOrEmpty(mapChoice);
            if (fightButton == null)
                return;

            fightButton.interactable = ready;
            var image = fightButton.targetGraphic as Image;
            if (image != null)
                image.color = ready ? FightReadyColor : FightDisabledColor;
        }

        void OnFightPressed()
        {
            if (!p1Choice.HasValue || !p2Choice.HasValue || string.IsNullOrEmpty(mapChoice))
                return;

            var session = GameSession.Instance;
            session.SetPlayer1Character(p1Choice.Value);
            session.SetPlayer2Character(p2Choice.Value);
            session.SetSelectedMap(mapChoice);
            session.LoadSelectedMap();
        }

        static Button GetButton(Button[] buttons, int index)
        {
            if (buttons == null || index < 0 || index >= buttons.Length)
                return null;
            return buttons[index];
        }

        static void SetHighlight(Button button, bool selected)
        {
            if (button == null)
                return;

            var image = button.targetGraphic as Image;
            if (image != null)
                image.color = selected ? SelectedColor : NormalColor;
        }
    }
}
