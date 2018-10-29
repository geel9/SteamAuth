using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class AddPhoneResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}