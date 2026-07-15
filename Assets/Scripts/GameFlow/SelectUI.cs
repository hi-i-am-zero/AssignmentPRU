using UnityEngine;
using UnityEngine.UI;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Màn Select: P1 trái / Map giữa / P2 phải. Fight chỉ bật khi chọn đủ.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SelectUI : MonoBehaviour
    {
        // 3 class cho phép chọn (được trùng nhau)
        static readonly CharacterType.Character[] Characters =
        {
            CharacterType.Character.Knight,
            CharacterType.Character.Ninja,
            CharacterType.Character.Sorcerer
        };

        // 5 map combat
        static readonly string[] MapSceneNames =
        {
            "Mountain",
            "Volcano",
            "Sky",
            "WeatherTest",
            "TestArena"
        };

        CharacterType.Character? p1Choice;
        CharacterType.Character? p2Choice;
        string mapChoice;

        Button fightButton;
        Button[] p1Buttons;
        Button[] p2Buttons;
        Button[] mapButtons;
        bool built;

        void Awake()
        {
            BuildUiIfNeeded();
        }

        void Start()
        {
            BuildUiIfNeeded();
            RefreshHighlights();
            RefreshFightButton();
        }

        void BuildUiIfNeeded()
        {
            if (built || transform.Find("SelectCanvas") != null)
            {
                built = true;
                return;
            }

            BuildUi();
            built = true;
        }

        void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas("SelectCanvas", transform);

            UiFactory.CreatePanel(
                canvas.transform,
                "Background",
                new Color(0.06f, 0.07f, 0.1f, 1f),
                Vector2.zero,
                Vector2.one);

            var header = UiFactory.CreateText(
                canvas.transform,
                "Header",
                "Select Fighters & Arena",
                40,
                TextAnchor.MiddleCenter,
                Color.white);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.1f, 0.88f);
            headerRect.anchorMax = new Vector2(0.9f, 0.98f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            BuildColumn(canvas.transform, "P1Column", "Player 1", new Vector2(0.03f, 0.18f), new Vector2(0.30f, 0.85f), true);
            BuildMapColumn(canvas.transform, new Vector2(0.34f, 0.18f), new Vector2(0.66f, 0.85f));
            BuildColumn(canvas.transform, "P2Column", "Player 2", new Vector2(0.70f, 0.18f), new Vector2(0.97f, 0.85f), false);

            // Back → Title
            var backButton = UiFactory.CreateButton(
                canvas.transform,
                "BackButton",
                "Back",
                new Vector2(-200f, 0f),
                new Vector2(180f, 56f),
                () => GameSession.Instance.LoadTitle());
            PlaceBottomButton(backButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.03f), new Vector2(0.42f, 0.12f));

            fightButton = UiFactory.CreateButton(
                canvas.transform,
                "FightButton",
                "Fight",
                Vector2.zero,
                new Vector2(240f, 64f),
                OnFightPressed);
            PlaceBottomButton(fightButton.GetComponent<RectTransform>(), new Vector2(0.44f, 0.02f), new Vector2(0.72f, 0.14f));
        }

        static void PlaceBottomButton(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        // Cột chọn nhân vật (P1 hoặc P2)
        void BuildColumn(Transform parent, string name, string title, Vector2 anchorMin, Vector2 anchorMax, bool isP1)
        {
            var panel = UiFactory.CreatePanel(parent, name, new Color(0.12f, 0.14f, 0.2f, 0.9f), anchorMin, anchorMax);

            var titleText = UiFactory.CreateText(panel.transform, "Title", title, 30, TextAnchor.UpperCenter, Color.white);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.82f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panel.transform, false);
            var listRect = listGo.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.08f, 0.08f);
            listRect.anchorMax = new Vector2(0.92f, 0.78f);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = Vector2.zero;

            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var buttons = new Button[Characters.Length];
            for (int i = 0; i < Characters.Length; i++)
            {
                var character = Characters[i];
                int captured = i;
                buttons[i] = UiFactory.CreateSelectableButton(
                    listGo.transform,
                    character.ToString(),
                    character.ToString(),
                    () =>
                    {
                        if (isP1)
                            p1Choice = Characters[captured];
                        else
                            p2Choice = Characters[captured];
                        RefreshHighlights();
                        RefreshFightButton();
                    });
            }

            if (isP1)
                p1Buttons = buttons;
            else
                p2Buttons = buttons;
        }

        // Cột chọn map
        void BuildMapColumn(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = UiFactory.CreatePanel(parent, "MapColumn", new Color(0.1f, 0.16f, 0.18f, 0.9f), anchorMin, anchorMax);

            var titleText = UiFactory.CreateText(panel.transform, "Title", "Map", 30, TextAnchor.UpperCenter, Color.white);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.82f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panel.transform, false);
            var listRect = listGo.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.08f, 0.08f);
            listRect.anchorMax = new Vector2(0.92f, 0.78f);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = Vector2.zero;

            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            mapButtons = new Button[MapSceneNames.Length];
            for (int i = 0; i < MapSceneNames.Length; i++)
            {
                string mapName = MapSceneNames[i];
                mapButtons[i] = UiFactory.CreateSelectableButton(
                    listGo.transform,
                    mapName,
                    mapName,
                    () =>
                    {
                        mapChoice = mapName;
                        RefreshHighlights();
                        RefreshFightButton();
                    });
            }
        }

        // Highlight nút đang chọn
        void RefreshHighlights()
        {
            for (int i = 0; i < Characters.Length; i++)
            {
                UiFactory.SetButtonHighlight(p1Buttons[i], p1Choice.HasValue && p1Choice.Value == Characters[i]);
                UiFactory.SetButtonHighlight(p2Buttons[i], p2Choice.HasValue && p2Choice.Value == Characters[i]);
            }

            for (int i = 0; i < MapSceneNames.Length; i++)
                UiFactory.SetButtonHighlight(mapButtons[i], mapChoice == MapSceneNames[i]);
        }

        // Fight chỉ interactable khi đủ P1 + P2 + map
        void RefreshFightButton()
        {
            bool ready = p1Choice.HasValue && p2Choice.HasValue && !string.IsNullOrEmpty(mapChoice);
            if (fightButton != null)
                fightButton.interactable = ready;

            var image = fightButton != null ? fightButton.GetComponent<Image>() : null;
            if (image != null)
                image.color = ready
                    ? new Color(0.75f, 0.2f, 0.2f, 1f)
                    : new Color(0.35f, 0.35f, 0.38f, 1f);
        }

        // Lưu session rồi load map combat
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
    }
}
