using System;

namespace SteamAuth.Exceptions
{
    /// <summary>
    ///     Represents an error raised due to the fact that the specified user account lacks an associated phone number
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class MissingPhoneNumberException : Exception
    {
        internal MissingPhoneNumberException() : base("This account doesn't have a valid associated phone number.")
        {
        }
    }
}