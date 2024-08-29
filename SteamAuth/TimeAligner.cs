using System;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SteamAuth
{
    /// <summary>
    /// Class to help align system time with the Steam server time. Not super advanced; probably not taking some things into account that it should.
    /// Necessary to generate up-to-date codes. In general, this will have an error of less than a second, assuming Steam is operational.
    /// </summary>
    public class TimeAligner
    {
        private static bool _aligned = false;
        private static int _timeDifference = 0;

        public static long GetSteamTime()
        {
            if (!TimeAligner._aligned)
            {
                TimeAligner.AlignTime();
            }
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _timeDifference;
        }

        public static async Task<long> GetSteamTimeAsync()
        {
            if (!TimeAligner._aligned)
            {
                await TimeAligner.AlignTimeAsync();
            }
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _timeDifference;
        }

        public static void AlignTime()
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                try
                {
                    var response = client.UploadString(ApiEndpoints.TwoFactorTimeQuery, "steamid=0");
                    var query = JsonSerializer.Deserialize<TimeQuery>(response);
                    TimeAligner._timeDifference = (int)(query.Response.ServerTime - currentTime);
                    TimeAligner._aligned = true;
                }
                catch (WebException)
                {
                    return;
                }
            }
        }

        public static async Task AlignTimeAsync()
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var client = new WebClient();
            try
            {
                client.Encoding = Encoding.UTF8;
                var response = await client.UploadStringTaskAsync(new Uri(ApiEndpoints.TwoFactorTimeQuery), "steamid=0");
                var query = JsonSerializer.Deserialize<TimeQuery>(response);
                TimeAligner._timeDifference = (int)(query.Response.ServerTime - currentTime);
                TimeAligner._aligned = true;
            }
            catch (WebException)
            {
                return;
            }
        }

        internal class TimeQuery
        {
            [JsonPropertyName("response")]
            internal TimeQueryResponse Response { get; set; }

            internal class TimeQueryResponse
            {
                [JsonPropertyName("server_time")]
                public long ServerTime { get; set; }
            }

        }
    }
}
