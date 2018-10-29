using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamAuth.Constants;
using SteamAuth.Exceptions;
using SteamAuth.Helpers;
using SteamAuth.Models;

namespace SteamAuth
{
    /// <summary>
    ///     Represents an Authenticator software or device for generating Steam guard codes as a two factor authentication
    ///     response or to accept or deny new confirmation requests
    /// </summary>
    public class Authenticator
    {
        private const long SteamGuardCodeGenerationStep = 30L;
        private const int SteamGuardCodeLength = 5;

        private static readonly Regex ConfirmationRegex =
            new Regex(
                "<div class=\"mobileconf_list_entry\" id=\"confirmation[0-9]+\" data-confid=\"(\\d+)\" data-key=\"(\\d+)\" data-type=\"(\\d+)\" data-creator=\"(\\d+)\"");

        private static readonly char[] SteamGuardCodeTranslations =
        {
            '2', '3', '4', '5', '6', '7', '8', '9', 'B', 'C',
            'D', 'F', 'G', 'H', 'J', 'K', 'M', 'N', 'P', 'Q',
            'R', 'T', 'V', 'W', 'X', 'Y'
        };

        [JsonConstructor]
        public Authenticator(AuthenticatorData authenticatorData, SessionData session, string deviceId)
        {
            AuthenticatorData = authenticatorData;
            Session = session;
            DeviceId = deviceId;
        }

        /// <summary>
        ///     Gets the authenticator data representing a valid registered authenticator.
        /// </summary>
        /// <value>
        ///     The authenticator data.
        /// </value>
        public AuthenticatorData AuthenticatorData { get; }

        /// <summary>
        ///     Gets the device identifier associated with the authenticator
        /// </summary>
        /// <value>
        ///     The device identifier of the associated authenticator.
        /// </value>
        public string DeviceId { get; }

        /// <summary>
        ///     Gets the session data required to access Steam network.
        /// </summary>
        /// <value>
        ///     The logged-in session data of the associated user account.
        /// </value>
        public SessionData Session { get; }

        /// <summary>
        ///     Returns a new instance of Authenticator class based on the description provided by the passed serialized string in
        ///     Json format
        /// </summary>
        /// <param name="serialized">The serialized representation of an Authenticator instance as string in Json format.</param>
        /// <returns>An instance of Authenticator class</returns>
        public static Authenticator DeSerialize(string serialized)
        {
            return JsonConvert.DeserializeObject<Authenticator>(serialized);
        }

        /// <summary>
        ///     Returns a new instance of Authenticator class based on the description provided by the passed serialized file name
        ///     in Json format
        /// </summary>
        /// <param name="fileName">The Json file to read serialized string from.</param>
        /// <returns>An instance of Authenticator class</returns>
        public static Authenticator DeSerializeFromFile(string fileName)
        {
            return JsonConvert.DeserializeObject<Authenticator>(File.ReadAllText(fileName));
        }

        /// <summary>
        ///     Accepts one or more confirmations
        /// </summary>
        /// <param name="confirmations">The [array of confirmations/confirmation] to accept.</param>
        /// <returns>true if [all confirmations/the confirmation] successfully accepted; otherwise false.</returns>
        public async Task<bool> AcceptConfirmation(params Confirmation[] confirmations)
        {
            if (confirmations.Length > 1)
            {
                return await ResponseToConfirmations(confirmations, true).ConfigureAwait(false);
            }

            if (confirmations.Length > 0)
            {
                return await ResponseToConfirmation(confirmations[0], true).ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        ///     Denies one or more confirmations
        /// </summary>
        /// <param name="confirmations">The [array of confirmations/confirmation] to deny.</param>
        /// <returns>true if [all confirmations/the confirmation] successfully denied; otherwise false.</returns>
        public async Task<bool> DenyConfirmation(params Confirmation[] confirmations)
        {
            if (confirmations.Length > 1)
            {
                return await ResponseToConfirmations(confirmations, false).ConfigureAwait(false);
            }

            if (confirmations.Length > 0)
            {
                return await ResponseToConfirmation(confirmations[0], false).ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        ///     Retrieves a list of confirmation waiting to be verified by user
        /// </summary>
        /// <returns></returns>
        /// <exception cref="TokenInvalidException">Provided session token is invalid.</exception>
        /// <exception cref="TokenExpiredException">Provided session token has expired.</exception>
        /// <exception cref="WebException">Failed to communicate with steam's network or a bad response received.</exception>
        public async Task<Confirmation[]> FetchConfirmations()
        {
            var response = await SteamWeb.DownloadString(
                Constants.Constants.MobileConfirmationsUrl,
                SteamWebRequestMethod.Get,
                await GetConfirmationParameters("confirmation").ConfigureAwait(false),
                Session.CreateCookiesContainer()
            ).ConfigureAwait(false);

            /*So you're going to see this abomination and you're going to be upset.
              It's understandable. But the thing is, regex for HTML -- while awful -- makes this way faster than parsing a DOM, plus we don't need another library.
              And because the data is always in the same place and same format... It's not as if we're trying to naturally understand HTML here. Just extract strings.
              I'm sorry. */

            if (response == null || !ConfirmationRegex.IsMatch(response))
            {
                if (string.IsNullOrWhiteSpace(response) || !response.Contains("<div>Nothing to confirm</div>"))
                {
                    throw new TokenInvalidException();
                }

                return new Confirmation[0];
            }

            return ConfirmationRegex.Matches(response).Cast<Match>()
                .Where(match => match.Groups.Count == 5)
                .Select(
                    match =>
                    {
                        if (ulong.TryParse(match.Groups[1].Value, out var id) &&
                            ulong.TryParse(match.Groups[2].Value, out var key) &&
                            int.TryParse(match.Groups[3].Value, out var type) &&
                            ulong.TryParse(match.Groups[4].Value, out var creator))
                        {
                            return new Confirmation(id, key, (ConfirmationType) type, creator);
                        }

                        return null;
                    }
                ).Where(confirmation => confirmation != null).ToArray();
        }

        /// <summary>
        ///     Generates and returns a new steam guard code.
        /// </summary>
        /// <returns>The newly generated steam guard code.</returns>
        public async Task<string> GenerateSteamGuardCode()
        {
            return GenerateSteamGuardCodeForTime(await SteamTime.GetTime().ConfigureAwait(false));
        }

        /// <summary>
        ///     Generates and returns a new steam guard code based on the time windows passed as an argument
        /// </summary>
        /// <param name="time">The time window to generate the code for.</param>
        /// <returns>The newly generated steam guard code.</returns>
        public string GenerateSteamGuardCodeForTime(DateTime time)
        {
            if (string.IsNullOrEmpty(AuthenticatorData.SharedSecret))
            {
                return "";
            }

            var sharedSecretUnEscaped = Regex.Unescape(AuthenticatorData.SharedSecret);
            var sharedSecretArray = Convert.FromBase64String(sharedSecretUnEscaped);

            var window = time.ToUnixTime() / SteamGuardCodeGenerationStep;
            var timeArray = BitConverter.GetBytes(window);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timeArray);
            }

            var hashedData = new HMACSHA1 {Key = sharedSecretArray}.ComputeHash(timeArray);

            var offset = hashedData[hashedData.Length - 1] & 0x0F;
            var token = ((hashedData[offset] & 0x7f) << 24) |
                        ((hashedData[offset + 1] & 0xff) << 16) |
                        ((hashedData[offset + 2] & 0xff) << 8) |
                        ((hashedData[offset + 3] & 0xff) % 1000000);

            var charArray = new char[SteamGuardCodeLength];

            for (var i = 0; i < charArray.Length; ++i)
            {
                charArray[i] = SteamGuardCodeTranslations[token % SteamGuardCodeTranslations.Length];
                token /= SteamGuardCodeTranslations.Length;
            }

            return new string(charArray);
        }

        /// <summary>
        ///     Gets details of a confirmation request as a string in HTML format.
        /// </summary>
        /// <param name="confirmation">The confirmation to get details of.</param>
        /// <returns>The details of confirmation as a string in HTML format; or null if the confirmation is not valid</returns>
        public async Task<string> GetConfirmationDetails(Confirmation confirmation)
        {
            var serverResponse = await SteamWeb.DownloadJson<ConfirmationDetailsResponse>(
                string.Format(Constants.Constants.MobileConfirmationDetailUrl, confirmation.Id),
                SteamWebRequestMethod.Get,
                await GetConfirmationParameters("details").ConfigureAwait(false),
                Session.CreateCookiesContainer(),
                referer: (await GetConfirmationParameters("confirmation").ConfigureAwait(false)).AppendToUrl(
                    Constants.Constants.MobileConfirmationsUrl)
            ).ConfigureAwait(false);

            if (serverResponse?.Success == true)
            {
                return serverResponse.HTML;
            }

            return null;
        }

        /// <summary>
        ///     Revokes this instance and removes it from the user account associated with it.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="RevokeAuthenticatorException">Failed to revoke the authenticator.</exception>
        public async Task RevokeAuthenticator()
        {
            var serverResponse = await SteamWeb.DownloadJson<ResponseWrapper<RemoveAuthenticatorInternalResponse>>(
                Constants.Constants.TwoFactorRemoveAuthenticatorUrl,
                SteamWebRequestMethod.Post,
                new QueryStringBuilder
                {
                    {"steamid", Session.SteamId},
                    {"steamguard_scheme", AuthenticatorData.SteamGuardScheme},
                    {"revocation_code", AuthenticatorData.RevocationCode},
                    {"access_token", Session.OAuthToken}
                },
                referer: Constants.Constants.MobileLoginReferer
            ).ConfigureAwait(false);

            if (serverResponse?.Response?.Success != true)
            {
                throw new RevokeAuthenticatorException(serverResponse?.Response);
            }
        }

        /// <summary>
        ///     Serializes this instance of Authenticator to a string in Json format
        /// </summary>
        /// <returns>The serialized representation of an Authenticator instance as string in Json format.</returns>
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        ///     Serializes this instance of Authenticator to a string in Json format and saves it to a file
        /// </summary>
        /// <param name="fileName">Name of the Json file to write serialized string to.</param>
        public void SerializeToFile(string fileName)
        {
            var serialized = Serialize();
            File.WriteAllText(fileName, serialized);
        }

        private string GenerateConfirmationHashForTime(DateTime time, string tag)
        {
            var dataArray = BitConverter.GetBytes(time.ToUnixTime());

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(dataArray);
            }

            if (tag != null)
            {
                var tagLen = Math.Min(32, tag.Length);
                Array.Resize(ref dataArray, dataArray.Length + tagLen);
                Array.Copy(Encoding.ASCII.GetBytes(tag), 0, dataArray, dataArray.Length - tagLen, tagLen);
            }

            var hashKey = Convert.FromBase64String(AuthenticatorData.IdentitySecret);

            using (var hmac = new HMACSHA1 {Key = hashKey})
            {
                var dataHash = hmac.ComputeHash(dataArray);
                var encodedDataHash = Convert.ToBase64String(dataHash, Base64FormattingOptions.None);

                return WebUtility.UrlEncode(encodedDataHash);
            }
        }

        private async Task<QueryStringBuilder> GetConfirmationParameters(string tag)
        {
            if (string.IsNullOrEmpty(DeviceId))
            {
                throw new ArgumentException("Device ID is not present");
            }

            var time = await SteamTime.GetTime().ConfigureAwait(false);

            return new QueryStringBuilder
            {
                {"p", DeviceId},
                {"a", Session.SteamId},
                {"k", GenerateConfirmationHashForTime(time, tag)},
                {"t", time.ToUnixTime()},
                {"m", Constants.Constants.ClientName},
                {"tag", tag}
            };
        }

        private async Task<bool> ResponseToConfirmation(Confirmation confirmation, bool allow)
        {
            var operation = allow ? "allow" : "cancel";

            return (await SteamWeb.DownloadJson<SendConfirmationResponse>(
                       Constants.Constants.MobileConfirmationOperationsUrl,
                       SteamWebRequestMethod.Get,
                       new QueryStringBuilder
                       {
                           {"op", operation}
                       }.Concat(
                           await GetConfirmationParameters(operation).ConfigureAwait(false)
                       ).Concat(
                           new QueryStringBuilder
                           {
                               {"cid", confirmation.Id},
                               {"ck", confirmation.Key}
                           }
                       ),
                       Session.CreateCookiesContainer(),
                       referer: (await GetConfirmationParameters("confirmation").ConfigureAwait(false)).AppendToUrl(
                           Constants.Constants.MobileConfirmationsUrl)
                   ).ConfigureAwait(false))?.Success ==
                   true;
        }

        private async Task<bool> ResponseToConfirmations(Confirmation[] confirmations, bool allow)
        {
            var operation = allow ? "allow" : "cancel";

            return (await SteamWeb.DownloadJson<SendConfirmationResponse>(
                       Constants.Constants.MobileConfirmationsOperationsUrl,
                       SteamWebRequestMethod.Post,
                       new QueryStringBuilder
                       {
                           {"op", operation}
                       }.Concat(
                           await GetConfirmationParameters(operation).ConfigureAwait(false)
                       ).Concat(
                           confirmations.SelectMany(confirmation => new QueryStringBuilder
                           {
                               {"cid[]", confirmation.Id},
                               {"ck[]", confirmation.Key}
                           })
                       ),
                       Session.CreateCookiesContainer(),
                       referer: (await GetConfirmationParameters("confirmation").ConfigureAwait(false)).AppendToUrl(
                           Constants.Constants.MobileConfirmationsUrl)
                   ).ConfigureAwait(false))?.Success ==
                   true;
        }
    }
}