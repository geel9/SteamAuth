namespace SteamAuth
{
    public static class APIEndpoints
    {
        public const string STEAMAPI_BASE = "https://api.steampowered.com";
        public const string COMMUNITY_BASE = "https://steamcommunity.com";
        public const string TWO_FACTOR_BASE = STEAMAPI_BASE + "/ITwoFactorService/%s/v0001";
        public static string TWO_FACTOR_TIME_QUERY = TWO_FACTOR_BASE.Replace("%s", "QueryTime");
    }
}
