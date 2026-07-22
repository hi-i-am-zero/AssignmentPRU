using System.Collections.Generic;
using SkyfallArena.Systems.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaEnvironment = Environment;
using SystemsItemDefinition = SkyfallArena.Systems.Items.ItemDefinition;
using SystemsItemLevelManager = SkyfallArena.Systems.Items.ItemLevelManager;
using SystemsItemPickup = SkyfallArena.Systems.Items.ItemPickup;
using SystemsItemSpawner = SkyfallArena.Systems.Items.ItemSpawner;
using ItemKind = SkyfallArena.Systems.Items.ItemType;

static class ItemSystemBuilder
{
    const string IconsFolder = "Assets/Sprite/Items";
    const string ItemsRoot = "Assets/Items";
    const string DefinitionsFolder = "Assets/Items/Definitions";
    const string PrefabFolder = "Assets/Prefabs/Items";
    const string PickupPrefabPath = "Assets/Prefabs/Items/ItemPickup.prefab";
    const string AttackPickupPrefabPath = "Assets/Prefabs/Items/ItemPickup_Attack.prefab";
    const string JumpPickupPrefabPath = "Assets/Prefabs/Items/ItemPickup_Jump.prefab";
    const string ShieldPickupPrefabPath = "Assets/Prefabs/Items/ItemPickup_Shield.prefab";

    [MenuItem("Skyfall Arena/Items/Setup Item System (All Maps)")]
    public static void SetupItemSystemAllMaps()
    {
        EnsureFolder("Assets", "Items");
        EnsureFolder(ItemsRoot, "Definitions");
        EnsureFolder("Assets/Prefabs", "Items");

        var spritesByType = LoadItemSprites();
        var pickupPrefab = EnsurePickupPrefabTemplate(spritesByType);
        var definitions = EnsureItemDefinitions(spritesByType, pickupPrefab);
        var splitPrefabs = EnsureSplitPickupPrefabs(spritesByType);
        ApplySplitPickupOverrides(definitions, splitPrefabs);
        var defaultPickupPrefab = splitPrefabs.TryGetValue(ItemKind.AttackChain, out var attackPrefab)
            ? attackPrefab
            : pickupPrefab;

        EnsureCharacterPrefabsHaveUpgradeBridge();
        SetupAllScenes(definitions, defaultPickupPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ItemSystemBuilder] Item system setup completed for all maps.");
    }

    static Dictionary<ItemKind, Sprite> LoadItemSprites()
    {
        var map = new Dictionary<ItemKind, Sprite>
        {
            [ItemKind.AttackChain] = LoadAndConfigureSprite($"{IconsFolder}/SkillAttack.png"),
            [ItemKind.JumpBoost] = LoadAndConfigureSprite($"{IconsFolder}/SkillJump.png"),
            [ItemKind.ShieldWall] = LoadAndConfigureSprite($"{IconsFolder}/SkillShield.png")
        };

        return map;
    }

    static Sprite LoadAndConfigureSprite(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
        {
            Debug.LogWarning($"[ItemSystemBuilder] Missing icon: {path}");
            return null;
        }

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static SystemsItemPickup EnsurePickupPrefabTemplate(Dictionary<ItemKind, Sprite> spritesByType)
    {
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
        bool loadedFromPrefab = existingPrefab != null;
        GameObject root = loadedFromPrefab
            ? PrefabUtility.LoadPrefabContents(PickupPrefabPath)
            : new GameObject("ItemPickup");

        try
        {
            root.name = "ItemPickup";
            int itemLayer = LayerMask.NameToLayer(ArenaEnvironment.GameLayers.Item);
            if (itemLayer >= 0)
                root.layer = itemLayer;

            var spriteRenderer = root.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = spritesByType.TryGetValue(ItemKind.AttackChain, out var attackIcon) ? attackIcon : null;
            spriteRenderer.color = Color.white;

            var collider = root.GetComponent<CircleCollider2D>();
            if (collider == null)
                collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;

            var pickup = root.GetComponent<SystemsItemPickup>();
            if (pickup == null)
                pickup = root.AddComponent<SystemsItemPickup>();

            var serializedPickup = new SerializedObject(pickup);
            serializedPickup.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            serializedPickup.FindProperty("pickupCollider").objectReferenceValue = collider;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();

            if (existingPrefab != null)
                PrefabUtility.SaveAsPrefabAsset(root, PickupPrefabPath);
            else
                PrefabUtility.SaveAsPrefabAsset(root, PickupPrefabPath);
        }
        finally
        {
            if (root != null)
            {
                if (loadedFromPrefab)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    Object.DestroyImmediate(root);
            }
        }

        return AssetDatabase.LoadAssetAtPath<SystemsItemPickup>(PickupPrefabPath);
    }

    static Dictionary<ItemKind, SystemsItemPickup> EnsureSplitPickupPrefabs(Dictionary<ItemKind, Sprite> spritesByType)
    {
        var map = new Dictionary<ItemKind, SystemsItemPickup>
        {
            [ItemKind.AttackChain] = EnsureSplitPickupPrefab(
                AttackPickupPrefabPath,
                "ItemPickup_Attack",
                spritesByType.TryGetValue(ItemKind.AttackChain, out var attackSprite) ? attackSprite : null),
            [ItemKind.JumpBoost] = EnsureSplitPickupPrefab(
                JumpPickupPrefabPath,
                "ItemPickup_Jump",
                spritesByType.TryGetValue(ItemKind.JumpBoost, out var jumpSprite) ? jumpSprite : null),
            [ItemKind.ShieldWall] = EnsureSplitPickupPrefab(
                ShieldPickupPrefabPath,
                "ItemPickup_Shield",
                spritesByType.TryGetValue(ItemKind.ShieldWall, out var shieldSprite) ? shieldSprite : null)
        };

        return map;
    }

    static SystemsItemPickup EnsureSplitPickupPrefab(string prefabPath, string prefabName, Sprite iconSprite)
    {
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        bool loadedFromPrefab = existingPrefab != null;
        GameObject root = loadedFromPrefab
            ? PrefabUtility.LoadPrefabContents(prefabPath)
            : new GameObject(prefabName);

        try
        {
            root.name = prefabName;
            int itemLayer = LayerMask.NameToLayer(ArenaEnvironment.GameLayers.Item);
            if (itemLayer >= 0)
                root.layer = itemLayer;

            var spriteRenderer = root.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = iconSprite;
            spriteRenderer.color = Color.white;

            var collider = root.GetComponent<CircleCollider2D>();
            if (collider == null)
                collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;

            var pickup = root.GetComponent<SystemsItemPickup>();
            if (pickup == null)
                pickup = root.AddComponent<SystemsItemPickup>();

            var serializedPickup = new SerializedObject(pickup);
            serializedPickup.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            serializedPickup.FindProperty("pickupCollider").objectReferenceValue = collider;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            if (root != null)
            {
                if (loadedFromPrefab)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    Object.DestroyImmediate(root);
            }
        }

        return AssetDatabase.LoadAssetAtPath<SystemsItemPickup>(prefabPath);
    }

    static void ApplySplitPickupOverrides(
        SystemsItemDefinition[] definitions,
        Dictionary<ItemKind, SystemsItemPickup> splitPrefabs)
    {
        if (definitions == null || splitPrefabs == null)
            return;

        for (int index = 0; index < definitions.Length; index++)
        {
            var definition = definitions[index];
            if (definition == null)
                continue;

            var serializedDefinition = new SerializedObject(definition);
            var itemTypeProperty = serializedDefinition.FindProperty("itemType");
            ItemKind itemType = (ItemKind)itemTypeProperty.enumValueIndex;

            if (!splitPrefabs.TryGetValue(itemType, out var splitPrefab) || splitPrefab == null)
                continue;

            serializedDefinition.FindProperty("pickupPrefabOverride").objectReferenceValue = splitPrefab;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);

            var serializedPickup = new SerializedObject(splitPrefab);
            serializedPickup.FindProperty("itemDefinition").objectReferenceValue = definition;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(splitPrefab);
        }
    }

    static SystemsItemDefinition[] EnsureItemDefinitions(Dictionary<ItemKind, Sprite> spritesByType, SystemsItemPickup pickupPrefab)
    {
        var definitions = new List<SystemsItemDefinition>();

        definitions.Add(EnsureDefinition(
            itemType: ItemKind.JumpBoost,
            fileName: "Item_JumpBoost.asset",
            displayName: "Jump Boost",
            worldSprite: spritesByType.TryGetValue(ItemKind.JumpBoost, out var jumpIcon) ? jumpIcon : null,
            pickupPrefab: pickupPrefab,
            levelDescriptions: new[]
            {
                "Lv1: +2 Jump Force",
                "Lv2: +4 Jump Force",
                "Lv3: +6 Jump Force"
            }));

        definitions.Add(EnsureDefinition(
            itemType: ItemKind.AttackChain,
            fileName: "Item_AttackChain.asset",
            displayName: "Attack Chain",
            worldSprite: spritesByType.TryGetValue(ItemKind.AttackChain, out var attackIcon) ? attackIcon : null,
            pickupPrefab: pickupPrefab,
            levelDescriptions: new[]
            {
                "Lv1: +5 Damage",
                "Lv2: +10 Damage",
                "Lv3: +15 Damage"
            }));

        definitions.Add(EnsureDefinition(
            itemType: ItemKind.ShieldWall,
            fileName: "Item_ShieldWall.asset",
            displayName: "Shield Wall",
            worldSprite: spritesByType.TryGetValue(ItemKind.ShieldWall, out var shieldIcon) ? shieldIcon : null,
            pickupPrefab: pickupPrefab,
            levelDescriptions: new[]
            {
                "Lv1: 10% Damage Reduction",
                "Lv2: 20% Damage Reduction",
                "Lv3: 30% Damage Reduction"
            }));

        return definitions.ToArray();
    }

    static SystemsItemDefinition EnsureDefinition(
        ItemKind itemType,
        string fileName,
        string displayName,
        Sprite worldSprite,
        SystemsItemPickup pickupPrefab,
        string[] levelDescriptions)
    {
        string path = $"{DefinitionsFolder}/{fileName}";
        var definition = AssetDatabase.LoadAssetAtPath<SystemsItemDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<SystemsItemDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        var serialized = new SerializedObject(definition);
        serialized.FindProperty("itemType").enumValueIndex = (int)itemType;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("maxLevel").intValue = 3;
        serialized.FindProperty("worldSprite").objectReferenceValue = worldSprite;
        serialized.FindProperty("tintColor").colorValue = Color.white;
        serialized.FindProperty("pickupPrefabOverride").objectReferenceValue = pickupPrefab;

        var levelData = serialized.FindProperty("levelData");
        levelData.arraySize = levelDescriptions != null ? levelDescriptions.Length : 0;
        for (int index = 0; index < levelData.arraySize; index++)
        {
            var entry = levelData.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("level").intValue = index + 1;
            entry.FindPropertyRelative("description").stringValue = levelDescriptions[index];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    static void SetupAllScenes(SystemsItemDefinition[] definitions, SystemsItemPickup pickupPrefab)
    {
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        for (int index = 0; index < scenePaths.Length; index++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(scenePaths[index]);
            if (string.IsNullOrEmpty(assetPath))
                continue;

            var scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            var arenaManager = Object.FindFirstObjectByType<ArenaEnvironment.ArenaManager>();
            if (arenaManager == null)
                continue;

            var itemRoot = GameObject.Find("Item Systems");
            if (itemRoot == null)
                itemRoot = new GameObject("Item Systems");

            var runtimeRoot = itemRoot.transform.Find("RuntimeItems");
            if (runtimeRoot == null)
            {
                var runtimeGo = new GameObject("RuntimeItems");
                runtimeRoot = runtimeGo.transform;
                runtimeRoot.SetParent(itemRoot.transform, worldPositionStays: false);
            }

            var levelManager = itemRoot.GetComponent<SystemsItemLevelManager>();
            if (levelManager == null)
                levelManager = itemRoot.AddComponent<SystemsItemLevelManager>();

            var levelManagerSerialized = new SerializedObject(levelManager);
            var availableItems = levelManagerSerialized.FindProperty("availableItems");
            availableItems.arraySize = definitions.Length;
            for (int itemIndex = 0; itemIndex < definitions.Length; itemIndex++)
                availableItems.GetArrayElementAtIndex(itemIndex).objectReferenceValue = definitions[itemIndex];
            levelManagerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var spawner = itemRoot.GetComponent<SystemsItemSpawner>();
            if (spawner == null)
                spawner = itemRoot.AddComponent<SystemsItemSpawner>();

            var spawnerSerialized = new SerializedObject(spawner);
            spawnerSerialized.FindProperty("arenaManager").objectReferenceValue = arenaManager;
            spawnerSerialized.FindProperty("itemLevelManager").objectReferenceValue = levelManager;
            spawnerSerialized.FindProperty("defaultItemPickupPrefab").objectReferenceValue = pickupPrefab;
            spawnerSerialized.FindProperty("runtimeItemRoot").objectReferenceValue = runtimeRoot;
            spawnerSerialized.FindProperty("spawnOnStart").boolValue = true;
            spawnerSerialized.FindProperty("fallbackRespawnDelay").floatValue = 8f;
            spawnerSerialized.FindProperty("usePooling").boolValue = true;
            spawnerSerialized.FindProperty("prewarmPerDefinition").intValue = 1;
            spawnerSerialized.FindProperty("allowHitDrops").boolValue = true;
            spawnerSerialized.FindProperty("hitDropScatterRadius").floatValue = 0.45f;
            spawnerSerialized.FindProperty("verboseLogs").boolValue = false;

            var itemPool = spawnerSerialized.FindProperty("itemPool");
            itemPool.arraySize = definitions.Length;
            for (int itemIndex = 0; itemIndex < definitions.Length; itemIndex++)
                itemPool.GetArrayElementAtIndex(itemIndex).objectReferenceValue = definitions[itemIndex];
            spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    static void EnsureCharacterPrefabsHaveUpgradeBridge()
    {
        string[] prefabPaths =
        {
            "Assets/Prefabs/Knight.prefab",
            "Assets/Prefabs/Ninja.prefab",
            "Assets/Prefabs/Sorcerer.prefab"
        };

        for (int index = 0; index < prefabPaths.Length; index++)
        {
            string prefabPath = prefabPaths[index];
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                continue;

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root.GetComponent<PlayerUpgrade>() == null)
                    root.AddComponent<PlayerUpgrade>();

                if (root.GetComponent<PlayerUpgradeBuffAdapter>() == null)
                    root.AddComponent<PlayerUpgradeBuffAdapter>();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    static void EnsureFolder(string parentPath, string childFolder)
    {
        string fullPath = $"{parentPath}/{childFolder}";
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parentPath, childFolder);
    }
}
