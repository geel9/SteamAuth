using System;
using SteamAuth;
using Newtonsoft.Json;
using System.IO;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;
using System.Threading.Tasks;

namespace TestBed
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //This basic loop will log into user accounts you specify, enable the mobile authenticator, and save a maFile (mobile authenticator file)
            while (true)
            {
                // Start a new SteamClient instance
                SteamClient steamClient = new SteamClient();

                // Connect to Steam
                steamClient.Connect();

                // Really basic way to wait until Steam is connected
                while (!steamClient.IsConnected)
                    await Task.Delay(500);

                Console.WriteLine("Enter username: ");
                string username = Console.ReadLine();

                Console.WriteLine("Enter password: ");
                string password = Console.ReadLine();

                // Create a new auth session
                CredentialsAuthSession authSession;
                try
                {
                    authSession = await steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                    {
                        Username = username,
                        Password = password,
                        IsPersistentSession = false,
                        PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
                        ClientOSType = EOSType.Android9,
                        Authenticator = new UserConsoleAuthenticator(),
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error logging in: " + ex.Message);
                    return;
                }

                // Starting polling Steam for authentication response
                var pollResponse = await authSession.PollingWaitForResultAsync();

                // Build a SessionData object
                SessionData sessionData = new SessionData()
                {
                    SteamID = authSession.SteamID.ConvertToUInt64(),
                    AccessToken = pollResponse.AccessToken,
                    RefreshToken = pollResponse.RefreshToken,
                };

                // Init AuthenticatorLinker
                AuthenticatorLinker linker = new AuthenticatorLinker(sessionData);

                Console.WriteLine("If account has no phone number, enter one now: (+1 XXXXXXXXXX)");
                string phoneNumber = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    linker.PhoneNumber = null;
                }
                else
                {
                    linker.PhoneNumber = phoneNumber;
                }

                int tries = 0;
                AuthenticatorLinker.LinkResult result = AuthenticatorLinker.LinkResult.GeneralFailure;
                while (tries <= 5)
                {
                    tries++;

                    // Add authenticator
                    result = await linker.AddAuthenticator();

                    if (result == AuthenticatorLinker.LinkResult.MustConfirmEmail)
                    {
                        Console.WriteLine("Click the link sent to your email address: " + linker.ConfirmationEmailAddress);
                        Console.WriteLine("Press enter when done");
                        Console.ReadLine();
                        continue;
                    }

                    if (result == AuthenticatorLinker.LinkResult.MustProvidePhoneNumber)
                    {
                        Console.WriteLine("Account requires a phone number. Login again and enter one.");
                        break;
                    }

                    if (result == AuthenticatorLinker.LinkResult.AuthenticatorPresent)
                    {
                        Console.WriteLine("Account already has an authenticator linked.");
                        break;
                    }

                    if (result != AuthenticatorLinker.LinkResult.AwaitingFinalization)
                    {
                        Console.WriteLine("Failed to add authenticator: " + result);
                        break;
                    }

                    // Write maFile
                    try
                    {
                        string sgFile = JsonConvert.SerializeObject(linker.LinkedAccount, Formatting.Indented);
                        string fileName = linker.LinkedAccount.AccountName + ".maFile";
                        File.WriteAllText(fileName, sgFile);
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("EXCEPTION saving maFile. For security, authenticator will not be finalized.");
                        break;
                    }
                }

                if (result != AuthenticatorLinker.LinkResult.AwaitingFinalization)
                    continue;

                tries = 0;
                while (tries <= 5)
                {
                    Console.WriteLine("Please enter SMS code: ");
                    string smsCode = Console.ReadLine();
                    var linkResult = await linker.FinalizeAddAuthenticator(smsCode);

                    if (linkResult != AuthenticatorLinker.FinalizeResult.Success)
                    {
                        Console.WriteLine("Failed to finalize authenticator: " + linkResult);
                        continue;
                    }

                    Console.WriteLine("Authenticator finalized!");
                    break;
                }
            }
        }
    }
}
