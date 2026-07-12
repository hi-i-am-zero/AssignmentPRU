namespace SkyfallArena.Systems.Items
{
    /// <summary>
    /// Member 1 buff systems should implement this interface to consume item buffs.
    /// </summary>
    public interface IBuffable
    {
        bool TryApplyBuff(ItemBuffContext context);
    }
}
