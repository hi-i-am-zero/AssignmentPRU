using SkyfallArena.Multiplayer;
using SkyfallArena.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Overlay thắng/thua: khóa combat, hiện kết quả.
    /// Character Select → về Select; Out → thoát game.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultUI : MonoBehaviour
    {
        [SerializeField] MatchResultSystem matchResultSystem;
        [SerializeField] LocalMultiplayerManager multiplayerManager;

        GameObject root;
        Text resultText;
        bool shown;

        void Awake()
        {
            if (matchResultSystem == null)
                matchResultSystem = FindFirstObjectByType<MatchResultSystem>();
            if (multiplayerManager == null)
                multiplayerManager = FindFirstObjectByType<LocalMultiplayerManager>();
        }

        void OnEnable()
        {
            if (matchResultSystem != null)
                matchResultSystem.MatchEnded += HandleMatchEnded;
        }

        void OnDisable()
        {
            if (matchResultSystem != null)
                matchResultSystem.MatchEnded -= HandleMatchEnded;
        }

        // Gọi khi còn ≤1 người sống
        void HandleMatchEnded(int winnerPlayerId, int aliveCount)
        {
            if (shown)
                return;

            shown = true;
            GameSession.Instance.SetLastResult(winnerPlayerId, aliveCount);

            if (multiplayerManager != null)
                multiplayerManager.SetCombatInputEnabled(false);

            FreezeCombatants();
            ShowOverlay(winnerPlayerId, aliveCount);
        }

        // Tắt điều khiển combat sau khi hết trận
        static void FreezeCombatants()
        {
            var controllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null)
                    controllers[i].enabled = false;
            }

            var attackers = FindObjectsByType<AttackController>(FindObjectsSortMode.None);
            for (int i = 0; i < attackers.Length; i++)
            {
                if (attackers[i] != null)
                    attackers[i].enabled = false;
            }

            var blockers = FindObjectsByType<PlayerBlockController>(FindObjectsSortMode.None);
            for (int i = 0; i < blockers.Length; i++)
            {
                if (blockers[i] != null)
                    blockers[i].enabled = false;
            }
        }

        void ShowOverlay(int winnerPlayerId, int aliveCount)
        {
            var canvas = UiFactory.CreateCanvas("ResultCanvas", transform);
            canvas.sortingOrder = 200;

            UiFactory.CreatePanel(
                canvas.transform,
                "Dim",
                new Color(0f, 0f, 0f, 0.72f),
                Vector2.zero,
                Vector2.one);

            var panel = UiFactory.CreatePanel(
                canvas.transform,
                "Panel",
                new Color(0.1f, 0.12f, 0.18f, 0.96f),
                new Vector2(0.25f, 0.3f),
                new Vector2(0.75f, 0.7f));

            string message;
            if (aliveCount <= 0)
                message = "Draw\nNo survivors";
            else if (winnerPlayerId == 1)
                message = "Player 1 Wins!\nPlayer 2 Loses";
            else if (winnerPlayerId == 2)
                message = "Player 2 Wins!\nPlayer 1 Loses";
            else
                message = $"Winner: Player {winnerPlayerId}";

            resultText = UiFactory.CreateText(
                panel.transform,
                "ResultText",
                message,
                42,
                TextAnchor.MiddleCenter,
                Color.white);
            var textRect = resultText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.35f);
            textRect.anchorMax = new Vector2(0.95f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Về màn chọn nhân vật/map
            var selectButton = UiFactory.CreateButton(
                panel.transform,
                "SelectAgainButton",
                "Character Select",
                Vector2.zero,
                new Vector2(220f, 56f),
                () => GameSession.Instance.LoadSelect());
            PlaceButton(selectButton.GetComponent<RectTransform>(), new Vector2(0.06f, 0.07f), new Vector2(0.48f, 0.28f));

            // Thoát Play / thoát build
            var outButton = UiFactory.CreateButton(
                panel.transform,
                "OutButton",
                "Out",
                Vector2.zero,
                new Vector2(180f, 56f),
                QuitGame);
            PlaceButton(outButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0.07f), new Vector2(0.94f, 0.28f));

            var outImage = outButton.GetComponent<Image>();
            if (outImage != null)
                outImage.color = new Color(0.55f, 0.18f, 0.18f, 1f);

            root = canvas.gameObject;
        }

        static void PlaceButton(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
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

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
