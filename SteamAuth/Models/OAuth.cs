using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class OAuth
    {
        [JsonProperty("account_name")]
        public string AccountName { get; set; }

        [JsonProperty("oauth_token")]
        public string OAuthToken { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }

        [JsonProperty("wgtoken")]
        public string SteamLogin { get; set; }

        [JsonProperty("wgtoken_secure")]
        public string SteamLoginSecure { get; set; }

        [JsonProperty("webcookie")]
        public string Webcookie { get; set; }
    }
}