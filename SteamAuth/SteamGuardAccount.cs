using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SteamAuth
{
    public class SteamGuardAccount
    {
        private static byte[] steamGuardCodeTranslations = new byte[] { 50, 51, 52, 53, 54, 55, 56, 57, 66, 67, 68, 70, 71, 72, 74, 75, 77, 78, 80, 81, 82, 84, 86, 87, 88, 89 };

        public string SharedSecret { get; set; }

        public string SerialNumber { get; set; }

        public string RevocationCode { get; set; }

        public string URI { get; set; }

        public long ServerTime { get; set; }

        public string AccountName { get; set; }

        public string TokenGID { get; set; }

        public string IdentitySecret { get; set; }

        public string Secret1 { get; set; }

        public int Status { get; set; }

        public SessionData Session { get; set; }

        public string GenerateSteamGuardCode()
        {
            return GenerateSteamGuardCodeForTime((long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
        }

        public string GenerateSteamGuardCodeForTime(long time)
        {
            if (this.SharedSecret == null || this.SharedSecret.Length == 0)
            {
                return "";
            }

            byte[] sharedSecretArray = Convert.FromBase64String(this.SharedSecret);
            byte[] timeArray = new byte[8];

            time /= 30L;

            for (int i = 8; i > 0; i--)
            {
                timeArray[i - 1] = (byte)time;
                time >>= 8;
            }

            HMACSHA1 hmacGenerator = new HMACSHA1();
            hmacGenerator.Key = sharedSecretArray;
            byte[] hashedData = hmacGenerator.ComputeHash(timeArray);
            byte[] codeArray = new byte[5];
            try
            {
                byte b = (byte)(hashedData[19] & 0xF);
                int codePoint = (hashedData[b] & 0x7F) << 24 | (hashedData[b + 1] & 0xFF) << 16 | (hashedData[b + 2] & 0xFF) << 8 | (hashedData[b + 3] & 0xFF);

                for (int i = 0; i < 5; ++i)
                {
                    codeArray[i] = steamGuardCodeTranslations[codePoint % steamGuardCodeTranslations.Length];
                    codePoint /= steamGuardCodeTranslations.Length;
                }
            }
            catch (Exception e)
            {
                return null; //Change later, catch-alls are bad!
            }
            return Encoding.UTF8.GetString(codeArray);
        }
    }
}
