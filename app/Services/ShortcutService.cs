using System;
using System.IO;
using WindowsAntifyProxy.Models;

namespace WindowsAntifyProxy.Services
{
    public static class ShortcutService
    {
        public static string CreateDesktopShortcut(RuleItem rule)
        {
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var safeName = string.Join("_", rule.Name.Split(Path.GetInvalidFileNameChars()));
            var shortcutPath = Path.Combine(desktopDir, $"{safeName} (Windows Antify Proxy).lnk");

            var currentExePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExePath))
            {
                currentExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WindowsAntifyProxy.exe");
            }

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("WScript.Shell COM object is not available on this system.");
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);

            shortcut.TargetPath = currentExePath;
            shortcut.Arguments = $"--launch \"{rule.Id}\"";
            shortcut.Description = $"Launch {rule.Name} via Windows Antify Proxy ({rule.Action})";
            shortcut.WorkingDirectory = Path.GetDirectoryName(currentExePath);

            // Use target app's real icon if available
            if (!string.IsNullOrEmpty(rule.ExecutablePath) && File.Exists(rule.ExecutablePath))
            {
                shortcut.IconLocation = $"{rule.ExecutablePath},0";
            }
            else
            {
                shortcut.IconLocation = $"{currentExePath},0";
            }

            shortcut.Save();
            return shortcutPath;
        }
    }
}
