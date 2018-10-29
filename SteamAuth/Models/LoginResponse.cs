using Newtonsoft.Json;

namespace SteamAuth.Models
{
    internal class LoginResponse
    {
        [JsonProperty("captcha_gid")]
        public string CaptchaGID { get; set; }

        [JsonProperty("captcha_needed")]
        public bool CaptchaNeeded { get; set; }

        [JsonProperty("emailauth_needed")]
        public bool EmailAuthNeeded { get; set; }

        [JsonProperty("emailsteamid")]
        public ulong EmailSteamId { get; set; }

        [JsonProperty("login_complete")]
        public bool LoginComplete { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonIgnore]
        public OAuth OAuthToken
        {
            get => !string.IsNullOrWhiteSpace(OAuthTokenString)
                ? JsonConvert.DeserializeObject<OAuth>(OAuthTokenString)
                : null;
        }

        [JsonProperty("oauth")]
        public string OAuthTokenString { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("requires_twofactor")]
        public bool TwoFactorNeeded { get; set; }
    }
}