namespace Environment
{
    /// <summary>Tên layer/tag dùng chung (Player, Ground, DeathZone...).</summary>
    public static class GameLayers
    {
        // --- Layer vật lý: Unity dùng cho va chạm và OverlapCircle ---

        public const string Ground = "Ground";       // Platform, tường biên — player đứng/nhảy được
        public const string Player = "Player";       // Nhân vật
        public const string Hazard = "Hazard";       // Lava, vùng nguy hiểm
        public const string DeathZone = "DeathZone"; // Vùng chết đáy map
        public const string Item = "Item";           // Item rơi trên map

        // --- Tag logic: gió, sét, lava kiểm tra CompareTag trước khi tác dụng ---

        public const string TagPlayer = "Player";
        public const string TagHazard = "Hazard";
        public const string TagDeathZone = "DeathZone";
        public const string TagSpawnPoint = "SpawnPoint";
        public const string TagItemSpawn = "ItemSpawn";
    }
}
