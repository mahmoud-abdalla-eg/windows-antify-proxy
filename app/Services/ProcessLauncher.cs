using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using WindowsAntifyProxy.Models;

namespace WindowsAntifyProxy.Services
{
    public class KillSwitchException : Exception
    {
        public string ProxyName { get; }
        public string ServerDisplay { get; }

        public KillSwitchException(string proxyName, string serverDisplay)
            : base($"Kill Switch Active: {proxyName} ({serverDisplay}) is currently offline. Launch was blocked to protect your real IP address from leaking.")
        {
            ProxyName = proxyName;
            ServerDisplay = serverDisplay;
        }
    }

    public static class ProcessLauncher
    {
        public static bool IsProxyListening(string host, int port, int timeoutMs = 800)
        {
            try
            {
                using var client = new TcpClient();
                var task = client.ConnectAsync(host, port);
                if (task.Wait(timeoutMs) && client.Connected)
                {
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static Process? LaunchRule(RuleItem rule, AppSettings settings, string? extraArgs = null, bool bypassKillSwitch = false)
        {
            if (string.IsNullOrWhiteSpace(rule.ExecutablePath))
            {
                throw new InvalidOperationException("Executable path cannot be empty.");
            }

            if (!File.Exists(rule.ExecutablePath))
            {
                throw new FileNotFoundException($"Executable not found at path: {rule.ExecutablePath}", rule.ExecutablePath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = rule.ExecutablePath,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            // Working directory
            if (!string.IsNullOrWhiteSpace(rule.WorkingDirectory) && Directory.Exists(rule.WorkingDirectory))
            {
                startInfo.WorkingDirectory = rule.WorkingDirectory;
            }
            else
            {
                var dir = Path.GetDirectoryName(rule.ExecutablePath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    startInfo.WorkingDirectory = dir;
                }
            }

            // Arguments
            string args = rule.Arguments ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(extraArgs))
            {
                args = string.IsNullOrWhiteSpace(args) ? extraArgs : $"{args} {extraArgs}";
            }
            startInfo.Arguments = args;

            // Apply Network/Proxy Environment Variables
            if (rule.IsEnabled)
            {
                if (rule.Action.Equals("Proxy", StringComparison.OrdinalIgnoreCase))
                {
                    var proxy = settings.Proxies.FirstOrDefault(p =>
                        (!string.IsNullOrEmpty(rule.ProxyId) && p.Id == rule.ProxyId) ||
                        p.Name.Equals(rule.ProxyName, StringComparison.OrdinalIgnoreCase))
                        ?? settings.Proxies.FirstOrDefault();

                    if (proxy != null)
                    {
                        // Check Kill Switch: Is proxy actually online?
                        if (rule.KillSwitch && !bypassKillSwitch)
                        {
                            bool isOnline = IsProxyListening(proxy.Host, proxy.Port);
                            if (!isOnline)
                            {
                                throw new KillSwitchException(proxy.Name, proxy.ServerDisplay);
                            }
                        }

                        var proxyUrl = proxy.Url;
                        startInfo.EnvironmentVariables["HTTP_PROXY"] = proxyUrl;
                        startInfo.EnvironmentVariables["http_proxy"] = proxyUrl;
                        startInfo.EnvironmentVariables["HTTPS_PROXY"] = proxyUrl;
                        startInfo.EnvironmentVariables["https_proxy"] = proxyUrl;

                        // Only set SOCKS5 / ALL_PROXY if the proxy is genuinely a SOCKS5 proxy.
                        // Setting ALL_PROXY to an http:// URL breaks Go, gRPC, and Node.js child processes (e.g. Antigravity language server).
                        if (proxy.Type.Equals("SOCKS5", StringComparison.OrdinalIgnoreCase))
                        {
                            var socksUrl = $"socks5://{proxy.Host}:{proxy.Port}";
                            startInfo.EnvironmentVariables["ALL_PROXY"] = socksUrl;
                            startInfo.EnvironmentVariables["all_proxy"] = socksUrl;
                            startInfo.EnvironmentVariables["SOCKS_PROXY"] = socksUrl;
                            startInfo.EnvironmentVariables["socks_proxy"] = socksUrl;
                            startInfo.EnvironmentVariables["SOCKS5_SERVER"] = $"{proxy.Host}:{proxy.Port}";
                        }
                        else
                        {
                            startInfo.EnvironmentVariables.Remove("ALL_PROXY");
                            startInfo.EnvironmentVariables.Remove("all_proxy");
                        }

                        // Essential loopback bypass: Antigravity runs local language servers, Electron IPC, and webviews on 127.0.0.1
                        const string noProxyList = "localhost,127.0.0.1,::1,127.0.0.0/8,*.local,*.internal";
                        startInfo.EnvironmentVariables["NO_PROXY"] = noProxyList;
                        startInfo.EnvironmentVariables["no_proxy"] = noProxyList;
                    }
                }
                else if (rule.Action.Equals("Direct", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.EnvironmentVariables["HTTP_PROXY"] = string.Empty;
                    startInfo.EnvironmentVariables["http_proxy"] = string.Empty;
                    startInfo.EnvironmentVariables["HTTPS_PROXY"] = string.Empty;
                    startInfo.EnvironmentVariables["https_proxy"] = string.Empty;
                    startInfo.EnvironmentVariables["ALL_PROXY"] = string.Empty;
                    startInfo.EnvironmentVariables["all_proxy"] = string.Empty;
                    startInfo.EnvironmentVariables["NO_PROXY"] = "*";
                    startInfo.EnvironmentVariables["no_proxy"] = "*";
                }
                else if (rule.Action.Equals("Block", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.EnvironmentVariables["HTTP_PROXY"] = "http://127.0.0.1:0";
                    startInfo.EnvironmentVariables["http_proxy"] = "http://127.0.0.1:0";
                    startInfo.EnvironmentVariables["HTTPS_PROXY"] = "http://127.0.0.1:0";
                    startInfo.EnvironmentVariables["https_proxy"] = "http://127.0.0.1:0";
                    startInfo.EnvironmentVariables["ALL_PROXY"] = "http://127.0.0.1:0";
                    startInfo.EnvironmentVariables["all_proxy"] = "http://127.0.0.1:0";
                }
            }

            // Custom Environment Variables configured specifically for this app
            if (rule.CustomVariables != null)
            {
                foreach (var customVar in rule.CustomVariables)
                {
                    if (customVar.IsEnabled && !string.IsNullOrWhiteSpace(customVar.Key))
                    {
                        startInfo.EnvironmentVariables[customVar.Key] = customVar.Value ?? string.Empty;
                        string lowerKey = customVar.Key.ToLowerInvariant();
                        if (lowerKey != customVar.Key)
                        {
                            startInfo.EnvironmentVariables[lowerKey] = customVar.Value ?? string.Empty;
                        }
                    }
                }
            }

            // Ensure NO_PROXY is always present whenever HTTP_PROXY is configured so internal IPC / loopback never breaks
            if (startInfo.EnvironmentVariables.ContainsKey("HTTP_PROXY") && !string.IsNullOrEmpty(startInfo.EnvironmentVariables["HTTP_PROXY"]))
            {
                if (!startInfo.EnvironmentVariables.ContainsKey("NO_PROXY") || string.IsNullOrWhiteSpace(startInfo.EnvironmentVariables["NO_PROXY"]))
                {
                    const string defaultNoProxy = "localhost,127.0.0.1,::1,127.0.0.0/8,*.local,*.internal";
                    startInfo.EnvironmentVariables["NO_PROXY"] = defaultNoProxy;
                    startInfo.EnvironmentVariables["no_proxy"] = defaultNoProxy;
                }
            }

            if (rule.RunAsAdmin)
            {
                startInfo.Verb = "runas";
            }

            var process = Process.Start(startInfo);
            rule.LastMatch = DateTime.Now.ToString("HH:mm:ss");

            return process;
        }
    }
}
