namespace SteamAuth
{
    public class SessionData
    {
        public string SessionID { get; set; }

        public string SteamLogin { get; set; }

        public string SteamLoginSecure { get; set; }

        public string WebCookie { get; set; }

        public string OAuthToken { get; set; }

        public ulong SteamID { get; set; }
    }
}
