using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamAuth.Exceptions;

namespace SteamAuth.Helpers
{
    internal static class SteamWeb
    {
        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the response as a Stream
        /// </summary>
        /// <param name="url">The URL of the HTTP server and resource to send the request to.</param>
        /// <param name="method">The request method to be used while sending the request.</param>
        /// <param name="data">The query string or post data to send as part of the request.</param>
        /// <param name="cookies">The cookies to send as part of the request.</param>
        /// <param name="headers">The headers to send as part of the request.</param>
        /// <param name="referer">The referer of the request.</param>
        /// <returns>Response of the HTTP server. Should be disposed after reading.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        /// <exception cref="TokenExpiredException">OAuth token has expired.</exception>
        // ReSharper disable once TooManyArguments
        public static Task<MemoryStream> Download(
            string url,
            SteamWebRequestMethod method,
            QueryStringBuilder data = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            string referer = Constants.Constants.CommunityBaseUrl)
        {
            return Download(url, method, data?.ToString(), cookies, headers, referer);
        }

        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the response as a Stream
        /// </summary>
        /// <param name="url">The URL of the HTTP server and resource to send the request to.</param>
        /// <param name="method">The request method to be used while sending the request.</param>
        /// <param name="dataString">The query string or post data to send as part of the request.</param>
        /// <param name="cookies">The cookies to send as part of the request.</param>
        /// <param name="headers">The headers to send as part of the request.</param>
        /// <param name="referer">The referer of the request.</param>
        /// <returns>Response of the HTTP server. Should be disposed after reading.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        /// <exception cref="TokenExpiredException">OAuth token has expired.</exception>
        // ReSharper disable once TooManyArguments
        public static async Task<MemoryStream> Download(
            string url,
            // ReSharper disable once FlagArgument
            SteamWebRequestMethod method,
            string dataString = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            string referer = Constants.Constants.CommunityBaseUrl)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dataString) && method == SteamWebRequestMethod.Get)
                {
                    url += (url.Contains("?") ? "&" : "?") + dataString;
                    dataString = null;
                }

                var request = (HttpWebRequest) WebRequest.Create(url);
                request.Method = method == SteamWebRequestMethod.Post ? "POST" : "GET";
                request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
                request.UserAgent = Constants.Constants.UserAgent;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                request.Referer = referer;

                if (headers != null)
                {
                    request.Headers.Add(headers);
                }

                if (cookies != null)
                {
                    request.CookieContainer = cookies;
                }

                if (!string.IsNullOrWhiteSpace(dataString) && method == SteamWebRequestMethod.Post)
                {
                    request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    request.ContentLength = dataString.Length;

                    using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                    {
                        using (var requestWriter = new StreamWriter(requestStream))
                        {
                            await requestWriter.WriteAsync(dataString).ConfigureAwait(false);
                            requestWriter.Close();
                        }
                    }
                }

                using (var response = (HttpWebResponse) await request.GetResponseAsync().ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new WebException("Unexpected status code.", null, WebExceptionStatus.UnknownError,
                            response);
                    }

                    var responseStream = response.GetResponseStream();

                    if (responseStream == null)
                    {
                        throw new WebException("Empty response returned.", null, WebExceptionStatus.UnknownError,
                            response);
                    }

                    var buffer = new byte[16 * 1024];

                    var memoryStream = new MemoryStream();
                    int read;

                    while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        memoryStream.Write(buffer, 0, read);
                    }

                    return memoryStream;
                }
            }
            catch (WebException e)
            {
                var response = e.Response as HttpWebResponse;

                //Redirecting -- likely to a steammobile:// URI
                if (response?.StatusCode == HttpStatusCode.Found)
                {
                    var location = response.Headers.Get("Location");

                    if (!string.IsNullOrEmpty(location))
                    {
                        //Our OAuth token has expired. This is given both when we must refresh our session, or the entire OAuth Token cannot be refreshed anymore.
                        //Thus, we should only throw this exception when we're attempting to refresh our session.
                        if (location == "steammobile://lostauth" &&
                            url == Constants.Constants.MobileAuthenticatorGetTokenUrl)
                        {
                            throw new TokenExpiredException(e);
                        }
                    }
                }

                throw;
            }
        }

        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the JSON response de-serialized as an object
        /// </summary>
        /// <param name="url">The URL of the HTTP server and resource to send the request to.</param>
        /// <param name="method">The request method to be used while sending the request.</param>
        /// <param name="data">The query string or post data to send as part of the request.</param>
        /// <param name="cookies">The cookies to send as part of the request.</param>
        /// <param name="headers">The headers to send as part of the request.</param>
        /// <param name="referer">The referer of the request.</param>
        /// <returns>Response of the HTTP server. Should be disposed after reading.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        /// <exception cref="TokenExpiredException">OAuth token has expired.</exception>
        // ReSharper disable once TooManyArguments
        // ReSharper disable once TooManyArguments
        public static Task<T> DownloadJson<T>(
            string url,
            SteamWebRequestMethod method,
            QueryStringBuilder data = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            string referer = Constants.Constants.CommunityBaseUrl)
        {
            return DownloadJson<T>(url, method, data?.ToString(), cookies, headers, referer);
        }

        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the JSON response de-serialized as an object
        /// </summary>
        /// <param name="url">The URL of the HTTP server and resource to send the request to.</param>
        /// <param name="method">The request method to be used while sending the request.</param>
        /// <param name="dataString">The query string or post data to send as part of the request.</param>
        /// <param name="cookies">The cookies to send as part of the request.</param>
        /// <param name="headers">The headers to send as part of the request.</param>
        /// <param name="referer">The referer of the request.</param>
        /// <returns>Response of the HTTP server.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        /// <exception cref="TokenExpiredException">OAuth token has expired.</exception>
        // ReSharper disable once TooManyArguments
        public static async Task<T> DownloadJson<T>(
            string url,
            SteamWebRequestMethod method,
            string dataString = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            string referer = Constants.Constants.CommunityBaseUrl)
        {
            var json = await DownloadString(url, method, dataString, cookies, headers, referer).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the response as a string
        /// </summary>
        /// <param name="url">The URL of the HTTP server and resource to send the request to.</param>
        /// <param name="method">The request method to be used while sending the request.</param>
        /// <param name="data">The query string or post data to send as part of the request.</param>
        /// <param name="cookies">The cookies to send as part of the request.</param>
        /// <param name="headers">The headers to send as part of the request.</param>
        /// <param name="referer">The referer of the request.</param>
        /// <returns>Response of the HTTP server. Should be disposed after reading.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        /// <exception cref="TokenExpiredException">OAuth token has expired.</exception>
        // ReSharper disable once TooManyArguments
        public static Task<string> DownloadString(
            string url,
            SteamWebRequestMethod method,
            QueryStringBuilder data = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            string referer = Constants.Constants.CommunityBaseUrl)
        {
            return DownloadString(url, method, data?.ToString(), cookies, headers, referer);
        }

        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the response as a string
        /// </summary>
        /// <param name="url">The URL of the HTTP server and resource to send the request to.</param>
        /// <param name="method">The request method to be used while sending the request.</param>
        /// <param name="dataString">The query string or post data to send as part of the request.</param>
        /// <param name="cookies">The cookies to send as part of the request.</param>
        /// <param name="headers">The headers to send as part of the request.</param>
        /// <param name="referer">The referer of the request.</param>
        /// <returns>Response of the HTTP server.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        /// <exception cref="TokenExpiredException">OAuth token has expired.</exception>
        // ReSharper disable once TooManyArguments
        public static async Task<string> DownloadString(
            string url,
            SteamWebRequestMethod method,
            string dataString = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            string referer = Constants.Constants.CommunityBaseUrl)
        {
            var responseStream =
                await Download(url, method, dataString, cookies, headers, referer).ConfigureAwait(false);
            responseStream.Seek(0, SeekOrigin.Begin);

            using (var responseReader = new StreamReader(responseStream))
            {
                return await responseReader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}