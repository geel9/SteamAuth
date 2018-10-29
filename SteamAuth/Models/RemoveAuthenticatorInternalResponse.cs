using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class RemoveAuthenticatorInternalResponse
    {
        [JsonProperty("revocation_attempts_remaining")]
        public uint RevocationAttemptsRemaining { get; set; }

        [JsonProperty("server_time")]
        public ulong ServerTime { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}