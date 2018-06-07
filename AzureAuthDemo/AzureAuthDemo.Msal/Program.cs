using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace AzureAuthDemo.Msal
{
    class Program
    {
        private const string STR_ClientId = "0b862af3-2d4e-4f7f-98ae-c864843a7dbf";
        static PublicClientApplication _pubClient = new PublicClientApplication(STR_ClientId);
        private static List<string> _scopes = new List<string>() {"User.Read"};
        static void Main(string[] args)
        {


            var authResult = getAuthResult().Result;
            authResult = getAuthResult().Result;
        }



        private static async Task<AuthenticationResult> getAuthResult()
        {
            try
            {
                return await _pubClient.AcquireTokenSilentAsync(_scopes, _pubClient.Users.FirstOrDefault());
            }
            catch (MsalUiRequiredException)
            {
                return await _pubClient.AcquireTokenAsync(_scopes);
            }
        }
    }
}
