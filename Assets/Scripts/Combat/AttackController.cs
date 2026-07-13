using UnityEngine;
using ArenaEnvironment = Environment;
using SkyfallArena.Systems.Items;
using System.Collections.Generic;

public class AttackController : MonoBehaviour
{
    [Header("Attack Settings")]
    public Transform attackPoint;      // Điểm xuất phát đòn đánh
    public LayerMask playerLayer;      // Layer chứa các Player có thể bị đánh

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

    // Các component cần dùng cho hệ thống combat
    private CharacterStatus stats;
    private PlayerController playerController;
    private ComboController comboController;
    private KnockbackController selfKnockback;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CharacterType characterType;

    // Thời điểm được phép thực hiện đòn đánh tiếp theo
    private float nextAttackTime;
    private float cachedArenaWidth;
    private float nextArenaWidthSampleTime;

    private void Awake()
    {
        // Lấy các component trên cùng GameObject
        stats = GetComponent<CharacterStatus>();
        playerController = GetComponent<PlayerController>();
        comboController = GetComponent<ComboController>();
        selfKnockback = GetComponent<KnockbackController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterType = GetComponent<CharacterType>();

        if (itemSpawner == null)
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
    }

    private void Update()
    {
        // Kiểm tra input đánh mỗi frame
        HandleAttackInput();
    }

    /// <summary>
    /// Nhận input tấn công của từng người chơi
    /// Player 1: F
    /// Player 2: Keypad 0
    /// Đồng thời kiểm tra trạng thái knockback và cooldown.
    /// </summary>
    void HandleAttackInput()
    {
        if (stats == null || playerController == null)
            return;

        // Không cho đánh khi đang bị hất văng
        if (selfKnockback != null && selfKnockback.IsKnocked)
            return;

        bool attackPressed;
        if (playerController.playerType == PlayerController.PlayerType.Player1)
            attackPressed = Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.J);
        else
            attackPressed = Input.GetKeyDown(KeyCode.Keypad0)
                || Input.GetKeyDown(KeyCode.RightControl)
                || Input.GetKeyDown(KeyCode.Slash);

        if (!attackPressed)
            return;

        // Kiểm tra thời gian hồi chiêu
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + stats.attackCooldown;
        Attack();
    }

    /// <summary>
    /// Thực hiện đòn đánh:
    /// - Tăng combo
    /// - Chạy animation
    /// - Tìm mục tiêu trong phạm vi đánh
    /// - Áp dụng knockback + damage
    /// </summary>
    void Attack()
    {
        if (stats == null)
            return;

        if (attackPoint == null)
            attackPoint = transform;

        // Lấy đòn hiện tại trong chuỗi combo
        int comboIndex = 1;
        if (comboController != null)
            comboIndex = comboController.NextCombo();

        Debug.Log(gameObject.name + " Combo " + comboIndex);

        // Chạy animation đánh
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

        // Tìm tất cả mục tiêu trong vùng đánh
        Vector2 attackCenter = GetAttackCenter();
        Collider2D[] targets = ReadTargetsInRange(attackCenter, stats.attackRange);

        var processedTargets = new HashSet<GameObject>();
        foreach (Collider2D target in targets)
        {
            if (target == null)
                continue;

            GameObject targetRoot = ResolveTargetRoot(target);
            if (targetRoot == null)
                continue;

            if (!processedTargets.Add(targetRoot))
                continue;

            // Chỉ xử lý mục tiêu là player.
            if (targetRoot.GetComponent<PlayerController>() == null)
                continue;

            // Không cho phép tự đánh chính mình
            if (targetRoot == gameObject)
                continue;

            // Chỉ gây sát thương cho mục tiêu ở phía trước mặt nhân vật
            Vector2 directionToTarget = targetRoot.transform.position - transform.position;
            if (spriteRenderer != null)
            {
                if (!spriteRenderer.flipX && directionToTarget.x < 0f)
                    continue;
                if (spriteRenderer.flipX && directionToTarget.x > 0f)
                    continue;
            }

            Debug.Log(
                gameObject.name +
                " attacked " +
                targetRoot.name +
                " | Damage = " +
                stats.damage);

            // Áp dụng hiệu ứng hất văng (knockback)
            var knockback = targetRoot.GetComponent<KnockbackController>();
            if (knockback != null)
            {
                knockback.ApplyKnockback(
                    transform.position,
                    stats.knockbackForce);
            }

            // Gây sát thương qua interface chung của hệ thống.
            var damageable = targetRoot.GetComponent<ArenaEnvironment.IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(stats.damage);
                TryDropItemOnHit(targetRoot.transform.position);
            }
        }
    }

    /// <summary>
    /// Hiển thị phạm vi tấn công trong Scene View
    /// Chỉ dùng để hỗ trợ debug khi thiết kế game.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        float radius = 1f;

        CharacterStatus status = GetComponent<CharacterStatus>();
        if (status != null)
            radius = status.attackRange;

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

    Collider2D[] ReadTargetsInRange(Vector2 center, float radius)
    {
        if (playerLayer.value != 0)
            return Physics2D.OverlapCircleAll(center, radius, playerLayer);

        return Physics2D.OverlapCircleAll(center, radius);
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
        Vector2 direction = Vector2.right;
        if (spriteRenderer != null && spriteRenderer.flipX)
            direction = Vector2.left;

        Vector2 spawnPosition;
        if (attackPoint != null)
            spawnPosition = attackPoint.position;
        else
            spawnPosition = transform.position;

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
            damage: stats != null ? stats.damage : 0,
            knockbackForce: stats != null ? stats.knockbackForce : 0f,
            speed: speed,
            lifetime: lifetime,
            radius: sorcererProjectileRadius,
            targetLayerMask: playerLayer,
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