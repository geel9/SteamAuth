using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamAuth;

namespace TestBed
{
    class Program
    {
        static void Main(string[] args)
        {
            SteamGuardAccount acc = new SteamGuardAccount();
            acc.SharedSecret = "gMTEuBXg2CWd8YN5N1CvOWlp64A=";

            string code = acc.GenerateSteamGuardCode();

            Console.WriteLine(code);
            Console.ReadLine();
        }
    }
}
