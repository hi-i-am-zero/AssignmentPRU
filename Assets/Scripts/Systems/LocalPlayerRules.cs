namespace SkyfallArena.Systems
{
    /// <summary>
    /// Quy tắc local 1v1: đúng 2 player, ID hợp lệ 1–2.
    /// </summary>
    public static class LocalPlayerRules
    {
        public const int FixedPlayerCount = 2;

        public static bool IsSupportedPlayerId(int playerId)
        {
            return playerId >= 1 && playerId <= FixedPlayerCount;
        }
    }
}
