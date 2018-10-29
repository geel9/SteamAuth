namespace SteamAuth.Constants
{
    /// <summary>
    ///     Error code that might happen during a login process started by an instance of UserLogin class
    /// </summary>
    public enum UserLoginErrorCode
    {
        /// <summary>
        ///     Login process failed due to an general failure or a bad response.
        /// </summary>
        GeneralFailure,

        /// <summary>
        ///     Server responded with an invalid RSA public key.
        /// </summary>
        BadRSAResponse,

        /// <summary>
        ///     User credentials are missing or invalid.
        /// </summary>
        BadCredentials,

        /// <summary>
        ///     Captcha verification is necessary.
        /// </summary>
        NeedsCaptchaCode,

        /// <summary>
        ///     Two factor authentication code is necessary.
        /// </summary>
        NeedsTwoFactorAuthenticationCode,

        /// <summary>
        ///     Email address verification is necessary.
        /// </summary>
        NeedsEmailVerificationCode,

        /// <summary>
        ///     Too many failed attempts to login received in the past.
        /// </summary>
        TooManyFailedLoginAttempts
    }
}