using System.IO;
using SkyfallArena.GameFlow;
using SkyfallArena.Multiplayer;
using SkyfallArena.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ArenaEnvironment = Environment;

public static class GameFlowSceneBuilder
{
    const string SystemsRootName = "Match Systems";
    const string TitleScenePath = "Assets/Scenes/Title.unity";
    const string SelectScenePath = "Assets/Scenes/Select.unity";
    const string MountainScenePath = "Assets/Scenes/Mountain.unity";
    const string UiSpritePath = "Assets/Art/UI/WhitePixel.png";
    const string SessionKeyMenuUiReady = "SkyfallArena.MenuUiReady.v2";
    const string SessionKeyHealthBarsReady = "SkyfallArena.HealthBarsUiReady.v4";
    const string HealthBarsCanvasName = "HealthBarsCanvas";

    static readonly string[] MapScenePaths =
    {
        MountainScenePath,
        "Assets/Scenes/Volcano.unity",
        "Assets/Scenes/Sky.unity",
        "Assets/Scenes/Weather.unity",
        "Assets/Scenes/TestArena.unity"
    };

    [InitializeOnLoadMethod]
    static void AutoBuildMenuScenesWhenMissingUi()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!SessionState.GetBool(SessionKeyMenuUiReady, false))
            {
                bool titleReady = File.Exists(TitleScenePath) && File.ReadAllText(TitleScenePath).Contains("TitleCanvas");
                bool selectReady = File.Exists(SelectScenePath) && File.ReadAllText(SelectScenePath).Contains("SelectCanvas");
                if (!titleReady || !selectReady)
                    RebuildMenuScenesOnly();
                SessionState.SetBool(SessionKeyMenuUiReady, true);
            }

            const string healthBarsFlag = "Assets/Editor/REBUILD_HEALTH_BARS.flag";
            bool forceHealthBars = File.Exists(healthBarsFlag);
            if (forceHealthBars)
            {
                File.Delete(healthBarsFlag);
                if (File.Exists(healthBarsFlag + ".meta"))
                    File.Delete(healthBarsFlag + ".meta");
            }

            if (forceHealthBars || !SessionState.GetBool(SessionKeyHealthBarsReady, false))
            {
                bool mountainReady = File.Exists(MountainScenePath)
                    && File.ReadAllText(MountainScenePath).Contains(HealthBarsCanvasName);
                if (forceHealthBars || !mountainReady)
                    RebuildHealthBarsOnMaps();

                mountainReady = File.Exists(MountainScenePath)
                    && File.ReadAllText(MountainScenePath).Contains(HealthBarsCanvasName);
                if (mountainReady)
                    SessionState.SetBool(SessionKeyHealthBarsReady, true);
            }
        };
    }

    static readonly string[] CharacterLabels = { "Knight", "Ninja", "Sorcerer" };
    static readonly string[] MapLabels = { "Mountain", "Volcano", "Sky", "Weather", "TestArena" };

    [MenuItem("Skyfall Arena/Game Flow/Setup Title Select And Maps")]
    public static void SetupAll()
    {
        EnsureUiSprite();
        BuildTitleScene();
        BuildSelectScene();

        for (int i = 0; i < MapScenePaths.Length; i++)
            WireMapScene(MapScenePaths[i], rebuildHealthBars: true);

        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SessionState.SetBool(SessionKeyHealthBarsReady, true);
        Debug.Log("[GameFlowSceneBuilder] Title/Select scene UI + maps/build settings setup completed.");
    }

    [MenuItem("Skyfall Arena/Game Flow/Rebuild Title And Select UI")]
    public static void RebuildMenuScenesOnly()
    {
        EnsureUiSprite();
        BuildTitleScene();
        BuildSelectScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameFlowSceneBuilder] Title/Select UI rebuilt.");
    }

    [MenuItem("Skyfall Arena/Game Flow/Rebuild Health Bars On Maps")]
    public static void RebuildHealthBarsOnMaps()
    {
        EnsureUiSprite();
        for (int i = 0; i < MapScenePaths.Length; i++)
            WireMapScene(MapScenePaths[i], rebuildHealthBars: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SessionState.SetBool(SessionKeyHealthBarsReady, true);
        Debug.Log("[GameFlowSceneBuilder] Health bars UI rebuilt on all maps.");
    }

    static void BuildTitleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.05f, 0.08f, 0.14f, 1f));
        EnsureEventSystem();

        var root = new GameObject("TitleRoot");
        var titleUi = root.AddComponent<TitleUI>();

        var canvas = CreateCanvas("TitleCanvas", root.transform);
        CreatePanel(canvas.transform, "Background", new Color(0.05f, 0.08f, 0.14f, 1f), Vector2.zero, Vector2.one);

        var title = CreateText(canvas.transform, "Title", "Skyfall Arena", 72, TextAnchor.MiddleCenter, Color.white);
        Stretch(title.rectTransform, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.85f));

        var subtitle = CreateText(
            canvas.transform,
            "Subtitle",
            "Local 1v1 Platform Fighter",
            28,
            TextAnchor.MiddleCenter,
            new Color(0.75f, 0.8f, 0.9f, 1f));
        Stretch(subtitle.rectTransform, new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.55f));

        var playButton = CreateButton(
            canvas.transform,
            "PlayButton",
            "Play",
            new Color(0.18f, 0.42f, 0.72f, 1f));
        Stretch(playButton.GetComponent<RectTransform>(), new Vector2(0.38f, 0.22f), new Vector2(0.62f, 0.34f));

        var so = new SerializedObject(titleUi);
        so.FindProperty("playButton").objectReferenceValue = playButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, TitleScenePath);
        Debug.Log($"[GameFlowSceneBuilder] Built {TitleScenePath}");
    }

    static void BuildSelectScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.06f, 0.07f, 0.1f, 1f));
        EnsureEventSystem();

        var root = new GameObject("SelectRoot");
        var selectUi = root.AddComponent<SelectUI>();

        var canvas = CreateCanvas("SelectCanvas", root.transform);
        CreatePanel(canvas.transform, "Background", new Color(0.06f, 0.07f, 0.1f, 1f), Vector2.zero, Vector2.one);

        var header = CreateText(canvas.transform, "Header", "Select Fighters & Arena", 40, TextAnchor.MiddleCenter, Color.white);
        Stretch(header.rectTransform, new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.98f));

        var p1Buttons = BuildCharacterColumn(
            canvas.transform,
            "P1Column",
            "Player 1",
            new Vector2(0.03f, 0.18f),
            new Vector2(0.30f, 0.85f),
            new Color(0.12f, 0.14f, 0.2f, 0.9f));

        var mapButtonList = BuildMapColumn(
            canvas.transform,
            new Vector2(0.34f, 0.18f),
            new Vector2(0.66f, 0.85f));

        var p2Buttons = BuildCharacterColumn(
            canvas.transform,
            "P2Column",
            "Player 2",
            new Vector2(0.70f, 0.18f),
            new Vector2(0.97f, 0.85f),
            new Color(0.12f, 0.14f, 0.2f, 0.9f));

        var backButton = CreateButton(canvas.transform, "BackButton", "Back", new Color(0.18f, 0.42f, 0.72f, 1f));
        Stretch(backButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.03f), new Vector2(0.42f, 0.12f));

        var fightButton = CreateButton(canvas.transform, "FightButton", "Fight", new Color(0.35f, 0.35f, 0.38f, 1f));
        Stretch(fightButton.GetComponent<RectTransform>(), new Vector2(0.44f, 0.02f), new Vector2(0.72f, 0.14f));
        fightButton.interactable = false;

        var so = new SerializedObject(selectUi);
        so.FindProperty("backButton").objectReferenceValue = backButton;
        so.FindProperty("fightButton").objectReferenceValue = fightButton;
        AssignButtonArray(so, "p1CharacterButtons", p1Buttons);
        AssignButtonArray(so, "p2CharacterButtons", p2Buttons);
        AssignButtonArray(so, "mapButtons", mapButtonList);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, SelectScenePath);
        Debug.Log($"[GameFlowSceneBuilder] Built {SelectScenePath}");
    }

    static Button[] BuildCharacterColumn(
        Transform parent,
        string name,
        string title,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color panelColor)
    {
        var panel = CreatePanel(parent, name, panelColor, anchorMin, anchorMax);
        var titleText = CreateText(panel.transform, "Title", title, 30, TextAnchor.UpperCenter, Color.white);
        Stretch(titleText.rectTransform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.98f));

        var list = CreateVerticalList(panel.transform, "List", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.78f), 12f);
        var buttons = new Button[CharacterLabels.Length];
        for (int i = 0; i < CharacterLabels.Length; i++)
            buttons[i] = CreateSelectableButton(list, CharacterLabels[i], CharacterLabels[i]);
        return buttons;
    }

    static Button[] BuildMapColumn(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var panel = CreatePanel(parent, "MapColumn", new Color(0.1f, 0.16f, 0.18f, 0.9f), anchorMin, anchorMax);
        var titleText = CreateText(panel.transform, "Title", "Map", 30, TextAnchor.UpperCenter, Color.white);
        Stretch(titleText.rectTransform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.98f));

        var list = CreateVerticalList(panel.transform, "List", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.78f), 10f);
        var buttons = new Button[MapLabels.Length];
        for (int i = 0; i < MapLabels.Length; i++)
            buttons[i] = CreateSelectableButton(list, MapLabels[i], MapLabels[i]);
        return buttons;
    }

    static Transform CreateVerticalList(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float spacing)
    {
        var listGo = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        listGo.transform.SetParent(parent, false);
        Stretch(listGo.GetComponent<RectTransform>(), anchorMin, anchorMax);

        var layout = listGo.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        return listGo.transform;
    }

    static void WireMapScene(string scenePath, bool rebuildHealthBars = false)
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset == null)
        {
            Debug.LogWarning($"[GameFlowSceneBuilder] Missing scene: {scenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            return;

        var arenaManager = Object.FindFirstObjectByType<ArenaEnvironment.ArenaManager>();

        var systemsRoot = GameObject.Find(SystemsRootName);
        if (systemsRoot == null)
            systemsRoot = new GameObject(SystemsRootName);

        var multiplayer = systemsRoot.GetComponent<LocalMultiplayerManager>() ?? systemsRoot.AddComponent<LocalMultiplayerManager>();
        var matchResult = systemsRoot.GetComponent<MatchResultSystem>() ?? systemsRoot.AddComponent<MatchResultSystem>();
        var hud = systemsRoot.GetComponent<MatchHud>() ?? systemsRoot.AddComponent<MatchHud>();
        var bootstrap = systemsRoot.GetComponent<MatchBootstrap>() ?? systemsRoot.AddComponent<MatchBootstrap>();
        var resultUi = systemsRoot.GetComponent<ResultUI>() ?? systemsRoot.AddComponent<ResultUI>();
        var healthBars = systemsRoot.GetComponent<PlayerHealthBarsHud>() ?? systemsRoot.AddComponent<PlayerHealthBarsHud>();

        var multiplayerSerialized = new SerializedObject(multiplayer);
        if (arenaManager != null)
            multiplayerSerialized.FindProperty("arenaManager").objectReferenceValue = arenaManager;
        AssignPrefab(multiplayerSerialized, "knightPrefab", "Assets/Prefabs/Knight.prefab");
        AssignPrefab(multiplayerSerialized, "ninjaPrefab", "Assets/Prefabs/Ninja.prefab");
        AssignPrefab(multiplayerSerialized, "sorcererPrefab", "Assets/Prefabs/Sorcerer.prefab");
        multiplayerSerialized.FindProperty("autoStartWhenBothPlayersJoined").boolValue = true;
        multiplayerSerialized.FindProperty("enableInArenaLobby").boolValue = false;
        multiplayerSerialized.FindProperty("verboseLogs").boolValue = false;
        multiplayerSerialized.ApplyModifiedPropertiesWithoutUndo();

        var matchSerialized = new SerializedObject(matchResult);
        matchSerialized.FindProperty("localMultiplayerManager").objectReferenceValue = multiplayer;
        matchSerialized.FindProperty("autoStartTrackingOnMatchStart").boolValue = true;
        matchSerialized.FindProperty("evaluateAsSoonAsPlayerEliminated").boolValue = true;
        matchSerialized.ApplyModifiedPropertiesWithoutUndo();

        var hudSerialized = new SerializedObject(hud);
        hudSerialized.FindProperty("multiplayerManager").objectReferenceValue = multiplayer;
        hudSerialized.FindProperty("matchResultSystem").objectReferenceValue = matchResult;
        hudSerialized.ApplyModifiedPropertiesWithoutUndo();

        var bootstrapSerialized = new SerializedObject(bootstrap);
        bootstrapSerialized.FindProperty("multiplayerManager").objectReferenceValue = multiplayer;
        bootstrapSerialized.FindProperty("useEditorFallbackWhenNoSession").boolValue = true;
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

        var resultSerialized = new SerializedObject(resultUi);
        resultSerialized.FindProperty("matchResultSystem").objectReferenceValue = matchResult;
        resultSerialized.FindProperty("multiplayerManager").objectReferenceValue = multiplayer;
        resultSerialized.ApplyModifiedPropertiesWithoutUndo();

        bool needsHealthBars = rebuildHealthBars
            || systemsRoot.transform.Find(HealthBarsCanvasName) == null;
        if (needsHealthBars)
            BuildHealthBarsUi(systemsRoot, healthBars, multiplayer, matchResult);
        else
        {
            var healthSerialized = new SerializedObject(healthBars);
            healthSerialized.FindProperty("multiplayerManager").objectReferenceValue = multiplayer;
            healthSerialized.FindProperty("matchResultSystem").objectReferenceValue = matchResult;
            healthSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[GameFlowSceneBuilder] Wired {scenePath}");
    }

    static void BuildHealthBarsUi(
        GameObject systemsRoot,
        PlayerHealthBarsHud healthBars,
        LocalMultiplayerManager multiplayer,
        MatchResultSystem matchResult)
    {
        EnsureUiSprite();
        EnsureEventSystem();

        var existing = systemsRoot.transform.Find(HealthBarsCanvasName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var canvas = CreateCanvas(HealthBarsCanvasName, systemsRoot.transform);
        canvas.sortingOrder = 50;

        var p1 = CreateHealthBarPanel(
            canvas.transform,
            "P1Health",
            isLeft: true,
            fillColor: Color.red,
            out var p1Fill,
            out var p1Chip,
            out var p1Label,
            out var p1Value,
            out var p1MpSegments,
            out var p1MpLabel);

        var p2 = CreateHealthBarPanel(
            canvas.transform,
            "P2Health",
            isLeft: false,
            fillColor: new Color(0.2f, 0.55f, 1f, 1f),
            out var p2Fill,
            out var p2Chip,
            out var p2Label,
            out var p2Value,
            out var p2MpSegments,
            out var p2MpLabel);

        var so = new SerializedObject(healthBars);
        so.FindProperty("hudRoot").objectReferenceValue = canvas.gameObject;
        so.FindProperty("multiplayerManager").objectReferenceValue = multiplayer;
        so.FindProperty("matchResultSystem").objectReferenceValue = matchResult;
        AssignBarBindings(so.FindProperty("player1Bar"), p1Fill, p1Chip, p1Label, p1Value, fillFromLeft: true);
        AssignBarBindings(so.FindProperty("player2Bar"), p2Fill, p2Chip, p2Label, p2Value, fillFromLeft: false);
        AssignMeterBindings(
            so.FindProperty("player1MpBar"),
            p1MpSegments,
            p1MpLabel,
            emptyColor: new Color(0.15f, 0.15f, 0.18f, 0.95f),
            filledColor: new Color(0.35f, 0.75f, 1f, 1f),
            readyColor: new Color(1f, 0.9f, 0.35f, 1f));
        AssignMeterBindings(
            so.FindProperty("player2MpBar"),
            p2MpSegments,
            p2MpLabel,
            emptyColor: new Color(0.15f, 0.15f, 0.18f, 0.95f),
            filledColor: new Color(0.95f, 0.7f, 0.25f, 1f),
            readyColor: new Color(1f, 0.9f, 0.35f, 1f));
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(healthBars);
        EditorUtility.SetDirty(p1);
        EditorUtility.SetDirty(p2);
    }

    static GameObject CreateHealthBarPanel(
        Transform parent,
        string name,
        bool isLeft,
        Color fillColor,
        out RectTransform fillRect,
        out RectTransform chipRect,
        out Text label,
        out Text value,
        out Image[] mpSegments,
        out Text mpLabel)
    {
        var panel = CreatePanel(
            parent,
            name,
            new Color(0f, 0f, 0f, 0.55f),
            isLeft ? new Vector2(0.02f, 0.84f) : new Vector2(0.62f, 0.84f),
            isLeft ? new Vector2(0.38f, 0.98f) : new Vector2(0.98f, 0.98f));

        label = CreateText(
            panel.transform,
            "Label",
            isLeft ? "P1" : "P2",
            20,
            isLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight,
            Color.white);
        Stretch(label.rectTransform, new Vector2(0.03f, 0.78f), new Vector2(0.72f, 0.98f));

        mpLabel = CreateText(
            panel.transform,
            "MpLabel",
            "MP",
            14,
            isLeft ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
            new Color(0.85f, 0.9f, 1f, 0.95f));
        Stretch(mpLabel.rectTransform, new Vector2(0.72f, 0.78f), new Vector2(0.97f, 0.98f));

        var track = CreatePanel(
            panel.transform,
            "Track",
            new Color(0.15f, 0.15f, 0.18f, 0.95f),
            new Vector2(0.03f, 0.42f),
            new Vector2(0.97f, 0.74f));

        var chipImage = CreatePanel(track.transform, "Chip", new Color(1f, 0.85f, 0.2f, 0.95f), Vector2.zero, Vector2.one);
        chipImage.raycastTarget = false;
        chipRect = chipImage.rectTransform;

        var fillImage = CreatePanel(track.transform, "Fill", fillColor, Vector2.zero, Vector2.one);
        fillImage.raycastTarget = false;
        fillRect = fillImage.rectTransform;

        value = CreateText(panel.transform, "Value", "100 / 100", 16, TextAnchor.MiddleCenter, Color.white);
        Stretch(value.rectTransform, new Vector2(0.03f, 0.42f), new Vector2(0.97f, 0.74f));

        var mpRow = CreatePanel(
            panel.transform,
            "MpRow",
            new Color(0f, 0f, 0f, 0f),
            new Vector2(0.03f, 0.08f),
            new Vector2(0.97f, 0.34f));
        mpRow.raycastTarget = false;

        mpSegments = new Image[3];
        const float gap = 0.04f;
        float slot = (1f - gap * 2f) / 3f;
        for (int i = 0; i < 3; i++)
        {
            float xMin = i * (slot + gap);
            float xMax = xMin + slot;

            var frame = CreatePanel(
                mpRow.transform,
                $"MpSlot_{i + 1}",
                new Color(0.08f, 0.08f, 0.1f, 0.95f),
                new Vector2(xMin, 0.05f),
                new Vector2(xMax, 0.95f));
            frame.raycastTarget = false;

            var segment = CreatePanel(
                frame.transform,
                "Fill",
                new Color(0.15f, 0.15f, 0.18f, 0.95f),
                new Vector2(0.12f, 0.18f),
                new Vector2(0.88f, 0.82f));
            segment.raycastTarget = false;
            mpSegments[i] = segment;
        }

        return panel.gameObject;
    }

    static void AssignBarBindings(
        SerializedProperty barProp,
        RectTransform fillRect,
        RectTransform chipRect,
        Text label,
        Text value,
        bool fillFromLeft)
    {
        if (barProp == null)
            return;

        barProp.FindPropertyRelative("fillRect").objectReferenceValue = fillRect;
        barProp.FindPropertyRelative("chipRect").objectReferenceValue = chipRect;
        barProp.FindPropertyRelative("fillImage").objectReferenceValue = fillRect != null
            ? fillRect.GetComponent<Image>()
            : null;
        barProp.FindPropertyRelative("chipImage").objectReferenceValue = chipRect != null
            ? chipRect.GetComponent<Image>()
            : null;
        barProp.FindPropertyRelative("label").objectReferenceValue = label;
        barProp.FindPropertyRelative("value").objectReferenceValue = value;
        barProp.FindPropertyRelative("fillFromLeft").boolValue = fillFromLeft;
    }

    static void AssignMeterBindings(
        SerializedProperty meterProp,
        Image[] segments,
        Text label,
        Color emptyColor,
        Color filledColor,
        Color readyColor)
    {
        if (meterProp == null)
            return;

        var segmentsProp = meterProp.FindPropertyRelative("segments");
        if (segmentsProp != null && segmentsProp.isArray)
        {
            int count = segments != null ? segments.Length : 0;
            segmentsProp.arraySize = count;
            for (int i = 0; i < count; i++)
                segmentsProp.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
        }

        meterProp.FindPropertyRelative("label").objectReferenceValue = label;
        meterProp.FindPropertyRelative("emptyColor").colorValue = emptyColor;
        meterProp.FindPropertyRelative("filledColor").colorValue = filledColor;
        meterProp.FindPropertyRelative("readyColor").colorValue = readyColor;
    }

    static void AssignPrefab(SerializedObject serializedObject, string propertyName, string prefabPath)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
            property.objectReferenceValue = prefab;
    }

    static void AssignButtonArray(SerializedObject serializedObject, string propertyName, Button[] buttons)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
            return;

        property.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
    }

    static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TitleScenePath, true),
            new EditorBuildSettingsScene(SelectScenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/Mountain.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Volcano.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Sky.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Weather.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/TestArena.unity", true)
        };
    }

    static void CreateCamera(Color background)
    {
        var cameraGo = new GameObject("Main Camera");
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.orthographic = true;
        cameraGo.tag = "MainCamera";
        cameraGo.AddComponent<AudioListener>();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    static Canvas CreateCanvas(string name, Transform parent)
    {
        var canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(parent, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static Image CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        var image = go.GetComponent<Image>();
        image.sprite = LoadUiSprite();
        image.type = Image.Type.Simple;
        image.color = color;
        return image;
    }

    static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static Button CreateButton(Transform parent, string name, string label, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.sprite = LoadUiSprite();
        image.type = Image.Type.Simple;
        image.color = color;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        CreateText(go.transform, "Label", label, 28, TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    static Button CreateSelectableButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var layout = go.GetComponent<LayoutElement>();
        layout.minHeight = 48f;
        layout.preferredHeight = 52f;

        var image = go.GetComponent<Image>();
        image.sprite = LoadUiSprite();
        image.type = Image.Type.Simple;
        image.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        CreateText(go.transform, "Label", label, 22, TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    static Sprite LoadUiSprite()
    {
        EnsureUiSprite();
        return AssetDatabase.LoadAssetAtPath<Sprite>(UiSpritePath);
    }

    static void EnsureUiSprite()
    {
        if (AssetDatabase.LoadAssetAtPath<Sprite>(UiSpritePath) != null)
            return;

        const string folder = "Assets/Art/UI";
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Art", "UI");

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(Application.dataPath, "Art/UI/WhitePixel.png"),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(UiSpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(UiSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
    }
}
