using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace EarTrumpet.Interop.Helpers
{
    internal static class StartupRegistryHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "EarTrumpet";

        public static bool IsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    var value = key?.GetValue(ValueName) as string;
                    return !string.IsNullOrWhiteSpace(value);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"StartupRegistryHelper IsEnabled Failed: {ex}");
                return false;
            }
        }

        public static void SetEnabled(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true) ??
                    Registry.CurrentUser.CreateSubKey(RunKeyPath, true))
                {
                    if (enable)
                    {
                        var exePath = Process.GetCurrentProcess().MainModule?.FileName ??
                            System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (!string.IsNullOrWhiteSpace(exePath))
                        {
                            key.SetValue(ValueName, QuotePath(exePath));
                        }
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"StartupRegistryHelper SetEnabled Failed: {ex}");
            }
        }

        private static string QuotePath(string path)
        {
            return path.Contains(" ") ? $"\"{path}\"" : path;
        }
    }
}
