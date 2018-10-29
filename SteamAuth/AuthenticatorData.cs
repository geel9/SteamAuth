using System;
using Newtonsoft.Json;
using SteamAuth.Constants;

namespace SteamAuth
{
    /// <summary>
    ///     Holds the necessary data to representing a valid registered authenticator
    /// </summary>
    /// <seealso cref="System.IEquatable{AuthenticatorData}" />
    public class AuthenticatorData : IEquatable<AuthenticatorData>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AuthenticatorData" /> class.
        /// </summary>
        /// <param name="accountName">Name of the account.</param>
        /// <param name="identitySecret">The identity secret.</param>
        /// <param name="revocationCode">The revocation code.</param>
        /// <param name="secret1">The secret1.</param>
        /// <param name="serialNumber">The serial number.</param>
        /// <param name="serverTime">The server time.</param>
        /// <param name="sharedSecret">The shared secret.</param>
        /// <param name="status">The linker status code.</param>
        /// <param name="steamGuardScheme">The steam guard scheme version.</param>
        /// <param name="steamId">The steam identifier number.</param>
        /// <param name="tokenGID">The token gid.</param>
        /// <param name="uri">The TOTP URI.</param>
        [JsonConstructor]
        public AuthenticatorData(
            string accountName,
            string identitySecret,
            string revocationCode,
            string secret1,
            string serialNumber,
            long serverTime,
            string sharedSecret,
            AuthenticatorLinkerErrorCode status,
            string steamGuardScheme,
            ulong steamId,
            string tokenGID,
            string uri)
        {
            AccountName = accountName;
            IdentitySecret = identitySecret;
            RevocationCode = revocationCode;
            Secret1 = secret1;
            SerialNumber = serialNumber;
            ServerTime = serverTime;
            SharedSecret = sharedSecret;
            Status = status;
            SteamGuardScheme = steamGuardScheme;
            SteamId = steamId;
            TokenGID = tokenGID;
            Uri = uri;
        }

        [JsonProperty("account_name")]
        public string AccountName { get; }

        [JsonProperty("identity_secret")]
        public string IdentitySecret { get; }

        [JsonProperty("revocation_code")]
        public string RevocationCode { get; }

        [JsonProperty("secret_1")]
        public string Secret1 { get; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; }

        [JsonProperty("server_time")]
        public long ServerTime { get; }

        [JsonProperty("shared_secret")]
        public string SharedSecret { get; }

        [JsonProperty("status")]
        public AuthenticatorLinkerErrorCode Status { get; }

        [JsonProperty("steamguard_scheme")]
        public string SteamGuardScheme { get; }

        [JsonProperty("steamid")]
        public ulong SteamId { get; }

        [JsonProperty("token_gid")]
        public string TokenGID { get; }

        [JsonProperty("uri")]
        public string Uri { get; }

        /// <inheritdoc />
        public bool Equals(AuthenticatorData other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(AccountName, other.AccountName) &&
                   string.Equals(IdentitySecret, other.IdentitySecret) &&
                   string.Equals(RevocationCode, other.RevocationCode) &&
                   string.Equals(Secret1, other.Secret1) &&
                   string.Equals(SerialNumber, other.SerialNumber) &&
                   ServerTime == other.ServerTime &&
                   string.Equals(SharedSecret, other.SharedSecret) &&
                   Status == other.Status &&
                   string.Equals(SteamGuardScheme, other.SteamGuardScheme) &&
                   SteamId == other.SteamId &&
                   string.Equals(TokenGID, other.TokenGID) &&
                   string.Equals(Uri, other.Uri);
        }

        public static bool operator ==(AuthenticatorData left, AuthenticatorData right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(AuthenticatorData left, AuthenticatorData right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((AuthenticatorData) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AccountName != null ? AccountName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (IdentitySecret != null ? IdentitySecret.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RevocationCode != null ? RevocationCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Secret1 != null ? Secret1.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SerialNumber != null ? SerialNumber.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ServerTime.GetHashCode();
                hashCode = (hashCode * 397) ^ (SharedSecret != null ? SharedSecret.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) Status;
                hashCode = (hashCode * 397) ^ (SteamGuardScheme != null ? SteamGuardScheme.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SteamId.GetHashCode();
                hashCode = (hashCode * 397) ^ (TokenGID != null ? TokenGID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Uri != null ? Uri.GetHashCode() : 0);

                return hashCode;
            }
        }
    }
}