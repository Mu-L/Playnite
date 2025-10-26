﻿using Microsoft.Win32;
using Playnite.SDK;
using Playnite.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Playnite.Common
{
    public class SystemInfo
    {
        public bool Is64Bit { get; set; }

        public string WindowsVersion { get; set; }

        public string ActualWindowsVersion { get; set; }

        public string WindowsEdition { get; set; }

        public int WindowsBuildVersion { get; set; }

        public string Cpu { get; set; }

        public int Ram { get; set; }

        public List<string> Gpus { get; set; }

        public List<ComputerScreen> Screens { get; set; }
    }

    public class ComputerScreen
    {
        public System.Drawing.Rectangle WorkingArea { get; private set; }
        public bool Primary { get; private set; }
        public string DeviceName { get; private set; }
        public System.Drawing.Rectangle Bounds { get; private set; }
        public int BitsPerPixel { get; private set; }

        public ComputerScreen()
        {
        }

        public ComputerScreen(Screen screen)
        {
            WorkingArea = screen.WorkingArea;
            Primary = screen.Primary;
            DeviceName = screen.DeviceFriendlyName();
            Bounds = screen.Bounds;
            BitsPerPixel = screen.BitsPerPixel;
        }
    }

    public enum WindowsVersion
    {
        Unknown,
        Win7,
        Win8,
        Win10,
        Win11
    }

    public enum HwCompany
    {
        Intel,
        AMD,
        Nvidia,
        VMware,
        Uknown
    }

    public static class Computer
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static readonly (string path, string args) ShutdownCmd = ("shutdown.exe", "-s -hybrid -t 0");
        public static readonly (string path, string args) RestartCmd = ("shutdown.exe", "-r -t 0");

        public static WindowsVersion WindowsVersion
        {
            get
            {
                var version = Environment.OSVersion.Version;
                if (version.Major == 6 && version.Minor == 1)
                {
                    return WindowsVersion.Win7;
                }
                else if (version.Major == 6 && (version.Minor == 2 || version.Minor == 3))
                {
                    return WindowsVersion.Win8;
                }
                else if (version.Major == 10)
                {
                    // Apparently some people are spoofing Windows 10 build versions but whatherer they are using
                    // is not updating instaled product name, so we need to check that as well.
                    var windowsProd = Computer.GetWindowsProductName();
                    if (windowsProd?.Contains("Windows 7") == true)
                        return WindowsVersion.Win7;

                    if (windowsProd?.Contains("Windows 8") == true)
                        return WindowsVersion.Win8;

                    return version.Build >= 22000 ? WindowsVersion.Win11 : WindowsVersion.Win10;
                }
                else
                {
                    return WindowsVersion.Unknown;
                }
            }
        }

        public static bool IsTLS13SystemWideEnabled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Client"))
                {
                    if (key != null)
                    {
                        var isEnabled = key.GetValue("Enabled");
                        if (isEnabled != null)
                        {
                            return Convert.ToBoolean(isEnabled);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to test TLS 1.3 state.");
            }

            return false;
        }

        public static int GetWindowsReleaseId()
        {
            var relVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "");
            if (relVal?.ToString().IsNullOrEmpty() == true)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(relVal);
            }
        }

        public static string GetWindowsProductName()
        {
            return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "")?.ToString();
        }

        public static Guid GetMachineGuid()
        {
            RegistryKey root = null;
            if (Environment.Is64BitOperatingSystem)
            {
                root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            else
            {
                root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            }

            try
            {
                using (var cryptography = root.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography"))
                {
                    return Guid.Parse((string)cryptography.GetValue("MachineGuid"));
                }
            }
            finally
            {
                root.Dispose();
            }
        }

        public static SystemInfo GetSystemInfo()
        {
            var info = new SystemInfo
            {
                Is64Bit = Environment.Is64BitOperatingSystem,
                WindowsVersion = Environment.OSVersion.VersionString,
                ActualWindowsVersion = Computer.WindowsVersion.ToString(),
                WindowsBuildVersion = GetWindowsReleaseId(),
                WindowsEdition = GetWindowsProductName()
            };

            using (var win32Proc = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (var obj in win32Proc.Get())
                {
                    info.Cpu = obj["Name"].ToString().Trim();
                    break;
                }
            }

            using (var memory = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                double totalCapacity = 0;
                foreach (var obj in memory.Get())
                {
                    totalCapacity += Convert.ToDouble(obj["TotalPhysicalMemory"]);
                }

                info.Ram = Convert.ToInt32(totalCapacity / 1048576);
            }

            using (var video = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                info.Gpus = new List<string>();
                foreach (var obj in video.Get())
                {
                    info.Gpus.Add(obj["Name"].ToString());
                }
            }

            info.Screens = GetScreens();
            return info;
        }

        public static List<ComputerScreen> GetScreens()
        {
            return Screen.AllScreens.Select(a => a.ToComputerScreen()).ToList();
        }

        public static ComputerScreen GetPrimaryScreen()
        {
            return Screen.PrimaryScreen?.ToComputerScreen();
        }

        public static int GetGetPrimaryScreenIndex()
        {
            var allScreens = Screen.AllScreens;
            for (int i = 0; i < allScreens.Length; i++)
            {
                if (allScreens[i].Primary)
                {
                    return i;
                }
            }

            return 0;
        }

        public static void SetMouseCursorVisibility(bool show)
        {
            if (show)
            {
                while (User32.ShowCursor(true) < 0)
                {
                }
            }
            else
            {
                while (User32.ShowCursor(false) >= 0)
                {
                }
            }
        }

        public static void Shutdown()
        {
            // Used instead of shutdown.exe because it doesn't replicate the same shutdown behavior
            // as user initiated "from start menu" shutdown: https://github.com/JosefNemec/Playnite/issues/3947
            // EnablePrivilege needed https://stackoverflow.com/a/24726453/1107424
            if (!User32.EnablePrivilege("SeShutdownPrivilege", true) ||
                !User32.ExitWindowsEx(
                    User32.ExitWindowsFlags.EWX_SHUTDOWN |
                    User32.ExitWindowsFlags.EWX_HYBRID_SHUTDOWN |
                    User32.ExitWindowsFlags.EWX_ARSO,
                    0))
            {
                logger.Error("ExitWindowsEx shutdown failed, using fallback via shutdown.exe");
                logger.Error(Marshal.GetLastWin32Error().ToString());
                ProcessStarter.StartProcess(ShutdownCmd.path, ShutdownCmd.args);
            }
        }

        public static void Restart()
        {
            ProcessStarter.StartProcess(RestartCmd.path, RestartCmd.args);
        }

        public static bool Sleep()
        {
            return Powrprof.SetSuspendState(false, true, false);
        }

        public static bool Hibernate()
        {
            return Powrprof.SetSuspendState(true, true, false);
        }

        public static bool Lock()
        {
            return User32.LockWorkStation();
        }

        public static bool Logout()
        {
            return User32.ExitWindowsEx(User32.ExitWindowsFlags.EWX_LOGOFF, 0);
        }

        public static ComputerScreen ToComputerScreen(this Screen screen)
        {
            if (screen == null)
            {
                return null;
            }
            else
            {
                return new ComputerScreen(screen);
            }
        }

        public static List<HwCompany> GetGpuVendors()
        {
            var gpus = new List<string>();
            var vendors = new List<HwCompany>();
            try
            {
                using (var video = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (var obj in video.Get())
                    {
                        gpus.Add(obj["Name"].ToString());
                    }
                }

                foreach (var gpu in gpus)
                {
                    if (gpu.Contains("intel", StringComparison.OrdinalIgnoreCase))
                    {
                        vendors.AddMissing(HwCompany.Intel);
                    }
                    else if (gpu.Contains("nvidia", StringComparison.OrdinalIgnoreCase))
                    {
                        vendors.AddMissing(HwCompany.Nvidia);
                    }
                    else if (gpu.Contains("amd", StringComparison.OrdinalIgnoreCase))
                    {
                        vendors.AddMissing(HwCompany.AMD);
                    }
                    else if (gpu.Contains("vmware", StringComparison.OrdinalIgnoreCase))
                    {
                        vendors.AddMissing(HwCompany.VMware);
                    }
                    else
                    {
                        return new List<HwCompany> { HwCompany.Uknown };
                    }
                }

                if (vendors.Count > 0)
                {
                    return vendors;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get GPU vendor.");
            }

            return new List<HwCompany> { HwCompany.Uknown };
        }

        private static string GetMonitorFriendlyName(LUID adapterId, uint targetId)
        {
            var deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME
            {
                header =
                {
                    size = (uint)Marshal.SizeOf(typeof (DISPLAYCONFIG_TARGET_DEVICE_NAME)),
                    adapterId = adapterId,
                    id = targetId,
                    type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME
                }
            };
            var error = User32.DisplayConfigGetDeviceInfo(ref deviceName);
            if (error != WinError.ERROR_SUCCESS)
            {
                throw new Win32Exception(error);
            }

            return deviceName.monitorFriendlyDeviceName;
        }

        private static IEnumerable<string> GetAllMonitorsFriendlyNames()
        {
            var error = User32.GetDisplayConfigBufferSizes(
                QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS,
                out uint pathCount,
                out uint modeCount);
            if (error != WinError.ERROR_SUCCESS)
            {
                throw new Win32Exception(error);
            }

            var displayPaths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var displayModes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            error = User32.QueryDisplayConfig(
                QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount,
                displayPaths,
                ref modeCount,
                displayModes,
                IntPtr.Zero);
            if (error != WinError.ERROR_SUCCESS)
            {
                throw new Win32Exception(error);
            }

            for (var i = 0; i < modeCount; i++)
            {
                if (displayModes[i].infoType == DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
                {
                    yield return GetMonitorFriendlyName(displayModes[i].adapterId, displayModes[i].id);
                }
            }
        }

        public static string DeviceFriendlyName(this Screen screen)
        {
            try
            {
                var allFriendlyNames = GetAllMonitorsFriendlyNames();
                for (var index = 0; index < Screen.AllScreens.Length; index++)
                {
                    if (Equals(screen, Screen.AllScreens[index]))
                    {
                        return allFriendlyNames.ToArray()[index];
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get display name.");
            }

            return screen.DeviceName;
        }

        public static bool GetScreenReaderActive()
        {
            // In theory this method should be using SystemParametersInfo API with SPI_GETSCREENREADER
            // but according to my testing that returns screen reader presence even if no screen reader is running.
            // No idea why, so for now we will just check for Narrator, NVDA and JAWS readers.

            if (Process.GetProcessesByName("narrator").HasItems())
            {
                return true;
            }

            if (Process.GetProcessesByName("nvda").HasItems())
            {
                return true;
            }

            if (Process.GetProcessesByName("jfw").HasItems())
            {
                return true;
            }

            return false;
        }
    }
}
