using System;
using System.Diagnostics;
using System.Net;

namespace SteamAuth
{
    public class CookieAwareWebClient : WebClient
    {
        public CookieContainer CookieContainer { get; set; } = new CookieContainer();
        public CookieCollection ResponseCookies { get; set; } = new CookieCollection();

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);
            Debug.Assert(request != null, nameof(request) + " != null");
            request.CookieContainer = CookieContainer;
            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            var response = (HttpWebResponse)base.GetWebResponse(request);
            Debug.Assert(response != null, nameof(response) + " != null");
            this.ResponseCookies = response.Cookies;
            return response;
        }
    }
}
