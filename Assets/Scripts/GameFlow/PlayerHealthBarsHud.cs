using System.Collections.Generic;
using SkyfallArena.Multiplayer;
using SkyfallArena.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Thanh máu P1 (trái) / P2 (phải): fill theo HP + chip vàng thể hiện đoạn vừa mất.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthBarsHud : MonoBehaviour
    {
        sealed class BarView
        {
            public RectTransform fillRect;   // thanh HP thật
            public RectTransform chipRect;   // vệt damage tạm
            public Image fillImage;
            public Image chipImage;
            public Text label;
            public Text value;
            public PlayerHealth bound;
            public bool fillFromLeft = true;

            public float displayedNormalized = 1f;
            public float chipNormalized = 1f;
            public float targetNormalized = 1f;
            public float currentHp;
            public float maxHp = 100f;
            public float chipDelayUntil;
            public bool chipCatchingUp;
        }

        [SerializeField] LocalMultiplayerManager multiplayerManager;
        [SerializeField] MatchResultSystem matchResultSystem;
        [SerializeField] bool hideWhenMatchEnds = true;
        [SerializeField, Min(0.01f)] float fillLerpSpeed = 18f;
        [SerializeField, Min(0f)] float chipHoldSeconds = 0.35f;
        [SerializeField, Min(0.01f)] float chipLerpSpeed = 3.5f;

        readonly Dictionary<int, BarView> barsByPlayer = new Dictionary<int, BarView>();
        readonly List<PlayerHealth> tracked = new List<PlayerHealth>();

        GameObject root;
        float nextScanTime;
        bool visible = true;
        static Sprite whiteSprite;

        void Awake()
        {
            if (multiplayerManager == null)
                multiplayerManager = FindFirstObjectByType<LocalMultiplayerManager>();
            if (matchResultSystem == null)
                matchResultSystem = FindFirstObjectByType<MatchResultSystem>();
        }

        void OnEnable()
        {
            if (multiplayerManager != null)
                multiplayerManager.MatchStarted += HandleMatchStarted;

            if (matchResultSystem != null)
                matchResultSystem.MatchEnded += HandleMatchEnded;
        }

        void OnDisable()
        {
            if (multiplayerManager != null)
                multiplayerManager.MatchStarted -= HandleMatchStarted;

            if (matchResultSystem != null)
                matchResultSystem.MatchEnded -= HandleMatchEnded;

            UnbindAll();
        }

        void Update()
        {
            if (!visible)
                return;

            AnimateBars();

            if (root == null)
                return;

            if (Time.unscaledTime >= nextScanTime)
            {
                nextScanTime = Time.unscaledTime + 0.35f;
                ScanAndBindPlayers();
            }
        }

        void HandleMatchStarted()
        {
            visible = true;
            EnsureUi();
            if (root != null)
                root.SetActive(true);

            nextScanTime = 0f;
            ScanAndBindPlayers();
        }

        void HandleMatchEnded(int winnerPlayerId, int aliveCount)
        {
            if (!hideWhenMatchEnds)
                return;

            visible = false;
            if (root != null)
                root.SetActive(false);
        }

        void EnsureUi()
        {
            if (root != null)
                return;

            var canvas = UiFactory.CreateCanvas("HealthBarsCanvas", transform);
            canvas.sortingOrder = 50;
            root = canvas.gameObject;

            barsByPlayer[1] = CreateBar(canvas.transform, "P1Health", isLeft: true, Color.red);
            barsByPlayer[2] = CreateBar(canvas.transform, "P2Health", isLeft: false, new Color(0.2f, 0.55f, 1f, 1f));
        }

        static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
                return whiteSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            whiteSprite.name = "HealthBarWhite";
            return whiteSprite;
        }

        static BarView CreateBar(Transform parent, string name, bool isLeft, Color fillColor)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var panelRect = panel.GetComponent<RectTransform>();
            if (isLeft)
            {
                panelRect.anchorMin = new Vector2(0.02f, 0.90f);
                panelRect.anchorMax = new Vector2(0.38f, 0.98f);
            }
            else
            {
                panelRect.anchorMin = new Vector2(0.62f, 0.90f);
                panelRect.anchorMax = new Vector2(0.98f, 0.98f);
            }

            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = GetWhiteSprite();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);
            panelImage.type = Image.Type.Simple;

            var label = UiFactory.CreateText(
                panel.transform,
                "Label",
                isLeft ? "P1" : "P2",
                22,
                isLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight,
                Color.white);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.03f, 0.55f);
            labelRect.anchorMax = new Vector2(0.97f, 0.95f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var trackGo = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(panel.transform, false);
            var trackRect = trackGo.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.03f, 0.12f);
            trackRect.anchorMax = new Vector2(0.97f, 0.48f);
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            var trackImage = trackGo.GetComponent<Image>();
            trackImage.sprite = GetWhiteSprite();
            trackImage.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
            trackImage.type = Image.Type.Simple;

            var chipGo = new GameObject("Chip", typeof(RectTransform), typeof(Image));
            chipGo.transform.SetParent(trackGo.transform, false);
            var chipRect = chipGo.GetComponent<RectTransform>();
            StretchFull(chipRect);
            var chip = chipGo.GetComponent<Image>();
            chip.sprite = GetWhiteSprite();
            chip.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            chip.type = Image.Type.Simple;
            chip.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            StretchFull(fillRect);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = GetWhiteSprite();
            fill.color = fillColor;
            fill.type = Image.Type.Simple;
            fill.raycastTarget = false;

            var value = UiFactory.CreateText(
                panel.transform,
                "Value",
                "100 / 100",
                18,
                TextAnchor.MiddleCenter,
                Color.white);
            var valueRect = value.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.03f, 0.12f);
            valueRect.anchorMax = new Vector2(0.97f, 0.48f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;

            var view = new BarView
            {
                fillRect = fillRect,
                chipRect = chipRect,
                fillImage = fill,
                chipImage = chip,
                label = label,
                value = value,
                fillFromLeft = isLeft,
                displayedNormalized = 1f,
                chipNormalized = 1f,
                targetNormalized = 1f
            };

            ApplyBarWidth(view, 1f, 1f);
            return view;
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void ApplyBarWidth(BarView view, float fillNormalized, float chipNormalized)
        {
            if (view == null)
                return;

            SetNormalizedWidth(view.chipRect, chipNormalized, view.fillFromLeft);
            SetNormalizedWidth(view.fillRect, fillNormalized, view.fillFromLeft);
        }

        static void SetNormalizedWidth(RectTransform rect, float normalized, bool fromLeft)
        {
            if (rect == null)
                return;

            normalized = Mathf.Clamp01(normalized);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (fromLeft)
            {
                // Grow from left edge.
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(normalized, 1f);
            }
            else
            {
                // Grow from right edge (P2).
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchorMin = new Vector2(1f - normalized, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
            }
        }

        void ScanAndBindPlayers()
        {
            EnsureUi();

            var found = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                var health = found[i];
                if (health == null)
                    continue;

                int playerId = health.PlayerId;
                if (playerId != 1 && playerId != 2)
                    continue;

                if (!barsByPlayer.TryGetValue(playerId, out var view))
                    continue;

                if (view.bound == health)
                {
                    // Keep target in sync even if already bound.
                    float safeMax = Mathf.Max(1f, health.MaxHealth);
                    float normalized = Mathf.Clamp01(health.CurrentHealth / safeMax);
                    if (!Mathf.Approximately(view.targetNormalized, normalized)
                        || !Mathf.Approximately(view.maxHp, safeMax))
                    {
                        HandleHealthChanged(health, health.CurrentHealth, health.MaxHealth);
                    }

                    continue;
                }

                if (view.bound != null)
                    view.bound.HealthChanged -= HandleHealthChanged;

                view.bound = health;
                health.HealthChanged += HandleHealthChanged;
                if (!tracked.Contains(health))
                    tracked.Add(health);

                SetBarImmediate(view, health.CurrentHealth, health.MaxHealth, playerId);
            }
        }

        void HandleHealthChanged(PlayerHealth health, float current, float max)
        {
            if (health == null)
                return;

            int playerId = health.PlayerId;
            if (!barsByPlayer.TryGetValue(playerId, out var view) || view == null)
                return;

            float safeMax = Mathf.Max(1f, max);
            float newNormalized = Mathf.Clamp01(current / safeMax);
            float previousTarget = view.targetNormalized;

            view.currentHp = current;
            view.maxHp = safeMax;
            view.targetNormalized = newNormalized;

            if (view.label != null)
                view.label.text = playerId == 1 ? "Player 1" : "Player 2";

            if (newNormalized < previousTarget - 0.0001f)
            {
                view.chipNormalized = Mathf.Max(view.chipNormalized, previousTarget);
                view.chipCatchingUp = false;
                view.chipDelayUntil = Time.unscaledTime + chipHoldSeconds;
            }
            else
            {
                view.chipNormalized = Mathf.Max(view.chipNormalized, newNormalized);
                view.chipCatchingUp = false;
                view.chipDelayUntil = 0f;
            }

            RefreshValueText(view);
        }

        void SetBarImmediate(BarView view, float current, float max, int playerId)
        {
            float safeMax = Mathf.Max(1f, max);
            float normalized = Mathf.Clamp01(current / safeMax);

            view.currentHp = current;
            view.maxHp = safeMax;
            view.targetNormalized = normalized;
            view.displayedNormalized = normalized;
            view.chipNormalized = normalized;
            view.chipCatchingUp = false;
            view.chipDelayUntil = 0f;

            ApplyBarWidth(view, normalized, normalized);

            if (view.label != null)
                view.label.text = playerId == 1 ? "Player 1" : "Player 2";

            RefreshValueText(view);
        }

        void AnimateBars()
        {
            if (barsByPlayer.Count == 0)
                return;

            float dt = Time.unscaledDeltaTime;

            foreach (var pair in barsByPlayer)
            {
                var view = pair.Value;
                if (view == null)
                    continue;

                view.displayedNormalized = Mathf.MoveTowards(
                    view.displayedNormalized,
                    view.targetNormalized,
                    fillLerpSpeed * dt);

                if (!view.chipCatchingUp && Time.unscaledTime >= view.chipDelayUntil)
                    view.chipCatchingUp = true;

                if (view.chipCatchingUp)
                {
                    view.chipNormalized = Mathf.MoveTowards(
                        view.chipNormalized,
                        view.targetNormalized,
                        chipLerpSpeed * dt);
                }
                else
                {
                    view.chipNormalized = Mathf.Max(view.chipNormalized, view.displayedNormalized);
                }

                ApplyBarWidth(view, view.displayedNormalized, view.chipNormalized);
                RefreshValueText(view);
            }
        }

        static void RefreshValueText(BarView view)
        {
            if (view == null || view.value == null)
                return;

            int shown = Mathf.Max(0, Mathf.CeilToInt(view.targetNormalized * view.maxHp));
            int max = Mathf.CeilToInt(view.maxHp);
            view.value.text = $"{shown} / {max}";
        }

        void UnbindAll()
        {
            for (int i = 0; i < tracked.Count; i++)
            {
                if (tracked[i] != null)
                    tracked[i].HealthChanged -= HandleHealthChanged;
            }

            tracked.Clear();

            foreach (var pair in barsByPlayer)
            {
                if (pair.Value != null)
                    pair.Value.bound = null;
            }
        }
    }
}
