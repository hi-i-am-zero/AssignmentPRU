namespace Environment
{
    /// <summary>Interface nhận damage (PlayerHealth implement).</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
        void Kill();
        bool IsAlive { get; }
    }
}
