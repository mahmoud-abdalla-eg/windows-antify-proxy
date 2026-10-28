using System;
using System.IO;
using Microsoft.Win32;

namespace WindowsAntifyProxy.Services
{
    public static class ShellContextMenuService
    {
        private const string KeyPath = @"Software\Classes\exefile\shell\WindowsAntifyProxy";

        public static bool IsContextMenuRegistered()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        public static void RegisterContextMenu()
        {
            try
            {
                var exePath = Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WindowsAntifyProxy.exe");

                using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
                if (key != null)
                {
                    key.SetValue("", "Run with Windows Antify Proxy");
                    key.SetValue("Icon", $"\"{exePath}\",0");

                    using var cmdKey = key.CreateSubKey("command");
                    if (cmdKey != null)
                    {
                        cmdKey.SetValue("", $"\"{exePath}\" --context-menu \"%1\"");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to register context menu: {ex.Message}");
            }
        }

        public static void UnregisterContextMenu()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(KeyPath, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to unregister context menu: {ex.Message}");
            }
        }
    }
}
