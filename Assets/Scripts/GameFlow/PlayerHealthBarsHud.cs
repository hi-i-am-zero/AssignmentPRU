using System;
using System.Collections.Generic;
using SkyfallArena.Multiplayer;
using SkyfallArena.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace SkyfallArena.GameFlow
{
    /// <summary>
    /// Thanh máu + MP (hit stacks 0–3) P1/P2: UI gắn sẵn trong scene (Inspector).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthBarsHud : MonoBehaviour
    {
        [Serializable]
        public sealed class BarBindings
        {
            public RectTransform fillRect;
            public RectTransform chipRect;
            public Image fillImage;
            public Image chipImage;
            public Text label;
            public Text value;
            public bool fillFromLeft = true;
        }

        [Serializable]
        public sealed class MeterBindings
        {
            public Image[] segments = new Image[SpecialAbilityController.MaxStacks];
            public Text label;
            public Color emptyColor = new Color(0.15f, 0.15f, 0.18f, 0.95f);
            public Color filledColor = new Color(0.35f, 0.75f, 1f, 1f);
            public Color readyColor = new Color(1f, 0.9f, 0.35f, 1f);
        }

        sealed class BarRuntime
        {
            public BarBindings ui;
            public MeterBindings mpUi;
            public PlayerHealth bound;
            public SpecialAbilityController meter;
            public float displayedNormalized = 1f;
            public float chipNormalized = 1f;
            public float targetNormalized = 1f;
            public float currentHp;
            public float maxHp = 100f;
            public float chipDelayUntil;
            public bool chipCatchingUp;
            public int stacks;
            public int maxStacks = SpecialAbilityController.MaxStacks;
        }

        [Header("Scene UI")]
        [SerializeField] GameObject hudRoot;
        [SerializeField] BarBindings player1Bar;
        [SerializeField] BarBindings player2Bar;
        [SerializeField] MeterBindings player1MpBar;
        [SerializeField] MeterBindings player2MpBar;

        [Header("Systems")]
        [SerializeField] LocalMultiplayerManager multiplayerManager;
        [SerializeField] MatchResultSystem matchResultSystem;
        [SerializeField] bool hideWhenMatchEnds = true;
        [SerializeField, Min(0.01f)] float fillLerpSpeed = 18f;
        [SerializeField, Min(0f)] float chipHoldSeconds = 0.35f;
        [SerializeField, Min(0.01f)] float chipLerpSpeed = 3.5f;

        readonly Dictionary<int, BarRuntime> barsByPlayer = new Dictionary<int, BarRuntime>();
        readonly List<PlayerHealth> tracked = new List<PlayerHealth>();
        readonly List<SpecialAbilityController> trackedMeters = new List<SpecialAbilityController>();

        float nextScanTime;
        bool visible = true;
        bool warnedMissingUi;

        void Awake()
        {
            if (multiplayerManager == null)
                multiplayerManager = FindFirstObjectByType<LocalMultiplayerManager>();
            if (matchResultSystem == null)
                matchResultSystem = FindFirstObjectByType<MatchResultSystem>();

            BuildRuntimeBars();
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

        void Start()
        {
            if (visible)
            {
                SetHudActive(true);
                ScanAndBindPlayers();
            }
        }

        void Update()
        {
            if (!visible)
                return;

            AnimateBars();

            if (Time.unscaledTime >= nextScanTime)
            {
                nextScanTime = Time.unscaledTime + 0.35f;
                ScanAndBindPlayers();
            }
        }

        void BuildRuntimeBars()
        {
            barsByPlayer.Clear();

            if (player1Bar != null && player1Bar.fillRect != null)
            {
                barsByPlayer[1] = new BarRuntime
                {
                    ui = player1Bar,
                    mpUi = player1MpBar
                };
            }

            if (player2Bar != null && player2Bar.fillRect != null)
            {
                barsByPlayer[2] = new BarRuntime
                {
                    ui = player2Bar,
                    mpUi = player2MpBar
                };
            }

            if (barsByPlayer.Count == 0 && !warnedMissingUi)
            {
                warnedMissingUi = true;
                Debug.LogError(
                    "[PlayerHealthBarsHud] Missing scene UI refs. Wire hudRoot / player1Bar / player2Bar in Inspector, or run Skyfall Arena > Game Flow > Rebuild Health Bars On Maps.",
                    this);
            }
        }

        void HandleMatchStarted()
        {
            visible = true;
            SetHudActive(true);
            nextScanTime = 0f;
            ScanAndBindPlayers();
        }

        void HandleMatchEnded(int winnerPlayerId, int aliveCount)
        {
            if (!hideWhenMatchEnds)
                return;

            visible = false;
            SetHudActive(false);
        }

        void SetHudActive(bool active)
        {
            if (hudRoot != null)
                hudRoot.SetActive(active);
        }

        void ScanAndBindPlayers()
        {
            if (barsByPlayer.Count == 0)
                BuildRuntimeBars();
            if (barsByPlayer.Count == 0)
                return;

            var found = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                var health = found[i];
                if (health == null)
                    continue;

                int playerId = health.PlayerId;
                if (!barsByPlayer.TryGetValue(playerId, out var view))
                    continue;

                if (view.bound == health)
                {
                    float safeMax = Mathf.Max(1f, health.MaxHealth);
                    float normalized = Mathf.Clamp01(health.CurrentHealth / safeMax);
                    if (!Mathf.Approximately(view.targetNormalized, normalized)
                        || !Mathf.Approximately(view.maxHp, safeMax))
                    {
                        HandleHealthChanged(health, health.CurrentHealth, health.MaxHealth);
                    }

                    BindMeter(view, health.GetComponent<SpecialAbilityController>());
                    continue;
                }

                if (view.bound != null)
                    view.bound.HealthChanged -= HandleHealthChanged;

                view.bound = health;
                health.HealthChanged += HandleHealthChanged;
                if (!tracked.Contains(health))
                    tracked.Add(health);

                SetBarImmediate(view, health.CurrentHealth, health.MaxHealth, playerId);
                BindMeter(view, health.GetComponent<SpecialAbilityController>());
            }
        }

        void BindMeter(BarRuntime view, SpecialAbilityController meter)
        {
            if (view == null)
                return;

            if (view.meter == meter)
            {
                if (meter != null)
                    ApplyMeter(view, meter.CurrentStacks, SpecialAbilityController.MaxStacks);
                return;
            }

            if (view.meter != null)
                view.meter.MeterChanged -= HandleMeterChanged;

            view.meter = meter;
            if (meter == null)
            {
                ApplyMeter(view, 0, SpecialAbilityController.MaxStacks);
                return;
            }

            meter.MeterChanged += HandleMeterChanged;
            if (!trackedMeters.Contains(meter))
                trackedMeters.Add(meter);

            ApplyMeter(view, meter.CurrentStacks, SpecialAbilityController.MaxStacks);
        }

        void HandleMeterChanged(SpecialAbilityController meter, int current, int max)
        {
            if (meter == null)
                return;

            int playerId = meter.PlayerId;
            if (playerId <= 0 && meter.TryGetComponent<PlayerHealth>(out var health))
                playerId = health.PlayerId;

            if (!barsByPlayer.TryGetValue(playerId, out var view) || view == null)
                return;

            ApplyMeter(view, current, max);
        }

        static void ApplyMeter(BarRuntime view, int stacks, int maxStacks)
        {
            view.stacks = Mathf.Clamp(stacks, 0, Mathf.Max(1, maxStacks));
            view.maxStacks = Mathf.Max(1, maxStacks);

            var mp = view.mpUi;
            if (mp == null)
                return;

            if (mp.label != null)
                mp.label.text = view.stacks >= view.maxStacks ? "READY" : "MP";

            if (mp.segments == null)
                return;

            bool ready = view.stacks >= view.maxStacks;
            for (int i = 0; i < mp.segments.Length; i++)
            {
                var segment = mp.segments[i];
                if (segment == null)
                    continue;

                bool lit = i < view.stacks;
                if (!lit)
                    segment.color = mp.emptyColor;
                else if (ready)
                    segment.color = mp.readyColor;
                else
                    segment.color = mp.filledColor;
            }
        }

        void HandleHealthChanged(PlayerHealth health, float current, float max)
        {
            if (health == null)
                return;

            if (!barsByPlayer.TryGetValue(health.PlayerId, out var view) || view == null)
                return;

            float safeMax = Mathf.Max(1f, max);
            float newNormalized = Mathf.Clamp01(current / safeMax);
            float previousTarget = view.targetNormalized;

            view.currentHp = current;
            view.maxHp = safeMax;
            view.targetNormalized = newNormalized;

            if (view.ui.label != null)
                view.ui.label.text = health.PlayerId == 1 ? "Player 1" : "Player 2";

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

        void SetBarImmediate(BarRuntime view, float current, float max, int playerId)
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

            ApplyBarWidth(view.ui, normalized, normalized);

            if (view.ui.label != null)
                view.ui.label.text = playerId == 1 ? "Player 1" : "Player 2";

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
                if (view?.ui == null)
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

                ApplyBarWidth(view.ui, view.displayedNormalized, view.chipNormalized);
                RefreshValueText(view);
            }
        }

        static void RefreshValueText(BarRuntime view)
        {
            if (view?.ui?.value == null)
                return;

            int shown = Mathf.Max(0, Mathf.CeilToInt(view.targetNormalized * view.maxHp));
            int max = Mathf.CeilToInt(view.maxHp);
            view.ui.value.text = $"{shown} / {max}";
        }

        static void ApplyBarWidth(BarBindings ui, float fillNormalized, float chipNormalized)
        {
            if (ui == null)
                return;

            SetNormalizedWidth(ui.chipRect, chipNormalized, ui.fillFromLeft);
            SetNormalizedWidth(ui.fillRect, fillNormalized, ui.fillFromLeft);
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
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(normalized, 1f);
            }
            else
            {
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchorMin = new Vector2(1f - normalized, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
            }
        }

        void UnbindAll()
        {
            for (int i = 0; i < tracked.Count; i++)
            {
                if (tracked[i] != null)
                    tracked[i].HealthChanged -= HandleHealthChanged;
            }

            tracked.Clear();

            for (int i = 0; i < trackedMeters.Count; i++)
            {
                if (trackedMeters[i] != null)
                    trackedMeters[i].MeterChanged -= HandleMeterChanged;
            }

            trackedMeters.Clear();

            foreach (var pair in barsByPlayer)
            {
                if (pair.Value != null)
                {
                    pair.Value.bound = null;
                    pair.Value.meter = null;
                }
            }
        }
    }
}
