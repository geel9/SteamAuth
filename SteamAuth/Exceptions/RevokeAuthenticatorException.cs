using System;
using SteamAuth.Models;

namespace SteamAuth.Exceptions
{
    /// <summary>
    ///     Represent an error that happened during the process of revoking a registered authenticator
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class RevokeAuthenticatorException : Exception
    {
        internal RevokeAuthenticatorException(RemoveAuthenticatorInternalResponse response) : base(
            "Failed to revoke the authenticator.")
        {
            AttemptsRemaining = response?.RevocationAttemptsRemaining;
        }

        /// <summary>
        ///     Gets the number of remaining possible attempts to revoke this authenticator
        /// </summary>
        /// <value>
        ///     Number of remaining attempts
        /// </value>
        public uint? AttemptsRemaining { get; }
    }
}