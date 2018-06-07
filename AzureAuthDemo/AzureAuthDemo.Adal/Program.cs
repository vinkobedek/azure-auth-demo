using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AzureAuthDemo.Adal
{
    class Program
    {
        private const string STR_ClientId = "0b862af3-2d4e-4f7f-98ae-c864843a7dbf";
        private const string STR_RedirectUri = "urn://syskit-security-manager/";

        static AuthenticationContext _authContext = new AuthenticationContext("https://login.windows.net/common/");
        static void Main(string[] args)
        {
            var res = getAuthResult().Result;
            res = getAuthResult().Result;
        }

        private static async Task<AuthenticationResult> getAuthResult()
        {
            // spomenuti tokencache konstruktor
            // po defaultu se koristi neki inmemory tokencache
            
            try
            {
                return await _authContext.AcquireTokenSilentAsync("https://graph.microsoft.com", STR_ClientId);
            }
            catch (AdalSilentTokenAcquisitionException)
            {
                return await _authContext.AcquireTokenAsync("https://graph.microsoft.com", STR_ClientId, new Uri(STR_RedirectUri), new PlatformParameters(PromptBehavior.Always));
            }
        }
    }
}
