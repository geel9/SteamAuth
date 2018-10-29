using System;

namespace SteamAuth.Exceptions
{
    /// <summary>
    ///     Represents an error raised due to the fact that the OAuth token associated with a SessionData is no longer valid
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class TokenInvalidException : Exception
    {
        internal TokenInvalidException(Exception innerException = null) : base("OAuth token is invalid.",
            innerException)
        {
        }
    }
}