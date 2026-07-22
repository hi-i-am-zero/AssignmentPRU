using System.Collections.Generic;
using SkyfallArena.Systems;
using UnityEngine;

/// <summary>
/// Projectile chieu dac biet (Shuriken / Fireball), pool theo ten.
/// </summary>
[DisallowMultipleComponent]
public sealed class SkillProjectile : MonoBehaviour
{
    static readonly Dictionary<string, Queue<SkillProjectile>> Pools = new Dictionary<string, Queue<SkillProjectile>>();

    string poolKey;
    GameObject owner;
    Vector2 direction;
    int damage;
    float knockbackForce;
    float speed;
    float lifetime;
    LayerMask targetLayerMask;
    LayerMask blockLayerMask;
    bool countsTowardOwnerMeter;
    Sprite[] frames;
    float frameTimer;
    int frameIndex;
    float frameDuration = 1f / 12f;

    Rigidbody2D body;
    CircleCollider2D triggerCollider;
    SpriteRenderer spriteRenderer;
    bool hasAppliedHit;
    float livedTime;

    public static SkillProjectile Spawn(
        GameObject owner,
        Vector2 spawnPosition,
        Vector2 direction,
        int damage,
        float knockbackForce,
        float speed,
        float lifetime,
        float radius,
        LayerMask targetLayerMask,
        LayerMask blockLayerMask,
        Sprite sprite,
        Sprite[] frames,
        Color tint,
        float visualWorldSize,
        string poolObjectName,
        bool countsTowardOwnerMeter)
    {
        var projectile = Acquire(poolObjectName);
        projectile.transform.position = spawnPosition;
        projectile.gameObject.SetActive(true);

        if (projectile.triggerCollider != null)
            projectile.triggerCollider.radius = Mathf.Max(0.05f, radius);

        projectile.frames = frames != null && frames.Length > 0 ? frames : null;
        projectile.frameIndex = 0;
        projectile.frameTimer = 0f;

        Sprite display = sprite;
        if (display == null && projectile.frames != null)
            display = projectile.frames[0];

        ConfigureVisual(projectile.spriteRenderer, display, tint, direction.x < 0f, visualWorldSize);

        projectile.owner = owner;
        projectile.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        projectile.damage = Mathf.Max(1, damage);
        projectile.knockbackForce = Mathf.Max(0f, knockbackForce);
        projectile.speed = Mathf.Max(0.5f, speed);
        projectile.lifetime = Mathf.Max(0.05f, lifetime);
        projectile.targetLayerMask = targetLayerMask;
        projectile.blockLayerMask = blockLayerMask;
        projectile.countsTowardOwnerMeter = countsTowardOwnerMeter;
        projectile.hasAppliedHit = false;
        projectile.livedTime = 0f;
        return projectile;
    }

    static void ConfigureVisual(
        SpriteRenderer renderer,
        Sprite sprite,
        Color tint,
        bool flipX,
        float visualWorldSize)
    {
        if (renderer == null)
            return;

        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.flipX = flipX;
        renderer.sortingOrder = 25;

        if (sprite == null)
        {
            renderer.transform.localScale = Vector3.one;
            return;
        }

        float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        float target = Mathf.Max(0.2f, visualWorldSize);
        float scale = size > 0.0001f ? target / size : 1f;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    static SkillProjectile Acquire(string poolKey)
    {
        if (string.IsNullOrEmpty(poolKey))
            poolKey = "SkillProjectile";

        if (!Pools.TryGetValue(poolKey, out var pool))
        {
            pool = new Queue<SkillProjectile>();
            Pools[poolKey] = pool;
        }

        while (pool.Count > 0)
        {
            var pooled = pool.Dequeue();
            if (pooled != null)
                return pooled;
        }

        var go = new GameObject(poolKey);
        var projectile = go.AddComponent<SkillProjectile>();
        projectile.poolKey = poolKey;

        var rigidBody = go.AddComponent<Rigidbody2D>();
        rigidBody.gravityScale = 0f;
        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.freezeRotation = true;
        rigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.2f;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 25;
        projectile.body = rigidBody;
        projectile.triggerCollider = collider;
        projectile.spriteRenderer = sr;
        return projectile;
    }

    void Awake()
    {
        if (body == null)
            body = GetComponent<Rigidbody2D>();
        if (triggerCollider == null)
            triggerCollider = GetComponent<CircleCollider2D>();
        if (spriteRenderer == null)
        {
            var visual = transform.Find("Visual");
            if (visual != null)
                spriteRenderer = visual.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (hasAppliedHit || body == null)
            return;

        livedTime += Time.deltaTime;
        if (livedTime >= lifetime)
        {
            Recycle();
            return;
        }

        AnimateFrames();
        body.MovePosition(body.position + direction * (speed * Time.deltaTime));
    }

    void AnimateFrames()
    {
        if (frames == null || frames.Length <= 1 || spriteRenderer == null)
            return;

        frameTimer += Time.deltaTime;
        if (frameTimer < frameDuration)
            return;

        frameTimer = 0f;
        frameIndex = (frameIndex + 1) % frames.Length;
        if (frames[frameIndex] != null)
            spriteRenderer.sprite = frames[frameIndex];
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasAppliedHit || other == null)
            return;

        if (IsBlockedByEnvironment(other))
        {
            hasAppliedHit = true;
            Recycle();
            return;
        }

        GameObject targetRoot = ResolveTargetRoot(other);
        if (targetRoot == null)
            return;

        if (owner != null && targetRoot == owner)
            return;

        if (targetRoot.GetComponent<PlayerController>() == null
            && targetRoot.GetComponent<PlayerHealth>() == null)
            return;

        hasAppliedHit = true;

        float appliedKnockback = knockbackForce;
        var targetBlock = targetRoot.GetComponent<PlayerBlockController>();
        if (targetBlock != null && targetBlock.IsBlocking)
            appliedKnockback *= targetBlock.KnockbackMultiplierWhileBlocking;

        var knockback = targetRoot.GetComponent<KnockbackController>();
        if (knockback != null)
            knockback.ApplyKnockback(transform.position, appliedKnockback);

        bool damaged = AttackController.TryApplyDamage(targetRoot, damage);
        if (damaged && countsTowardOwnerMeter && owner != null)
        {
            var meter = owner.GetComponent<SpecialAbilityController>();
            if (meter != null)
                meter.RegisterSuccessfulHit();
        }

        Recycle();
    }

    bool IsBlockedByEnvironment(Collider2D other)
    {
        if (other == null || blockLayerMask.value == 0)
            return false;

        int layerBit = 1 << other.gameObject.layer;
        return (blockLayerMask.value & layerBit) != 0;
    }

    void Recycle()
    {
        gameObject.SetActive(false);
        if (string.IsNullOrEmpty(poolKey))
            poolKey = gameObject.name;

        if (!Pools.TryGetValue(poolKey, out var pool))
        {
            pool = new Queue<SkillProjectile>();
            Pools[poolKey] = pool;
        }

        pool.Enqueue(this);
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

        return target.transform.root != null ? target.transform.root.gameObject : target.gameObject;
    }
}
