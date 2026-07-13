using SkyfallArena.Systems.Items;
using UnityEngine;
using ArenaEnvironment = Environment;

[DisallowMultipleComponent]
public sealed class SorcererProjectile : MonoBehaviour
{
    GameObject owner;
    Vector2 direction;
    int damage;
    float knockbackForce;
    float speed;
    float lifetime;
    LayerMask targetLayerMask;
    ItemSpawner itemSpawner;
    bool allowHitDrop;
    float dropChanceOnHit;

    Rigidbody2D body;
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
        Sprite sprite,
        Color tint,
        ItemSpawner itemSpawner,
        bool allowHitDrop,
        float dropChanceOnHit)
    {
        var projectileObject = new GameObject("SorcererProjectile");
        projectileObject.transform.position = spawnPosition;

        var projectile = projectileObject.AddComponent<SorcererProjectile>();

        var rigidBody = projectileObject.AddComponent<Rigidbody2D>();
        rigidBody.gravityScale = 0f;
        rigidBody.isKinematic = true;
        rigidBody.freezeRotation = true;
        rigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = projectileObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = Mathf.Max(0.05f, radius);

        var spriteRenderer = projectileObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = tint;
        spriteRenderer.sortingOrder = 10;

        projectile.Initialize(
            owner: owner,
            direction: direction,
            damage: damage,
            knockbackForce: knockbackForce,
            speed: speed,
            lifetime: lifetime,
            targetLayerMask: targetLayerMask,
            itemSpawner: itemSpawner,
            allowHitDrop: allowHitDrop,
            dropChanceOnHit: dropChanceOnHit,
            body: rigidBody);

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
        ItemSpawner itemSpawner,
        bool allowHitDrop,
        float dropChanceOnHit,
        Rigidbody2D body)
    {
        this.owner = owner;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.damage = Mathf.Max(0, damage);
        this.knockbackForce = Mathf.Max(0f, knockbackForce);
        this.speed = Mathf.Max(0.5f, speed);
        this.lifetime = Mathf.Max(0.05f, lifetime);
        this.targetLayerMask = targetLayerMask;
        this.itemSpawner = itemSpawner;
        this.allowHitDrop = allowHitDrop;
        this.dropChanceOnHit = Mathf.Clamp01(dropChanceOnHit);
        this.body = body;
    }

    void Update()
    {
        if (body == null)
            return;

        livedTime += Time.deltaTime;
        if (livedTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 nextPosition = body.position + direction * (speed * Time.deltaTime);
        body.MovePosition(nextPosition);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasAppliedHit || other == null)
            return;

        GameObject targetRoot = ResolveTargetRoot(other);
        if (targetRoot == null)
            return;

        if (owner != null && targetRoot == owner)
            return;

        if (targetLayerMask.value != 0)
        {
            int layerMask = 1 << targetRoot.layer;
            if ((targetLayerMask.value & layerMask) == 0)
                return;
        }

        if (targetRoot.GetComponent<PlayerController>() == null)
            return;

        hasAppliedHit = true;

        var knockback = targetRoot.GetComponent<KnockbackController>();
        if (knockback != null)
            knockback.ApplyKnockback(transform.position, knockbackForce);

        var damageable = targetRoot.GetComponent<ArenaEnvironment.IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(damage);

        TryDropItem(targetRoot.transform.position);
        Destroy(gameObject);
    }

    void TryDropItem(Vector2 hitPosition)
    {
        if (!allowHitDrop || itemSpawner == null || dropChanceOnHit <= 0f)
            return;

        if (Random.value > dropChanceOnHit)
            return;

        itemSpawner.TrySpawnHitDrop(hitPosition);
    }

    static GameObject ResolveTargetRoot(Collider2D target)
    {
        if (target == null)
            return null;

        if (target.attachedRigidbody != null)
            return target.attachedRigidbody.gameObject;

        if (target.transform.root != null)
            return target.transform.root.gameObject;

        return target.gameObject;
    }
}
