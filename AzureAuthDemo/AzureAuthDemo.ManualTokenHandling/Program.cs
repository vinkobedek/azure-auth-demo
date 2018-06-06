using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AzureAuthDemo.ManualTokenHandling
{
    class Program
    {
        private const string STR_ClientId = "0b862af3-2d4e-4f7f-98ae-c864843a7dbf";
        private const string STR_RedirectUri = "urn://syskit-security-manager/";
        private const string STR_AuthorityFormatString = "https://login.windows.net/{0}";


        static void Main(string[] args)
        {
            var authUrl = getAuthorizationCodeUrl(STR_ClientId, STR_RedirectUri, "https://graph.microsoft.com");
            Console.WriteLine(authUrl);
            var code = getAuthorizationCode(STR_ClientId, STR_RedirectUri, "https://graph.microsoft.com");
            Console.WriteLine(code);
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

        private static string getAuthorizationCodeUrl(string clientId, string redirectUri, string resourceId)
        {
            return getAuthorityUrl("common") +"/oauth2/authorize?"+
                "response_type=code" + 
                "&client_id=" + clientId +
                "&prompt=login" +
                "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                "&display=popup" +
                "&resource=" + Uri.EscapeDataString(resourceId);
        }

        private static string getAuthorizationCode(string clientId, string redirectUri, string resourceId)
        {
            var authUrl = getAuthorizationCodeUrl(clientId, redirectUri, resourceId);
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
    }
}
