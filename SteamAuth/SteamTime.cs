using System;
using System.Net;
using System.Threading.Tasks;
using SteamAuth.Helpers;
using SteamAuth.Models;

namespace SteamAuth
{
    /// <summary>
    ///     Class to help align system time with the Steam server time. Not super advanced; probably not taking some things
    ///     into account that it should.
    ///     Necessary to generate up-to-date codes. In general, this will have an error of less than a second, assuming Steam
    ///     is operational.
    /// </summary>
    public static class SteamTime
    {
        private static bool _aligned;
        private static TimeSpan _timeDifference;
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        /// <summary>
        ///     Gets the steam time.
        /// </summary>
        /// <returns>Steam time as an instance of DateTime</returns>
        public static async Task<DateTime> GetTime()
        {
            if (!_aligned)
            {
                await ReAlignTime().ConfigureAwait(false);
            }

            return DateTime.UtcNow + _timeDifference;
        }

        /// <summary>
        ///     Gets the steam time in unix time format.
        /// </summary>
        /// <returns>Steam time as an number representing the number of seconds since 1/1/1970</returns>
        public static async Task<long> GetUnixTime()
        {
            return (await GetTime().ConfigureAwait(false)).ToUnixTime();
        }

        /// <summary>
        ///     Re-aligns the internal clock with Steam network
        /// </summary>
        /// <returns>true if operation ended successfully; otherwise false</returns>
        public static async Task<bool> ReAlignTime()
        {
            try
            {
                var preRequestTime = DateTime.UtcNow;
                var serverResponse = await SteamWeb.DownloadJson<ResponseWrapper<TimeQueryResponse>>(
                    Constants.Constants.TwoFactorTimeQueryUrl,
                    SteamWebRequestMethod.Post,
                    new QueryStringBuilder
                    {
                        {"steamid", 0}
                    }
                ).ConfigureAwait(false);
                var postRequestTime = DateTime.UtcNow;
                var actualResponseTime = preRequestTime.AddTicks((postRequestTime - preRequestTime).Ticks / 2);
                _timeDifference = serverResponse.Response.ServerTime.FromUnixTime() - actualResponseTime;
                _aligned = true;

                return true;
            }
            catch (WebException)
            {
                // ignore any web exception
            }

            return false;
        }

        /// <summary>
        ///     Converts a number representing the time in unix time format to an instance of DateTime.
        /// </summary>
        /// <param name="unixTime">The number in Unix time format representing the number of seconds since 1/1/1970</param>
        /// <returns>A DateTime instance representing the passed Unix time</returns>
        internal static DateTime FromUnixTime(this long unixTime)
        {
            return Epoch.AddSeconds(unixTime);
        }

        /// <summary>
        ///     Converts a instance of DateTime to a number in unix time format.
        /// </summary>
        /// <param name="dateTime">The DateTime instance to be converted to Unix time</param>
        /// <returns>A number representing the passed DateTime as the number of seconds since 1/1/1970</returns>
        internal static long ToUnixTime(this DateTime dateTime)
        {
            return (long) Math.Floor((dateTime - Epoch).TotalSeconds);
        }
    }
}