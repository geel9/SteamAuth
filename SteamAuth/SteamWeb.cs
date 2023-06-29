using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamAuth
{
    public class SteamWeb
    {
        public static string MOBILE_APP_USER_AGENT = "okhttp/3.12.12";

        public static async Task<string> GETRequest(string url, CookieContainer cookies)
        {
            string response;
            using (CookieAwareWebClient wc = new CookieAwareWebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.CookieContainer = cookies;
                wc.Headers[HttpRequestHeader.UserAgent] = SteamWeb.MOBILE_APP_USER_AGENT;
                response = await wc.DownloadStringTaskAsync(url);
            }
            return response;
        }

        public static async Task<string> POSTRequest(string url, CookieContainer cookies, NameValueCollection body)
        {
            if (body == null)
                body = new NameValueCollection();

            string response;
            using (CookieAwareWebClient wc = new CookieAwareWebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.CookieContainer = cookies;
                wc.Headers[HttpRequestHeader.UserAgent] = SteamWeb.MOBILE_APP_USER_AGENT;
                byte[] result = await wc.UploadValuesTaskAsync(new Uri(url), "POST", body);
                response = Encoding.UTF8.GetString(result);
            }
            return response;
        }
    }
}
