using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteamAuth
{
    public class PhoneBridge
    {
        private Process console;
        private ManualResetEvent mreOutput = new ManualResetEvent(false);

        public delegate EventHandler BridgeError(string msg);
        public event BridgeError PhoneBridgeError;
        private void OnPhoneBridgeError(string msg)
        {
            if (PhoneBridgeError != null)
                PhoneBridgeError(msg);
        }

        private void InitConsole()
        {
            if (console != null) return;

            console = new Process();

            console.StartInfo.UseShellExecute = false;
            console.StartInfo.RedirectStandardOutput = true;
            console.StartInfo.RedirectStandardInput = true;
            console.StartInfo.CreateNoWindow = false;
            console.StartInfo.FileName = "CMD.exe";
            console.StartInfo.Arguments = "/K";
            console.Start();
            console.BeginOutputReadLine();

            console.OutputDataReceived += (sender, e) =>
            {
                if (e.Data.Contains(">@")) return;
                Console.WriteLine(e.Data);
            };
        }

        public SteamGuardAccount ExtractSteamGuardAccount()
        {
            string errored = "";

            InitConsole(); // Init the console

            // Check required states
            if (!CheckAdb()) errored = "ADB not found";
            if (!DeviceUp()) errored = "Device not detected";
            if (!IsRooted()) errored = "Device not rooted";
            if (!SteamAppInstalled()) errored = "Steam Community App not installed";
            if (errored != null)
            {
                OnPhoneBridgeError(errored);
                return null;
            }

            // Pull the JSON from the device
            var sgj = JsonConvert.DeserializeObject<SteamGuardAccount>(PullJson());

            return sgj;
        }

        private string PullJson()
        {
            string json = null;
            ManualResetEventSlim mre = new ManualResetEventSlim();
            DataReceivedEventHandler f1 = (sender, e) =>
            {
                if (e.Data.Contains(">@") || e.Data == "") return;
                if (!e.Data.StartsWith("{")) return;
                if (e.Data.Contains("No such file or directory"))
                {
                    mre.Set();
                    return;
                }
                json = e.Data;
                mre.Set();
            };

            console.OutputDataReceived += f1;

            ExecuteCommand("adb shell su -c \"cat /data/data/com.valvesoftware.android.steam.community/files/Steamguard-*\"");
            mre.Wait();

            console.OutputDataReceived -= f1;

            return json;
        }
        private bool CheckAdb()
        {
            bool exists = true;
            Process p = new Process();

            p.StartInfo.FileName = "adb.exe";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;

            try
            {
                p.Start();
            }
            catch (Exception)
            {
                exists = false;
            }

            return exists;
        }
        private bool DeviceUp()
        {
            bool up = false;
            ManualResetEventSlim mre = new ManualResetEventSlim();
            DataReceivedEventHandler f1 = (sender, e) =>
            {
                if (e.Data.Contains(">@")) return;
                if (e.Data.Contains("device"))
                    up = true;
                mre.Set();
            };

            console.OutputDataReceived += f1;

            ExecuteCommand("adb get-state");
            mre.Wait();

            console.OutputDataReceived -= f1;

            return up;
        }
        private bool SteamAppInstalled()
        {
            bool ins = false;
            ManualResetEventSlim mre = new ManualResetEventSlim();
            DataReceivedEventHandler f1 = (sender, e) =>
            {
                if (e.Data.Contains(">@") || e.Data == "") return;
                if (e.Data == "Yes")
                    ins = true;
                mre.Set();
            };

            console.OutputDataReceived += f1;

            ExecuteCommand("adb shell \"cd /data/data/com.valvesoftware.android.steam.community && echo Yes\"");
            mre.Wait();

            console.OutputDataReceived -= f1;

            return ins;
        }
        private bool IsRooted()
        {
            bool root = false;
            ManualResetEventSlim mre = new ManualResetEventSlim();
            DataReceivedEventHandler f1 = (sender, e) =>
            {
                if (e.Data.Contains(">@") || e.Data == "") return;
                if (e.Data == "Yes")
                    root = true;
                mre.Set();
            };

            console.OutputDataReceived += f1;

            ExecuteCommand("adb shell su -c echo Yes");
            mre.Wait();

            console.OutputDataReceived -= f1;

            return root;
        }

        private void ExecuteCommand(string cmd)
        {
            console.StandardInput.WriteLine("@" + cmd);
            console.StandardInput.Flush();
        }
    }
}
