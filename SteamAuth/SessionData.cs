using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamAuth.Helpers;
using SteamAuth.Models;

namespace SteamAuth
{
    /// <summary>
    ///     Represents pair of OAuth and web session
    /// </summary>
    /// <seealso cref="System.IEquatable{SessionData}" />
    public class SessionData : IEquatable<SessionData>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SessionData" /> class.
        /// </summary>
        /// <param name="oAuthToken">The OAuth token.</param>
        /// <param name="steamId">The steam user identifier number.</param>
        /// <param name="steamLogin">The steam user login.</param>
        /// <param name="steamLoginSecure">The steam user login secure.</param>
        /// <param name="webCookie">The session web cookie.</param>
        /// <param name="sessionId">The session identifier string.</param>
        [JsonConstructor]
        // ReSharper disable once TooManyDependencies
        public SessionData(
            string oAuthToken,
            ulong steamId,
            string steamLogin,
            string steamLoginSecure,
            string webCookie,
            string sessionId)
        {
            OAuthToken = oAuthToken;
            SteamId = steamId;
            SteamLogin = steamLogin;
            SteamLoginSecure = steamLoginSecure;
            WebCookie = webCookie;
            SessionId = sessionId;
        }

        internal SessionData(OAuth oAuth, string sessionId)
        {
            OAuthToken = oAuth.OAuthToken;
            SteamId = oAuth.SteamId;
            SteamLogin = SteamId + "%7C%7C" + oAuth.SteamLogin;
            SteamLoginSecure = SteamId + "%7C%7C" + oAuth.SteamLoginSecure;
            WebCookie = oAuth.Webcookie;
            SessionId = sessionId;
        }

        /// <summary>
        ///     Gets the OAuth token
        /// </summary>
        public string OAuthToken { get; }

        /// <summary>
        ///     Gets the web session identifier string.
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        ///     Gets the steam user identifier number.
        /// </summary>
        public ulong SteamId { get; }

        /// <summary>
        ///     Gets the steam user login.
        /// </summary>
        public string SteamLogin { get; private set; }

        /// <summary>
        ///     Gets the steam user login secure.
        /// </summary>
        public string SteamLoginSecure { get; private set; }

        /// <summary>
        ///     Gets the web cookie associated with this session.
        /// </summary>
        public string WebCookie { get; }

        /// <inheritdoc />
        public bool Equals(SessionData other)
        {
            return other != null &&
                   SessionId == other.SessionId &&
                   SteamLogin == other.SteamLogin &&
                   SteamLoginSecure == other.SteamLoginSecure &&
                   WebCookie == other.WebCookie &&
                   OAuthToken == other.OAuthToken &&
                   SteamId == other.SteamId;
        }

        public static bool operator ==(SessionData data1, SessionData data2)
        {
            return Equals(data1, data2) || data1?.Equals(data2) == true;
        }

        public static bool operator !=(SessionData data1, SessionData data2)
        {
            return !(data1 == data2);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as SessionData);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = -823311899;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SessionId);
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SteamLogin);
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SteamLoginSecure);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(WebCookie);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(OAuthToken);
            hashCode = hashCode * -1521134295 + SteamId.GetHashCode();

            return hashCode;
        }

        /// <summary>
        ///     Creates and returns a cookies container along with all required cookies to represents a web session
        /// </summary>
        /// <returns>A newly created cookie container</returns>
        public CookieContainer CreateCookiesContainer()
        {
            var cookies = new CookieContainer();
            cookies.Add(new Cookie("mobileClientVersion", Constants.Constants.ClientVersion, "/",
                Constants.Constants.CommunityCookieDomain));
            cookies.Add(new Cookie("mobileClient", Constants.Constants.ClientName, "/",
                Constants.Constants.CommunityCookieDomain));
            cookies.Add(new Cookie("steamid", SteamId.ToString(), "/", Constants.Constants.CommunityCookieDomain));
            cookies.Add(new Cookie("steamLogin", SteamLogin, "/", Constants.Constants.CommunityCookieDomain)
            {
                HttpOnly = true
            });
            cookies.Add(new Cookie("steamLoginSecure", SteamLoginSecure, "/", Constants.Constants.CommunityCookieDomain)
            {
                HttpOnly = true,
                Secure = true
            });
            cookies.Add(new Cookie("Steam_Language", Constants.Constants.ClientLanguage, "/",
                Constants.Constants.CommunityCookieDomain));
            cookies.Add(new Cookie("dob", "", "/", Constants.Constants.CommunityCookieDomain));
            cookies.Add(new Cookie("sessionid", SessionId, "/", Constants.Constants.CommunityCookieDomain));

            return cookies;
        }

        /// <summary>
        ///     Refreshes the Steam session. Necessary to perform confirmations if your session has expired or changed.
        /// </summary>
        /// <returns>true if the operation completed successfully; otherwise false</returns>
        public async Task<bool> RefreshSession()
        {
            try
            {
                var refreshResponse = await SteamWeb
                    .DownloadJson<ResponseWrapper<RefreshSessionDataInternalResponse>>(
                        Constants.Constants.MobileAuthenticatorGetTokenUrl,
                        SteamWebRequestMethod.Post,
                        new QueryStringBuilder
                        {
                            {"access_token", OAuthToken}
                        }
                    ).ConfigureAwait(false);

                if (string.IsNullOrEmpty(refreshResponse?.Response?.Token))
                {
                    return false;
                }

                SteamLogin = SteamId + "%7C%7C" + refreshResponse.Response.Token;
                SteamLoginSecure = SteamId + "%7C%7C" + refreshResponse.Response.TokenSecure;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}