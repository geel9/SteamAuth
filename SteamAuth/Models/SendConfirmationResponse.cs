using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class SendConfirmationResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}