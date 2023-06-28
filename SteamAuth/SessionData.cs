using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SteamAuth
{
    public class SessionData
    {
        public ulong SteamID { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public string SessionID { get; set; }

        public async Task RefreshAccessToken()
        {
            if (string.IsNullOrEmpty(this.RefreshToken))
                throw new Exception("Refresh token is empty");

            if (IsTokenExpired(this.RefreshToken))
                throw new Exception("Refresh token is expired");

            string responseStr;
            try
            {
                var postData = new NameValueCollection();
                postData.Add("refresh_token", this.RefreshToken);
                postData.Add("steamid", this.SteamID.ToString());
                responseStr = await SteamWeb.POSTRequest("https://api.steampowered.com/IAuthenticationService/GenerateAccessTokenForApp/v1/", null, postData);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to refresh token: " + ex.Message);
            }

            var response = JsonConvert.DeserializeObject<GenerateAccessTokenForAppResponse>(responseStr);
            this.AccessToken = response.Response.AccessToken;
        }

        public bool IsAccessTokenExpired()
        {
            if (string.IsNullOrEmpty(this.AccessToken))
                return true;

            return IsTokenExpired(this.AccessToken);
        }

        public bool IsRefreshTokenExpired()
        {
            if (string.IsNullOrEmpty(this.RefreshToken))
                return true;

            return IsTokenExpired(this.RefreshToken);
        }

        private bool IsTokenExpired(string token)
        {
            var tokenComponents = token.Split('.');
            // Fix up base64url to normal base64
            var base64 = tokenComponents[1].Replace('-', '+').Replace('_', '/');

            if (base64.Length % 4 != 0)
            {
                base64 += new string('=', 4 - base64.Length % 4);
            }

            var payloadBytes = Convert.FromBase64String(base64);
            var jwt = JsonConvert.DeserializeObject<SteamAccessToken>(System.Text.Encoding.UTF8.GetString(payloadBytes));

            // Compare expire time of the token to the current time
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() > jwt.exp;
        }

        public CookieContainer GetCookies()
        {
            if (this.SessionID == null)
                this.SessionID = GenerateSessionID();

            var cookies = new CookieContainer();
            cookies.Add(new Cookie("steamLoginSecure", this.GetSteamLoginSecure(), "/", "steamcommunity.com"));
            cookies.Add(new Cookie("sessionid", this.SessionID, "/", "steamcommunity.com"));
            cookies.Add(new Cookie("mobileClient", "android", "/", "steamcommunity.com"));
            cookies.Add(new Cookie("mobileClientVersion", "777777 3.6.1", "/", "steamcommunity.com"));
            return cookies;
        }

        private string GetSteamLoginSecure()
        {
            return this.SteamID.ToString() + "%7C%7C" + this.AccessToken;
        }

        private static string GenerateSessionID()
        {
            return GetRandomHexNumber(32);
        }

        private static string GetRandomHexNumber(int digits)
        {
            Random random = new Random();
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        private class SteamAccessToken
        {
            public long exp { get; set; }
        }

        private class GenerateAccessTokenForAppResponse
        {
            [JsonProperty("response")]
            public GenerateAccessTokenForAppResponseResponse Response;
        }

        private class GenerateAccessTokenForAppResponseResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
        }
    }
}
