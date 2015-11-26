using System;
using SteamAuth;
using Newtonsoft.Json;
using System.IO;

namespace TestBed
{
    class Program
    {
        static void Main(string[] args)
        {
            //This basic loop will log into user accounts you specify, enable the mobile authenticator, and save a maFile (mobile authenticator file)
            while (true)
            {
                Console.WriteLine("Enter user/password: ");
                string username = Console.ReadLine();
                string password = Console.ReadLine();
                UserLogin login = new UserLogin(username, password);
                LoginResult response = LoginResult.BadCredentials;
                while ((response = login.DoLogin()) != LoginResult.LoginOkay)
                {
                    switch (response)
                    {
                        case LoginResult.NeedEmail:
                            Console.WriteLine("Please enter your email code: ");
                            string code = Console.ReadLine();
                            login.EmailCode = code;
                            break;

                        case LoginResult.NeedCaptcha:
                            System.Diagnostics.Process.Start(APIEndpoints.COMMUNITY_BASE + "/public/captcha.php?gid=" + login.CaptchaGID); //Open a web browser to the captcha image
                            Console.WriteLine("Please enter captcha text: ");
                            string captchaText = Console.ReadLine();
                            login.CaptchaText = captchaText;
                            break;

                        case LoginResult.Need2FA:
                            Console.WriteLine("Please enter your mobile authenticator code: ");
                            code = Console.ReadLine();
                            login.TwoFactorCode = code;
                            break;
                    }
                }

                AuthenticatorLinker linker = new AuthenticatorLinker(login.Session);
                linker.PhoneNumber = null; //Set this to non-null to add a new phone number to the account.
                var result = linker.AddAuthenticator();

                if (result != AuthenticatorLinker.LinkResult.AwaitingFinalization)
                {
                    Console.WriteLine("Failed to add authenticator: " + result);
                    continue;
                }

                try
                {
                    string sgFile = JsonConvert.SerializeObject(linker.LinkedAccount, Formatting.Indented);
                    string fileName = linker.LinkedAccount.AccountName + ".maFile";
                    File.WriteAllText(fileName, sgFile);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("EXCEPTION saving maFile. For security, authenticator will not be finalized.");
                    continue;
                }

                Console.WriteLine("Please enter SMS code: ");
                string smsCode = Console.ReadLine();
                var linkResult = linker.FinalizeAddAuthenticator(smsCode);

                if (linkResult != AuthenticatorLinker.FinalizeResult.Success)
                {
                    Console.WriteLine("Unable to finalize authenticator: " + linkResult);
                }
            }
        }
    }
}
