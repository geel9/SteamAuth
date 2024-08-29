namespace SteamAuth
{
    public static class ApiEndpoints
    {
        public const string SteamapiBase = "https://api.steampowered.com";
        public const string CommunityBase = "https://steamcommunity.com";
        public const string MobileauthBase = SteamapiBase + "/IMobileAuthService/%s/v0001";
        public static string MobileauthGetwgtoken = MobileauthBase.Replace("%s", "GetWGToken");
        public const string TwoFactorBase = SteamapiBase + "/ITwoFactorService/%s/v0001";
        public static string TwoFactorTimeQuery = TwoFactorBase.Replace("%s", "QueryTime");
    }
}
