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

        /// <summary>
        /// Refresh your access token, optionally also getting a new refresh token
        /// </summary>
        /// <param name="allowRenewal">Allow getting a new refresh token as well. If one is returned, this.RefreshToken will be overwritten. You must save this new token!</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task RefreshAccessToken(bool allowRenewal = false)
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
                postData.Add("renewal_type", allowRenewal ? "1" : "0");
                responseStr = await SteamWeb.POSTRequest("https://api.steampowered.com/IAuthenticationService/GenerateAccessTokenForApp/v1/", null, postData);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to refresh token: " + ex.Message);
            }

            var response = JsonConvert.DeserializeObject<GenerateAccessTokenForAppResponse>(responseStr);
            this.AccessToken = response.Response.AccessToken;

            if (!string.IsNullOrEmpty(response.Response.RefreshToken))
                this.RefreshToken = response.Response.RefreshToken;
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
            // Compare expire time of the token to the current time
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() > GetTokenExpirationTime(token);
        }

        /// <summary>
        /// If the token is going to expire within the next 24h.
        /// </summary>
        /// <returns></returns>
        public bool IsRefreshTokenAboutToExpire()
        {
            return IsRefreshTokenExpired() || IsTokenAboutToExpire(RefreshToken);
        }

        /// <summary>
        /// Returns if the token will expire
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsTokenAboutToExpire(string token)
        {
            // Compare expire time of the token to the current time
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (24 * 60 * 60) > GetTokenExpirationTime(token);
        }

        /// <summary>
        /// Fetches JWT expiration time.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private long GetTokenExpirationTime(string token)
        {
            string[] tokenComponents = token.Split('.');
            // Fix up base64url to normal base64
            string base64 = tokenComponents[1].Replace('-', '+').Replace('_', '/');

            if (base64.Length % 4 != 0)
            {
                base64 += new string('=', 4 - base64.Length % 4);
            }

            byte[] payloadBytes = Convert.FromBase64String(base64);
            SteamAccessToken jwt = JsonConvert.DeserializeObject<SteamAccessToken>(System.Text.Encoding.UTF8.GetString(payloadBytes));

            return jwt.exp;
        }

        public CookieContainer GetCookies()
        {
            if (this.SessionID == null)
                this.SessionID = GenerateSessionID();

            var cookies = new CookieContainer();
            foreach (string domain in new string[] { "steamcommunity.com", "store.steampowered.com" })
            {
                cookies.Add(new Cookie("steamLoginSecure", this.GetSteamLoginSecure(), "/", domain));
                cookies.Add(new Cookie("sessionid", this.SessionID, "/", domain));
                cookies.Add(new Cookie("mobileClient", "android", "/", domain));
                cookies.Add(new Cookie("mobileClientVersion", "777777 3.6.4", "/", domain));
            }
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

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
        }
    }
}
