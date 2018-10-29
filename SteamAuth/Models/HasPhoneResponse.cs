using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class HasPhoneResponse
    {
        [JsonProperty("has_phone")]
        public bool HasPhone { get; set; }
    }
}