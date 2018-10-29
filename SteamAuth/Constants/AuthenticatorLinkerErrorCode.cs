namespace SteamAuth.Constants
{
    public enum AuthenticatorLinkerErrorCode
    {
        /// <summary>
        ///     General failure happened or bad response
        /// </summary>
        GeneralFailure = 0,

        /// <summary>
        ///     Operation completed successfully
        /// </summary>
        Success = 1,

        /// <summary>
        ///     An active authenticator is already present
        /// </summary>
        AuthenticatorAlreadyPresent = 29,

        /// <summary>
        ///     Invalid SMS code provided
        /// </summary>
        BadSMSCode = 89,

        /// <summary>
        ///     Incorrect steam guard code generated
        /// </summary>
        IncorrectSteamGuardCode = 88
    }
}