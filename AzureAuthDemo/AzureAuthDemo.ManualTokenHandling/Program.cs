using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace AzureAuthDemo.ManualTokenHandling
{
    class Program
    {
        private const string STR_ClientId = "0b862af3-2d4e-4f7f-98ae-c864843a7dbf";
        private const string STR_RedirectUri = "urn://syskit-security-manager/";
        private const string STR_AuthorityFormatString = "https://login.windows.net/{0}/";
        private const string STR_DefaultResourceId = "https://graph.microsoft.com";


        static void Main(string[] args)
        {

            Console.WriteLine("1. Constructing authorization URL");
            var authUrl = getAuthorizationCodeUrl(STR_ClientId, STR_RedirectUri);
            Console.WriteLine(authUrl);

            Console.WriteLine("2. Retrieving authorization code");
            var code = getAuthorizationCode(STR_ClientId, STR_RedirectUri);
            Console.WriteLine(code);


            Console.WriteLine("4. Retrieving access token");
            var tokenResponse = getTokenResponseAsync(STR_ClientId, STR_RedirectUri, code).Result;


            Console.WriteLine("5. Using access token");
            var graphResponse = GetGraphResponseAsync("me", tokenResponse.AccessToken).Result;

            Console.WriteLine("6. Retrieving access token");
            tokenResponse = getTokenResponseFromRefreshTokenAsync(STR_ClientId, STR_RedirectUri, tokenResponse.RefreshToken).Result;


            Console.WriteLine("7. Using access token");
            graphResponse = GetGraphResponseAsync("me", tokenResponse.AccessToken).Result;



            Console.WriteLine("8. get V2 endpoint authentication code");
            string v2Scopes = "offline_access User.Read Files.ReadWrite https://graph.microsoft.com/Calendars.Read";
            var v2Code = getAuthorizationCodeV2(STR_ClientId, STR_RedirectUri, v2Scopes);

            Console.WriteLine("9. Retrieving V2 access token");
            var v2TokenResponse = getTokenResponseV2Async(STR_ClientId, STR_RedirectUri, v2Code, v2Scopes).Result;

            Console.WriteLine("10. Using V2 access token");
            var v2GraphResponse = GetGraphResponseAsync("me", v2TokenResponse.AccessToken).Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tenantId">tenantId moze biti ime domene, ili tenant guid</param>
        /// <returns></returns>
        private static string getAuthorityUrl(string tenantId)
        {
            return string.Format(STR_AuthorityFormatString, tenantId);
        }

        private static string getAuthorizationCodeUrl(string clientId, string redirectUri, string resourceId = "https://graph.microsoft.com")
        {
            return getAuthorityUrl("common") +"oauth2/authorize?"+
                "response_type=code" + 
                "&client_id=" + clientId +
                "&prompt=consent" +
                "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                "&resource=" + Uri.EscapeDataString(resourceId);
        }

        private static string getAuthorizationCodeUrlV2(string clientId, string redirectUri, string scope)
        {
            return getAuthorityUrl("common") + "oauth2/v2.0/authorize?" +
                   "response_type=code" +
                   "&client_id=" + clientId +
                   "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                   "&scope=" + Uri.EscapeDataString(scope);
        }

        private static string getAuthorizationCode(string clientId, string redirectUri, string resourceId = "https://graph.microsoft.com" )
        {
            var authUrl = getAuthorizationCodeUrl(clientId, redirectUri, resourceId);
            return getAuthorizationCodeCore(authUrl);
        }



        private static string getAuthorizationCodeV2(string clientId, string redirectUri, string scope)
        {
            var authUrl = getAuthorizationCodeUrlV2(clientId, redirectUri, scope);
            return getAuthorizationCodeCore(authUrl);
        }

        private static string getAuthorizationCodeCore(string authUrl)
        {
            string authCode = "";

            // Thread umjesto taskova tako da mozemo apartment state postaviti
            // inace ce web kontrola pucati
            Thread thread = new Thread(() =>
            {
                using (var authForm = new AuthDialog())
                {
                    authForm.NavigateTo(authUrl);
                    authForm.ShowDialog();
                    authCode = authForm.AuthorizationCode;
                }
            });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return authCode;
        }


        private static async Task<TokenResponse> getTokenResponseAsync(string clientId, string redirectUri, string authcode, string resourceId = "https://graph.microsoft.com")
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(getAuthorityUrl("common"));
                //client.DefaultRequestHeaders.Host = "https://login.microsoftonline.com";
                var httpResponse = (await client.PostAsync("oauth2/token", new FormUrlEncodedContent(
                    new Dictionary<string,string>()
                    {
                        { "grant_type", "authorization_code" },
                        { "client_id", clientId },
                        { "code",  authcode },
                        { "redirect_uri", redirectUri},
                        { "resource", resourceId}
                    }
                )));
                var content = await httpResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TokenResponse>(content);                
            }
        }

        private static async Task<TokenResponse> getTokenResponseV2Async(string clientId, string redirectUri, string authcode, string scope)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(getAuthorityUrl("common"));
                //client.DefaultRequestHeaders.Host = "https://login.microsoftonline.com";
                var httpResponse = (await client.PostAsync("oauth2/v2.0/token", new FormUrlEncodedContent(
                    new Dictionary<string, string>()
                    {
                        { "grant_type", "authorization_code" },
                        { "client_id", clientId },
                        { "code",  authcode },
                        { "redirect_uri", redirectUri},
                        { "scope", scope}
                    }
                )));
                var content = await httpResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TokenResponse>(content);
            }
        }

        private static async Task<TokenResponse> getTokenResponseFromRefreshTokenAsync(string clientId, string redirectUri, string refreshToken, string resourceId = "https://graph.microsoft.com")
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(getAuthorityUrl("common"));
                var httpResponse = (await client.PostAsync("oauth2/token", new FormUrlEncodedContent(
                    new Dictionary<string, string>()
                    {
                        { "grant_type", "refresh_token" },
                        { "client_id", clientId },
                        { "refresh_token",  refreshToken },
                        { "resource", resourceId}
                    }
                )));
                var content = await httpResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TokenResponse>(content);
            }
        }

        private static async Task<string> GetGraphResponseAsync(string resource, string accessToken)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync(resource);
                var content = await response.Content.ReadAsStringAsync();

                return content;
            }
        }


        class AuthDialog : Form
        {
            WebBrowser browser;
            public AuthDialog()
            {
                this.Size = new Size(568, 720);
                
                browser = new WebBrowser();
                browser.Dock = DockStyle.Fill;
                browser.Navigated += Browser_Navigated;
                this.Controls.Add(browser);
            }

            private void Browser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
            {
                if (e.Url.AbsoluteUri.Contains(("code=")))
                {
                    var startCodeIndex = e.Url.AbsoluteUri.IndexOf("code=") + 5;
                    var code = e.Url.AbsoluteUri.Substring(startCodeIndex, e.Url.AbsoluteUri.IndexOf("&", startCodeIndex) - startCodeIndex);
                    AuthorizationCode = code;
                    this.Close();
                }
            }

            public void NavigateTo(string url)
            {
                browser.Navigate(url);
            }

            public string AuthorizationCode { get; private set; }
        }


        class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
            [JsonProperty("id_token")]
            public string IdToken { get; set; }
            public string Resource { get; set; }
            public string Scope { get; set; }
            [JsonProperty("expires_on")]
            [JsonConverter(typeof(MicrosecondEpochConverter))]
            public DateTime ExpiresOn { get; set; }
        }


        public class MicrosecondEpochConverter : DateTimeConverterBase
        {
            private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteRawValue(((int)((DateTime)value - _epoch).TotalSeconds).ToString());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.Value == null) { return null; }
                return _epoch.AddSeconds(Convert.ToInt64(reader.Value));
            }
        }

    }
}
