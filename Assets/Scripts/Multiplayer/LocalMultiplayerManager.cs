using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ArenaEnvironment = Environment;
using SkyfallArena.Systems;
using SkyfallArena.Systems.Items;

namespace SkyfallArena.Multiplayer
{
    /// <summary>
    /// Local 1v1: spawn 2 player từ session, publish input keyboard (WASD / Arrows + attack keys).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalMultiplayerManager : MonoBehaviour
    {
        const int FixedPlayers = LocalPlayerRules.FixedPlayerCount;

        [Serializable]
        struct KeyboardLayout
        {
            public string displayName;
            public Key join;

            [Header("Gameplay")]
            public Key left;
            public Key right;
            public Key up;
            public Key down;
            public Key attack1;
            public Key attack2;
            public Key attack3;
            public Key attack4;
            public Key block;
            public Key interact;
            public Key crouch;
            public Key sprint;

            [Header("Lobby")]
            public Key previousCharacter;
            public Key nextCharacter;
            public Key submit;
            public Key cancel;
        }

        enum ControlSource
        {
            Keyboard,
            Gamepad
        }

        sealed class JoinedPlayer
        {
            public int playerId;
            public ControlSource source;
            public int keyboardLayoutIndex = -1;
            public int gamepadDeviceId = -1;
            public int selectionIndex;

            public GameObject spawnedObject;
            public LocalPlayerInputSource inputSource;
        }

        [Header("References")]
        [SerializeField] CharacterRosterConfig rosterConfig;
        [SerializeField] ArenaEnvironment.ArenaManager arenaManager;
        [SerializeField] Transform runtimePlayerRoot;

        [Header("Fallback Character Prefabs (Used if rosterConfig is empty)")]
        [SerializeField] GameObject knightPrefab;
        [SerializeField] GameObject ninjaPrefab;
        [SerializeField] GameObject sorcererPrefab;

        [Header("Lobby")]
        [SerializeField] bool autoStartWhenBothPlayersJoined = true;
        [SerializeField] bool enableInArenaLobby = false;
        [SerializeField] bool verboseLogs = true;

        [Header("Member 2 Core Systems")]
        [SerializeField] bool autoAttachCoreSystems = true;
        [SerializeField] bool autoAttachFallDetection = true;
        [SerializeField] bool autoAttachItemUpgradeBridge = true;

        [Header("Keyboard Splits")]
        [SerializeField] KeyboardLayout[] keyboardLayouts;

        readonly List<JoinedPlayer> joinedPlayers = new List<JoinedPlayer>();
        readonly CharacterType.Character[] fallbackSelectionOrder =
        {
            CharacterType.Character.Knight,
            CharacterType.Character.Ninja,
            CharacterType.Character.Sorcerer
        };

        bool matchStarted;
        bool combatInputEnabled = true;
        bool sessionMatchPending;

        public event Action<int> PlayerJoined;
        public event Action<int> PlayerLeft;
        public event Action<int, CharacterType.Character> CharacterChanged;
        public event Action MatchStarted;

        public int JoinedPlayerCount => joinedPlayers.Count;
        public bool MatchHasStarted => matchStarted;
        public bool CombatInputEnabled => combatInputEnabled;

        void Reset()
        {
            keyboardLayouts = BuildDefaultKeyboardLayouts();
        }

        void OnValidate()
        {
            if (keyboardLayouts != null && keyboardLayouts.Length > FixedPlayers)
                System.Array.Resize(ref keyboardLayouts, FixedPlayers);
        }

        void Awake()
        {
            // Always refresh default combat key layouts for the dedicated attack-key scheme.
            keyboardLayouts = BuildDefaultKeyboardLayouts();

            if (arenaManager == null)
                arenaManager = FindFirstObjectByType<ArenaEnvironment.ArenaManager>();

            EnsureFallbackPrefabs();
            DisableScenePlacedPlayers();
        }

        void EnsureFallbackPrefabs()
        {
#if UNITY_EDITOR
            if (knightPrefab == null)
                knightPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Knight.prefab");
            if (ninjaPrefab == null)
                ninjaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ninja.prefab");
            if (sorcererPrefab == null)
                sorcererPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Sorcerer.prefab");
#else
            if (knightPrefab == null)
                knightPrefab = Resources.Load<GameObject>("Knight");
            if (ninjaPrefab == null)
                ninjaPrefab = Resources.Load<GameObject>("Ninja");
            if (sorcererPrefab == null)
                sorcererPrefab = Resources.Load<GameObject>("Sorcerer");
#endif
        }

        void DisableScenePlacedPlayers()
        {
            var placedPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (int index = 0; index < placedPlayers.Length; index++)
            {
                var player = placedPlayers[index];
                if (player == null)
                    continue;

                // Keep only runtime-spawned players from this manager.
                if (player.GetComponent<LocalPlayerInputSource>() != null)
                    continue;

                player.gameObject.SetActive(false);
                Log($"Disabled scene-placed player '{player.name}' (lobby spawn mode).");
            }
        }

        void Start()
        {
            EnsureResultUiPresent();

            if (matchStarted || enableInArenaLobby)
                return;

            // Prefer MatchBootstrap when present on this object.
            if (GetComponent<SkyfallArena.GameFlow.MatchBootstrap>() != null)
                return;

            var session = SkyfallArena.GameFlow.GameSession.Instance;
            if (session != null && session.HasMatchSelection)
            {
                StartMatchFromSession(session.Player1Character, session.Player2Character);
                return;
            }

            StartMatchFromSession(CharacterType.Character.Knight, CharacterType.Character.Ninja);
            Log("Started match with editor fallback characters (Knight vs Ninja).");
        }

        void EnsureResultUiPresent()
        {
            if (FindFirstObjectByType<SkyfallArena.GameFlow.ResultUI>() == null)
                gameObject.AddComponent<SkyfallArena.GameFlow.ResultUI>();

            if (FindFirstObjectByType<SkyfallArena.GameFlow.PlayerHealthBarsHud>() == null)
                gameObject.AddComponent<SkyfallArena.GameFlow.PlayerHealthBarsHud>();
        }

        void Update()
        {
            if (matchStarted)
            {
                if (combatInputEnabled)
                    PublishInputFrames();
                return;
            }

            if (sessionMatchPending || !enableInArenaLobby)
                return;

            UpdateLobby();
        }

        /// <summary>
        /// Bắt đầu trận ngay từ nhân vật đã chọn ở màn Select (bỏ lobby in-arena).
        /// </summary>
        public void StartMatchFromSession(CharacterType.Character player1Character, CharacterType.Character player2Character)
        {
            if (matchStarted)
                return;

            sessionMatchPending = true;
            enableInArenaLobby = false;
            combatInputEnabled = true;
            joinedPlayers.Clear();

            AddSessionPlayer(1, 0, player1Character);
            AddSessionPlayer(2, 1, player2Character);
            StartMatchInternal();
            sessionMatchPending = false;
        }

        public void SetCombatInputEnabled(bool enabled)
        {
            combatInputEnabled = enabled;
        }

        void AddSessionPlayer(int playerId, int keyboardLayoutIndex, CharacterType.Character character)
        {
            int selectionIndex = FindSelectionIndex(character);
            var joined = new JoinedPlayer
            {
                playerId = playerId,
                source = ControlSource.Keyboard,
                keyboardLayoutIndex = keyboardLayoutIndex,
                selectionIndex = selectionIndex
            };

            joinedPlayers.Add(joined);
            PlayerJoined?.Invoke(playerId);
            CharacterChanged?.Invoke(playerId, character);
            Log($"Session player {playerId} ready as {character}.");
        }

        int FindSelectionIndex(CharacterType.Character character)
        {
            var order = GetSelectionOrder();
            for (int index = 0; index < order.Count; index++)
            {
                if (order[index] == character)
                    return index;
            }

            return 0;
        }

        void UpdateLobby()
        {
            TryJoinWithKeyboard();
            TryJoinWithGamepads();
            ProcessLobbyActions();
            TryStartMatch();
        }

        void TryJoinWithKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || keyboardLayouts == null)
                return;

            for (int index = 0; index < keyboardLayouts.Length; index++)
            {
                if (joinedPlayers.Count >= FixedPlayers)
                    return;

                if (IsKeyboardLayoutJoined(index))
                    continue;

                var layout = keyboardLayouts[index];
                if (layout.join == Key.None)
                    continue;

                if (WasPressedThisFrame(keyboard, layout.join))
                    AddKeyboardPlayer(index);
            }
        }

        void TryJoinWithGamepads()
        {
            if (joinedPlayers.Count >= FixedPlayers)
                return;

            var gamepads = Gamepad.all;
            for (int index = 0; index < gamepads.Count; index++)
            {
                if (joinedPlayers.Count >= FixedPlayers)
                    return;

                var pad = gamepads[index];
                if (pad == null || IsGamepadJoined(pad.deviceId))
                    continue;

                if (pad.startButton.wasPressedThisFrame)
                    AddGamepadPlayer(pad.deviceId);
            }
        }

        void ProcessLobbyActions()
        {
            for (int index = joinedPlayers.Count - 1; index >= 0; index--)
            {
                var joined = joinedPlayers[index];
                if (TryLeaveLobby(joined))
                {
                    RemovePlayer(index);
                    continue;
                }

                int cycleDirection = ReadCharacterCycle(joined);
                if (cycleDirection != 0)
                    CycleCharacter(joined, cycleDirection);
            }
        }

        void TryStartMatch()
        {
            if (joinedPlayers.Count < FixedPlayers)
                return;

            if (autoStartWhenBothPlayersJoined)
            {
                StartMatchInternal();
                return;
            }

            for (int index = 0; index < joinedPlayers.Count; index++)
            {
                var joined = joinedPlayers[index];
                if (ReadSubmitPressed(joined))
                {
                    StartMatchInternal();
                    return;
                }
            }
        }

        void StartMatchInternal()
        {
            if (matchStarted)
                return;

            if (arenaManager == null)
                arenaManager = FindFirstObjectByType<ArenaEnvironment.ArenaManager>();

            for (int index = 0; index < joinedPlayers.Count; index++)
            {
                SpawnJoinedPlayer(joinedPlayers[index]);
            }

            matchStarted = true;
            MatchStarted?.Invoke();
            Log($"Match started with {joinedPlayers.Count} players.");
        }

        void SpawnJoinedPlayer(JoinedPlayer joined)
        {
            var selectedCharacter = GetCharacterForSelection(joined.selectionIndex);
            var prefab = ResolveCharacterPrefab(selectedCharacter);

            if (prefab == null)
            {
                Debug.LogError($"[LocalMultiplayerManager] Missing prefab for character {selectedCharacter}.");
                return;
            }

            Vector3 spawnPosition = Vector3.zero;
            if (arenaManager != null)
                spawnPosition = arenaManager.GetSpawnPosition(joined.playerId);

            var spawned = Instantiate(prefab, spawnPosition, Quaternion.identity, runtimePlayerRoot);
            spawned.name = $"{selectedCharacter}_P{joined.playerId}";

            int playerLayer = LayerMask.NameToLayer(ArenaEnvironment.GameLayers.Player);
            if (playerLayer >= 0)
                SetLayerRecursively(spawned, playerLayer);

            if (!string.IsNullOrEmpty(ArenaEnvironment.GameLayers.TagPlayer))
                spawned.tag = ArenaEnvironment.GameLayers.TagPlayer;

            joined.spawnedObject = spawned;

            var identity = spawned.GetComponent<PlayerIdentity>();
            if (identity == null)
                identity = spawned.AddComponent<PlayerIdentity>();
            identity.Initialize(joined.playerId);

            if (autoAttachCoreSystems)
                EnsureCoreSystems(spawned);

            if (autoAttachItemUpgradeBridge)
                EnsureItemUpgradeBridge(spawned);

            if (spawned.GetComponent<PlayerBlockController>() == null)
                spawned.AddComponent<PlayerBlockController>();

            var source = spawned.GetComponent<LocalPlayerInputSource>();
            if (source == null)
                source = spawned.AddComponent<LocalPlayerInputSource>();

            source.Initialize(joined.playerId);
            joined.inputSource = source;

            // Keep compatibility with existing Member 1 controller if present.
            var legacyController = spawned.GetComponent<PlayerController>();
            if (legacyController != null)
            {
                legacyController.playerType = joined.playerId == 1
                    ? PlayerController.PlayerType.Player1
                    : PlayerController.PlayerType.Player2;
            }
        }

        void PublishInputFrames()
        {
            for (int index = 0; index < joinedPlayers.Count; index++)
            {
                var joined = joinedPlayers[index];
                if (joined.inputSource == null)
                    continue;

                var frame = joined.source == ControlSource.Keyboard
                    ? ReadKeyboardGameplayFrame(joined)
                    : ReadGamepadGameplayFrame(joined);

                joined.inputSource.PublishFrame(frame);
            }
        }

        LocalPlayerInputFrame ReadKeyboardGameplayFrame(JoinedPlayer joined)
        {
            var frame = new LocalPlayerInputFrame
            {
                PlayerId = joined.playerId,
                DeviceName = GetKeyboardDisplayName(joined.keyboardLayoutIndex)
            };

            var keyboard = Keyboard.current;
            if (keyboard == null || joined.keyboardLayoutIndex < 0 || joined.keyboardLayoutIndex >= keyboardLayouts.Length)
                return frame;

            var layout = keyboardLayouts[joined.keyboardLayoutIndex];

            frame.Move = ReadMove(
                IsPressed(keyboard, layout.left),
                IsPressed(keyboard, layout.right),
                IsPressed(keyboard, layout.down),
                IsPressed(keyboard, layout.up));

            frame.JumpPressed = WasPressedThisFrame(keyboard, layout.up);
            frame.JumpHeld = IsPressed(keyboard, layout.up);
            frame.Attack1Pressed = WasPressedThisFrame(keyboard, layout.attack1);
            frame.Attack2Pressed = WasPressedThisFrame(keyboard, layout.attack2);
            frame.Attack3Pressed = WasPressedThisFrame(keyboard, layout.attack3);
            frame.Attack4Pressed = WasPressedThisFrame(keyboard, layout.attack4);
            frame.BlockHeld = IsPressed(keyboard, layout.block);
            frame.AttackPressed = frame.Attack1Pressed || frame.Attack2Pressed || frame.Attack3Pressed || frame.Attack4Pressed;
            frame.AttackHeld = IsPressed(keyboard, layout.attack1)
                || IsPressed(keyboard, layout.attack2)
                || IsPressed(keyboard, layout.attack3)
                || IsPressed(keyboard, layout.attack4);
            frame.InteractPressed = WasPressedThisFrame(keyboard, layout.interact);
            frame.CrouchHeld = IsPressed(keyboard, layout.crouch);
            frame.SprintHeld = IsPressed(keyboard, layout.sprint);
            frame.SubmitPressed = WasPressedThisFrame(keyboard, layout.submit);
            frame.CancelPressed = WasPressedThisFrame(keyboard, layout.cancel);

            return frame;
        }

        LocalPlayerInputFrame ReadGamepadGameplayFrame(JoinedPlayer joined)
        {
            // Gamepad combat mapping is deferred (keyboard-first control scheme).
            var frame = new LocalPlayerInputFrame
            {
                PlayerId = joined.playerId
            };

            var pad = FindGamepadByDeviceId(joined.gamepadDeviceId);
            if (pad == null)
                return frame;

            frame.DeviceName = pad.displayName;
            frame.Move = pad.leftStick.ReadValue();
            frame.JumpPressed = pad.buttonSouth.wasPressedThisFrame;
            frame.JumpHeld = pad.buttonSouth.isPressed;
            frame.SubmitPressed = pad.startButton.wasPressedThisFrame;
            frame.CancelPressed = pad.selectButton.wasPressedThisFrame;

            return frame;
        }

        bool TryLeaveLobby(JoinedPlayer joined)
        {
            if (joined.source == ControlSource.Keyboard)
            {
                var keyboard = Keyboard.current;
                if (keyboard == null)
                    return false;

                if (joined.keyboardLayoutIndex < 0 || joined.keyboardLayoutIndex >= keyboardLayouts.Length)
                    return false;

                var layout = keyboardLayouts[joined.keyboardLayoutIndex];
                return WasPressedThisFrame(keyboard, layout.cancel);
            }

            var pad = FindGamepadByDeviceId(joined.gamepadDeviceId);
            return pad != null && pad.selectButton.wasPressedThisFrame;
        }

        int ReadCharacterCycle(JoinedPlayer joined)
        {
            if (joined.source == ControlSource.Keyboard)
            {
                var keyboard = Keyboard.current;
                if (keyboard == null || joined.keyboardLayoutIndex < 0 || joined.keyboardLayoutIndex >= keyboardLayouts.Length)
                    return 0;

                var layout = keyboardLayouts[joined.keyboardLayoutIndex];
                bool prev = WasPressedThisFrame(keyboard, layout.previousCharacter);
                bool next = WasPressedThisFrame(keyboard, layout.nextCharacter);

                if (prev) return -1;
                if (next) return 1;
                return 0;
            }

            var pad = FindGamepadByDeviceId(joined.gamepadDeviceId);
            if (pad == null)
                return 0;

            if (pad.dpad.left.wasPressedThisFrame) return -1;
            if (pad.dpad.right.wasPressedThisFrame) return 1;
            return 0;
        }

        bool ReadSubmitPressed(JoinedPlayer joined)
        {
            if (joined.source == ControlSource.Keyboard)
            {
                var keyboard = Keyboard.current;
                if (keyboard == null || joined.keyboardLayoutIndex < 0 || joined.keyboardLayoutIndex >= keyboardLayouts.Length)
                    return false;

                var layout = keyboardLayouts[joined.keyboardLayoutIndex];
                return WasPressedThisFrame(keyboard, layout.submit);
            }

            var pad = FindGamepadByDeviceId(joined.gamepadDeviceId);
            return pad != null && pad.startButton.wasPressedThisFrame;
        }

        void AddKeyboardPlayer(int keyboardLayoutIndex)
        {
            int newPlayerId = GetNextAvailablePlayerId();
            if (newPlayerId <= 0)
                return;

            var joined = new JoinedPlayer
            {
                playerId = newPlayerId,
                source = ControlSource.Keyboard,
                keyboardLayoutIndex = keyboardLayoutIndex,
                selectionIndex = Mathf.Clamp(newPlayerId - 1, 0, GetSelectionCount() - 1)
            };

            joinedPlayers.Add(joined);
            PlayerJoined?.Invoke(newPlayerId);
            CharacterChanged?.Invoke(newPlayerId, GetCharacterForSelection(joined.selectionIndex));
            Log($"Player {newPlayerId} joined with {GetKeyboardDisplayName(keyboardLayoutIndex)}.");
        }

        void AddGamepadPlayer(int gamepadDeviceId)
        {
            int newPlayerId = GetNextAvailablePlayerId();
            if (newPlayerId <= 0)
                return;

            var joined = new JoinedPlayer
            {
                playerId = newPlayerId,
                source = ControlSource.Gamepad,
                gamepadDeviceId = gamepadDeviceId,
                selectionIndex = Mathf.Clamp(newPlayerId - 1, 0, GetSelectionCount() - 1)
            };

            joinedPlayers.Add(joined);
            PlayerJoined?.Invoke(newPlayerId);
            CharacterChanged?.Invoke(newPlayerId, GetCharacterForSelection(joined.selectionIndex));

            var gamepad = FindGamepadByDeviceId(gamepadDeviceId);
            string padName = gamepad != null ? gamepad.displayName : "Gamepad";
            Log($"Player {newPlayerId} joined with {padName}.");
        }

        void RemovePlayer(int joinedIndex)
        {
            var joined = joinedPlayers[joinedIndex];
            joinedPlayers.RemoveAt(joinedIndex);

            PlayerLeft?.Invoke(joined.playerId);
            Log($"Player {joined.playerId} left lobby.");
        }

        void CycleCharacter(JoinedPlayer joined, int direction)
        {
            int selectionCount = GetSelectionCount();
            if (selectionCount <= 0)
                return;

            joined.selectionIndex = WrapIndex(joined.selectionIndex + direction, selectionCount);
            var selected = GetCharacterForSelection(joined.selectionIndex);
            CharacterChanged?.Invoke(joined.playerId, selected);
            Log($"Player {joined.playerId} selected {selected}.");
        }

        CharacterType.Character GetCharacterForSelection(int selectionIndex)
        {
            var order = GetSelectionOrder();
            int safeIndex = WrapIndex(selectionIndex, order.Count);
            return order[safeIndex];
        }

        IReadOnlyList<CharacterType.Character> GetSelectionOrder()
        {
            if (rosterConfig != null && rosterConfig.SelectionOrder != null && rosterConfig.SelectionOrder.Count > 0)
                return rosterConfig.SelectionOrder;

            return fallbackSelectionOrder;
        }

        int GetSelectionCount()
        {
            return GetSelectionOrder().Count;
        }

        GameObject ResolveCharacterPrefab(CharacterType.Character character)
        {
            if (rosterConfig != null && rosterConfig.TryGetPrefab(character, out var prefab))
                return prefab;

            switch (character)
            {
                case CharacterType.Character.Knight:
                    return knightPrefab;
                case CharacterType.Character.Ninja:
                    return ninjaPrefab;
                case CharacterType.Character.Sorcerer:
                    return sorcererPrefab;
                default:
                    return null;
            }
        }

        int GetNextAvailablePlayerId()
        {
            for (int id = 1; id <= FixedPlayers; id++)
            {
                bool occupied = false;
                for (int index = 0; index < joinedPlayers.Count; index++)
                {
                    if (joinedPlayers[index].playerId == id)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                    return id;
            }

            return -1;
        }

        bool IsKeyboardLayoutJoined(int layoutIndex)
        {
            for (int index = 0; index < joinedPlayers.Count; index++)
            {
                var joined = joinedPlayers[index];
                if (joined.source == ControlSource.Keyboard && joined.keyboardLayoutIndex == layoutIndex)
                    return true;
            }

            return false;
        }

        bool IsGamepadJoined(int deviceId)
        {
            for (int index = 0; index < joinedPlayers.Count; index++)
            {
                var joined = joinedPlayers[index];
                if (joined.source == ControlSource.Gamepad && joined.gamepadDeviceId == deviceId)
                    return true;
            }

            return false;
        }

        Gamepad FindGamepadByDeviceId(int deviceId)
        {
            var gamepads = Gamepad.all;
            for (int index = 0; index < gamepads.Count; index++)
            {
                var pad = gamepads[index];
                if (pad != null && pad.deviceId == deviceId)
                    return pad;
            }

            return null;
        }

        static Vector2 ReadMove(bool left, bool right, bool down, bool up)
        {
            float x = 0f;
            if (left) x -= 1f;
            if (right) x += 1f;

            float y = 0f;
            if (down) y -= 1f;
            if (up) y += 1f;

            return new Vector2(x, y);
        }

        static bool IsPressed(Keyboard keyboard, Key key)
        {
            return keyboard != null && key != Key.None && keyboard[key].isPressed;
        }

        static bool WasPressedThisFrame(Keyboard keyboard, Key key)
        {
            return keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame;
        }

        string GetKeyboardDisplayName(int layoutIndex)
        {
            if (keyboardLayouts == null || layoutIndex < 0 || layoutIndex >= keyboardLayouts.Length)
                return "Keyboard";

            string name = keyboardLayouts[layoutIndex].displayName;
            return string.IsNullOrEmpty(name) ? $"Keyboard {layoutIndex + 1}" : name;
        }

        public bool TryGetLobbyPlayerInfo(int playerId, out CharacterType.Character selectedCharacter, out string controlDisplayName)
        {
            for (int index = 0; index < joinedPlayers.Count; index++)
            {
                var joined = joinedPlayers[index];
                if (joined.playerId != playerId)
                    continue;

                selectedCharacter = GetCharacterForSelection(joined.selectionIndex);
                controlDisplayName = joined.source == ControlSource.Keyboard
                    ? GetKeyboardDisplayName(joined.keyboardLayoutIndex)
                    : FindGamepadByDeviceId(joined.gamepadDeviceId)?.displayName ?? "Gamepad";
                return true;
            }

            selectedCharacter = CharacterType.Character.Knight;
            controlDisplayName = string.Empty;
            return false;
        }

        static int WrapIndex(int value, int length)
        {
            if (length <= 0)
                return 0;

            int wrapped = value % length;
            if (wrapped < 0)
                wrapped += length;
            return wrapped;
        }

        void Log(string message)
        {
            if (verboseLogs)
                Debug.Log($"[LocalMultiplayerManager] {message}", this);
        }

        static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0)
                return;

            root.layer = layer;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null)
                    transforms[i].gameObject.layer = layer;
            }
        }

        void EnsureCoreSystems(GameObject playerObject)
        {
            if (playerObject == null)
                return;

            if (playerObject.GetComponent<PlayerHealth>() == null)
                playerObject.AddComponent<PlayerHealth>();

            if (playerObject.GetComponent<DeathSystem>() == null)
                playerObject.AddComponent<DeathSystem>();

            if (playerObject.GetComponent<SpecialAbilityController>() == null)
                playerObject.AddComponent<SpecialAbilityController>();

            if (autoAttachFallDetection && playerObject.GetComponent<FallDetection>() == null)
                playerObject.AddComponent<FallDetection>();
        }

        static void EnsureItemUpgradeBridge(GameObject playerObject)
        {
            if (playerObject == null)
                return;

            if (playerObject.GetComponent<PlayerUpgrade>() == null)
                playerObject.AddComponent<PlayerUpgrade>();

            if (playerObject.GetComponent<PlayerUpgradeBuffAdapter>() == null)
                playerObject.AddComponent<PlayerUpgradeBuffAdapter>();
        }

        static KeyboardLayout[] BuildDefaultKeyboardLayouts()
        {
            return new[]
            {
                new KeyboardLayout
                {
                    displayName = "Keyboard P1 (WASD + JKL)",
                    join = Key.F,
                    left = Key.A,
                    right = Key.D,
                    up = Key.W,
                    down = Key.S,
                    attack1 = Key.J,
                    attack2 = Key.K,
                    attack3 = Key.L,
                    attack4 = Key.U,
                    block = Key.S,
                    interact = Key.E,
                    crouch = Key.C,
                    sprint = Key.LeftShift,
                    previousCharacter = Key.Q,
                    nextCharacter = Key.E,
                    submit = Key.Enter,
                    cancel = Key.Backspace
                },
                new KeyboardLayout
                {
                    displayName = "Keyboard P2 (Arrows + Numpad)",
                    join = Key.Numpad0,
                    left = Key.LeftArrow,
                    right = Key.RightArrow,
                    up = Key.UpArrow,
                    down = Key.DownArrow,
                    attack1 = Key.Numpad1,
                    attack2 = Key.Numpad2,
                    attack3 = Key.Numpad3,
                    attack4 = Key.Numpad4,
                    block = Key.DownArrow,
                    interact = Key.NumpadEnter,
                    crouch = Key.RightCtrl,
                    sprint = Key.RightShift,
                    previousCharacter = Key.Numpad7,
                    nextCharacter = Key.Numpad9,
                    submit = Key.NumpadEnter,
                    cancel = Key.NumpadMinus
                }
            };
        }
    }
}
