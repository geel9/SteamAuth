namespace SteamAuth.Constants
{
    internal static class Constants
    {
        public const string ClientLanguage = "english";

        public const string ClientName = "android";

        public const string ClientVersion = "0 (2.1.3)";

        public const string ClientXRequestedWith = "com.valvesoftware.android.steam.community";

        public const string CommunityBaseUrl = "https://steamcommunity.com";

        public const string CommunityCookieDomain = ".steamcommunity.com";

        public const string LoginCaptchaUrl = CommunityBaseUrl + "/login/rendercaptcha/";

        public const string MobileAuthenticatorGetTokenUrl = SteamAPIBaseUrl + "/IMobileAuthService/GetWGToken/v0001";

        public const string MobileConfirmationDetailUrl = CommunityBaseUrl + "/mobileconf/details/{0}";

        public const string MobileConfirmationOperationsUrl = CommunityBaseUrl + "/mobileconf/ajaxop";

        public const string MobileConfirmationsOperationsUrl = CommunityBaseUrl + "/mobileconf/multiajaxop";

        public const string MobileConfirmationsUrl = CommunityBaseUrl + "/mobileconf/confirmation";

        public const string MobileLoginInitializeUrl = CommunityBaseUrl + "/login";

        public const string MobileLoginReferer = CommunityBaseUrl +
                                                 "/mobilelogin?oauth_client_id=DE45CD61&oauth_scope=read_profile%20write_profile%20read_client%20write_client";

        public const string MobileLoginRSAUrl = CommunityBaseUrl + "/login/getrsakey";

        public const string MobileLoginUrl = CommunityBaseUrl + "/login/dologin";

        public const string SteamAPIBaseUrl = "https://api.steampowered.com";
        public const string SteamGuardPhoneOperationsUrl = CommunityBaseUrl + "/steamguard/phoneajax";

        public const string TwoFactorAddAuthenticatorUrl =
            SteamAPIBaseUrl + "/ITwoFactorService/AddAuthenticator/v0001";

        public const string TwoFactorFinalizeAddAuthenticatorUrl =
            SteamAPIBaseUrl + "/ITwoFactorService/FinalizeAddAuthenticator/v0001";

        public const string TwoFactorRemoveAuthenticatorUrl =
            SteamAPIBaseUrl + "/ITwoFactorService/RemoveAuthenticator/v0001";

        public const string TwoFactorTimeQueryUrl = SteamAPIBaseUrl + "/ITwoFactorService/QueryTime/v0001";

        public const string UserAgent =
            "Mozilla/5.0 (Linux; U; Android 4.1.1; en-us; Google Nexus 4 - 4.1.1 - API 16 - 768x1280 Build/JRO03S) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30";
    }
}