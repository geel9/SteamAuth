using Newtonsoft.Json;
using SteamAuth.Constants;

namespace SteamAuth.Models
{
    internal class FinalizeAuthenticatorInternalResponse
    {
        [JsonProperty("server_time")]
        public ulong ServerTime { get; set; }

        [JsonProperty("status")]
        public AuthenticatorLinkerErrorCode Status { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("want_more")]
        public bool WantMore { get; set; }
    }
}