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
        private static byte[] s_rgchSteamguardCodeChars = new byte[] { 50, 51, 52, 53, 54, 55, 56, 57, 66, 67, 68, 70, 71, 72, 74, 75, 77, 78, 80, 81, 82, 84, 86, 87, 88, 89 };

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

        public string GenerateSteamGuardCodeForTime(long n)
        {
            //Convert the unsigned byte array returned from Convert.FromBase64String to a signed byte array.
            //byte[] signedSharedSecretArray = Array.ConvertAll(Convert.FromBase64String(this.SharedSecret), b => unchecked((byte)b));

            byte[] sharedSecretArray = Convert.FromBase64String(this.SharedSecret);

            n /= 30L;
            byte[] array = new byte[8];
            int n2 = 8;
            while (true)
            {
                int n3 = n2 - 1;
                if (n2 <= 0)
                {
                    break;
                }
                array[n3] = (byte)n;
                n >>= 8;
                n2 = n3;
            }

            HMACSHA1 hmacsha1 = new HMACSHA1();
            hmacsha1.Key = sharedSecretArray;
            byte[] final = hmacsha1.ComputeHash(array);
            byte[] array2 = new byte[5];
            try
            {
                byte b = (byte)(final[19] & 0xF);
                int n4 = (final[b] & 0x7F) << 24 | (final[b + 1] & 0xFF) << 16 | (final[b + 2] & 0xFF) << 8 | (final[b + 3] & 0xFF);

                for (int i = 0; i < 5; ++i)
                {
                    array2[i] = s_rgchSteamguardCodeChars[n4 % s_rgchSteamguardCodeChars.Length];
                    n4 /= s_rgchSteamguardCodeChars.Length;
                }
            }
            catch (Exception e)
            {
                return null; //Change later, catch-alls are badde
            }
            return System.Text.Encoding.UTF8.GetString(array2);
        }
    }
}
