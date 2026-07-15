using SkyfallArena.Systems;
using SkyfallArena.Systems.Items;
using UnityEngine;
using ArenaEnvironment = Environment;
using System.Collections.Generic;

/// <summary>
/// Đạn tầm xa Sorcerer (pool): bay theo hướng, gây damage + knockback khi chạm địch.
/// </summary>
[DisallowMultipleComponent]
public sealed class SorcererProjectile : MonoBehaviour
{
    static readonly Queue<SorcererProjectile> Pool = new Queue<SorcererProjectile>();

    GameObject owner;
    Vector2 direction;
    int damage;
    float knockbackForce;
    float speed;
    float lifetime;
    LayerMask targetLayerMask;
    LayerMask blockLayerMask;
    ItemSpawner itemSpawner;
    bool allowHitDrop;
    float dropChanceOnHit;

    Rigidbody2D body;
    CircleCollider2D triggerCollider;
    SpriteRenderer spriteRenderer;
    bool hasAppliedHit;
    float livedTime;

    public static SorcererProjectile Spawn(
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
        Color tint,
        ItemSpawner itemSpawner,
        bool allowHitDrop,
        float dropChanceOnHit)
    {
        SorcererProjectile projectile = Acquire();
        projectile.transform.position = spawnPosition;
        projectile.gameObject.SetActive(true);

        if (projectile.triggerCollider != null)
            projectile.triggerCollider.radius = Mathf.Max(0.05f, radius);

        if (projectile.spriteRenderer != null)
        {
            projectile.spriteRenderer.sprite = sprite;
            projectile.spriteRenderer.color = tint;
            projectile.spriteRenderer.flipX = direction.x < 0f;
        }

        projectile.Initialize(
            owner: owner,
            direction: direction,
            damage: damage,
            knockbackForce: knockbackForce,
            speed: speed,
            lifetime: lifetime,
            targetLayerMask: targetLayerMask,
            blockLayerMask: blockLayerMask,
            itemSpawner: itemSpawner,
            allowHitDrop: allowHitDrop,
            dropChanceOnHit: dropChanceOnHit);

        return projectile;
    }

    static SorcererProjectile Acquire()
    {
        while (Pool.Count > 0)
        {
            var pooled = Pool.Dequeue();
            if (pooled != null)
                return pooled;
        }

        var projectileObject = new GameObject("SorcererProjectile");
        var projectile = projectileObject.AddComponent<SorcererProjectile>();

        var rigidBody = projectileObject.AddComponent<Rigidbody2D>();
        rigidBody.gravityScale = 0f;
        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.freezeRotation = true;
        rigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = projectileObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.2f;

        var spriteRenderer = projectileObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 10;

        projectile.body = rigidBody;
        projectile.triggerCollider = collider;
        projectile.spriteRenderer = spriteRenderer;
        return projectile;
    }

    void Initialize(
        GameObject owner,
        Vector2 direction,
        int damage,
        float knockbackForce,
        float speed,
        float lifetime,
        LayerMask targetLayerMask,
        LayerMask blockLayerMask,
        ItemSpawner itemSpawner,
        bool allowHitDrop,
        float dropChanceOnHit)
    {
        this.owner = owner;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.damage = Mathf.Max(0, damage);
        this.knockbackForce = Mathf.Max(0f, knockbackForce);
        this.speed = Mathf.Max(0.5f, speed);
        this.lifetime = Mathf.Max(0.05f, lifetime);
        this.targetLayerMask = targetLayerMask;
        this.blockLayerMask = blockLayerMask;
        this.itemSpawner = itemSpawner;
        this.allowHitDrop = allowHitDrop;
        this.dropChanceOnHit = Mathf.Clamp01(dropChanceOnHit);
        hasAppliedHit = false;
        livedTime = 0f;
    }

    void Update()
    {
        if (body == null)
            return;

        livedTime += Time.deltaTime;
        if (livedTime >= lifetime)
        {
            Recycle();
            return;
        }

        Vector2 nextPosition = body.position + direction * (speed * Time.deltaTime);
        body.MovePosition(nextPosition);
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

        if (targetLayerMask.value != 0)
        {
            int rootMask = 1 << targetRoot.layer;
            int colliderMask = 1 << other.gameObject.layer;
            if ((targetLayerMask.value & rootMask) == 0 && (targetLayerMask.value & colliderMask) == 0)
                return;
        }

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

        AttackController.TryApplyDamage(targetRoot, damage);

        TryDropItem(targetRoot.transform.position);
        Recycle();
    }

    bool IsBlockedByEnvironment(Collider2D other)
    {
        if (other == null || blockLayerMask.value == 0)
            return false;

        int layerBit = 1 << other.gameObject.layer;
        return (blockLayerMask.value & layerBit) != 0;
    }

    void TryDropItem(Vector2 hitPosition)
    {
        if (!allowHitDrop || itemSpawner == null || dropChanceOnHit <= 0f)
            return;

        if (Random.value > dropChanceOnHit)
            return;

        itemSpawner.TrySpawnHitDrop(hitPosition);
    }

    void Recycle()
    {
        gameObject.SetActive(false);
        Pool.Enqueue(this);
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
}
