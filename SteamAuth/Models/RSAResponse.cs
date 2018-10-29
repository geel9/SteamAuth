using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class RSAResponse
    {
        [JsonProperty("publickey_exp")]
        public string Exponent { get; set; }

        [JsonProperty("publickey_mod")]
        public string Modulus { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
    }
}