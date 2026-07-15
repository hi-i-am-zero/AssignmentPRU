using UnityEngine;
using ArenaEnvironment = Environment;
using SkyfallArena.Systems;
using SkyfallArena.Systems.Items;
using SkyfallArena.Multiplayer;
using System.Collections.Generic;

public class AttackController : MonoBehaviour
{
    [Header("Attack Settings")]
    public Transform attackPoint;
    public LayerMask playerLayer;

    [Header("Item Drop On Hit")]
    [SerializeField] bool dropItemOnHit = true;
    [SerializeField, Range(0f, 1f)] float dropChanceOnHit = 0.2f;
    [SerializeField] ItemSpawner itemSpawner;

    [Header("Sorcerer Ranged Attack")]
    [SerializeField] bool useRangedProjectilesForSorcerer = true;
    [SerializeField, Min(0.5f)] float sorcererProjectileSpeed = 11f;
    [SerializeField, Min(0.5f)] float sorcererFallbackRange = 6f;
    [SerializeField, Range(0.05f, 1f)] float sorcererRangeFractionOfMapWidth = 0.25f;
    [SerializeField, Min(0.05f)] float sorcererProjectileRadius = 0.2f;
    [SerializeField] Vector2 sorcererProjectileSpawnOffset = new Vector2(0.65f, 0f);
    [SerializeField] Color sorcererProjectileTint = new Color(0.65f, 0.9f, 1f, 0.95f);
    [SerializeField] Sprite sorcererProjectileType1Sprite;
    [SerializeField] Sprite sorcererProjectileType2Sprite;
    [SerializeField] LayerMask sorcererProjectileBlockLayers;

    [Header("Hit Feel")]
    [SerializeField, Min(0f)] float hitStopDuration = 0.045f;
    [SerializeField] bool requireFacingTarget = true;
    [SerializeField, Range(-1f, 1f)] float minFacingDot = -0.15f;

    CharacterStatus stats;
    PlayerController playerController;
    ComboController comboController;
    KnockbackController selfKnockback;
    Animator animator;
    SpriteRenderer spriteRenderer;
    CharacterType characterType;
    LocalPlayerInputSource inputSource;
    PlayerBlockController blockController;

    float nextAttackTime;
    float cachedArenaWidth;
    float nextArenaWidthSampleTime;
    float hitStopUntil;
    readonly Collider2D[] overlapBuffer = new Collider2D[16];

    void Awake()
    {
        stats = GetComponent<CharacterStatus>();
        playerController = GetComponent<PlayerController>();
        comboController = GetComponent<ComboController>();
        selfKnockback = GetComponent<KnockbackController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterType = GetComponent<CharacterType>();
        inputSource = GetComponent<LocalPlayerInputSource>();
        blockController = GetComponent<PlayerBlockController>();

        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();

        if (sorcererProjectileBlockLayers.value == 0)
            sorcererProjectileBlockLayers = LayerMask.GetMask(ArenaEnvironment.GameLayers.Ground);

        EnsurePlayerLayerMask();
    }

    void EnsurePlayerLayerMask()
    {
        if (playerLayer.value != 0)
            return;

        int mask = LayerMask.GetMask(ArenaEnvironment.GameLayers.Player);
        if (mask != 0)
            playerLayer = mask;
    }

    void Update()
    {
        if (Time.time < hitStopUntil)
            return;

        HandleAttackInput();
    }

    void HandleAttackInput()
    {
        if (stats == null || playerController == null)
            return;

        if (selfKnockback != null && selfKnockback.IsKnocked)
            return;

        if (blockController != null && blockController.IsBlocking)
            return;

        int attackSlot = ReadAttackSlotPressed();
        if (attackSlot <= 0)
            return;

        if (!IsAttackSlotAllowed(attackSlot))
            return;

        if (Time.time < nextAttackTime)
            return;

        EnsureCombatStatsReady();
        nextAttackTime = Time.time + Mathf.Max(0.05f, stats.attackCooldown);
        Attack(attackSlot);
    }

    void EnsureCombatStatsReady()
    {
        if (stats == null)
            stats = GetComponent<CharacterStatus>();

        if (stats == null)
            return;

        if (stats.damage > 0 && stats.attackRange > 0f)
            return;

        var initializer = GetComponent<CharacterInitializer>();
        if (initializer != null)
            initializer.ApplyStats();

        if (stats.damage <= 0)
            stats.damage = 15;
        if (stats.attackRange <= 0f)
            stats.attackRange = 1.25f;
        if (stats.attackCooldown <= 0f)
            stats.attackCooldown = 0.4f;
        if (stats.knockbackForce <= 0f)
            stats.knockbackForce = 6f;
    }

    int ReadAttackSlotPressed()
    {
        if (playerController.playerType == PlayerController.PlayerType.Player1)
        {
            if (Input.GetKeyDown(KeyCode.J)) return 1;
            if (Input.GetKeyDown(KeyCode.K)) return 2;
            if (Input.GetKeyDown(KeyCode.L)) return 3;
            if (Input.GetKeyDown(KeyCode.U)) return 4;
            return 0;
        }

        if (Input.GetKeyDown(KeyCode.Keypad1)) return 1;
        if (Input.GetKeyDown(KeyCode.Keypad2)) return 2;
        if (Input.GetKeyDown(KeyCode.Keypad3)) return 3;
        if (Input.GetKeyDown(KeyCode.Keypad4)) return 4;
        return 0;
    }

    bool IsAttackSlotAllowed(int attackSlot)
    {
        int maxCombo = comboController != null ? Mathf.Max(1, comboController.maxCombo) : 3;
        if (attackSlot < 1 || attackSlot > maxCombo)
            return false;

        if (attackSlot == 4)
        {
            return characterType != null
                && characterType.character == CharacterType.Character.Sorcerer;
        }

        return true;
    }

    void Attack(int comboIndex)
    {
        if (stats == null)
            return;

        if (attackPoint == null)
            attackPoint = transform;

        if (comboController != null)
            comboController.SetComboStep(comboIndex);

        if (animator != null)
        {
            animator.SetInteger("Combo", comboIndex);
            animator.SetTrigger("Attack");
        }

        if (ShouldUseSorcererProjectile())
        {
            FireSorcererProjectile(comboIndex);
            return;
        }

        Vector2 attackCenter = GetAttackCenter();
        float radius = GetEffectiveAttackRange();
        int hitCount = ReadTargetsInRange(attackCenter, radius);

        var processedTargets = new HashSet<GameObject>();
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D target = overlapBuffer[i];
            if (target == null)
                continue;

            GameObject targetRoot = ResolveTargetRoot(target);
            if (targetRoot == null)
                continue;

            if (!processedTargets.Add(targetRoot))
                continue;

            if (targetRoot == gameObject)
                continue;

            if (!IsValidCombatTarget(targetRoot))
                continue;

            if (requireFacingTarget && !IsTargetInAttackFacing(targetRoot.transform.position))
                continue;

            ApplyHitToTarget(targetRoot, transform.position);
        }
    }

    float GetEffectiveAttackRange()
    {
        float baseRange = stats != null ? Mathf.Max(0.35f, stats.attackRange) : 1.25f;
        float scale = Mathf.Max(1f, Mathf.Abs(transform.lossyScale.x));
        return baseRange * scale;
    }

    bool IsValidCombatTarget(GameObject targetRoot)
    {
        if (targetRoot == null)
            return false;

        if (targetRoot.GetComponent<PlayerController>() != null)
            return true;

        if (targetRoot.GetComponent<PlayerHealth>() != null)
            return true;

        if (targetRoot.GetComponent<PlayerIdentity>() != null)
            return true;

        return false;
    }

    bool IsTargetInAttackFacing(Vector2 targetPosition)
    {
        Vector2 toTarget = targetPosition - (Vector2)transform.position;
        if (toTarget.sqrMagnitude < 0.0001f)
            return true;

        Vector2 facing = Vector2.right;
        if (spriteRenderer != null && spriteRenderer.flipX)
            facing = Vector2.left;

        return Vector2.Dot(facing, toTarget.normalized) >= minFacingDot;
    }

    void ApplyHitToTarget(GameObject targetRoot, Vector2 attackOrigin)
    {
        if (targetRoot == null || stats == null)
            return;

        float damageAmount = Mathf.Max(1f, stats.damage);
        float knockbackForce = Mathf.Max(0f, stats.knockbackForce);

        var targetBlock = targetRoot.GetComponent<PlayerBlockController>();
        if (targetBlock != null && targetBlock.IsBlocking)
            knockbackForce *= targetBlock.KnockbackMultiplierWhileBlocking;

        var knockback = targetRoot.GetComponent<KnockbackController>();
        if (knockback != null)
            knockback.ApplyKnockback(attackOrigin, knockbackForce);

        if (TryApplyDamage(targetRoot, damageAmount))
        {
            TryDropItemOnHit(targetRoot.transform.position);
            TriggerHitStop();
        }
    }

    public static bool TryApplyDamage(GameObject targetRoot, float damageAmount)
    {
        if (targetRoot == null || damageAmount <= 0f)
            return false;

        var health = targetRoot.GetComponent<PlayerHealth>();
        if (health == null)
            health = targetRoot.GetComponentInChildren<PlayerHealth>();
        if (health == null)
            health = targetRoot.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            if (!health.IsAlive)
                return false;

            health.TakeDamage(damageAmount);
            return true;
        }

        var damageable = targetRoot.GetComponent<ArenaEnvironment.IDamageable>();
        if (damageable == null)
            damageable = targetRoot.GetComponentInChildren<ArenaEnvironment.IDamageable>();

        if (damageable == null || !damageable.IsAlive)
            return false;

        damageable.TakeDamage(damageAmount);
        return true;
    }

    void TriggerHitStop()
    {
        if (hitStopDuration <= 0f)
            return;

        hitStopUntil = Time.time + hitStopDuration;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        float radius = 1f;

        CharacterStatus status = GetComponent<CharacterStatus>();
        if (status != null)
            radius = Mathf.Max(0.35f, status.attackRange) * Mathf.Max(1f, Mathf.Abs(transform.lossyScale.x));

        Gizmos.DrawWireSphere(GetAttackCenter(), radius);
    }

    Vector2 GetAttackCenter()
    {
        if (attackPoint == null)
            return transform.position;

        if (spriteRenderer == null || attackPoint.parent != transform)
            return attackPoint.position;

        float localX = Mathf.Abs(attackPoint.localPosition.x);
        float x = spriteRenderer.flipX ? -localX : localX;
        return (Vector2)transform.position + new Vector2(x, attackPoint.localPosition.y);
    }

    int ReadTargetsInRange(Vector2 center, float radius)
    {
        EnsurePlayerLayerMask();

        var filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = playerLayer.value != 0;
        if (filter.useLayerMask)
            filter.SetLayerMask(playerLayer);

        return Physics2D.OverlapCircle(center, radius, filter, overlapBuffer);
    }

    static GameObject ResolveTargetRoot(Collider2D target)
    {
        if (target == null)
            return null;

        var health = target.GetComponentInParent<PlayerHealth>();
        if (health != null)
            return health.gameObject;

        var controller = target.GetComponentInParent<PlayerController>();
        if (controller != null)
            return controller.gameObject;

        if (target.attachedRigidbody != null)
            return target.attachedRigidbody.gameObject;

        if (target.transform.root != null)
            return target.transform.root.gameObject;

        return target.gameObject;
    }

    void TryDropItemOnHit(Vector2 hitPosition)
    {
        if (!dropItemOnHit || itemSpawner == null || dropChanceOnHit <= 0f)
            return;

        if (Random.value > dropChanceOnHit)
            return;

        itemSpawner.TrySpawnHitDrop(hitPosition);
    }

    bool ShouldUseSorcererProjectile()
    {
        return useRangedProjectilesForSorcerer
            && characterType != null
            && characterType.character == CharacterType.Character.Sorcerer;
    }

    void FireSorcererProjectile(int comboIndex)
    {
        EnsurePlayerLayerMask();

        Vector2 direction = Vector2.right;
        if (spriteRenderer != null && spriteRenderer.flipX)
            direction = Vector2.left;

        Vector2 spawnPosition = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
        spawnPosition += new Vector2(
            direction.x * Mathf.Abs(sorcererProjectileSpawnOffset.x),
            sorcererProjectileSpawnOffset.y);

        int projectileType = comboIndex % 2 == 0 ? 2 : 1;
        Sprite projectileSprite = projectileType == 1 ? sorcererProjectileType1Sprite : sorcererProjectileType2Sprite;

        float speed = Mathf.Max(0.5f, sorcererProjectileSpeed);
        float fixedRange = CalculateSorcererProjectileRange();
        float lifetime = Mathf.Max(0.05f, fixedRange / speed);

        SorcererProjectile.Spawn(
            owner: gameObject,
            spawnPosition: spawnPosition,
            direction: direction,
            damage: stats != null ? Mathf.Max(1, stats.damage) : 1,
            knockbackForce: stats != null ? stats.knockbackForce : 0f,
            speed: speed,
            lifetime: lifetime,
            radius: sorcererProjectileRadius,
            targetLayerMask: playerLayer,
            blockLayerMask: sorcererProjectileBlockLayers,
            sprite: projectileSprite,
            tint: sorcererProjectileTint,
            itemSpawner: itemSpawner,
            allowHitDrop: dropItemOnHit,
            dropChanceOnHit: dropChanceOnHit);
    }

    float CalculateSorcererProjectileRange()
    {
        if (Time.time >= nextArenaWidthSampleTime || cachedArenaWidth <= 0f)
        {
            if (TryGetArenaWidth(out var sampledWidth))
                cachedArenaWidth = sampledWidth;

            nextArenaWidthSampleTime = Time.time + 1f;
        }

        if (cachedArenaWidth > 0f)
        {
            float fraction = Mathf.Clamp(sorcererRangeFractionOfMapWidth, 0.05f, 1f);
            return Mathf.Max(0.5f, cachedArenaWidth * fraction);
        }

        return Mathf.Max(0.5f, sorcererFallbackRange);
    }

    static bool TryGetArenaWidth(out float width)
    {
        width = 0f;

        var boundaries = FindObjectsByType<ArenaEnvironment.ArenaBoundary>(FindObjectsSortMode.None);
        if (boundaries != null && boundaries.Length > 0)
        {
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;

            for (int index = 0; index < boundaries.Length; index++)
            {
                var boundary = boundaries[index];
                if (boundary == null)
                    continue;

                var collider = boundary.GetComponent<Collider2D>();
                if (collider == null)
                    continue;

                var bounds = collider.bounds;
                minX = Mathf.Min(minX, bounds.min.x);
                maxX = Mathf.Max(maxX, bounds.max.x);
            }

            if (!float.IsInfinity(minX) && !float.IsInfinity(maxX) && maxX > minX)
            {
                width = maxX - minX;
                return true;
            }
        }

        var camera = Camera.main;
        if (camera == null)
            camera = FindFirstObjectByType<Camera>();

        if (camera != null && camera.orthographic)
        {
            width = camera.orthographicSize * 2f * Mathf.Max(0.1f, camera.aspect);
            return width > 0f;
        }

        return false;
    }
}
