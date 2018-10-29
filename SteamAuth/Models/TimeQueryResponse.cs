using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class TimeQueryResponse
    {
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }
    }
}