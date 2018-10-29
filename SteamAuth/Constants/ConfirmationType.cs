namespace SteamAuth.Constants
{
    /// <summary>
    ///     Known types of confirmation that a valid instance of Authenticator might receive
    /// </summary>
    public enum ConfirmationType
    {
        /// <summary>
        ///     A generic confirmation request
        /// </summary>
        GenericConfirmation = 1,

        /// <summary>
        ///     A trade confirmation request
        /// </summary>
        Trade = 2,

        /// <summary>
        ///     A market sell transaction request
        /// </summary>
        MarketSellTransaction = 3,

        /// <summary>
        ///     Unknown confirmation request
        /// </summary>
        Unknown = 0
    }
}