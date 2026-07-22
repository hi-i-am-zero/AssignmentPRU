using System;
using SkyfallArena.Systems;
using UnityEngine;
using ArenaEnvironment = Environment;

/// <summary>
/// Tich don danh trung (max 3). Knight hoi mau bi dong; Ninja/Sorcerer chieu chu dong (I / Numpad5).
/// </summary>
[DisallowMultipleComponent]
public sealed class SpecialAbilityController : MonoBehaviour
{
    public const int MaxStacks = 3;

    [Header("Knight Passive")]
    [SerializeField, Range(0.01f, 0.5f)] float knightHealFraction = 0.07f;

    [Header("Special Projectile")]
    [SerializeField, Min(1f)] float ninjaSpecialDamageMultiplier = 1.8f;
    [SerializeField, Min(1f)] float sorcererSpecialDamageMultiplier = 1.6f;
    [SerializeField, Min(0.5f)] float projectileSpeed = 12f;
    [SerializeField, Min(0.05f)] float projectileRadius = 0.22f;
    [SerializeField] Vector2 projectileSpawnOffset = new Vector2(0.7f, 0.1f);
    [SerializeField] Sprite shurikenSprite;
    [SerializeField] Sprite fireballSprite;
    [SerializeField] Sprite[] shurikenFrames;
    [SerializeField] Sprite[] fireballFrames;
    [SerializeField] Color shurikenTint = Color.white;
    [SerializeField] Color fireballTint = new Color(1f, 0.55f, 0.2f, 1f);
    [SerializeField, Min(0.2f)] float projectileVisualSize = 0.7f;

    CharacterType characterType;
    PlayerController playerController;
    PlayerHealth playerHealth;
    PlayerIdentity playerIdentity;
    CharacterStatus stats;
    CharacterAnimationSync animationSync;
    SpriteRenderer spriteRenderer;
    Transform attackPoint;
    PlayerBlockController blockController;
    KnockbackController selfKnockback;

    int stacks;

    public int CurrentStacks => stacks;

    public int PlayerId
    {
        get
        {
            if (playerIdentity != null && playerIdentity.PlayerId > 0)
                return playerIdentity.PlayerId;

            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (playerController != null)
            {
                if (playerController.playerType == PlayerController.PlayerType.Player1)
                    return 1;
                if (playerController.playerType == PlayerController.PlayerType.Player2)
                    return 2;
            }

            return 0;
        }
    }

    public float Normalized => MaxStacks <= 0 ? 0f : (float)stacks / MaxStacks;

    public event Action<SpecialAbilityController, int, int> MeterChanged;

    void Awake()
    {
        characterType = GetComponent<CharacterType>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
        playerIdentity = GetComponent<PlayerIdentity>();
        stats = GetComponent<CharacterStatus>();
        animationSync = GetComponent<CharacterAnimationSync>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockController = GetComponent<PlayerBlockController>();
        selfKnockback = GetComponent<KnockbackController>();

        var attack = GetComponent<AttackController>();
        if (attack != null && attack.attackPoint != null)
            attackPoint = attack.attackPoint;
        else
        {
            var found = transform.Find("AttackPoint");
            attackPoint = found != null ? found : transform;
        }

        EnsureSpritesReady();
    }

    void Update()
    {
        if (!CanUseActiveSpecial())
            return;

        if (!IsSpecialKeyPressed())
            return;

        TryActivateActiveSpecial();
    }

    /// <summary>Goi khi chu so huu gay damage thanh cong bang don thuong.</summary>
    public void RegisterSuccessfulHit()
    {
        if (playerHealth != null && !playerHealth.IsAlive)
            return;

        if (stacks >= MaxStacks)
            return;

        stacks++;
        BroadcastMeter();

        if (stacks < MaxStacks)
            return;

        if (IsKnight())
            TriggerKnightPassive();
    }

    public void SetSprites(Sprite shuriken, Sprite fireball)
    {
        if (shuriken != null)
            shurikenSprite = shuriken;
        if (fireball != null)
            fireballSprite = fireball;
    }

    public void SetSpriteFrames(Sprite[] shuriken, Sprite[] fireball)
    {
        if (shuriken != null && shuriken.Length > 0)
        {
            shurikenFrames = shuriken;
            shurikenSprite = shuriken[0];
        }

        if (fireball != null && fireball.Length > 0)
        {
            fireballFrames = fireball;
            fireballSprite = fireball[0];
        }
    }

    void TriggerKnightPassive()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null && playerHealth.IsAlive)
        {
            float healAmount = playerHealth.MaxHealth * Mathf.Clamp01(knightHealFraction);
            playerHealth.Heal(healAmount);
        }

        ResetStacks();
    }

    void TryActivateActiveSpecial()
    {
        if (stacks < MaxStacks)
            return;

        if (blockController != null && blockController.IsBlocking)
            return;

        if (selfKnockback != null && selfKnockback.IsKnocked)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
            return;

        EnsureSpritesReady();

        if (IsNinja())
        {
            if (!HasUsableSprite(shurikenSprite, shurikenFrames))
            {
                Debug.LogWarning("[SpecialAbility] Missing Shuriken sprite on Ninja.", this);
                return;
            }

            FireProjectile(
                ResolveSprite(shurikenSprite, shurikenFrames),
                shurikenFrames,
                shurikenTint,
                "NinjaShuriken",
                ninjaSpecialDamageMultiplier);
        }
        else if (IsSorcerer())
        {
            if (!HasUsableSprite(fireballSprite, fireballFrames))
            {
                Debug.LogWarning("[SpecialAbility] Missing Fireball sprite on Sorcerer.", this);
                return;
            }

            FireProjectile(
                ResolveSprite(fireballSprite, fireballFrames),
                fireballFrames,
                fireballTint,
                "SorcererFireball",
                sorcererSpecialDamageMultiplier);
        }
        else
            return;

        if (animationSync != null)
            animationSync.TriggerSpecial();

        ResetStacks();
    }

    void FireProjectile(
        Sprite sprite,
        Sprite[] frames,
        Color tint,
        string poolName,
        float damageMultiplier)
    {
        Vector2 direction = Vector2.right;
        if (spriteRenderer != null && spriteRenderer.flipX)
            direction = Vector2.left;

        Vector2 spawn = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
        spawn += new Vector2(direction.x * Mathf.Abs(projectileSpawnOffset.x), projectileSpawnOffset.y);

        int damage = 15;
        float knockback = 6f;
        if (stats != null)
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(stats.damage * damageMultiplier));
            knockback = Mathf.Max(0f, stats.knockbackForce);
        }

        LayerMask playerMask = LayerMask.GetMask(ArenaEnvironment.GameLayers.Player);
        LayerMask groundMask = LayerMask.GetMask(ArenaEnvironment.GameLayers.Ground);

        SkillProjectile.Spawn(
            owner: gameObject,
            spawnPosition: spawn,
            direction: direction,
            damage: damage,
            knockbackForce: knockback,
            speed: projectileSpeed,
            lifetime: 1.6f,
            radius: projectileRadius,
            targetLayerMask: playerMask,
            blockLayerMask: groundMask,
            sprite: sprite,
            frames: frames,
            tint: tint,
            visualWorldSize: projectileVisualSize,
            poolObjectName: poolName,
            countsTowardOwnerMeter: false);
    }

    void ResetStacks()
    {
        stacks = 0;
        BroadcastMeter();
    }

    void BroadcastMeter()
    {
        MeterChanged?.Invoke(this, stacks, MaxStacks);
    }

    bool CanUseActiveSpecial()
    {
        return stacks >= MaxStacks && (IsNinja() || IsSorcerer());
    }

    bool IsSpecialKeyPressed()
    {
        if (playerController == null)
            return false;

        if (playerController.playerType == PlayerController.PlayerType.Player1)
            return Input.GetKeyDown(KeyCode.I);

        return Input.GetKeyDown(KeyCode.Keypad5);
    }

    bool IsKnight() =>
        characterType != null && characterType.character == CharacterType.Character.Knight;

    bool IsNinja() =>
        characterType != null && characterType.character == CharacterType.Character.Ninja;

    bool IsSorcerer() =>
        characterType != null && characterType.character == CharacterType.Character.Sorcerer;

    static bool HasUsableSprite(Sprite primary, Sprite[] frames)
    {
        if (primary != null)
            return true;
        return frames != null && frames.Length > 0 && frames[0] != null;
    }

    static Sprite ResolveSprite(Sprite primary, Sprite[] frames)
    {
        if (primary != null)
            return primary;
        if (frames != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                    return frames[i];
            }
        }

        return null;
    }

    void EnsureSpritesReady()
    {
        if (shurikenSprite == null && (shurikenFrames == null || shurikenFrames.Length == 0))
            TryLoadSheet("Assets/Sprite/Ninja/Shuriken.png", out shurikenSprite, out shurikenFrames);

        if (fireballSprite == null && (fireballFrames == null || fireballFrames.Length == 0))
            TryLoadSheet("Assets/Sprite/Sorcerer/Fireball.png", out fireballSprite, out fireballFrames);

        if (shurikenSprite == null && shurikenFrames != null && shurikenFrames.Length > 0)
            shurikenSprite = shurikenFrames[0];
        if (fireballSprite == null && fireballFrames != null && fireballFrames.Length > 0)
            fireballSprite = fireballFrames[0];
    }

    static void TryLoadSheet(string assetPath, out Sprite primary, out Sprite[] frames)
    {
        primary = null;
        frames = null;

#if UNITY_EDITOR
        var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (loaded == null || loaded.Length == 0)
            return;

        var list = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] is Sprite sprite)
                list.Add(sprite);
        }

        if (list.Count == 0)
            return;

        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        frames = list.ToArray();
        primary = frames[0];
#endif
    }
}
