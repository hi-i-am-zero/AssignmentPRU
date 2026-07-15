using SkyfallArena.GameFlow;
using SkyfallArena.Multiplayer;
using SkyfallArena.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaEnvironment = Environment;

public static class GameFlowSceneBuilder
{
    const string SystemsRootName = "Match Systems";
    const string TitleScenePath = "Assets/Scenes/Title.unity";
    const string SelectScenePath = "Assets/Scenes/Select.unity";

    static readonly string[] MapScenePaths =
    {
        "Assets/Scenes/Mountain.unity",
        "Assets/Scenes/Volcano.unity",
        "Assets/Scenes/Sky.unity",
        "Assets/Scenes/WeatherTest.unity",
        "Assets/Scenes/TestArena.unity"
    };

    [MenuItem("Skyfall Arena/Game Flow/Setup Title Select And Maps")]
    public static void SetupAll()
    {
        CreateMenuScene(TitleScenePath, "TitleRoot", typeof(TitleUI));
        CreateMenuScene(SelectScenePath, "SelectRoot", typeof(SelectUI));

        for (int i = 0; i < MapScenePaths.Length; i++)
            WireMapScene(MapScenePaths[i]);

        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameFlowSceneBuilder] Title/Select/maps/build settings setup completed.");
    }

    static void CreateMenuScene(string scenePath, string rootName, System.Type uiComponentType)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGo = new GameObject("Main Camera");
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.08f, 0.14f, 1f);
        camera.orthographic = true;
        cameraGo.tag = "MainCamera";
        cameraGo.AddComponent<AudioListener>();

        var root = new GameObject(rootName);
        root.AddComponent(uiComponentType);

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[GameFlowSceneBuilder] Created {scenePath}");
    }

    static void WireMapScene(string scenePath)
    {
        if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "..", scenePath)))
        {
            // Path relative check via AssetDatabase
        }

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

        var multiplayer = systemsRoot.GetComponent<LocalMultiplayerManager>();
        if (multiplayer == null)
            multiplayer = systemsRoot.AddComponent<LocalMultiplayerManager>();

        var matchResult = systemsRoot.GetComponent<MatchResultSystem>();
        if (matchResult == null)
            matchResult = systemsRoot.AddComponent<MatchResultSystem>();

        var hud = systemsRoot.GetComponent<MatchHud>();
        if (hud == null)
            hud = systemsRoot.AddComponent<MatchHud>();

        var bootstrap = systemsRoot.GetComponent<MatchBootstrap>();
        if (bootstrap == null)
            bootstrap = systemsRoot.AddComponent<MatchBootstrap>();

        var resultUi = systemsRoot.GetComponent<ResultUI>();
        if (resultUi == null)
            resultUi = systemsRoot.AddComponent<ResultUI>();

        var healthBars = systemsRoot.GetComponent<PlayerHealthBarsHud>();
        if (healthBars == null)
            healthBars = systemsRoot.AddComponent<PlayerHealthBarsHud>();

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

        var healthSerialized = new SerializedObject(healthBars);
        healthSerialized.FindProperty("multiplayerManager").objectReferenceValue = multiplayer;
        healthSerialized.FindProperty("matchResultSystem").objectReferenceValue = matchResult;
        healthSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[GameFlowSceneBuilder] Wired {scenePath}");
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

    static void ConfigureBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene(TitleScenePath, true),
            new EditorBuildSettingsScene(SelectScenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/Mountain.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Volcano.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Sky.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/WeatherTest.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/TestArena.unity", true)
        };

        EditorBuildSettings.scenes = scenes;
        Debug.Log("[GameFlowSceneBuilder] Build settings updated (Title first).");
    }
}
