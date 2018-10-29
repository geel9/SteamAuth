using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class CheckPhoneSMSCode
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}