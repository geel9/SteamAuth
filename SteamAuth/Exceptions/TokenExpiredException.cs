using System;

namespace SteamAuth.Exceptions
{
    /// <summary>
    ///     Represents an error raised due to the fact that the OAuth token associated with a SessionData has already expired
    ///     and should be refreshed
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class TokenExpiredException : Exception
    {
        internal TokenExpiredException(Exception innerException = null) : base("OAuth token has expired.",
            innerException)
        {
        }
    }
}