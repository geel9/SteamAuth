using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SteamAuth
{
    /// <summary>
    /// Handles the linking process for a new mobile authenticator.
    /// </summary>
    public class AuthenticatorLinker
    {
        /// <summary>
        /// Session data containing an access token for a steam account generated with k_EAuthTokenPlatformType_MobileApp
        /// </summary>
        private SessionData _session = null;

        /// <summary>
        /// Set to register a new phone number when linking. If a phone number is not set on the account, this must be set. If a phone number is set on the account, this must be null.
        /// </summary>
        public string PhoneNumber = null;
        public string PhoneCountryCode = null;

        /// <summary>
        /// Randomly-generated device ID. Should only be generated once per linker.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// After the initial link step, if successful, this will be the SteamGuard data for the account. PLEASE save this somewhere after generating it; it's vital data.
        /// </summary>
        public SteamGuardAccount LinkedAccount { get; private set; }

        /// <summary>
        /// True if the authenticator has been fully finalized.
        /// </summary>
        public bool Finalized = false;

        /// <summary>
        /// Set when the confirmation email to set a phone number is set
        /// </summary>
        private bool _confirmationEmailSent = false;

        /// <summary>
        /// Email address the confirmation email was sent to when adding a phone number
        /// </summary>
        public string ConfirmationEmailAddress;

        /// <summary>
        /// Create a new instance of AuthenticatorLinker
        /// </summary>
        /// <param name="accessToken">Access token for a Steam account created with k_EAuthTokenPlatformType_MobileApp</param>
        /// <param name="steamid">64 bit formatted steamid for the account</param>
        public AuthenticatorLinker(SessionData sessionData)
        {
            this._session = sessionData;
            this.DeviceId = GenerateDeviceId();
        }

        /// <summary>
        /// First step in adding a mobile authenticator to an account
        /// </summary>
        public async Task<LinkResult> AddAuthenticator()
        {
            // This method will be called again once the user confirms their phone number email
            if (this._confirmationEmailSent)
            {
                // Check if email was confirmed
                var isStillWaiting = await _isAccountWaitingForEmailConfirmation();
                if (isStillWaiting)
                {
                    return LinkResult.MustConfirmEmail;
                }
                else
                {
                    // Now send the SMS to the phone number
                    await _sendPhoneVerificationCode();

                    // This takes time so wait a bit
                    await Task.Delay(2000);
                }
            }

            // Make request to ITwoFactorService/AddAuthenticator
            var addAuthenticatorBody = new NameValueCollection();
            addAuthenticatorBody.Add("steamid", this._session.SteamId.ToString());
            addAuthenticatorBody.Add("authenticator_time", (await TimeAligner.GetSteamTimeAsync()).ToString());
            addAuthenticatorBody.Add("authenticator_type", "1");
            addAuthenticatorBody.Add("device_identifier", this.DeviceId);
            addAuthenticatorBody.Add("sms_phone_id", "1");
            var addAuthenticatorResponseStr = await SteamWeb.PostRequest("https://api.steampowered.com/ITwoFactorService/AddAuthenticator/v1/?access_token=" + this._session.AccessToken, null, addAuthenticatorBody);

            // Parse response json to object
            var addAuthenticatorResponse = JsonSerializer.Deserialize<AddAuthenticatorResponse>(addAuthenticatorResponseStr);

            if (addAuthenticatorResponse == null || addAuthenticatorResponse.Response == null)
                return LinkResult.GeneralFailure;

            // Status 2 means no phone number is on the account
            if (addAuthenticatorResponse.Response.Status == 2)
            {
                if (this.PhoneNumber == null)
                {
                    return LinkResult.MustProvidePhoneNumber;
                }
                else
                {
                    // Add phone number

                    // Get country code
                    var countryCode = this.PhoneCountryCode;

                    // If given country code is null, use the one from the Steam account
                    if (string.IsNullOrEmpty(countryCode))
                    {
                        countryCode = await GetUserCountry();
                    }

                    // Set the phone number
                    var res = await _setAccountPhoneNumber(this.PhoneNumber, countryCode);

                    // Make sure it's successful then respond that we must confirm via email
                    if (res != null && res.Response.ConfirmationEmailAddress != null)
                    {
                        this.ConfirmationEmailAddress = res.Response.ConfirmationEmailAddress;
                        this._confirmationEmailSent = true;
                        return LinkResult.MustConfirmEmail;
                    }

                    // If something else fails, we end up here
                    return LinkResult.FailureAddingPhone;
                }
            }

            if (addAuthenticatorResponse.Response.Status == 29)
                return LinkResult.AuthenticatorPresent;

            if (addAuthenticatorResponse.Response.Status != 1)
                return LinkResult.GeneralFailure;

            // Setup this.LinkedAccount
            this.LinkedAccount = addAuthenticatorResponse.Response;
            this.LinkedAccount.DeviceId = this.DeviceId;
            this.LinkedAccount.Session = this._session;

            return LinkResult.AwaitingFinalization;
        }

        public async Task<FinalizeResult> FinalizeAddAuthenticator(string smsCode)
        {
            var tries = 0;
            while (tries <= 10)
            {
                var finalizeAuthenticatorValues = new NameValueCollection();
                finalizeAuthenticatorValues.Add("steamid", this._session.SteamId.ToString());
                finalizeAuthenticatorValues.Add("authenticator_code", LinkedAccount.GenerateSteamGuardCode());
                finalizeAuthenticatorValues.Add("authenticator_time", TimeAligner.GetSteamTime().ToString());
                finalizeAuthenticatorValues.Add("activation_code", smsCode);
                finalizeAuthenticatorValues.Add("validate_sms_code", "1");

                string finalizeAuthenticatorResultStr;
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers[HttpRequestHeader.UserAgent] = SteamWeb.MobileAppUserAgent;
                    var finalizeAuthenticatorResult = await wc.UploadValuesTaskAsync(new Uri("https://api.steampowered.com/ITwoFactorService/FinalizeAddAuthenticator/v1/?access_token=" + this._session.AccessToken), "POST", finalizeAuthenticatorValues);
                    finalizeAuthenticatorResultStr = Encoding.UTF8.GetString(finalizeAuthenticatorResult);
                }

                var finalizeAuthenticatorResponse = JsonSerializer.Deserialize<FinalizeAuthenticatorResponse>(finalizeAuthenticatorResultStr);

                if (finalizeAuthenticatorResponse == null || finalizeAuthenticatorResponse.Response == null)
                {
                    return FinalizeResult.GeneralFailure;
                }

                if (finalizeAuthenticatorResponse.Response.Status == 89)
                {
                    return FinalizeResult.BadSmsCode;
                }

                if (finalizeAuthenticatorResponse.Response.Status == 88)
                {
                    if (tries >= 10)
                    {
                        return FinalizeResult.UnableToGenerateCorrectCodes;
                    }
                }

                if (!finalizeAuthenticatorResponse.Response.Success)
                {
                    return FinalizeResult.GeneralFailure;
                }

                if (finalizeAuthenticatorResponse.Response.WantMore)
                {
                    tries++;
                    continue;
                }

                this.LinkedAccount.FullyEnrolled = true;
                return FinalizeResult.Success;
            }

            return FinalizeResult.GeneralFailure;
        }

        private async Task<string> GetUserCountry()
        {
            var getCountryBody = new NameValueCollection();
            getCountryBody.Add("steamid", this._session.SteamId.ToString());
            var getCountryResponseStr = await SteamWeb.PostRequest("https://api.steampowered.com/IUserAccountService/GetUserCountry/v1?access_token=" + this._session.AccessToken, null, getCountryBody);

            // Parse response json to object
            var response = JsonSerializer.Deserialize<GetUserCountryResponse>(getCountryResponseStr);
            return response.Response.Country;
        }

        private async Task<SetAccountPhoneNumberResponse> _setAccountPhoneNumber(string phoneNumber, string countryCode)
        {
            var setPhoneBody = new NameValueCollection();
            setPhoneBody.Add("phone_number", phoneNumber);
            setPhoneBody.Add("phone_country_code", countryCode);
            var getCountryResponseStr = await SteamWeb.PostRequest("https://api.steampowered.com/IPhoneService/SetAccountPhoneNumber/v1?access_token=" + this._session.AccessToken, null, setPhoneBody);
            return JsonSerializer.Deserialize<SetAccountPhoneNumberResponse>(getCountryResponseStr);
        }

        private async Task<bool> _isAccountWaitingForEmailConfirmation()
        {
            var waitingForEmailResponse = await SteamWeb.PostRequest("https://api.steampowered.com/IPhoneService/IsAccountWaitingForEmailConfirmation/v1?access_token=" + this._session.AccessToken, null, null);

            // Parse response json to object
            var response = JsonSerializer.Deserialize<IsAccountWaitingForEmailConfirmationResponse>(waitingForEmailResponse);
            return response.Response.AwaitingEmailConfirmation;
        }

        private async Task<bool> _sendPhoneVerificationCode()
        {
            await SteamWeb.PostRequest("https://api.steampowered.com/IPhoneService/SendPhoneVerificationCode/v1?access_token=" + this._session.AccessToken, null, null);
            return true;
        }

        public enum LinkResult
        {
            MustProvidePhoneNumber, //No phone number on the account
            MustRemovePhoneNumber, //A phone number is already on the account
            MustConfirmEmail, //User need to click link from confirmation email
            AwaitingFinalization, //Must provide an SMS code
            GeneralFailure, //General failure (really now!)
            AuthenticatorPresent,
            FailureAddingPhone
        }

        public enum FinalizeResult
        {
            BadSmsCode,
            UnableToGenerateCorrectCodes,
            Success,
            GeneralFailure
        }

        private class GetUserCountryResponse
        {
            [JsonPropertyName("response")]
            public GetUserCountryResponseResponse Response { get; set; }
        }

        private class GetUserCountryResponseResponse
        {
            [JsonPropertyName("country")]
            public string Country { get; set; }
        }

        private class SetAccountPhoneNumberResponse
        {
            [JsonPropertyName("response")]
            public SetAccountPhoneNumberResponseResponse Response { get; set; }
        }

        private class SetAccountPhoneNumberResponseResponse
        {
            [JsonPropertyName("confirmation_email_address")]
            public string ConfirmationEmailAddress { get; set; }

            [JsonPropertyName("phone_number_formatted")]
            public string PhoneNumberFormatted { get; set; }
        }

        private class IsAccountWaitingForEmailConfirmationResponse
        {
            [JsonPropertyName("response")]
            public IsAccountWaitingForEmailConfirmationResponseResponse Response { get; set; }
        }

        private class IsAccountWaitingForEmailConfirmationResponseResponse
        {
            [JsonPropertyName("awaiting_email_confirmation")]
            public bool AwaitingEmailConfirmation { get; set; }

            [JsonPropertyName("seconds_to_wait")]
            public int SecondsToWait { get; set; }
        }

        private class AddAuthenticatorResponse
        {
            [JsonPropertyName("response")]
            public SteamGuardAccount Response { get; set; }
        }

        private class FinalizeAuthenticatorResponse
        {
            [JsonPropertyName("response")]
            public FinalizeAuthenticatorInternalResponse Response { get; set; }

            internal class FinalizeAuthenticatorInternalResponse
            {
                [JsonPropertyName("success")]
                public bool Success { get; set; }

                [JsonPropertyName("want_more")]
                public bool WantMore { get; set; }

                [JsonPropertyName("server_time")]
                public long ServerTime { get; set; }

                [JsonPropertyName("status")]
                public int Status { get; set; }
            }
        }

        public static string GenerateDeviceId()
        {
            return "android:" + Guid.NewGuid().ToString();
        }
    }
}
