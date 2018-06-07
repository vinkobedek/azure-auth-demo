using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;

namespace AzureAuthDemo.CSOM
{
    class Program
    {
        private const string STR_ClientId = "0b862af3-2d4e-4f7f-98ae-c864843a7dbf";
        private const string STR_RedirectUri = "urn://syskit-security-manager/";

        static void Main(string[] args)
        {
            //doCSOMStuff("https://M365x991682.sharepoint.com", (url) => createContextWithCredentials(url, "HenriettaM@M365x991682.OnMicrosoft.com", "pass@word1"));


            doCSOMStuff("https://M365x991682.sharepoint.com", createContextWithAccessToken);

           // doCSOMStuff("https://M365x991682.sharepoint.com", createPnPContextWithAcessToken);


        }

        private static ClientContext createContextWithCredentials(string url, string username, string password)
        {
            var secPassword = new SecureString();
            foreach (var c in password)
            {
                secPassword.AppendChar(c);
            }
            var context = new ClientContext(url)
            {
                Credentials = new SharePointOnlineCredentials(username, secPassword)
            };

            return context;
        }

        private static ClientContext createContextWithAccessToken(string url)
        {
            // namjerno novi tokencache jer se inace koristi nesto static u pozadini pa ne bi radilo 
            // prilikom PnP demoa
            var authContext = new AuthenticationContext("https://login.windows.net/common/", new TokenCache());
            
            var csomContext= new ClientContext(url);
            csomContext.ExecutingWebRequest += (sender, args) =>
            {
                var uri = new Uri(url);
                var resourceId = uri.Scheme + "://" + uri.Host;
                var authResult = authContext.AcquireTokenAsync(resourceId, STR_ClientId, new Uri(STR_RedirectUri),
                    new PlatformParameters(PromptBehavior.Auto));
                args.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + authResult.Result.AccessToken;
            };

            return csomContext;

        }

        private static ClientContext createPnPContextWithAcessToken(string url)
        {
            AuthenticationManager pnpAuthManager = new AuthenticationManager();
            return pnpAuthManager.GetAzureADNativeApplicationAuthenticatedContext(url, STR_ClientId, STR_RedirectUri);
        }

        private static void doCSOMStuff(string siteUrl, Func<string, ClientContext> clientContextFactory)
        {
            using (var ctx = clientContextFactory(siteUrl))
            {
                ctx.Load(ctx.Web, w => w.Title);
                ctx.ExecuteQuery();

                Console.WriteLine("web title: {0}", ctx.Web.Title);

                ctx.Load(ctx.Web.RoleAssignments, x => x.Include(y => y.Member.LoginName, y => y.RoleDefinitionBindings.Include(rb => rb.Name)));
                ctx.ExecuteQuery();

                foreach (var ra in ctx.Web.RoleAssignments)
                {
                    var roleDefinitions = string.Join(",", ra.RoleDefinitionBindings.Select(x => x.Name));
                    Console.WriteLine($"{ra.Member.LoginName} - {roleDefinitions}");
                }
            }
        }
    }
}
