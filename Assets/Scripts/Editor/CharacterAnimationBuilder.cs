using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

static class CharacterAnimationBuilder
{
    const string SpriteRoot = "Assets/Sprite";
    const string AnimationRoot = "Assets/Animations";
    const string PrefabRoot = "Assets/Prefabs";
    const float DefaultFps = 12f;

    sealed class ClipSpec
    {
        public string StateName;
        public string[] SourcePngFileNames;
        public bool Loop;
        public bool Optional;
        public float Fps;

        public ClipSpec(string stateName, bool loop, bool optional = false, float fps = DefaultFps, params string[] sourcePngFileNames)
        {
            StateName = stateName;
            SourcePngFileNames = sourcePngFileNames ?? Array.Empty<string>();
            Loop = loop;
            Optional = optional;
            Fps = fps;
        }
    }

    sealed class CharacterSpec
    {
        public string CharacterName;
        public string PrefabPath;
        public int ComboCount;
        public bool UseShield;
        public bool UseCharge;
        public List<ClipSpec> Clips;

        public CharacterSpec(
            string characterName,
            string prefabPath,
            int comboCount,
            bool useShield,
            bool useCharge,
            List<ClipSpec> clips)
        {
            CharacterName = characterName;
            PrefabPath = prefabPath;
            ComboCount = comboCount;
            UseShield = useShield;
            UseCharge = useCharge;
            Clips = clips;
        }
    }

    static readonly Regex TrailingNumberRegex = new Regex(@"(\d+)$", RegexOptions.Compiled);

    [MenuItem("Skyfall Arena/Animation/Build Character Animations")]
    public static void BuildCharacterAnimations()
    {
        EnsureFolder("Assets", "Animations");

        foreach (var spec in GetCharacterSpecs())
            BuildCharacter(spec, forceRebuildController: false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CharacterAnimationBuilder] Build completed.");
    }

    [MenuItem("Skyfall Arena/Animation/Rebuild Character Animations (Force Controllers)")]
    public static void RebuildCharacterAnimationsForceController()
    {
        EnsureFolder("Assets", "Animations");

        foreach (var spec in GetCharacterSpecs())
            BuildCharacter(spec, forceRebuildController: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CharacterAnimationBuilder] Rebuild completed.");
    }

    [MenuItem("Skyfall Arena/Animation/Build And Assign All Characters")]
    public static void BuildAndAssignAllCharacters()
    {
        EnsureFolder("Assets", "Animations");
        EnsureFolder("Assets", "Prefabs");

        foreach (var spec in GetCharacterSpecs())
            BuildCharacter(spec, forceRebuildController: true);

        AssignControllersToPrefabsInternal(createMissingPrefabFromTemplate: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CharacterAnimationBuilder] Build + Assign completed.");
    }

    [MenuItem("Skyfall Arena/Animation/Assign Controllers To Prefabs")]
    public static void AssignControllersToPrefabs()
    {
        AssignControllersToPrefabsInternal(createMissingPrefabFromTemplate: false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void AssignControllersToPrefabsInternal(bool createMissingPrefabFromTemplate)
    {
        foreach (var spec in GetCharacterSpecs())
        {
            if (!EnsureCharacterPrefabExists(spec, createMissingPrefabFromTemplate))
            {
                Debug.LogWarning($"[CharacterAnimationBuilder] Prefab missing for {spec.CharacterName}: {spec.PrefabPath}");
                continue;
            }

            string controllerPath = $"{AnimationRoot}/{spec.CharacterName}/{spec.CharacterName}.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"[CharacterAnimationBuilder] Controller missing for {spec.CharacterName}: {controllerPath}");
                continue;
            }

            var root = PrefabUtility.LoadPrefabContents(spec.PrefabPath);
            try
            {
                var animator = root.GetComponent<Animator>();
                if (animator == null)
                    animator = root.AddComponent<Animator>();

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var characterType = root.GetComponent<CharacterType>();
                if (characterType == null)
                    characterType = root.AddComponent<CharacterType>();
                characterType.character = ResolveCharacterEnum(spec.CharacterName);

                if (root.GetComponent<CharacterAnimationSync>() == null)
                    root.AddComponent<CharacterAnimationSync>();

                PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
                Debug.Log($"[CharacterAnimationBuilder] Assigned controller to {spec.PrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

    }

    static void BuildCharacter(CharacterSpec spec, bool forceRebuildController)
    {
        EnsureFolder(AnimationRoot, spec.CharacterName);
        string characterAnimFolder = $"{AnimationRoot}/{spec.CharacterName}";
        string characterSpriteFolder = $"{SpriteRoot}/{spec.CharacterName}";

        var clipsByState = new Dictionary<string, AnimationClip>();

        foreach (var clipSpec in spec.Clips)
        {
            string sourcePath = ResolveSourcePath(characterSpriteFolder, clipSpec.SourcePngFileNames);
            if (string.IsNullOrEmpty(sourcePath))
            {
                if (!clipSpec.Optional)
                    Debug.LogWarning($"[CharacterAnimationBuilder] Missing sprite sheet for {spec.CharacterName}/{clipSpec.StateName}.");
                continue;
            }

            string clipPath = $"{characterAnimFolder}/{spec.CharacterName}_{clipSpec.StateName}.anim";
            var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (existingClip != null)
            {
                clipsByState[clipSpec.StateName] = existingClip;
                continue;
            }

            var sprites = LoadSpritesFromSheet(sourcePath);
            if (sprites.Count == 0)
            {
                if (!clipSpec.Optional)
                    Debug.LogWarning($"[CharacterAnimationBuilder] No sprites found in: {sourcePath}");
                continue;
            }

            var clip = CreateClip(sprites, clipSpec.Fps, clipSpec.Loop);
            AssetDatabase.CreateAsset(clip, clipPath);
            clipsByState[clipSpec.StateName] = clip;
            Debug.Log($"[CharacterAnimationBuilder] Created clip: {clipPath}");
        }

        string controllerPath = $"{characterAnimFolder}/{spec.CharacterName}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        bool controllerCreatedNow = false;
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controllerCreatedNow = true;
            Debug.Log($"[CharacterAnimationBuilder] Created controller: {controllerPath}");
        }

        if (forceRebuildController || controllerCreatedNow || IsControllerEmpty(controller))
        {
            ConfigureController(controller, clipsByState, spec.ComboCount, spec.UseShield, spec.UseCharge);
        }
        else
        {
            Debug.LogWarning($"[CharacterAnimationBuilder] Existing controller kept as-is (not overwritten): {controllerPath}");
        }
    }

    static AnimationClip CreateClip(IReadOnlyList<Sprite> sprites, float fps, bool loop)
    {
        var clip = new AnimationClip { frameRate = Mathf.Max(1f, fps) };
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int index = 0; index < sprites.Count; index++)
        {
            keyframes[index] = new ObjectReferenceKeyframe
            {
                time = index / clip.frameRate,
                value = sprites[index]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    static List<Sprite> LoadSpritesFromSheet(string sourcePath)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
            .OfType<Sprite>()
            .ToList();

        sprites.Sort((left, right) =>
        {
            int leftNum = ExtractTrailingNumber(left.name);
            int rightNum = ExtractTrailingNumber(right.name);
            int numCompare = leftNum.CompareTo(rightNum);
            return numCompare != 0 ? numCompare : string.CompareOrdinal(left.name, right.name);
        });

        return sprites;
    }

    static int ExtractTrailingNumber(string text)
    {
        if (string.IsNullOrEmpty(text))
            return int.MaxValue;

        var match = TrailingNumberRegex.Match(text);
        if (!match.Success)
            return int.MaxValue;

        return int.TryParse(match.Value, out var result) ? result : int.MaxValue;
    }

    static void ConfigureController(
        AnimatorController controller,
        Dictionary<string, AnimationClip> clipsByState,
        int comboCount,
        bool useShield,
        bool useCharge)
    {
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Combo", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "Hurt", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsBlocking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Charge", AnimatorControllerParameterType.Trigger);
        SetDefaultBool(controller, "IsGrounded", true);

        var layer = controller.layers[0];
        var stateMachine = layer.stateMachine;

        ClearStateMachine(stateMachine);

        var states = new Dictionary<string, AnimatorState>();
        foreach (var pair in clipsByState)
        {
            var state = stateMachine.AddState(pair.Key);
            state.motion = pair.Value;
            state.speed = 1f;
            states[pair.Key] = state;
        }

        if (states.TryGetValue("Idle", out var idleState))
            stateMachine.defaultState = idleState;
        else if (states.Count > 0)
            stateMachine.defaultState = states.First().Value;

        var locomotionStates = new List<AnimatorState>();
        if (states.TryGetValue("Idle", out var idle))
            locomotionStates.Add(idle);
        if (states.TryGetValue("Walk", out var walk))
            locomotionStates.Add(walk);
        if (states.TryGetValue("Run", out var run))
            locomotionStates.Add(run);

        if (idle != null && walk != null)
        {
            var idleToWalk = idle.AddTransition(walk);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.05f;
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");

            var walkToIdle = walk.AddTransition(idle);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.05f;
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }

        if (walk != null && run != null)
        {
            var walkToRun = walk.AddTransition(run);
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.05f;
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.75f, "Speed");

            var runToWalk = run.AddTransition(walk);
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.05f;
            runToWalk.AddCondition(AnimatorConditionMode.Less, 0.75f, "Speed");
            runToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        }

        // Fallback if a character has no Walk clip.
        if (walk == null && idle != null && run != null)
        {
            var idleToRun = idle.AddTransition(run);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.05f;
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var runToIdle = run.AddTransition(idle);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.05f;
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }

        if (states.TryGetValue("Jump", out var jump))
        {
            for (int index = 0; index < locomotionStates.Count; index++)
            {
                var toJump = locomotionStates[index].AddTransition(jump);
                toJump.hasExitTime = false;
                toJump.duration = 0.05f;
                toJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            }

            if (run != null)
            {
                var jumpToRun = jump.AddTransition(run);
                jumpToRun.hasExitTime = false;
                jumpToRun.duration = 0.05f;
                jumpToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
                jumpToRun.AddCondition(AnimatorConditionMode.Greater, 0.75f, "Speed");
            }

            if (walk != null)
            {
                var jumpToWalk = jump.AddTransition(walk);
                jumpToWalk.hasExitTime = false;
                jumpToWalk.duration = 0.05f;
                jumpToWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
                jumpToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                jumpToWalk.AddCondition(AnimatorConditionMode.Less, 0.75f, "Speed");
            }

            if (idle != null)
            {
                var jumpToIdle = jump.AddTransition(idle);
                jumpToIdle.hasExitTime = false;
                jumpToIdle.duration = 0.05f;
                jumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
                jumpToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }
        }

        if (states.TryGetValue("Hurt", out var hurt))
        {
            var anyToHurt = stateMachine.AddAnyStateTransition(hurt);
            anyToHurt.hasExitTime = false;
            anyToHurt.duration = 0.02f;
            anyToHurt.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");
            anyToHurt.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

            if (idle != null)
            {
                var hurtToIdle = hurt.AddTransition(idle);
                hurtToIdle.hasExitTime = true;
                hurtToIdle.exitTime = 0.95f;
                hurtToIdle.duration = 0.05f;
            }
        }

        if (states.TryGetValue("Death", out var death))
        {
            var anyToDeath = stateMachine.AddAnyStateTransition(death);
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.02f;
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");
        }

        for (int comboIndex = 1; comboIndex <= comboCount; comboIndex++)
        {
            string stateName = $"Attack_{comboIndex}";
            if (!states.TryGetValue(stateName, out var attackState))
                continue;

            var anyToAttack = stateMachine.AddAnyStateTransition(attackState);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.02f;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToAttack.AddCondition(AnimatorConditionMode.Equals, comboIndex, "Combo");
            anyToAttack.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

            if (idle != null)
            {
                var attackToIdle = attackState.AddTransition(idle);
                attackToIdle.hasExitTime = true;
                attackToIdle.exitTime = 0.95f;
                attackToIdle.duration = 0.05f;
            }
        }

        if (useShield && states.TryGetValue("Shield", out var shield))
        {
            for (int index = 0; index < locomotionStates.Count; index++)
            {
                var toShield = locomotionStates[index].AddTransition(shield);
                toShield.hasExitTime = false;
                toShield.duration = 0.03f;
                toShield.AddCondition(AnimatorConditionMode.If, 0f, "IsBlocking");
                toShield.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
            }

            if (idle != null)
            {
                var shieldToIdle = shield.AddTransition(idle);
                shieldToIdle.hasExitTime = false;
                shieldToIdle.duration = 0.03f;
                shieldToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsBlocking");
            }
        }

        if (useCharge && states.TryGetValue("Charge", out var charge))
        {
            for (int index = 0; index < locomotionStates.Count; index++)
            {
                var toCharge = locomotionStates[index].AddTransition(charge);
                toCharge.hasExitTime = false;
                toCharge.duration = 0.03f;
                toCharge.AddCondition(AnimatorConditionMode.If, 0f, "Charge");
                toCharge.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
            }

            if (idle != null)
            {
                var chargeToIdle = charge.AddTransition(idle);
                chargeToIdle.hasExitTime = true;
                chargeToIdle.exitTime = 0.95f;
                chargeToIdle.duration = 0.05f;
            }
        }

        controller.layers = new[] { layer };
        EditorUtility.SetDirty(controller);
    }

    static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        var anyTransitions = stateMachine.anyStateTransitions.ToArray();
        for (int index = 0; index < anyTransitions.Length; index++)
            stateMachine.RemoveAnyStateTransition(anyTransitions[index]);

        var states = stateMachine.states.ToArray();
        for (int index = 0; index < states.Length; index++)
            stateMachine.RemoveState(states[index].state);
    }

    static bool IsControllerEmpty(AnimatorController controller)
    {
        if (controller == null || controller.layers == null || controller.layers.Length == 0)
            return true;

        var sm = controller.layers[0].stateMachine;
        if (sm == null)
            return true;

        // Unity default controller usually has one placeholder state and no transitions.
        return sm.states.Length <= 1 && sm.anyStateTransitions.Length == 0;
    }

    static void EnsureParameter(AnimatorController controller, string parameterName, AnimatorControllerParameterType parameterType)
    {
        for (int index = 0; index < controller.parameters.Length; index++)
        {
            if (controller.parameters[index].name != parameterName)
                continue;

            if (controller.parameters[index].type == parameterType)
                return;

            RemoveParameter(controller, parameterName);
            break;
        }

        controller.AddParameter(parameterName, parameterType);
    }

    static void RemoveParameter(AnimatorController controller, string parameterName)
    {
        var parameters = controller.parameters.ToList();
        for (int index = parameters.Count - 1; index >= 0; index--)
        {
            if (parameters[index].name == parameterName)
                parameters.RemoveAt(index);
        }

        controller.parameters = parameters.ToArray();
    }

    static void SetDefaultBool(AnimatorController controller, string parameterName, bool value)
    {
        var parameters = controller.parameters;
        for (int index = 0; index < parameters.Length; index++)
        {
            if (parameters[index].name != parameterName || parameters[index].type != AnimatorControllerParameterType.Bool)
                continue;

            parameters[index].defaultBool = value;
            controller.parameters = parameters;
            return;
        }
    }

    static bool AssetExists(string path)
    {
        return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null;
    }

    static bool EnsureCharacterPrefabExists(CharacterSpec spec, bool createMissingFromTemplate)
    {
        if (string.IsNullOrEmpty(spec.PrefabPath))
            return false;

        if (AssetExists(spec.PrefabPath))
            return true;

        if (!createMissingFromTemplate)
            return false;

        return TryCreateMissingPrefabFromTemplate(spec);
    }

    static bool TryCreateMissingPrefabFromTemplate(CharacterSpec spec)
    {
        string templatePath = FindFirstExistingPath(
            $"{PrefabRoot}/Knight.prefab",
            $"{PrefabRoot}/Ninja.prefab");

        if (string.IsNullOrEmpty(templatePath))
        {
            Debug.LogWarning($"[CharacterAnimationBuilder] Could not create missing prefab for {spec.CharacterName}. No template prefab found.");
            return false;
        }

        var root = PrefabUtility.LoadPrefabContents(templatePath);
        try
        {
            root.name = spec.CharacterName;

            var characterType = root.GetComponent<CharacterType>();
            if (characterType == null)
                characterType = root.AddComponent<CharacterType>();
            characterType.character = ResolveCharacterEnum(spec.CharacterName);

            var animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            Debug.Log($"[CharacterAnimationBuilder] Created missing prefab from template: {spec.PrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetExists(spec.PrefabPath);
    }

    static string FindFirstExistingPath(params string[] candidatePaths)
    {
        if (candidatePaths == null)
            return null;

        for (int index = 0; index < candidatePaths.Length; index++)
        {
            string path = candidatePaths[index];
            if (!string.IsNullOrWhiteSpace(path) && AssetExists(path))
                return path;
        }

        return null;
    }

    static CharacterType.Character ResolveCharacterEnum(string characterName)
    {
        if (string.Equals(characterName, "Ninja", StringComparison.OrdinalIgnoreCase))
            return CharacterType.Character.Ninja;

        if (string.Equals(characterName, "Sorcerer", StringComparison.OrdinalIgnoreCase))
            return CharacterType.Character.Sorcerer;

        return CharacterType.Character.Knight;
    }

    static string ResolveSourcePath(string folderPath, string[] candidates)
    {
        if (candidates == null)
            return null;

        for (int index = 0; index < candidates.Length; index++)
        {
            var candidate = candidates[index];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            string path = $"{folderPath}/{candidate}.png";
            if (AssetExists(path))
                return path;
        }

        return null;
    }

    static void EnsureFolder(string parentPath, string childFolder)
    {
        string fullPath = $"{parentPath}/{childFolder}";
        if (AssetDatabase.IsValidFolder(fullPath))
            return;

        AssetDatabase.CreateFolder(parentPath, childFolder);
    }

    static IEnumerable<CharacterSpec> GetCharacterSpecs()
    {
        yield return new CharacterSpec(
            "Knight",
            "Assets/Prefabs/Knight.prefab",
            comboCount: 3,
            useShield: true,
            useCharge: false,
            clips: new List<ClipSpec>
            {
                new ClipSpec("Idle", loop: true, sourcePngFileNames: new[] { "KnightIdle" }),
                new ClipSpec("Walk", loop: true, sourcePngFileNames: new[] { "KnightWalk" }),
                new ClipSpec("Run", loop: true, sourcePngFileNames: new[] { "KnightRun" }),
                new ClipSpec("Jump", loop: false, sourcePngFileNames: new[] { "KnightJump" }),
                new ClipSpec("Attack_1", loop: false, sourcePngFileNames: new[] { "KnightAttack_1" }),
                new ClipSpec("Attack_2", loop: false, sourcePngFileNames: new[] { "KnightAttack_2" }),
                new ClipSpec("Attack_3", loop: false, sourcePngFileNames: new[] { "KnightAttack_3" }),
                new ClipSpec("Hurt", loop: false, sourcePngFileNames: new[] { "KnightHurt" }),
                new ClipSpec("Death", loop: false, sourcePngFileNames: new[] { "KnightDead" }),
                new ClipSpec("Shield", loop: true, optional: true, sourcePngFileNames: new[] { "KnightShield" })
            });

        yield return new CharacterSpec(
            "Ninja",
            "Assets/Prefabs/Ninja.prefab",
            comboCount: 3,
            useShield: true,
            useCharge: false,
            clips: new List<ClipSpec>
            {
                new ClipSpec("Idle", loop: true, sourcePngFileNames: new[] { "NinjaIdle" }),
                new ClipSpec("Walk", loop: true, sourcePngFileNames: new[] { "NinjaWalk" }),
                new ClipSpec("Run", loop: true, sourcePngFileNames: new[] { "NinjaRun" }),
                new ClipSpec("Jump", loop: false, sourcePngFileNames: new[] { "NinjaJump" }),
                new ClipSpec("Attack_1", loop: false, sourcePngFileNames: new[] { "NinjaAttack_1" }),
                new ClipSpec("Attack_2", loop: false, sourcePngFileNames: new[] { "NinjaAttack_2" }),
                new ClipSpec("Attack_3", loop: false, sourcePngFileNames: new[] { "NinjaAttack_3" }),
                new ClipSpec("Hurt", loop: false, sourcePngFileNames: new[] { "NinjaHurt" }),
                new ClipSpec("Death", loop: false, sourcePngFileNames: new[] { "NinjaDead" }),
                new ClipSpec("Shield", loop: true, optional: true, sourcePngFileNames: new[] { "NinjaShield" })
            });

        yield return new CharacterSpec(
            "Sorcerer",
            "Assets/Prefabs/Sorcerer.prefab",
            comboCount: 3,
            useShield: false,
            useCharge: true,
            clips: new List<ClipSpec>
            {
                new ClipSpec("Idle", loop: true, sourcePngFileNames: new[] { "SorcererIdle" }),
                new ClipSpec("Walk", loop: true, sourcePngFileNames: new[] { "SorcererWalk" }),
                new ClipSpec("Run", loop: true, sourcePngFileNames: new[] { "SorcererRun" }),
                // No dedicated Jump sheet found for Sorcerer in current project.
                new ClipSpec("Attack_1", loop: false, sourcePngFileNames: new[] { "SorcererAttack_1" }),
                new ClipSpec("Attack_2", loop: false, sourcePngFileNames: new[] { "SorcererAttack_2" }),
                new ClipSpec("Attack_3", loop: false, sourcePngFileNames: new[] { "SorcererAttack_3" }),
                new ClipSpec("Hurt", loop: false, sourcePngFileNames: new[] { "SorcererHurt" }),
                new ClipSpec("Death", loop: false, sourcePngFileNames: new[] { "SorcererDead" }),
                new ClipSpec("Charge", loop: false, optional: true, sourcePngFileNames: new[] { "SorcererCharge", "SorcererCharge_1", "SorcererCharge_2" })
            });
    }
}
