using System;
using System.IO;
using System.Linq;
using System.Windows;
using WindowsAntifyProxy.Models;
using WindowsAntifyProxy.Services;

namespace WindowsAntifyProxy
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0)
            {
                // Handle Self-Test: WindowsAntifyProxy.exe --test
                if (e.Args.Any(a => a.Equals("--test", StringComparison.OrdinalIgnoreCase)))
                {
                    RunSelfTest();
                    Shutdown();
                    return;
                }

                // Handle Headless CLI launch: WindowsAntifyProxy.exe --launch <ruleId>
                for (int i = 0; i < e.Args.Length; i++)
                {
                    if (e.Args[i].Equals("--launch", StringComparison.OrdinalIgnoreCase) && i + 1 < e.Args.Length)
                    {
                        var ruleIdOrName = e.Args[i + 1].Trim('"');
                        LaunchHeadless(ruleIdOrName);
                        Shutdown();
                        return;
                    }
                }

                // Handle Context Menu launch: WindowsAntifyProxy.exe --context-menu <exePath>
                for (int i = 0; i < e.Args.Length; i++)
                {
                    if (e.Args[i].Equals("--context-menu", StringComparison.OrdinalIgnoreCase) && i + 1 < e.Args.Length)
                    {
                        var exePath = e.Args[i + 1].Trim('"');
                        LaunchWithContextMenu(exePath);
                        return;
                    }
                }
            }

            // Normal launch: Open MainWindow
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void RunSelfTest()
        {
            try
            {
                var settings = ConfigService.LoadSettings();
                var resultLog = new System.Text.StringBuilder();
                resultLog.AppendLine("=== Windows Antify Proxy Self-Test ===");
                resultLog.AppendLine($"Loaded {settings.Proxies.Count} proxies and {settings.Rules.Count} rules.");

                foreach (var p in settings.Proxies)
                {
                    resultLog.AppendLine($"Proxy: {p.Name} ({p.Type}) => {p.ServerDisplay} (Url: {p.Url})");
                }

                var powershellPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
                var testOutputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "selftest_output.txt");

                if (File.Exists(powershellPath))
                {
                    var testRule = new RuleItem
                    {
                        Name = "SelfTest_PowerShell",
                        ExecutablePath = powershellPath,
                        Arguments = $"-NoProfile -Command \"Set-Content -Path '{testOutputFile}' -Value ('HTTP_PROXY=' + $env:HTTP_PROXY + [Environment]::NewLine + 'HTTPS_PROXY=' + $env:HTTPS_PROXY + [Environment]::NewLine + 'NO_PROXY=' + $env:NO_PROXY + [Environment]::NewLine + 'ALL_PROXY=' + $env:ALL_PROXY)\"",
                        Action = "Proxy",
                        ProxyName = settings.Proxies.FirstOrDefault()?.Name ?? "Local Proxy (7897)"
                    };

                    var proc = ProcessLauncher.LaunchRule(testRule, settings, bypassKillSwitch: true);
                    proc?.WaitForExit(5000);

                    if (File.Exists(testOutputFile))
                    {
                        var content = File.ReadAllText(testOutputFile);
                        resultLog.AppendLine("\nChild Process Scoped Environment Output:");
                        resultLog.AppendLine(content);
                        File.Delete(testOutputFile);
                    }
                }

                resultLog.AppendLine("Windows Antify Proxy Self-Test completed successfully.");
                var reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "selftest_report.txt");
                File.WriteAllText(reportPath, resultLog.ToString());
                Console.WriteLine(resultLog.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Self-Test failed: {ex.Message}");
            }
        }

        private void LaunchHeadless(string ruleIdOrName)
        {
            try
            {
                var settings = ConfigService.LoadSettings();
                var rule = settings.Rules.FirstOrDefault(r =>
                    r.Id.Equals(ruleIdOrName, StringComparison.OrdinalIgnoreCase) ||
                    r.Name.Equals(ruleIdOrName, StringComparison.OrdinalIgnoreCase))
                    ?? settings.Rules.FirstOrDefault(r => r.Name.Contains(ruleIdOrName, StringComparison.OrdinalIgnoreCase))
                    ?? settings.Rules.FirstOrDefault();

                if (rule != null)
                {
                    ProcessLauncher.LaunchRule(rule, settings, bypassKillSwitch: true);
                }
                else
                {
                    MessageBox.Show($"Rule '{ruleIdOrName}' was not found in Windows Antify Proxy configuration.",
                        "Windows Antify Proxy", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch app: {ex.Message}", "Windows Antify Proxy", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchWithContextMenu(string exePath)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();

            if (File.Exists(exePath))
            {
                var settings = mainWindow.Settings;
                var existingRule = settings.Rules.FirstOrDefault(r =>
                    r.ExecutablePath.Equals(exePath, StringComparison.OrdinalIgnoreCase));

                if (existingRule == null)
                {
                    var fileName = Path.GetFileNameWithoutExtension(exePath);
                    var newRule = new RuleItem
                    {
                        Name = fileName,
                        Subtitle = exePath,
                        ExecutablePath = exePath,
                        CachedIcon = IconHelper.GetIconForFile(exePath),
                        Action = "Proxy",
                        Type = "App",
                        KillSwitch = true,
                        ProxyName = settings.Proxies.FirstOrDefault()?.Name ?? "Local Proxy (7897)"
                    };

                    newRule.CustomVariables.Add(new EnvironmentVariableItem
                    {
                        Key = "HTTP_PROXY",
                        Value = "http://127.0.0.1:7897",
                        IsEnabled = true
                    });
                    newRule.CustomVariables.Add(new EnvironmentVariableItem
                    {
                        Key = "HTTPS_PROXY",
                        Value = "http://127.0.0.1:7897",
                        IsEnabled = true
                    });

                    settings.Rules.Add(newRule);
                    ConfigService.SaveSettings(settings);
                }
            }
        }
    }
}
