using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class ConfirmationDetailsResponse
    {
        [JsonProperty("html")]
        public string HTML { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}