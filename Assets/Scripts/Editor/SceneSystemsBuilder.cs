using SkyfallArena.GameFlow;
using SkyfallArena.Multiplayer;
using SkyfallArena.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ArenaEnvironment = Environment;

static class SceneSystemsBuilder
{
    const string SystemsRootName = "Match Systems";

    [MenuItem("Skyfall Arena/Systems/Setup Match Systems (All Maps)")]
    public static void SetupMatchSystemsAllMaps()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        for (int index = 0; index < sceneGuids.Length; index++)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[index]);
            if (string.IsNullOrEmpty(scenePath) || scenePath.Contains("SampleScene"))
                continue;

            if (scenePath.EndsWith("/Title.unity") || scenePath.EndsWith("/Select.unity"))
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            SetupCurrentScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SceneSystemsBuilder] Wired match systems in {scenePath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SceneSystemsBuilder] Match systems setup completed.");
    }

    static void SetupCurrentScene()
    {
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
        var lobbyProp = multiplayerSerialized.FindProperty("enableInArenaLobby");
        if (lobbyProp != null)
            lobbyProp.boolValue = false;
        multiplayerSerialized.FindProperty("verboseLogs").boolValue = true;
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

        // Health bar UI (scene GameObjects) — rebuild via Game Flow menu if missing.
        if (systemsRoot.transform.Find("HealthBarsCanvas") == null)
            Debug.LogWarning("[SceneSystemsBuilder] HealthBarsCanvas missing. Run Skyfall Arena > Game Flow > Rebuild Health Bars On Maps.");

        var healthSerialized = new SerializedObject(healthBars);
        healthSerialized.FindProperty("multiplayerManager").objectReferenceValue = multiplayer;
        healthSerialized.FindProperty("matchResultSystem").objectReferenceValue = matchResult;
        healthSerialized.ApplyModifiedPropertiesWithoutUndo();

        DisableScenePlacedPlayers();
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

    static void DisableScenePlacedPlayers()
    {
        var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int index = 0; index < players.Length; index++)
        {
            var player = players[index];
            if (player == null)
                continue;

            if (player.GetComponent<LocalPlayerInputSource>() != null)
                continue;

            player.gameObject.SetActive(false);
            EditorUtility.SetDirty(player.gameObject);
        }
    }
}
