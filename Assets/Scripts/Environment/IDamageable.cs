namespace Environment
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
        void Kill();
        bool IsAlive { get; }
    }
}
