using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Web;
using System.Threading;
using System;
using Claw.Modules.Eshop.Helper;
using System.Threading.Tasks;
using System.Net.Http;

namespace Claw.Modules.Eshop
{
    public class MsTranslator
    {
        #region AdmAccessToken
        [DataContract]
        public class AdmAccessToken
        {
            [DataMember]
            public string access_token { get; set; }
            [DataMember]
            public string token_type { get; set; }
            [DataMember]
            public string expires_in { get; set; }
            [DataMember]
            public string scope { get; set; }
        }
        #endregion
        #region AdmAuthentication

        public class AdmAuthentication
        {
            private string _request;
            private AdmAccessToken _token;
            
            #region AdmAuthentication

            public AdmAuthentication(string clientId, string clientSecret)
            {
                _request = string.Format(
                        "grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com",
                        HttpUtility.UrlEncode(clientId), HttpUtility.UrlEncode(clientSecret));

                _token = HttpPost(_request);
            } 

            #endregion

            #region GetAccessToken
            public AdmAccessToken GetAccessToken()
            {
                return _token;
            } 
            #endregion

            #region HttpPost

            /// <summary>Prepare OAuth request</summary>
            private AdmAccessToken HttpPost(string requestDetails)
            {
                const string accessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13/";
                var webRequest = WebRequest.Create(accessUri);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                var bytes = Encoding.ASCII.GetBytes(requestDetails);
                webRequest.ContentLength = bytes.Length;
                using (var outputStream = webRequest.GetRequestStream())
                {
                    outputStream.Write(bytes, 0, bytes.Length);
                }

                using (var webResponse = webRequest.GetResponse())
                {
                    var serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));
                    return (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());
                }
            } 

            #endregion
        }

        #endregion

        #region GetAccessToken

        public static AdmAccessToken GetAccessToken()
        {
            AzureAuthToken authToken = null;
            int nTries = 3;

            LB_LOOP:
            try
            {
                var setting = SettingsBiz.GetSettings().Display.Admin.MsTranslator;
                //var admAuth = new AdmAuthentication(setting.ClientId, setting.ClientSecret);
                //return admAuth.GetAccessToken();
                authToken = new AzureAuthToken(setting.ClientSecret);
                string token = authToken.GetAccessToken();
                return new AdmAccessToken() {access_token = token};

            }
            catch (Exception er)
            {
                if (--nTries >= 0 && authToken != null &&
                    !authToken.RequestStatusCode.InList(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden))
                {
                    Thread.Sleep(1000);
                    goto LB_LOOP;
                }
                else throw er;
            }
        }

        #endregion
    }

    #region AzureAuthToken

    /// <summary>
    /// Client to call Cognitive Services Azure Auth Token service in order to get an access token.
    /// Exposes asynchronous as well as synchronous methods.
    /// </summary>
    public class AzureAuthToken
    {
        #region Fields

        /// URL of the token service
        private static readonly Uri ServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");

        /// Name of header used to pass the subscription key to the token service
        private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        /// After obtaining a valid token, this class will cache it for this duration.
        /// Use a duration of 5 minutes, which is less than the actual token lifetime of 10 minutes.
        private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 5, 0);

        /// Cache the value of the last valid token obtained from the token service.
        private string _storedTokenValue = string.Empty;

        /// When the last valid token was obtained.
        private DateTime _storedTokenTime = DateTime.MinValue;

        /// Gets the subscription key.
        public string SubscriptionKey { get; set; }

        /// Gets the HTTP status code for the most recent request to the token service.
        public HttpStatusCode RequestStatusCode { get; private set; } 

        #endregion

        #region AzureAuthToken

        /// <summary>
        /// Creates a client to obtain an access token.
        /// </summary>
        /// <param name="key">Subscription key to use to get an authentication token.</param>
        public AzureAuthToken(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key"/*nameof(key)*/, "A subscription key is required");
            }

            this.SubscriptionKey = key;
            this.RequestStatusCode = HttpStatusCode.InternalServerError;
        } 

        #endregion

        #region GetAccessTokenAsync

        /// <summary>
        /// Gets a token for the specified subscription.
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public async Task<string> GetAccessTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(this.SubscriptionKey))
            {
                return string.Empty;
            }

            // Re-use the cached token if there is one.
            if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
            {
                return _storedTokenValue;
            }

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = ServiceUrl;
                request.Content = new StringContent(string.Empty);
                request.Headers.TryAddWithoutValidation(OcpApimSubscriptionKeyHeader, this.SubscriptionKey);
                client.Timeout = TimeSpan.FromSeconds(30);
                var response = await client.SendAsync(request);
                this.RequestStatusCode = response.StatusCode;
                response.EnsureSuccessStatusCode();
                var token = await response.Content.ReadAsStringAsync();
                _storedTokenTime = DateTime.Now;
                _storedTokenValue = /*"Bearer " +*/ token;
                return _storedTokenValue;
            }
        } 

        #endregion

        #region GetAccessToken

        /// <summary>
        /// Gets a token for the specified subscription. Synchronous version.
        /// Use of async version preferred
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public string GetAccessToken()
        {
            // Re-use the cached token if there is one.
            if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
            {
                return _storedTokenValue;
            }

            string accessToken = null;
            var task = Task.Run(async () =>
            {
                accessToken = await this.GetAccessTokenAsync();
            });

            while (!task.IsCompleted)
            {
                System.Threading.Thread.Yield();
            }
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
            if (task.IsCanceled)
            {
                throw new Exception("Timeout obtaining access token.");
            }
            return accessToken;
        } 

        #endregion

    }

    #endregion
}