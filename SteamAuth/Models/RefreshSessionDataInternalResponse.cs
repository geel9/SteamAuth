using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class RefreshSessionDataInternalResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("token_secure")]
        public string TokenSecure { get; set; }
    }
}