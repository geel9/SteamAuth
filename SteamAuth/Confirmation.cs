using Newtonsoft.Json;
using SteamAuth.Constants;

namespace SteamAuth
{
    /// <summary>
    ///     Represents a confirmation request waiting to be responded
    /// </summary>
    public class Confirmation
    {
        // ReSharper disable once TooManyDependencies
        [JsonConstructor]
        public Confirmation(ulong id, ulong key, ConfirmationType type, ulong creator)
        {
            Id = id;
            Key = key;
            Creator = creator;

            // Doesn't matter if we are not sure about all confirmation types. 
            // Probably so as the library user. And it is always possible to convert to int.
            Type = type;
        }

        /// <summary>
        ///     Gets the an identification number either the Trade Offer or market transaction that caused this confirmation to be
        ///     created.
        /// </summary>
        public ulong Creator { get; }

        /// <summary>
        ///     Gets the identification number of this confirmation.
        /// </summary>
        public ulong Id { get; }

        /// <summary>
        ///     Gets the unique key used to act upon this confirmation.
        /// </summary>
        public ulong Key { get; }

        /// <summary>
        ///     Gets the type of this confirmation.
        /// </summary>
        public ConfirmationType Type { get; }
    }
}