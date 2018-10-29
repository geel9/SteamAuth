using System;
using SteamAuth.Constants;

namespace SteamAuth.Exceptions
{
    /// <summary>
    ///     Represents an error that happened during the process of linking a new Authenticator started with an instance of
    ///     AuthenticatorLinker class
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class AuthenticatorLinkerException : Exception
    {
        internal AuthenticatorLinkerException(AuthenticatorLinkerErrorCode? linkerErrorCode = null) : base(
            GetMessage(linkerErrorCode))
        {
            ErrorCode = linkerErrorCode ?? ErrorCode;
        }

        /// <summary>
        ///     Gets the error code of this exception
        /// </summary>
        /// <value>
        ///     The error code
        /// </value>
        public AuthenticatorLinkerErrorCode ErrorCode { get; } = AuthenticatorLinkerErrorCode.GeneralFailure;

        private static string GetMessage(AuthenticatorLinkerErrorCode? result)
        {
            switch (result)
            {
                case AuthenticatorLinkerErrorCode.AuthenticatorAlreadyPresent:

                    return
                        "Authenticator registration failed due an already active authenticator being present on this account.";
                case AuthenticatorLinkerErrorCode.BadSMSCode:

                    return "Authenticator registration failed due to a bad sms code provided.";
                case AuthenticatorLinkerErrorCode.IncorrectSteamGuardCode:

                    return "Authenticator registration failed due to an incorrect steam guard code generated.";
                default:

                    return "Authenticator registration failed due to an general failure or a bad response.";
            }
        }
    }
}