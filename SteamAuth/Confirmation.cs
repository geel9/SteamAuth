using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SteamAuth
{
    public class Confirmation
    {
        [JsonProperty(PropertyName = "id")]
        public ulong ID { get; set; }

        [JsonProperty(PropertyName = "nonce")]
        public ulong Key { get; set; }

        [JsonProperty(PropertyName = "creator_id")]
        public ulong Creator { get; set; }

        [JsonProperty(PropertyName = "headline")]
        public string Headline { get; set; }

        [JsonProperty(PropertyName = "summary")]
        public List<String> Summary { get; set; }

        [JsonProperty(PropertyName = "accept")]
        public string Accept { get; set; }

        [JsonProperty(PropertyName = "cancel")]
        public string Cancel { get; set; }

        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; set; }

        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public EMobileConfirmationType ConfType { get; set; } = EMobileConfirmationType.Invalid;

        public enum EMobileConfirmationType
        {
            Invalid = 0,
            Test = 1,
            Trade = 2,
            MarketListing = 3,
            FeatureOptOut = 4,
            PhoneNumberChange = 5,
            AccountRecovery = 6
        }
    }

    public class ConfirmationsResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("needauth")]
        public bool NeedAuthentication { get; set; }

        [JsonProperty("conf")]
        public Confirmation[] Confirmations { get; set; }
    }
}
