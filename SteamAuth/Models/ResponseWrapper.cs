using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class ResponseWrapper<T>
    {
        [JsonProperty("response")]
        public T Response { get; set; }
    }
}