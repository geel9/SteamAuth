using System;
using System.Net;
using System.Threading.Tasks;
using SteamAuth.Constants;
using SteamAuth.Exceptions;
using SteamAuth.Helpers;
using SteamAuth.Models;

namespace SteamAuth
{
    /// <summary>
    ///     Handles the linking process for a new mobile authenticator.
    /// </summary>
    public class AuthenticatorLinker
    {
        private readonly CookieContainer _cookies;
        private readonly SessionData _session;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AuthenticatorLinker" /> class.
        /// </summary>
        /// <param name="session">An logged-in session's data.</param>
        public AuthenticatorLinker(SessionData session)
        {
            _session = session;
            _cookies = _session.CreateCookiesContainer();
        }

        /// <summary>
        ///     Check if the account has a phone number associated with it
        /// </summary>
        /// <returns>true if the account has a phone number associated; otherwise false</returns>
        public async Task<bool> DoesAccountHasPhoneNumber()
        {
            return (await SteamWeb.DownloadJson<HasPhoneResponse>(
                       Constants.Constants.SteamGuardPhoneOperationsUrl,
                       SteamWebRequestMethod.Post,
                       new QueryStringBuilder
                       {
                           {"op", "has_phone"},
                           {"arg", "null"},
                           {"sessionid", _session.SessionId}
                       },
                       _cookies
                   ).ConfigureAwait(false))?.HasPhone ==
                   true;
        }

        /// <summary>
        ///     Finalizes the registration process of an already retrieved authenticator. If this process succeeds, this instance
        ///     of Authenticator should be considered as registered and verified and must be saved safely to an persistence storage
        ///     for later usages. Failing to do so results in losing access to the user account associated with it.
        /// </summary>
        /// <param name="requestedAuthenticator">The already retrieved authenticator to register.</param>
        /// <param name="smsCode">The SMS code received on the phone number.</param>
        /// <returns></returns>
        public async Task FinilizeAddAuthenticator(Authenticator requestedAuthenticator, string smsCode)
        {
            const int maxRetries = 30;

            for (var i = 1; i <= maxRetries; i++)
            {
                var serverResponse = await SteamWeb
                    .DownloadJson<ResponseWrapper<FinalizeAuthenticatorInternalResponse>>(
                        Constants.Constants.TwoFactorFinalizeAddAuthenticatorUrl,
                        SteamWebRequestMethod.Post,
                        new QueryStringBuilder
                        {
                            {"steamid", _session.SteamId},
                            {"access_token", _session.OAuthToken},
                            {"activation_code", smsCode},
                            {"authenticator_code", requestedAuthenticator.GenerateSteamGuardCode()},
                            {"authenticator_time", SteamTime.GetUnixTime()}
                        },
                        referer: Constants.Constants.MobileLoginReferer
                    ).ConfigureAwait(false);

                if (serverResponse?.Response?.Status == AuthenticatorLinkerErrorCode.IncorrectSteamGuardCode)
                {
                    if (i >= maxRetries)
                    {
                        throw new AuthenticatorLinkerException(serverResponse.Response?.Status);
                    }
                }

                if (serverResponse?.Response?.Success == true && serverResponse.Response?.WantMore == false)
                {
                    return;
                }

                if (serverResponse?.Response?.WantMore != true)
                {
                    throw new AuthenticatorLinkerException(serverResponse?.Response?.Status);
                }

                await SteamTime.ReAlignTime().ConfigureAwait(false);
            }

            throw new AuthenticatorLinkerException();
        }

        /// <summary>
        ///     Requests to add a new authenticator to the user account.
        ///     This new authenticator is not yet valid unless a call to FinilizeAddAuthenticator method succeeds.
        /// </summary>
        /// <returns>The newly added authenticator.</returns>
        /// <exception cref="SteamAuth.Exceptions.MissingPhoneNumberException">
        ///     This account doesn't have a valid associated phone
        ///     number.
        /// </exception>
        /// <exception cref="SteamAuth.Exceptions.AuthenticatorLinkerException">
        ///     Authenticator registration failed due to an general
        ///     failure, a bad response or because of some missing or invalid information provided.
        /// </exception>
        public async Task<Authenticator> RequestToAddAuthenticator()
        {
            // Check if account has a valid authenticator
            if (!await DoesAccountHasPhoneNumber().ConfigureAwait(false))
            {
                throw new MissingPhoneNumberException();
            }

            // Better to wait 300ms before issuing another request
            await Task.Delay(300).ConfigureAwait(false);

            var deviceKey = Constants.Constants.ClientName + ":" + Guid.NewGuid().ToString("D").ToLower();

            var addAuthenticatorResponse = await SteamWeb
                .DownloadJson<ResponseWrapper<AuthenticatorData>>(
                    Constants.Constants.TwoFactorAddAuthenticatorUrl,
                    SteamWebRequestMethod.Post,
                    new QueryStringBuilder
                    {
                        {"steamid", _session.SteamId},
                        {"access_token", _session.OAuthToken},
                        {"authenticator_type", "1"},
                        {"device_identifier", deviceKey},
                        {"sms_phone_id", "1"}
                    },
                    referer: Constants.Constants.MobileLoginReferer
                ).ConfigureAwait(false);

            if (addAuthenticatorResponse?.Response?.Status != AuthenticatorLinkerErrorCode.Success)
            {
                throw new AuthenticatorLinkerException(addAuthenticatorResponse?.Response?.Status);
            }

            return new Authenticator(addAuthenticatorResponse.Response, _session, deviceKey);
        }

        /// <summary>
        ///     Sends a request to add a phone number to the account. It is also necessary to confirm the newly added phone number
        ///     by verifying it via SMS using the VerifyPhoneNumberBySMS method.
        /// </summary>
        /// <param name="phoneNumber">The phone number to add.</param>
        /// <returns>true if request was received successfully; otherwise false.</returns>
        public async Task<bool> RequestToAddPhoneNumber(string phoneNumber)
        {
            return (await SteamWeb.DownloadJson<AddPhoneResponse>(
                       Constants.Constants.SteamGuardPhoneOperationsUrl,
                       SteamWebRequestMethod.Post,
                       new QueryStringBuilder
                       {
                           {"op", "add_phone_number"},
                           {"arg", phoneNumber},
                           {"sessionid", _session.SessionId}
                       },
                       _cookies
                   ).ConfigureAwait(false))?.Success ==
                   true;
        }

        /// <summary>
        ///     Verifies the phone number by providing the code sent via SMS.
        /// </summary>
        /// <param name="smsCode">The SMS code.</param>
        /// <returns>true if request was received successfully; otherwise false.</returns>
        public async Task<bool> VerifyPhoneNumberBySMS(string smsCode)
        {
            var serverResponse = await SteamWeb.DownloadJson<CheckPhoneSMSCode>(
                Constants.Constants.SteamGuardPhoneOperationsUrl,
                SteamWebRequestMethod.Post,
                new QueryStringBuilder
                {
                    {"op", "check_sms_code"},
                    {"arg", smsCode},
                    {"checkfortos", "0"},
                    {"skipvoip", "1"},
                    {"sessionid", _session.SessionId}
                },
                _cookies
            ).ConfigureAwait(false);

            if (serverResponse?.Success != true)
            {
                // It seems that Steam sometimes needs a few seconds to finalize the phone number on the account.
                await Task.Delay(3500)
                    .ConfigureAwait(
                        false);

                return await DoesAccountHasPhoneNumber().ConfigureAwait(false);
            }

            return true;
        }
    }
}