using System.Collections.Generic;
using System.IO;
using Environment;
using Environment.Weather;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Environment.Editor
{
    [InitializeOnLoad]
    static class EnvironmentAutoRebuild
    {
        const string VersionFile = "Assets/Scenes/.environment_version";
        const string CurrentVersion = "18";

        static EnvironmentAutoRebuild()
        {
            EditorApplication.delayCall += TryRebuild;
        }

        static void TryRebuild()
        {
            if (Application.isPlaying) return;
            if (File.Exists(VersionFile) && File.ReadAllText(VersionFile).Trim() == CurrentVersion) return;
            if (!File.Exists("Assets/Scripts/Editor/EnvironmentSceneBuilder.cs")) return;

            Debug.Log("[Environment] Rebuilding scenes (v18 storm chasm bg + cleanup unused assets)...");
            try
            {
                EnvironmentSceneBuilder.RebuildAllScenesForce();
                Directory.CreateDirectory(Path.GetDirectoryName(VersionFile)!);
                File.WriteAllText(VersionFile, CurrentVersion);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Environment] Scene rebuild failed: {e.Message}");
            }
        }
    }

    public static class EnvironmentSceneBuilder
    {
        const string ScenesPath = "Assets/Scenes";
        const string SpritesPath = "Assets/Art/Sprites";

        // Khung gọn để arena lấp đầy màn hình (ortho 7.5 -> view ~26.7 x 15)
        const float CamSize = 7.5f;
        const float ArenaW = 24f;
        const float ArenaH = 14f;
        const float PlayHalfW = 11f;
        const float SpawnFeetOffset = 0.42f;
        const float DeathZoneY = -8.2f;

        readonly struct Plat
        {
            public readonly string Name, Sprite;
            public readonly Vector2 Pos, Size;
            public readonly bool Slippery;

            public Plat(string name, Vector2 pos, Vector2 size, string sprite, bool slippery = false)
            {
                Name = name;
                Pos = pos;
                Size = size;
                Sprite = sprite;
                Slippery = slippery;
            }
        }

        static float ViewWidth => CamSize * 2f * (16f / 9f);
        static float ViewHeight => CamSize * 2f;

        [MenuItem("Environment/Setup Tags and Layers")]
        public static void SetupTagsAndLayers()
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var tags = tagManager.FindProperty("tags");
            AddTag(tags, GameLayers.TagPlayer);
            AddTag(tags, GameLayers.TagHazard);
            AddTag(tags, GameLayers.TagDeathZone);
            AddTag(tags, GameLayers.TagSpawnPoint);
            AddTag(tags, GameLayers.TagItemSpawn);

            var layers = tagManager.FindProperty("layers");
            SetLayer(layers, 6, GameLayers.Ground);
            SetLayer(layers, 7, GameLayers.Player);
            SetLayer(layers, 8, GameLayers.Hazard);
            SetLayer(layers, 9, GameLayers.DeathZone);
            SetLayer(layers, 10, GameLayers.Item);

            tagManager.ApplyModifiedProperties();
        }

        [MenuItem("Environment/Build All Scenes")]
        public static void BuildAllScenes() => RebuildAllScenes(false);

        [MenuItem("Environment/Rebuild All Scenes (Force)")]
        public static void RebuildAllScenesForce() => RebuildAllScenes(true);

        static void RebuildAllScenes(bool force)
        {
            if (force)
            {
                string[] scenes = { "TestArena", "Mountain", "Volcano", "Sky", "WeatherTest" };
                foreach (var s in scenes)
                {
                    var path = $"{ScenesPath}/{s}.unity";
                    if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                }
            }

            SetupTagsAndLayers();
            ConfigureSpriteImports();

            try { BuildTestArena(); } catch (System.Exception e) { Debug.LogError($"[Environment] TestArena failed: {e}"); }
            try { BuildMountainMap(); } catch (System.Exception e) { Debug.LogError($"[Environment] Mountain failed: {e}"); }
            try { BuildVolcanoMap(); } catch (System.Exception e) { Debug.LogError($"[Environment] Volcano failed: {e}"); }
            try { BuildSkyMap(); } catch (System.Exception e) { Debug.LogError($"[Environment] Sky failed: {e}"); }
            try { BuildWeatherTestArena(); } catch (System.Exception e) { Debug.LogError($"[Environment] WeatherTest failed: {e}"); }
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Environment] All 5 scenes rebuilt with updated layouts & assets.");
        }

        // Sprite platform dạng "thanh" dùng 9-slice để co giãn không méo
        static bool IsPlatformSlice(string path) =>
            path.Contains("platform_grass") || path.Contains("platform_stone")
            || path.Contains("platform_ice") || path.Contains("platform_cloud")
            || path.Contains("platform_volcanic") || path.Contains("cloud_platform")
            || path.Contains("rock_volcanic");

        static void ConfigureSpriteImports()
        {
            string[] envFolder = { $"{SpritesPath}/Environment" };
            string[] bgFolder = { $"{SpritesPath}/Backgrounds" };

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", envFolder))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                if (IsPlatformSlice(path))
                {
                    importer.spritePixelsPerUnit = 400f;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                    // Lần 2: gán border 9-slice theo kích thước thực
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null)
                    {
                        var settings = new TextureImporterSettings();
                        importer.ReadTextureSettings(settings);
                        settings.spriteBorder = new Vector4(
                            tex.width * 0.24f, tex.height * 0.28f,
                            tex.width * 0.24f, tex.height * 0.28f);
                        importer.SetTextureSettings(settings);
                        importer.SaveAndReimport();
                    }
                    continue;
                }

                importer.spritePixelsPerUnit = 64;
                bool soft = path.Contains("prop_") || path.Contains("lava");
                importer.filterMode = soft ? FilterMode.Bilinear : FilterMode.Point;
                importer.SaveAndReimport();
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", bgFolder))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("_archive")) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = path.Contains("_full") ? 72f : 100f;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        static float SurfaceY(float centerY, float height) => centerY + height * 0.5f;

        static Vector2 StandOn(Vector2 platPos, Vector2 platSize, float xOffset = 0f)
            => new Vector2(platPos.x + xOffset, SurfaceY(platPos.y, platSize.y) + SpawnFeetOffset);

        static void BuildPlatforms(Transform root, IEnumerable<Plat> plats)
        {
            foreach (var p in plats)
            {
                if (p.Slippery)
                    CreateSlipperyPlatform(root, p.Name, p.Pos, p.Size, p.Sprite);
                else
                    CreatePlatform(root, p.Name, p.Pos, p.Size, p.Sprite);
            }
        }

        static void BuildTestArena()
        {
            var bg = new Color(0.78f, 0.82f, 0.88f);
            var (scene, root) = CreateBaseScene("TestArena", bg);
            CreateMapBackground(root, "bg_arena_full", bg, AmbientParticleStyle.None);
            CreateBoundaries(root, ArenaW, ArenaH);

            // Bố cục đối xứng: sàn chính + 2 bệ phụ + bệ trên giữa
            var main = new Vector2(0, -5f);
            var mainSize = new Vector2(17f, 0.85f);
            var padL = new Vector2(-6.8f, -1.4f);
            var padR = new Vector2(6.8f, -1.4f);
            var padSize = new Vector2(5f, 0.6f);
            var top = new Vector2(0, 2.3f);
            var topSize = new Vector2(6f, 0.55f);
            BuildPlatforms(root, new[]
            {
                new Plat("Main", main, mainSize, "platform_stone"),
                new Plat("Pad_L", padL, padSize, "platform_stone"),
                new Plat("Pad_R", padR, padSize, "platform_stone"),
                new Plat("Top", top, topSize, "platform_stone"),
            });

            CreateDeathZone(root, new Vector2(0, DeathZoneY), new Vector2(ArenaW + 2, 2));
            CreatePlayerSpawns(root, new[]
            {
                StandOn(main, mainSize, -mainSize.x * 0.35f),
                StandOn(main, mainSize, mainSize.x * 0.35f),
                StandOn(padL, padSize, 0),
                StandOn(padR, padSize, 0),
            });
            CreateItemSpawns(root, new[] { StandOn(top, topSize, 0) });
            CreateWeatherSystem(root, WeatherType.Clear);
            AddArenaManager(root, "Test Arena");
            SaveScene(scene, $"{ScenesPath}/TestArena.unity");
        }

        static void BuildMountainMap()
        {
            var bg = new Color(0.45f, 0.68f, 0.82f);
            var (scene, root) = CreateBaseScene("Mountain", bg);
            CreateMapBackground(root, "bg_mountain_full", bg, AmbientParticleStyle.None);
            SpawnBackgroundDecor(root, new (string, Vector2, float)[]
            {
                ("prop_tree", new Vector2(-11f, -3.2f), -12f),
                ("prop_tree", new Vector2(11f, -3.2f), -12f),
                ("prop_bush", new Vector2(-9f, -4f), -10f),
                ("prop_bush", new Vector2(9f, -4f), -10f),
                ("prop_bird", new Vector2(-5f, 5f), -8f),
                ("prop_bird", new Vector2(6f, 5.6f), -7f),
            });
            CreateBoundaries(root, ArenaW, ArenaH);

            // Thung lũng làm sàn chính, hai mỏm hai bên, cầu trên giữa
            var valley = new Vector2(0, -5f);
            var valleySize = new Vector2(17f, 0.85f);
            var ledgeL = new Vector2(-6.8f, -1.4f);
            var ledgeR = new Vector2(6.8f, -1.4f);
            var ledgeSize = new Vector2(5f, 0.6f);
            var bridge = new Vector2(0, 2.3f);
            var bridgeSize = new Vector2(6f, 0.55f);
            BuildPlatforms(root, new[]
            {
                new Plat("Valley", valley, valleySize, "platform_grass"),
                new Plat("Ledge_L", ledgeL, ledgeSize, "platform_grass"),
                new Plat("Ledge_R", ledgeR, ledgeSize, "platform_grass"),
                new Plat("Bridge", bridge, bridgeSize, "platform_grass"),
            });

            CreateDeathZone(root, new Vector2(0, DeathZoneY), new Vector2(ArenaW + 2, 2));
            CreatePlayerSpawns(root, new[]
            {
                StandOn(valley, valleySize, -valleySize.x * 0.35f),
                StandOn(valley, valleySize, valleySize.x * 0.35f),
                StandOn(ledgeL, ledgeSize, 0),
                StandOn(ledgeR, ledgeSize, 0),
            });
            CreateItemSpawns(root, new[]
            {
                StandOn(bridge, bridgeSize, 0),
            });
            CreateWeatherSystem(root, WeatherType.Rain);
            AddArenaManager(root, "Mountain");
            SaveScene(scene, $"{ScenesPath}/Mountain.unity");
        }

        static void BuildVolcanoMap()
        {
            var bg = new Color(0.22f, 0.10f, 0.08f);
            var (scene, root) = CreateBaseScene("Volcano", bg);
            CreateMapBackground(root, "bg_volcano_full", bg, AmbientParticleStyle.Embers);
            CreateBoundaries(root, ArenaW, ArenaH);

            var terrain = new GameObject("--- Terrain ---").transform;
            terrain.SetParent(root);

            // Dung nham full-width ngay ĐÁY màn hình (thấy rõ ở mép dưới)
            const float lavaHeight = 1.7f;
            const float lavaCenterY = -6.4f;
            CreateLavaRiver(terrain, lavaCenterY, ArenaW + 2f, lavaHeight);

            // Sàn đá chính lơ lửng trên dung nham, hai ledge, hai spire trên cao
            var floor = new Vector2(0f, -4.6f);
            var floorSize = new Vector2(13f, 0.95f);
            var ledgeL = new Vector2(-8f, -0.8f);
            var ledgeR = new Vector2(8f, -0.8f);
            var ledgeSize = new Vector2(4.5f, 0.7f);
            var spireL = new Vector2(-3.6f, 2.6f);
            var spireR = new Vector2(3.6f, 2.6f);
            var spireSize = new Vector2(4f, 0.7f);

            CreateRockPlatform(terrain, "Floor", floor, floorSize, "rock_volcanic_l");
            CreateRockPlatform(terrain, "Ledge_L", ledgeL, ledgeSize, "rock_volcanic_m");
            CreateRockPlatform(terrain, "Ledge_R", ledgeR, ledgeSize, "rock_volcanic_m");
            CreateRockPlatform(terrain, "Spire_L", spireL, spireSize, "rock_volcanic_s");
            CreateRockPlatform(terrain, "Spire_R", spireR, spireSize, "rock_volcanic_s");

            CreateDeathZone(root, new Vector2(0, DeathZoneY), new Vector2(ArenaW + 2, 2));
            CreatePlayerSpawns(root, new[]
            {
                StandOn(floor, floorSize, -floorSize.x * 0.35f),
                StandOn(floor, floorSize, floorSize.x * 0.35f),
                StandOn(ledgeL, ledgeSize, 0),
                StandOn(ledgeR, ledgeSize, 0),
            });
            CreateItemSpawns(root, new[]
            {
                StandOn(spireL, spireSize, 0),
                StandOn(spireR, spireSize, 0),
            });
            var lightning = CreateLightningArea(root, new Vector2(0f, 0.5f), new Vector2(7f, 3f));
            CreateWeatherSystem(root, WeatherType.Thunder, lightning);
            AddArenaManager(root, "Volcano");
            SaveScene(scene, $"{ScenesPath}/Volcano.unity"); // v13 lava-bottom layout
        }

        static void BuildSkyMap()
        {
            var bg = new Color(0.42f, 0.62f, 0.90f);
            var (scene, root) = CreateBaseScene("Sky", bg);
            CreateMapBackground(root, "bg_sky_full", bg, AmbientParticleStyle.Stars);
            // Mây trang trí nền (không va chạm)
            SpawnBackgroundDecor(root, new (string, Vector2, float)[]
            {
                ("cloud_0", new Vector2(-9f, 5f), -18f),
                ("cloud_1", new Vector2(7f, 5.5f), -17f),
                ("cloud_2", new Vector2(-2f, 6f), -16f),
                ("cloud_0", new Vector2(11f, 1f), -15f),
                ("cloud_1", new Vector2(-11f, -1f), -14f),
                ("prop_bird", new Vector2(-4f, 4f), -9f),
                ("prop_bird", new Vector2(5f, 5f), -8f),
            });
            CreateBoundaries(root, ArenaW, ArenaH);

            // Khối mây = platform di chuyển: dài hơn + thấp hơn -> dễ nhảy, đánh nhau đã hơn
            var main = new Vector2(0f, -5.4f);   var sMain = new Vector2(13f, 1.1f);
            var sideL = new Vector2(-8f, -3f);   var sSide = new Vector2(6.5f, 1f);
            var sideR = new Vector2(8f, -3f);
            var upL = new Vector2(-4f, -0.6f);   var sUp = new Vector2(6f, 0.95f);
            var upR = new Vector2(4f, -0.6f);
            var crown = new Vector2(0f, 1.6f);   var sCrown = new Vector2(5.5f, 0.9f);

            BuildCloudPlatforms(root, new[]
            {
                (main, sMain, "Cloud_Main"),
                (sideL, sSide, "Cloud_L"), (sideR, sSide, "Cloud_R"),
                (upL, sUp, "Cloud_UpL"), (upR, sUp, "Cloud_UpR"),
                (crown, sCrown, "Cloud_Crown"),
            });

            CreateDeathZone(root, new Vector2(0, DeathZoneY), new Vector2(ArenaW + 2, 2));
            CreatePlayerSpawns(root, new[]
            {
                StandOn(main, sMain, -2f),
                StandOn(main, sMain, 2f),
                StandOn(sideL, sSide, 0),
                StandOn(sideR, sSide, 0),
            });
            CreateItemSpawns(root, new[] { StandOn(crown, sCrown, 0) });
            var wind = CreateWindZone(root, new Vector2(0, 0.5f), new Vector2(ArenaW, 10f), new Vector2(7f, 0.3f));
            CreateWeatherSystem(root, WeatherType.Wind, null, new[] { wind }, Vector2.right);
            AddArenaManager(root, "Sky");
            SaveScene(scene, $"{ScenesPath}/Sky.unity");
        }

        static void BuildWeatherTestArena()
        {
            var bg = new Color(0.38f, 0.42f, 0.52f);
            var (scene, root) = CreateBaseScene("WeatherTest", bg);
            CreateMapBackground(root, "bg_storm_full", bg, AmbientParticleStyle.Snow);
            CreateBoundaries(root, ArenaW, ArenaH);

            var basePlat = new Vector2(0, -5f);
            var baseSize = new Vector2(17f, 0.85f);
            var iceL = new Vector2(-6.8f, -1.4f);
            var iceR = new Vector2(6.8f, -1.4f);
            var iceSize = new Vector2(5f, 0.55f);
            var crown = new Vector2(0, 2.3f);
            var crownSize = new Vector2(6f, 0.55f);
            BuildPlatforms(root, new[]
            {
                new Plat("Stone_Base", basePlat, baseSize, "platform_stone"),
                new Plat("Ice_L", iceL, iceSize, "platform_ice", slippery: true),
                new Plat("Ice_R", iceR, iceSize, "platform_ice", slippery: true),
                new Plat("Ice_Crown", crown, crownSize, "platform_ice", slippery: true),
            });

            var wind = CreateWindZone(root, new Vector2(0, 0.5f), new Vector2(ArenaW, 11f), new Vector2(8f, 0.35f));
            var lightning = CreateLightningArea(root, new Vector2(0, -2f), new Vector2(7f, 2f));

            CreateDeathZone(root, new Vector2(0, DeathZoneY), new Vector2(ArenaW + 2, 2));
            CreatePlayerSpawns(root, new[]
            {
                StandOn(basePlat, baseSize, -baseSize.x * 0.35f),
                StandOn(basePlat, baseSize, baseSize.x * 0.35f),
                StandOn(iceL, iceSize, 0),
                StandOn(iceR, iceSize, 0),
            });
            CreateItemSpawns(root, new[] { StandOn(crown, crownSize, 0) });
            CreateWeatherSystem(root, WeatherType.Storm, lightning, new[] { wind }, Vector2.right);
            AddArenaManager(root, "Weather Test");
            SaveScene(scene, $"{ScenesPath}/WeatherTest.unity");
        }

        #region Weather & Effects

        static Environment.Weather.WindZone CreateWindZone(Transform parent, Vector2 pos, Vector2 size, Vector2 force)
        {
            var go = new GameObject("WindZone");
            go.transform.SetParent(parent);
            go.transform.position = (Vector3)pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
            var wind = go.AddComponent<Environment.Weather.WindZone>();
            var so = new SerializedObject(wind);
            so.FindProperty("windForce").vector2Value = force;
            so.ApplyModifiedProperties();
            return wind;
        }

        static LightningArea CreateLightningArea(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("LightningArea");
            go.transform.SetParent(parent);
            go.transform.position = (Vector3)pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
            go.AddComponent<LightningArea>();
            AddZoneLight(go.transform, ZoneLightColors.Hazard, 0.45f, Mathf.Max(size.x, size.y) * 0.4f);
            return go.GetComponent<LightningArea>();
        }

        static void CreateWeatherSystem(Transform parent, WeatherType weather,
            LightningArea lightning = null,
            Environment.Weather.WindZone[] windZones = null,
            Vector2 windVisualDir = default)
        {
            var weatherRoot = new GameObject("--- Weather ---");
            weatherRoot.transform.SetParent(parent);

            var ambient = weatherRoot.AddComponent<MapAmbientController>();

            var rainGo = new GameObject("RainEffect");
            rainGo.transform.SetParent(weatherRoot.transform);
            rainGo.transform.position = new Vector3(0, CamSize - 1, 0);
            ConfigureRainParticles(rainGo.AddComponent<ParticleSystem>());
            rainGo.GetComponent<ParticleSystemRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            var rainEffect = rainGo.AddComponent<RainEffect>();

            var thunderGo = new GameObject("ThunderEffect");
            thunderGo.transform.SetParent(weatherRoot.transform);
            var flashLight = thunderGo.AddComponent<Light2D>();
            flashLight.lightType = Light2D.LightType.Point;
            flashLight.pointLightOuterRadius = 22f;
            flashLight.intensity = 0;
            flashLight.enabled = false;
            var thunderEffect = thunderGo.AddComponent<ThunderEffect>();

            var windVisGo = new GameObject("WindVisual");
            windVisGo.transform.SetParent(weatherRoot.transform);
            windVisGo.transform.position = new Vector3(0, 1, 0);
            ConfigureWindParticles(windVisGo.AddComponent<ParticleSystem>(), windVisualDir);
            windVisGo.GetComponent<ParticleSystemRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            var windVisual = windVisGo.AddComponent<WindVisualEffect>();

            var weatherMgr = weatherRoot.AddComponent<WeatherManager>();
            var wmSo = new SerializedObject(weatherMgr);
            SetSoRef(wmSo, "currentWeather", (int)weather, true);
            SetSoRef(wmSo, "rainEffect", rainEffect);
            SetSoRef(wmSo, "thunderEffect", thunderEffect);
            SetSoRef(wmSo, "windVisual", windVisual);
            SetSoRef(wmSo, "ambientController", ambient);

            if (windZones != null && windZones.Length > 0)
            {
                var arr = wmSo.FindProperty("windZones");
                if (arr != null)
                {
                    arr.arraySize = windZones.Length;
                    for (int i = 0; i < windZones.Length; i++)
                        arr.GetArrayElementAtIndex(i).objectReferenceValue = windZones[i];
                }
            }

            wmSo.ApplyModifiedProperties();

            bool needsRain = weather == WeatherType.Rain || weather == WeatherType.Storm;
            bool needsThunder = weather == WeatherType.Thunder || weather == WeatherType.Storm;
            bool needsWind = weather == WeatherType.Wind || weather == WeatherType.Storm;

            rainGo.SetActive(needsRain);
            thunderGo.SetActive(needsThunder);
            windVisGo.SetActive(needsWind);

            if (lightning != null)
                lightning.gameObject.SetActive(needsThunder);
        }

        static void ConfigureRainParticles(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 14f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(0.7f, 0.8f, 1f, 0.6f);
            main.maxParticles = 1000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.5f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 80f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(ArenaW + 2, 0.5f, 1);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(-2f);
        }

        static void ConfigureWindParticles(ParticleSystem ps, Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            dir.Normalize();

            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new Color(1f, 1f, 1f, 0.3f);
            main.maxParticles = 250;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 30f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(ArenaW, 8f, 1);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(dir.x * 6f);
            vel.y = new ParticleSystem.MinMaxCurve(dir.y * 2f);
        }

        static void SetSoRef(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }

        static void SetSoRef(SerializedObject so, string prop, int enumIndex, bool isEnum)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.enumValueIndex = enumIndex;
        }

        #endregion

        #region Scene Helpers

        static (Scene scene, Transform root) CreateBaseScene(string sceneName, Color bgColor)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject($"--- {sceneName} ---").transform;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.backgroundColor = bgColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();
            var urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderType = CameraRenderType.Base;
            var mapCam = camGo.AddComponent<MapCamera>();
            mapCam.SetBackground(bgColor);

            var lightGo = new GameObject("Global Light 2D");
            var globalLight = lightGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 1f;
            globalLight.color = Color.white;

            return (scene, root);
        }

        /// <summary>Single full-screen HQ background + optional ambient particles.</summary>
        static void CreateMapBackground(Transform parent, string fullBgSprite,
            Color cameraFallback, AmbientParticleStyle ambient)
        {
            var bgRoot = new GameObject("Background");
            bgRoot.transform.SetParent(parent);

            var baseGo = new GameObject("BG_Base");
            baseGo.transform.SetParent(bgRoot.transform);
            baseGo.transform.localPosition = new Vector3(0, 0, 10);
            var baseSr = baseGo.AddComponent<SpriteRenderer>();
            baseSr.sortingOrder = -35;
            var sprite = LoadSprite(fullBgSprite);
            if (sprite != null)
            {
                baseSr.sprite = sprite;
                ScaleSpriteToCover(baseGo.transform, sprite, 1.0f);
                var drift = baseGo.AddComponent<ParallaxDrift>();
                var driftSo = new SerializedObject(drift);
                driftSo.FindProperty("amplitude").vector2Value = new Vector2(0.06f, 0.03f);
                driftSo.FindProperty("speed").vector2Value = new Vector2(0.12f, 0.08f);
                driftSo.ApplyModifiedProperties();
            }
            else
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, cameraFallback);
                tex.Apply();
                baseSr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
                baseGo.transform.localScale = new Vector3(ViewWidth, ViewHeight, 1);
            }

            if (ambient == AmbientParticleStyle.None) return;

            var amb = new GameObject("AmbientFX");
            amb.transform.SetParent(bgRoot.transform);
            amb.transform.localPosition = Vector3.zero;
            var ap = amb.AddComponent<AmbientParticles>();
            var ambSo = new SerializedObject(ap);
            ambSo.FindProperty("style").enumValueIndex = (int)ambient;
            ambSo.FindProperty("areaWidth").floatValue = ArenaW + 2f;
            ambSo.FindProperty("areaHeight").floatValue = ArenaH;
            ambSo.ApplyModifiedProperties();
        }

        static void ScaleSpriteToCover(Transform t, Sprite sprite, float bleed = 1f)
        {
            var b = sprite.bounds.size;
            t.localScale = new Vector3(ViewWidth * bleed / b.x, ViewHeight * bleed / b.y, 1f);
        }

        static void ScaleSpriteToFit(Transform t, Sprite sprite, float widthFrac, float heightFrac)
        {
            var b = sprite.bounds.size;
            t.localScale = new Vector3(ViewWidth * widthFrac / b.x, ViewHeight * heightFrac / b.y, 1f);
        }

        static void CreateBoundaries(Transform parent, float width, float height)
        {
            var bounds = new GameObject("Boundaries");
            bounds.transform.SetParent(parent);
            float halfW = width / 2f;
            float halfH = height / 2f;
            float t = 0.5f;
            CreateWall(bounds.transform, "Wall_L", new Vector2(-halfW - t / 2, 0), new Vector2(t, height + 2));
            CreateWall(bounds.transform, "Wall_R", new Vector2(halfW + t / 2, 0), new Vector2(t, height + 2));
            CreateWall(bounds.transform, "Wall_T", new Vector2(0, halfH + t / 2), new Vector2(width + 2, t));
        }

        static void CreateWall(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.position = (Vector3)pos;
            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
            wall.AddComponent<ArenaBoundary>();
        }

        static void SpawnBackgroundDecor(Transform parent, (string sprite, Vector2 pos, float sortOrder)[] props)
        {
            var decorRoot = new GameObject("BG_DecorProps");
            decorRoot.transform.SetParent(parent);

            foreach (var (spriteName, pos, sortOrder) in props)
            {
                var sprite = LoadSprite(spriteName);
                if (sprite == null) continue;

                var go = new GameObject(spriteName);
                go.transform.SetParent(decorRoot.transform);
                go.transform.position = new Vector3(pos.x, pos.y, 8f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = Mathf.RoundToInt(sortOrder);
                float scale = spriteName.StartsWith("cloud") ? 2.4f : 0.35f;
                go.transform.localScale = Vector3.one * scale;
                var prop = go.AddComponent<BackgroundProp>();
                var pso = new SerializedObject(prop);
                pso.FindProperty("phase").floatValue = pos.x * 0.17f;
                pso.ApplyModifiedProperties();
            }
        }

        static void BuildCloudPlatforms(Transform root, (Vector2 pos, Vector2 size, string name)[] clouds)
        {
            foreach (var (pos, size, name) in clouds)
                CreateCloudPlatform(root, name, pos, size);
        }

        static GameObject CreateCloudPlatform(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = (Vector3)pos;

            var sprite = LoadSprite("cloud_platform_m") ?? LoadSprite("platform_cloud");
            var sr = go.AddComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
                sr.sortingOrder = 2;
                sr.color = new Color(1f, 1f, 1f, 0.98f);
            }

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            go.AddComponent<Platform>();

            var floatMot = go.AddComponent<FloatingMotion>();
            var fso = new SerializedObject(floatMot);
            fso.FindProperty("phase").floatValue = pos.x * 0.2f;
            fso.FindProperty("amplitudeY").floatValue = 0.05f;
            fso.ApplyModifiedProperties();

            return go;
        }

        static GameObject CreateRockPlatform(Transform parent, string name, Vector2 pos, Vector2 size, string spriteName)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = (Vector3)pos;

            var sprite = LoadSprite(spriteName) ?? LoadSprite("rock_volcanic_m");
            var sr = go.AddComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
                sr.sortingOrder = 2;
            }

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            go.AddComponent<Platform>();
            AddPlatformShadow(go.transform, size);
            return go;
        }

        static void CreateLavaRiver(Transform parent, float y, float width, float height)
        {
            var go = new GameObject("Lava_River");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0, y, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            var sprite = LoadSprite("lava_tile");
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(width, height);
                sr.color = Color.white;
                sr.sortingOrder = -5;
            }

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
            col.isTrigger = true;
            go.AddComponent<HazardZone>();

            var flow = go.AddComponent<LavaFlowEffect>();
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("scrollSpeed").floatValue = 1.4f;
            flowSo.ApplyModifiedProperties();

            var pulse = go.AddComponent<SpritePulse>();
            var pulseSo = new SerializedObject(pulse);
            pulseSo.FindProperty("tintA").colorValue = new Color(0.95f, 0.82f, 0.7f, 1f);
            pulseSo.FindProperty("tintB").colorValue = new Color(1f, 1f, 0.92f, 1f);
            pulseSo.FindProperty("speed").floatValue = 1.5f;
            pulseSo.ApplyModifiedProperties();
        }

        static GameObject CreatePlatform(Transform parent, string name, Vector2 pos, Vector2 size, string spriteName)
        {
            if (spriteName == "platform_cloud")
                return CreateCloudPlatform(parent, name, pos, size);

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = (Vector3)pos;
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            var sprite = LoadSprite(spriteName);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
                sr.sortingOrder = 1;
            }

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            go.AddComponent<Platform>();
            AddPlatformShadow(go.transform, size);
            return go;
        }

        static void AddPlatformShadow(Transform platform, Vector2 size)
        {
            var shadowSprite = LoadSprite("platform_shadow");
            if (shadowSprite == null) return;

            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(platform);
            shadow.transform.localPosition = new Vector3(0f, -size.y * 0.42f, 0.1f);
            var ssr = shadow.AddComponent<SpriteRenderer>();
            ssr.sprite = shadowSprite;
            ssr.sortingOrder = 0;
            ssr.color = new Color(0f, 0f, 0f, 0.4f);
            float sw = Mathf.Max(size.x * 0.9f, 1f);
            shadow.transform.localScale = new Vector3(
                sw / shadowSprite.bounds.size.x,
                0.4f,
                1f);
        }

        static void CreateSlipperyPlatform(Transform parent, string name, Vector2 pos, Vector2 size, string spriteName = "platform_ice")
        {
            var go = CreatePlatform(parent, name, pos, size, spriteName);
            go.AddComponent<SlipperyPlatform>();
            var effector = go.GetComponent<PlatformEffector2D>();
            if (effector == null) effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 180f;
            go.GetComponent<BoxCollider2D>().usedByEffector = true;
        }

        static void CreateHazardZone(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = (Vector3)pos;
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            var sprite = LoadSprite("lava_tile");
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = size;
                sr.color = new Color(1f, 0.55f, 0.25f, 0.92f);
                var pulse = go.AddComponent<SpritePulse>();
                var pulseSo = new SerializedObject(pulse);
                pulseSo.FindProperty("tintA").colorValue = sr.color;
                pulseSo.FindProperty("tintB").colorValue = new Color(1f, 0.85f, 0.35f, 0.95f);
                pulseSo.FindProperty("phase").floatValue = pos.x * 0.2f;
                pulseSo.ApplyModifiedProperties();
            }
            sr.sortingOrder = 0;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
            go.AddComponent<HazardZone>();
        }

        static void CreateDeathZone(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("DeathZone");
            go.transform.SetParent(parent);
            go.transform.position = (Vector3)pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
            go.AddComponent<DeathZone>();
        }

        static void CreatePlayerSpawns(Transform parent, Vector2[] positions)
        {
            var spawns = new GameObject("PlayerSpawnPoints");
            spawns.transform.SetParent(parent);

            for (int i = 0; i < positions.Length; i++)
            {
                var go = new GameObject($"Spawn_P{i + 1}");
                go.transform.SetParent(spawns.transform);
                go.transform.position = (Vector3)positions[i];
                go.tag = GameLayers.TagSpawnPoint;
                var sp = go.AddComponent<PlayerSpawnPoint>();
                var so = new SerializedObject(sp);
                so.FindProperty("playerIndex").intValue = i + 1;
                so.ApplyModifiedProperties();
            }
        }

        static void CreateItemSpawns(Transform parent, Vector2[] positions)
        {
            var spawns = new GameObject("ItemSpawnPoints");
            spawns.transform.SetParent(parent);

            foreach (var pos in positions)
            {
                var go = new GameObject("ItemSpawn");
                go.transform.SetParent(spawns.transform);
                go.transform.position = (Vector3)pos;
                go.tag = GameLayers.TagItemSpawn;
                go.AddComponent<ItemSpawnPoint>();
                AddZoneLight(go.transform, ZoneLightColors.ItemSpawn, 0.25f, 0.45f);
            }
        }

        static void AddZoneLight(Transform parent, Color color, float intensity, float radius)
        {
            var lightGo = new GameObject("ZoneLight");
            lightGo.transform.SetParent(parent);
            lightGo.transform.localPosition = Vector3.zero;
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = radius * 0.4f;
            light.pointLightOuterRadius = radius;
        }

        static void AddArenaManager(Transform parent, string mapName)
        {
            var manager = new GameObject("ArenaManager");
            manager.transform.SetParent(parent);
            var am = manager.AddComponent<ArenaManager>();
            var so = new SerializedObject(am);
            so.FindProperty("mapName").stringValue = mapName;
            so.ApplyModifiedProperties();
        }

        static Sprite LoadSprite(string name)
        {
            var guids = AssetDatabase.FindAssets($"{name} t:Sprite", new[] { SpritesPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("_archive") || path.Contains("_hd")) continue;
                if (Path.GetFileNameWithoutExtension(path) == name)
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            var texGuids = AssetDatabase.FindAssets($"{name} t:Texture2D", new[] { SpritesPath });
            foreach (var guid in texGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("_archive") || path.Contains("_hd")) continue;
                if (Path.GetFileNameWithoutExtension(path) == name)
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Environment] Saved: {path}");
        }

        static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesPath}/TestArena.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/Mountain.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/Volcano.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/Sky.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/WeatherTest.unity", true),
            };
        }

        static void AddTag(SerializedProperty tags, string tag)
        {
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        }

        static void SetLayer(SerializedProperty layers, int index, string name)
        {
            var prop = layers.GetArrayElementAtIndex(index);
            if (prop.stringValue != name) prop.stringValue = name;
        }

        #endregion
    }
}
