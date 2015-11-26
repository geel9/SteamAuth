using System;
using SteamAuth;

namespace TestBed
{
    class Program
    {
        static void Main(string[] args)
        {
            string username = "";
            string password = "";
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
                        Console.WriteLine("Captcha GID: " + login.CaptchaGID);
                        Console.WriteLine("Please enter captcha text: ");
                        string captchaText = Console.ReadLine();
                        login.CaptchaText = captchaText;
                        break;
                }
            }
        }
    }
}
