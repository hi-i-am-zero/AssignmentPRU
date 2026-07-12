namespace SkyfallArena.Systems
{
    public static class LocalPlayerRules
    {
        public const int FixedPlayerCount = 2;

        public static bool IsSupportedPlayerId(int playerId)
        {
            return playerId >= 1 && playerId <= FixedPlayerCount;
        }
    }
}
