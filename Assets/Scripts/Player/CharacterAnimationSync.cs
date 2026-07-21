using SkyfallArena.Systems;
using UnityEngine;

/// <summary>
/// Đồng bộ Animator: Speed, grounded, hurt, chết, block, charge.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class CharacterAnimationSync : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D body;
    [SerializeField] PlayerController playerController;
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] DeathSystem deathSystem;

    [Header("Speed Parameter")]
    [SerializeField, Min(0.01f)] float speedReference = 6f;
    [SerializeField, Min(0f)] float speedDamping = 0.08f;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    static readonly int HurtHash = Animator.StringToHash("Hurt");
    static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    static readonly int ChargeHash = Animator.StringToHash("Charge");
    static readonly int SpecialHash = Animator.StringToHash("Special");

    bool hasSpeedParam;
    bool hasIsGroundedParam;
    bool hasHurtParam;
    bool hasIsDeadParam;
    bool hasIsBlockingParam;
    bool hasChargeParam;
    bool hasSpecialParam;

    void Reset()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
        deathSystem = GetComponent<DeathSystem>();
    }

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (deathSystem == null)
            deathSystem = GetComponent<DeathSystem>();

        CacheAnimatorParameters();
        UpdateSpeedReferenceFromController();
    }

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.Damaged += HandleDamaged;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.Damaged -= HandleDamaged;
    }

    void Update()
    {
        if (animator == null)
            return;

        UpdateLocomotionParameters();
        UpdateDeathParameter();
    }

    public void SetBlocking(bool isBlocking)
    {
        if (animator != null && hasIsBlockingParam)
            animator.SetBool(IsBlockingHash, isBlocking);
    }

    public void TriggerCharge()
    {
        if (animator != null && hasChargeParam)
            animator.SetTrigger(ChargeHash);
    }

    public void TriggerSpecial()
    {
        if (animator != null && hasSpecialParam)
            animator.SetTrigger(SpecialHash);
    }

    void HandleDamaged(PlayerHealth _, float damageAmount)
    {
        if (damageAmount <= 0f || animator == null || !hasHurtParam)
            return;

        animator.SetTrigger(HurtHash);
    }

    void UpdateLocomotionParameters()
    {
        if (body != null && hasSpeedParam)
        {
            float reference = Mathf.Max(0.01f, speedReference);
            float targetSpeed = Mathf.Clamp01(Mathf.Abs(body.linearVelocity.x) / reference);
            animator.SetFloat(SpeedHash, targetSpeed, speedDamping, Time.deltaTime);
        }

        if (hasIsGroundedParam)
            animator.SetBool(IsGroundedHash, EvaluateGrounded());
    }

    void UpdateDeathParameter()
    {
        if (!hasIsDeadParam)
            return;

        bool isDead = deathSystem != null
            ? deathSystem.IsEliminated
            : (playerHealth != null && !playerHealth.IsAlive);

        animator.SetBool(IsDeadHash, isDead);
    }

    bool EvaluateGrounded()
    {
        if (playerController == null || playerController.groundCheck == null)
            return true;

        return Physics2D.OverlapCircle(
            playerController.groundCheck.position,
            playerController.groundRadius,
            playerController.groundLayer);
    }

    void CacheAnimatorParameters()
    {
        if (animator == null)
            return;

        var parameters = animator.parameters;
        for (int index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.nameHash == SpeedHash)
                hasSpeedParam = true;
            else if (parameter.type == AnimatorControllerParameterType.Bool && parameter.nameHash == IsGroundedHash)
                hasIsGroundedParam = true;
            else if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == HurtHash)
                hasHurtParam = true;
            else if (parameter.type == AnimatorControllerParameterType.Bool && parameter.nameHash == IsDeadHash)
                hasIsDeadParam = true;
            else if (parameter.type == AnimatorControllerParameterType.Bool && parameter.nameHash == IsBlockingHash)
                hasIsBlockingParam = true;
            else if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == ChargeHash)
                hasChargeParam = true;
            else if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == SpecialHash)
                hasSpecialParam = true;
        }
    }

    void UpdateSpeedReferenceFromController()
    {
        if (playerController != null && playerController.moveSpeed > 0f)
            speedReference = playerController.moveSpeed;
    }
}
