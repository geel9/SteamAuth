using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SteamAuth;
using SteamAuth.Constants;
using SteamAuth.Exceptions;

namespace TestBed
{
    internal class Program
    {
        private static async Task AddAuthenticator(SessionData sessionData, string sms)
        {
            var linker = new AuthenticatorLinker(sessionData);

            var authenticator = await linker.RequestToAddAuthenticator().ConfigureAwait(false);
            string fileName = null;

            for (var i = 0; i < 100; i++)
            {
                fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    authenticator.AuthenticatorData.AccountName + (i > 0 ? i.ToString() : "") + ".maFile2");

                if (!File.Exists(fileName))
                {
                    PrintMessage($"Saving authenticator to file: {fileName}");
                    authenticator.SerializeToFile(fileName);

                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            {
                // ReSharper disable once ThrowingSystemException
                throw new Exception(
                    "Failed to save the authenticator to a file. A file with the same name might already exist.");
            }

            while (true)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(sms))
                    {
                        sms = PrintQuestion("Enter Received SMS Code:");
                    }

                    await linker.FinilizeAddAuthenticator(authenticator, sms).ConfigureAwait(false);

                    break;
                }
                catch (AuthenticatorLinkerException e)
                {
                    PrintMessage(e.Message);

                    if (e.ErrorCode == AuthenticatorLinkerErrorCode.BadSMSCode)
                    {
                        sms = null;

                        continue;
                    }

                    throw;
                }
            }

            PrintSuccess("Authenticator added.");
        }

        private static async Task<string> CheckPhoneAssociation(SessionData sessionData)
        {
            var linker = new AuthenticatorLinker(sessionData);
            string latestSMS = null;

            if (!await linker.DoesAccountHasPhoneNumber().ConfigureAwait(false))
            {
                PrintMessage("To add a new authenticator, you first need to add a phone number.");
                var phoneNumber = PrintQuestion("Enter A Phone Number:");

                while (!await linker.RequestToAddPhoneNumber(phoneNumber).ConfigureAwait(false))
                {
                    PrintMessage("Failed to add this phone number.");
                    phoneNumber = PrintQuestion("Enter A New Phone Number:");
                }

                latestSMS = PrintQuestion("Enter Received SMS Code:");

                while (!await linker.VerifyPhoneNumberBySMS(latestSMS).ConfigureAwait(false))
                {
                    PrintMessage("Failed to add this phone number.");
                    latestSMS = PrintQuestion("Enter Received SMS Code:");
                }

                PrintSuccess($"Phone number {phoneNumber} added.");
            }
            else
            {
                PrintSuccess("There is a phone number already associated with this account.");
            }

            return latestSMS;
        }

        private static async Task<SessionData> DoLogin()
        {
            PrintMessage("Enter your username and password to start the process of logging in.");
            var username = PrintQuestion("Enter Username:");
            var password = PrintQuestion("Enter Password:");
            var credentials = new LoginCredentials(username, password);
            var loginProcess = new UserLogin();
            SessionData sessionData;

            while (true)
            {
                try
                {
                    sessionData = await loginProcess.DoLogin(credentials).ConfigureAwait(false);

                    break;
                }
                catch (UserLoginException e)
                {
                    PrintMessage(e.Message);

                    switch (e.ErrorCode)
                    {
                        case UserLoginErrorCode.BadCredentials:
                            // Ask again for user credentials
                            username = PrintQuestion("Enter Username:");
                            password = PrintQuestion("Enter Password:");
                            credentials = new LoginCredentials(username, password);

                            continue;
                        case UserLoginErrorCode.NeedsCaptchaCode:
                            // Save captcha image to disk
                            var captchaImage = await loginProcess.DownloadCaptchaImage().ConfigureAwait(false);
                            var filePath = Path.Combine(Path.GetTempPath(),
                                Guid.NewGuid().ToString("N") + ".png");
                            File.WriteAllBytes(filePath, captchaImage);

                            // Show captcha image to the user
                            Process.Start(filePath);

                            // Ask for captcha code
                            credentials.CaptchaCode = PrintQuestion("Enter Captcha Code:");

                            continue;
                        case UserLoginErrorCode.NeedsTwoFactorAuthenticationCode:
                            // Ask for 2FA code
                            credentials.TwoFactorAuthenticationCode =
                                PrintQuestion("Enter Two Factor Authentication Code (Steam Guard):");

                            continue;
                        case UserLoginErrorCode.NeedsEmailVerificationCode:
                            // Ask for mailed code
                            credentials.EmailVerificationCode =
                                PrintQuestion("Enter Email Verification Code:");

                            continue;
                        default:

                            // Any other error and we simply rethrow
                            throw;
                    }
                }
            }

            PrintSuccess($"Login for SteamId `{sessionData.SteamId}` was successful.");

            return sessionData;
        }

        private static void Main()
        {
            // ------------- Login Process
            PrintCaption("Logging In");
            SessionData sessionData;

            try
            {
                sessionData = DoLogin().Result;
            }
            catch (Exception e)
            {
                PrintException(e, "The process of logging in failed.");

                return;
            }

            // ------------- Phone Number
            PrintCaption("Checking Phone Number");
            string latestSMS;

            try
            {
                latestSMS = CheckPhoneAssociation(sessionData).Result;
            }
            catch (Exception e)
            {
                PrintException(e, "The process of checking account for having a valid associated phone number failed.");

                return;
            }

            // ------------- Authenticator
            PrintCaption("Adding an Authenticator");

            try
            {
                AddAuthenticator(sessionData, latestSMS).Wait();
            }
            catch (Exception e)
            {
                PrintException(e, "The process of adding a new authenticator failed.");

                return;
            }

            // ------------- Exit
            PrintCaption("End of Program");
            Console.WriteLine("Press `Enter` to exit.");
            Console.ReadLine();
        }

        private static void PrintCaption(string caption)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            var sideArm = new string('-', (int) Math.Floor((Console.BufferWidth - (caption.Length + 4)) / 2d));
            Console.WriteLine($"{sideArm} [{caption}] {sideArm}");
            Console.ResetColor();
        }

        private static void PrintException(Exception e, string message)
        {
            if (e is AggregateException)
            {
                e = ((AggregateException) e).InnerExceptions[0];
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine(e.Message);
            Console.WriteLine("---- " + message);
            Console.WriteLine("---- Press `Enter` to exit and try again later.");
            Console.ReadLine();
            Environment.Exit(-1);
        }

        private static void PrintMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("---- " + message);
            Console.ResetColor();
        }

        private static string PrintQuestion(string message)
        {
            var response = "";

            while (string.IsNullOrWhiteSpace(response))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("-- " + message + " ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                response = Console.ReadLine();
                Console.ResetColor();
            }

            return response.Trim();
        }

        private static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}