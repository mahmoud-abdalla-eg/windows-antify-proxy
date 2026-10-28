using System;
using System.IO;
using System.Text.Json;
using WindowsAntifyProxy.Models;

namespace WindowsAntifyProxy.Services
{
    public static class ConfigService
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowsAntifyProxy");

        private static readonly string ConfigFilePath = Path.Combine(ConfigDir, "config.json");

        private static readonly string LegacyConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Antify\antify_config.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (settings != null)
                    {
                        HydrateRuleIcons(settings);
                        return settings;
                    }
                }
                else if (File.Exists(LegacyConfigFilePath))
                {
                    var json = File.ReadAllText(LegacyConfigFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (settings != null)
                    {
                        HydrateRuleIcons(settings);
                        SaveSettings(settings);
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            return CreateDefaultSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                {
                    Directory.CreateDirectory(ConfigDir);
                }

                // Update icon base64 caches before saving
                foreach (var rule in settings.Rules)
                {
                    if (rule.CachedIcon is System.Windows.Media.Imaging.BitmapSource bmp && string.IsNullOrEmpty(rule.IconBase64))
                    {
                        rule.IconBase64 = IconHelper.BitmapSourceToBase64(bmp);
                    }
                }

                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private static void HydrateRuleIcons(AppSettings settings)
        {
            foreach (var rule in settings.Rules)
            {
                if (!string.IsNullOrEmpty(rule.ExecutablePath) && File.Exists(rule.ExecutablePath))
                {
                    rule.CachedIcon = IconHelper.GetIconForFile(rule.ExecutablePath);
                }
                else if (!string.IsNullOrEmpty(rule.IconBase64))
                {
                    rule.CachedIcon = IconHelper.Base64ToBitmapSource(rule.IconBase64);
                }
            }
        }

        public static AppSettings CreateDefaultSettings()
        {
            var settings = new AppSettings
            {
                IsProxyActive = true,
                ExplorerContextMenu = ShellContextMenuService.IsContextMenuRegistered()
            };

            // Main configured proxy matching user environment: http://127.0.0.1:7897
            var defaultProxy = new ProxyItem
            {
                Name = "Local Proxy (7897)",
                Type = "HTTP",
                Host = "127.0.0.1",
                Port = 7897,
                LatencyMs = null,
                Health = "Configured"
            };

            settings.Proxies.Add(defaultProxy);

            // Default Configuration Profile
            var defaultProfile = new ConfigurationProfile
            {
                Name = "Default",
                IsActive = true,
                DefaultAction = "Direct",
                WifiAutoSwitch = false
            };

            settings.Configurations.Add(defaultProfile);
            settings.ActiveConfigurationId = defaultProfile.Id;

            // Detect Antigravity executable path on user machine
            string antigravityPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Programs\antigravity\Antigravity.exe");

            if (!File.Exists(antigravityPath))
            {
                antigravityPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    @"Antigravity\Antigravity.exe");
            }

            var antigravityRule = new RuleItem
            {
                IsEnabled = true,
                Name = "Antigravity",
                Subtitle = File.Exists(antigravityPath) ? antigravityPath : "Antigravity AI Assistant",
                ExecutablePath = File.Exists(antigravityPath) ? antigravityPath : antigravityPath,
                CachedIcon = File.Exists(antigravityPath) ? IconHelper.GetIconForFile(antigravityPath) : null,
                Type = "App",
                Action = "Proxy",
                ProxyName = defaultProxy.Name,
                ProxyId = defaultProxy.Id,
                KillSwitch = false,
                LastMatch = "—"
            };

            // Add the exact user environment variables requested:
            antigravityRule.CustomVariables.Add(new EnvironmentVariableItem
            {
                Key = "HTTP_PROXY",
                Value = "http://127.0.0.1:7897",
                IsEnabled = true
            });

            antigravityRule.CustomVariables.Add(new EnvironmentVariableItem
            {
                Key = "HTTPS_PROXY",
                Value = "http://127.0.0.1:7897",
                IsEnabled = true
            });

            antigravityRule.CustomVariables.Add(new EnvironmentVariableItem
            {
                Key = "NO_PROXY",
                Value = "localhost,127.0.0.1,::1,127.0.0.0/8,*.local,*.internal",
                IsEnabled = true
            });

            // ONLY Antigravity is added
            settings.Rules.Add(antigravityRule);

            SaveSettings(settings);
            return settings;
        }
    }
}
