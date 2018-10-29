using System;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamAuth.Constants;
using SteamAuth.Exceptions;
using SteamAuth.Helpers;
using SteamAuth.Models;

namespace SteamAuth
{
    /// <summary>
    ///     Handles logging the user into the mobile Steam website. Necessary to generate OAuth token and session cookies.
    /// </summary>
    public class UserLogin
    {
        private readonly SemaphoreSlim _lockObject = new SemaphoreSlim(1, 1);

        private byte[] _cachedCaptchaImage;
        private CookieContainer _cookies;

        /// <summary>
        ///     Gets the captcha GID required to get the captcha image associated with the latest login attempt.
        /// </summary>
        /// <value>
        ///     The captcha gid identification string
        /// </value>
        public string CaptchaGID { get; private set; }

        public bool RequiresCaptchaCode { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether email verification is required.
        /// </summary>
        /// <value>
        ///     <c>true</c> if user needs to verify his/her access to the email address associated with this user account;
        ///     otherwise, <c>false</c>.
        /// </value>
        public bool RequiresEmailVerification { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether two factor authentication code is required.
        /// </summary>
        /// <value>
        ///     <c>true</c> if user needs to verify his identity by providing the two factor authentication code generated for this
        ///     user account; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresTwoFactorAuthenticationCode { get; private set; }

        /// <summary>
        ///     Gets the session identifier string provided by Steam. Availability of this value doesn't guarantees an
        ///     authenticated user session. Sessions might also get issued for guest.
        /// </summary>
        /// <value>
        ///     The session identifier string
        /// </value>
        public string SessionId
        {
            get => _cookies?.GetCookies(new Uri(Constants.Constants.CommunityBaseUrl))["sessionid"]?.Value;
        }

        /// <summary>
        ///     Gets the Steam account identifier provided for recognizing user's account in case of email and/or two factor
        ///     authentication requirement
        /// </summary>
        /// <value>
        ///     The Steam account identifier
        /// </value>
        public ulong? SteamId { get; private set; }

        private static byte[] HexStringToByteArray(string hex)
        {
            var hexLen = hex.Length;
            var ret = new byte[hexLen / 2];

            for (var i = 0; i < hexLen; i += 2)
            {
                ret[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return ret;
        }

        /// <summary>
        ///     Tries to authenticate a user with the provided user credentials and returns session data corresponding to a
        ///     successful login; fails if information provided is not enough or service is unavailable.
        /// </summary>
        /// <param name="credentials">The credentials to be used for login process.</param>
        /// <returns>Logged in session to be used with other classes</returns>
        /// <exception cref="ArgumentException">
        ///     Username and/or password is missing. - credentials
        ///     or
        ///     Two factor authentication code is required for login process to continue. - credentials
        ///     or
        ///     Email verification code is required for login process to continue. - credentials
        ///     or
        ///     Captcha is required for login process to continue. - credentials
        /// </exception>
        /// <exception cref="UserLoginException">
        ///     Raises when there is a problem with login process or there is a need for more information. Capture and decide if
        ///     you should repeat the process.
        /// </exception>
        public async Task<SessionData> DoLogin(LoginCredentials credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.UserName) || string.IsNullOrWhiteSpace(credentials.Password))
            {
                throw new ArgumentException("Username and/or password is missing.", nameof(credentials));
            }

            if (RequiresTwoFactorAuthenticationCode &&
                string.IsNullOrWhiteSpace(credentials.TwoFactorAuthenticationCode))
            {
                throw new ArgumentException("Two factor authentication code is required for login process to continue.",
                    nameof(credentials));
            }

            if (RequiresEmailVerification && string.IsNullOrWhiteSpace(credentials.EmailVerificationCode))
            {
                throw new ArgumentException("Email verification code is required for login process to continue.",
                    nameof(credentials));
            }

            if (RequiresCaptchaCode && string.IsNullOrWhiteSpace(credentials.CaptchaCode))
            {
                throw new ArgumentException("Captcha is required for login process to continue.", nameof(credentials));
            }

            // Lock this instance
            await _lockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                // Initialize cookies for login process if missing
                if (!(_cookies?.Count > 0))
                {
                    await RefreshSession().ConfigureAwait(false);
                }

                // Get a RSA public key for password encryption
                var serverResponse = await SteamWeb.DownloadString(
                    Constants.Constants.MobileLoginRSAUrl,
                    SteamWebRequestMethod.Post,
                    new QueryStringBuilder
                    {
                        {"donotcache", await SteamTime.GetUnixTime().ConfigureAwait(false) * 1000},
                        {"username", credentials.UserName}
                    },
                    _cookies,
                    referer: Constants.Constants.MobileLoginReferer
                ).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(serverResponse) ||
                    serverResponse.Contains("<BODY>\nAn error occurred while processing your request."))
                {
                    throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
                }

                var rsaResponse = JsonConvert.DeserializeObject<RSAResponse>(serverResponse);

                if (rsaResponse?.Success != true)
                {
                    throw new UserLoginException(UserLoginErrorCode.BadRSAResponse, this);
                }

                // Sleep for a bit to give Steam a chance to catch up??
                await Task.Delay(350).ConfigureAwait(false);

                byte[] encryptedPasswordBytes;

                using (var rsaEncrypt = new RSACryptoServiceProvider())
                {
                    var passwordBytes = Encoding.UTF8.GetBytes(credentials.Password);
                    var rsaParameters = rsaEncrypt.ExportParameters(false);
                    rsaParameters.Exponent = HexStringToByteArray(rsaResponse.Exponent);
                    rsaParameters.Modulus = HexStringToByteArray(rsaResponse.Modulus);
                    rsaEncrypt.ImportParameters(rsaParameters);
                    encryptedPasswordBytes = rsaEncrypt.Encrypt(passwordBytes, false);
                }

                var encryptedPassword = Convert.ToBase64String(encryptedPasswordBytes);

                var loginResponse = await SteamWeb.DownloadJson<LoginResponse>(
                    Constants.Constants.MobileLoginUrl,
                    SteamWebRequestMethod.Post,
                    new QueryStringBuilder
                    {
                        {"donotcache", await SteamTime.GetUnixTime().ConfigureAwait(false) * 1000},
                        {"password", encryptedPassword},
                        {"username", credentials.UserName},
                        {"twofactorcode", credentials.TwoFactorAuthenticationCode ?? ""},
                        {"emailauth", RequiresEmailVerification ? (credentials.EmailVerificationCode ?? "") : ""},
                        {"loginfriendlyname", ""},
                        {"captchagid", RequiresCaptchaCode ? (CaptchaGID ?? "-1") : "-1"},
                        {"captcha_text", RequiresCaptchaCode ? (credentials.CaptchaCode ?? "") : ""},
                        {
                            "emailsteamid",
                            RequiresTwoFactorAuthenticationCode || RequiresEmailVerification
                                ? (SteamId?.ToString() ?? "")
                                : ""
                        },
                        {"rsatimestamp", rsaResponse.Timestamp},
                        {"remember_login", "true"},
                        {"oauth_client_id", "DE45CD61"},
                        {"oauth_scope", "read_profile write_profile read_client write_client"}
                    },
                    _cookies,
                    referer: Constants.Constants.MobileLoginReferer
                ).ConfigureAwait(false);

                if (loginResponse == null)
                {
                    throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
                }

                if (loginResponse.Message?.ToLower().Contains("incorrect login") == true ||
                    loginResponse.Message?.ToLower().Contains("password") == true &&
                    loginResponse.Message?.ToLower().Contains("incorrect") == true)
                {
                    throw new UserLoginException(UserLoginErrorCode.BadCredentials, this, loginResponse);
                }

                if (loginResponse.CaptchaNeeded)
                {
                    RequiresCaptchaCode = true;
                    CaptchaGID = loginResponse.CaptchaGID;
                    _cachedCaptchaImage = null;

                    throw new UserLoginException(UserLoginErrorCode.NeedsCaptchaCode, this, loginResponse);
                }

                if (!string.IsNullOrWhiteSpace(loginResponse.CaptchaGID) &&
                    loginResponse.CaptchaGID != "-1" &&
                    CaptchaGID != loginResponse.CaptchaGID)
                {
                    CaptchaGID = loginResponse.CaptchaGID;
                    _cachedCaptchaImage = null;
                }

                if (loginResponse.EmailAuthNeeded)
                {
                    RequiresEmailVerification = true;
                    SteamId = loginResponse.EmailSteamId > 0 ? loginResponse.EmailSteamId : SteamId;

                    throw new UserLoginException(UserLoginErrorCode.NeedsEmailVerificationCode, this, loginResponse);
                }

                if (loginResponse.TwoFactorNeeded && !loginResponse.Success)
                {
                    RequiresTwoFactorAuthenticationCode = true;
                    SteamId = loginResponse.EmailSteamId > 0 ? loginResponse.EmailSteamId : SteamId;

                    throw new UserLoginException(UserLoginErrorCode.NeedsTwoFactorAuthenticationCode, this,
                        loginResponse);
                }

                if (loginResponse.EmailSteamId > 0 && SteamId != loginResponse.EmailSteamId)
                {
                    SteamId = loginResponse.EmailSteamId;
                }

                if (loginResponse.Message?.Contains("too many login failures") == true)
                {
                    throw new UserLoginException(UserLoginErrorCode.TooManyFailedLoginAttempts, this, loginResponse);
                }

                if (!loginResponse.LoginComplete)
                {
                    throw new UserLoginException(UserLoginErrorCode.BadCredentials, this, loginResponse);
                }

                if (!(loginResponse.OAuthToken?.OAuthToken?.Length > 0))
                {
                    throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this, loginResponse);
                }

                var sessionData = new SessionData(loginResponse.OAuthToken, SessionId);
                ResetStates();

                return sessionData;
            }
            finally
            {
                // Unlock this instance
                _lockObject.Release();
            }
        }


        /// <summary>
        ///     Downloads the captcha image associated with the latest login attempt.
        /// </summary>
        /// <returns>An array of bytes representing an image in PNG file format</returns>
        public async Task<byte[]> DownloadCaptchaImage()
        {
            if (_cachedCaptchaImage != null)
            {
                return _cachedCaptchaImage;
            }

            if (string.IsNullOrWhiteSpace(CaptchaGID))
            {
                return null;
            }

            // Lock this instance
            await _lockObject.WaitAsync().ConfigureAwait(false);
            _cachedCaptchaImage = null;

            try
            {
                _cachedCaptchaImage = (await SteamWeb.Download(
                    Constants.Constants.LoginCaptchaUrl,
                    SteamWebRequestMethod.Get,
                    new QueryStringBuilder
                    {
                        {"gid", CaptchaGID}
                    },
                    _cookies,
                    new NameValueCollection
                    {
                        {"X-Requested-With", Constants.Constants.ClientXRequestedWith}
                    },
                    Constants.Constants.MobileLoginReferer
                ).ConfigureAwait(false)).ToArray();

                return _cachedCaptchaImage;
            }
            finally
            {
                // Unlock this instance
                _lockObject.Release();
            }
        }

        /// <summary>
        ///     Resets this instance state and allows for a new authentication process to start.
        /// </summary>
        /// <returns><c>true</c> if a new guest session identification string retrieved; otherwise, <c>false</c>.</returns>
        public async Task<bool> Reset()
        {
            // Lock this instance
            await _lockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                ResetStates();

                return await RefreshSession().ConfigureAwait(false);
            }
            finally
            {
                // Unlock this instance
                _lockObject.Release();
            }
        }

        private async Task<bool> RefreshSession()
        {
            // Get a new SessionId
            _cookies = new CookieContainer();
            _cookies.Add(new Cookie("mobileClientVersion", Constants.Constants.ClientVersion, "/",
                Constants.Constants.CommunityCookieDomain));
            _cookies.Add(new Cookie("mobileClient", Constants.Constants.ClientName, "/",
                Constants.Constants.CommunityCookieDomain));
            _cookies.Add(new Cookie("Steam_Language", Constants.Constants.ClientLanguage, "/",
                Constants.Constants.CommunityCookieDomain));

            (await SteamWeb.Download(
                Constants.Constants.MobileLoginInitializeUrl,
                SteamWebRequestMethod.Get,
                new QueryStringBuilder
                {
                    {"oauth_client_id", "DE45CD61"},
                    {"oauth_scope", "read_profile write_profile read_client write_client"}
                },
                _cookies,
                new NameValueCollection
                {
                    {"X-Requested-With", Constants.Constants.ClientXRequestedWith}
                },
                Constants.Constants.MobileLoginReferer
            ).ConfigureAwait(false)).Dispose();

            return !string.IsNullOrWhiteSpace(SessionId);
        }

        private void ResetStates()
        {
            // Reset variables
            _cookies = null;
            RequiresCaptchaCode = false;
            RequiresTwoFactorAuthenticationCode = false;
            RequiresEmailVerification = false;
            CaptchaGID = null;
            SteamId = null;
        }
    }
}