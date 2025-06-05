using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;


namespace ConsoleApp3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<string> driverList = new List<string>();
            //Get CPU Name
            Console.WriteLine("=== CPU Information ===");
            using (var cpuSearcher = new ManagementObjectSearcher("select * from Win32_Processor"))
            {
                foreach (ManagementObject obj in cpuSearcher.Get())
                {
                    string name = obj["Name"]?.ToString();
                    string dedicatedCpu = name;
                    if (name.Contains("AMD"))
                    {
                        driverList.Add("AMD");
                    } else
                    {
                        driverList.Add("INTEL");
                    }
                    Console.WriteLine("Detected Driver: " + driverList[driverList.Count() - 1]);
                    Console.WriteLine("CPU Name: " + name);
                }
            }

            // Get GPU Name
            Console.WriteLine("\n=== GPU Information ===");
            using (var gpuSearcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                string dedicatedGpu = null;
                foreach (ManagementObject obj in gpuSearcher.Get())
                {
                    string name = obj["Name"]?.ToString();
                    if (name.Contains("NVIDIA"))
                    {
                        driverList.Add("NVIDIA");
                        dedicatedGpu = name;
                        break;
                    } else if (name.Contains("AMD") && !driverList.Contains("AMD"))
                    {
                        driverList.Add("AMD");
                        dedicatedGpu = name;
                    }
                }
                Console.WriteLine("Detected Driver: " + driverList[driverList.Count() - 1]);
                Console.WriteLine("GPU Name: " + dedicatedGpu);
            }

            // Gets Peripheral Devices
            Console.WriteLine("\n=== Input Devices with Vendor Info ===");

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity"))
            {
                foreach (ManagementObject device in searcher.Get())
                {
                    string name = device["Name"]?.ToString().ToLower();
                    string deviceId = device["DeviceID"]?.ToString();
                    
                    // Checks if device is a mouse or keyboard
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(deviceId) && (name.Contains("mouse") ||
                         name.Contains("keyboard")))
                    {
                        Console.WriteLine($"Device: {name}");

                        // Checks vendor map for a match
                        var match = Regex.Match(deviceId, @"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            string vid = match.Groups[1].Value.ToUpper();
                            string pid = match.Groups[2].Value.ToUpper();
                            Console.WriteLine($"  VID: {vid}, PID: {pid}");

                            string vendor = GetVendorName(vid);
                            // adds drivers to driver list
                            //WIP NEED TO MAKE A METHOD TO AUTOMATE THIS
                            if (vendor != null)
                            {   
                                if (vendor.Contains("LOGITECH") && !driverList.Contains("LOGITECH"))
                                {
                                    driverList.Add("LOGITECH");
                                } else if (vendor.Contains("RAZER") && !driverList.Contains("RAZER"))
                                {
                                    driverList.Add("RAZER");
                                }
                                    Console.WriteLine($"  Manufacturer: {vendor}");
                            }
                            else
                            {
                                Console.WriteLine("  Manufacturer: Unknown");
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Detected Driver: " + driverList[driverList.Count() - 1]);
            //sends list of drivers to the download handler
            string[] driverArray = driverList.ToArray();
            downloadHandler(driverArray);
        }

        // Vendor map used to decode VID of peripherals to get name of vendor
        static string GetVendorName(string vid)
        {
            var vendorMap = new Dictionary<string, string>
        {
            { "046D", "LOGITECH" },
            { "045E", "Microsoft" },
            { "1532", "RAZER" },
            { "1E4E", "SteelSeries" },
            { "0B05", "ASUS" },
            { "0C45", "Microdia" },
            { "056E", "Elecom" },
            { "04F2", "Chicony" },
            { "054C", "Sony" },
            { "28DE", "Valve" }
        };

            return vendorMap.TryGetValue(vid, out var name) ? name : null;

            //opens file
            //var p = new Process();
            //p.StartInfo.FileName = "notepad.exe";  // just for example, you can use yours.
            //p.Start();
        }

        //downloads files
        static void downloadHandler(string[] driverList)
        {
            //gets username
            string userNameRaw = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\')[1];
            Console.WriteLine("\nCurrent User: " + userNameRaw);

            // Url and Filename varibles for webclient downloads
            string activeUrl = null;
            string activeFileName = null;

            string nvidiaUrl = "https://in.download.nvidia.com/GFE/GFEClient/3.28.0.417/GeForce_Experience_v3.28.0.417.exe";
            string nvidiaFileName = "GeForce_Experience_v3.28.0.417.exe";
            string amdUrl = "https://drivers.amd.com/drivers/installer/25.10/whql/amd-software-adrenalin-edition-25.5.1-minimalsetup-250513_web.exe";
            string amdFileName = "amd-software-adrenalin-edition-25.5.1-minimalsetup-250513_web.exe";
            string intelUrl = "https://downloadmirror.intel.com/850983/gfx_win_101.2135.exe";
            string intelFileName = "gfx_win_101.2135.exe";
            string logitechUrl = "https://download01.logi.com/web/ftp/pub/techsupport/gaming/lghub_installer.exe";
            string logitechFileName = "lghub_installer.exe";

            // Webclient init
            WebClient webClient = new WebClient();
            webClient.Headers.Add("User-Agent: Other");

            // Loop that tells webclient which drivers to download
            foreach (string driver in driverList)
            {
                Console.WriteLine("\nDownloading " + driver + " drivers...");
                switch (driver)
                {
                    case "AMD":
                        activeUrl = amdUrl;
                        activeFileName = amdFileName;
                        webClient.Headers.Add("Referer", "https://www.amd.com/en/support/download/drivers.html");
                        break;
                    case "INTEL":
                        activeUrl = intelUrl;
                        activeFileName = intelFileName;
                        break;
                    case "NVIDIA":
                        activeUrl = nvidiaUrl;
                        activeFileName = nvidiaFileName;
                        break;
                    case "LOGITECH":
                        activeUrl = logitechUrl;
                        activeFileName = logitechFileName;
                        break;
                }
                // Download runs for each driver in list
                webClient.DownloadFile(activeUrl, @"C:\Users\" + userNameRaw + @"\Downloads\" + activeFileName);
                Console.WriteLine("Download Complete");
            }

        }
    }
}
