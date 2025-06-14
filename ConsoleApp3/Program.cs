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
                                    driverList.Add("ASUS");
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
            { "046D", "LOGITECH" }, // Y
            { "045E", "MICROSOFT" },
            { "1532", "RAZER" }, // Y
            { "1E4E", "STEELSERIES" }, // Y
            { "0B05", "ASUS" }, // Y
            { "0C45", "MICRODIA" },
            { "056E", "ELECOM" },
            { "04F2", "CHICONY" },
            { "054C", "SONY" },
            { "28DE", "VALVE" },
            { "1B1C", "CORSAIR"} // Y
        };

            return vendorMap.TryGetValue(vid, out var name) ? name : null;

            //opens file
            //var p = new Process();
            //p.StartInfo.FileName = "notepad.exe";  // just for example, you can use yours.
            //p.Start();
        }

        // Driver dictionary stores urls for each driver
        static string GetDriverUrl(string driver) {
            var driverMap = new Dictionary<string, string>
            {
                {"NVIDIA", @"https://in.download.nvidia.com/GFE/GFEClient/3.28.0.417/GeForce_Experience_v3.28.0.417.exe" },
                {"AMD", @"https://drivers.amd.com/drivers/installer/25.10/whql/amd-software-adrenalin-edition-25.5.1-minimalsetup-250513_web.exe" },
                {"INTEL", @"https://dsadata.intel.com/installer" },
                {"LOGITECH", @"https://download01.logi.com/web/ftp/pub/techsupport/gaming/lghub_installer.exe" },
                {"RAZER", @"https://rzr.to/synapse-4-pc-download" },
                {"CORSAIR", @"https://www3.corsair.com/software/CUE_V5/public/modules/windows/installer/Install%20iCUE.exe?_gl=1*1cff191*_gcl_au*MjA5NTE4Njk1OC4xNzQ5ODc0OTM3" },
                {"STEELSERIES", @"https://steelseries.com/gg/downloads/gg/latest/windows" },
                {"ASUS", @"https://dlcdnets.asus.com/pub/ASUS/mb/14Utilities/ArmouryCrateInstallTool.zip?model=Armoury%20Crate" }
            };

                return driverMap.TryGetValue(driver, out var url) ? url : null;
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

            // Webclient init
            WebClient webClient = new WebClient();
            webClient.Headers.Add("User-Agent: Other");
            webClient.Headers.Add("Referer", "https://www.amd.com/en/support/download/drivers.html");


            // Loop that tells webclient which drivers to download
            foreach (string driver in driverList)
            {
                Console.WriteLine("\nDownload " + driver + " drivers? (Y/N): ");
                Char answer = Convert.ToChar(Console.ReadLine().ToUpper());

                if (answer == 'Y')
                {
                    Console.WriteLine("\nDownloading " + driver + " drivers...");

                    // gets driver url from dictionary
                    activeUrl = GetDriverUrl(driver);

                    // gets filename from url
                    // razer doesnt have filename in url so this checks if razer and adds filename manually
                    switch (driver)
                    {
                        case "RAZER" :
                            activeFileName = "RazerSynapseInstaller.exe";
                            break;
                        case "INTEL" :
                            activeFileName = "Intel-Driver-and-Support-Assistant-Installer.exe";
                            break;
                        case "CORSAIR":
                            activeFileName = "Install-iCUE.exe";
                            break;
                        case "STEELSERIES":
                            activeFileName = "SteelSeriesGG88.0.0Setup.exe";
                            break;
                        case "ASUS":
                            activeFileName = "ArmouryCrateInstallTool.zip";
                            break;
                        default :
                            activeFileName = activeUrl.Split('/')[activeUrl.Split('/').Length - 1];
                            break;
                    }
                    // Download runs for each driver in list
                    webClient.DownloadFile(activeUrl, @"C:\Users\" + userNameRaw + @"\Downloads\" + activeFileName);
                    Console.WriteLine("Download Complete");
                }

            }

        }
    }
}
