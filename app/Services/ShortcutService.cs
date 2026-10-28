using System;
using System.Diagnostics;
using System.IO;
using WindowsAntifyProxy.Models;

namespace WindowsAntifyProxy.Services
{
    public enum ShortcutDestination
    {
        Desktop,
        StartMenu,
        Taskbar,
        Everywhere
    }

    public static class ShortcutService
    {
        public static string GetValidExecutablePath()
        {
            var currentExePath = Environment.ProcessPath ?? string.Empty;
            if (string.IsNullOrEmpty(currentExePath) || Path.GetFileName(currentExePath).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var directCandidate = Path.Combine(baseDir, "WindowsAntifyProxy.exe");
                if (File.Exists(directCandidate)) return directCandidate;

                var releaseCandidate = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\release\WindowsAntifyProxy.exe"));
                if (File.Exists(releaseCandidate)) return releaseCandidate;

                var parentCandidate = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\WindowsAntifyProxy.exe"));
                if (File.Exists(parentCandidate)) return parentCandidate;

                return directCandidate;
            }
            return currentExePath;
        }

        public static string CreateShortcut(RuleItem rule, ShortcutDestination destination)
        {
            if (destination == ShortcutDestination.Everywhere)
            {
                CreateShortcut(rule, ShortcutDestination.Desktop);
                CreateShortcut(rule, ShortcutDestination.StartMenu);
                return CreateShortcut(rule, ShortcutDestination.Taskbar);
            }

            string targetFolder;
            switch (destination)
            {
                case ShortcutDestination.StartMenu:
                    targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    break;
                case ShortcutDestination.Taskbar:
                    // Create in dedicated Start Menu location so it's clean, permanent and indexed
                    targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Windows Antify Proxy");
                    break;
                case ShortcutDestination.Desktop:
                default:
                    targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    break;
            }

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var safeName = string.Join("_", rule.Name.Split(Path.GetInvalidFileNameChars()));
            var shortcutFileName = $"{safeName} (Proxy).lnk";
            var shortcutPath = Path.Combine(targetFolder, shortcutFileName);

            var currentExePath = GetValidExecutablePath();

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("WScript.Shell COM object is not available on this system.");
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);

            shortcut.TargetPath = currentExePath;
            shortcut.Arguments = $"--launch \"{rule.Name}\"";
            shortcut.Description = $"Directly launch {rule.Name} with isolated proxy variables ({rule.ProxyName})";
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

            // If StartMenu, also attempt programmatic Pin to Start
            if (destination == ShortcutDestination.StartMenu)
            {
                TryPinToStart(shortcutPath);
            }

            // If Taskbar, reveal in Explorer so user can 1-click Pin to Taskbar
            if (destination == ShortcutDestination.Taskbar)
            {
                RevealInExplorer(shortcutPath);
            }

            return shortcutPath;
        }

        public static bool TryPinToStart(string shortcutPath)
        {
            try
            {
                Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType != null)
                {
                    dynamic shellApp = Activator.CreateInstance(shellAppType)!;
                    dynamic folder = shellApp.Namespace(Path.GetDirectoryName(shortcutPath));
                    dynamic item = folder.ParseName(Path.GetFileName(shortcutPath));
                    foreach (var verb in item.Verbs())
                    {
                        string verbName = verb.Name.ToString();
                        if (verbName.Contains("Pin to Start", StringComparison.OrdinalIgnoreCase) ||
                            verbName.Contains("startpin", StringComparison.OrdinalIgnoreCase))
                        {
                            verb.DoIt();
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public static void RevealInExplorer(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }

        public static string CreateDesktopShortcut(RuleItem rule) => CreateShortcut(rule, ShortcutDestination.Desktop);
        public static string CreateStartMenuShortcut(RuleItem rule) => CreateShortcut(rule, ShortcutDestination.StartMenu);
        public static string CreateTaskbarShortcut(RuleItem rule) => CreateShortcut(rule, ShortcutDestination.Taskbar);
        public static string CreateEverywhereShortcuts(RuleItem rule) => CreateShortcut(rule, ShortcutDestination.Everywhere);
    }
}
