namespace SkyfallArena.Systems.Items
{
    /// <summary>
    /// Interface nhận buff từ item (PlayerUpgradeBuffAdapter implement).
    /// </summary>
    public interface IBuffable
    {
        bool TryApplyBuff(ItemBuffContext context);
    }
}
