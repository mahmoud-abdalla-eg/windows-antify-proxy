using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using WindowsAntifyProxy.Models;

namespace WindowsAntifyProxy.Services
{
    public static class ProxyTesterService
    {
        public static async Task<int?> TestProxyLatencyAsync(ProxyItem proxy, int timeoutMs = 2500)
        {
            if (string.IsNullOrWhiteSpace(proxy.Host) || proxy.Port <= 0)
            {
                return null;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                using var client = new TcpClient();

                var connectTask = client.ConnectAsync(proxy.Host, proxy.Port);
                var delayTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(connectTask, delayTask);
                stopwatch.Stop();

                if (completedTask == connectTask && client.Connected)
                {
                    var latency = (int)Math.Max(1, stopwatch.ElapsedMilliseconds);
                    proxy.LatencyMs = latency;
                    proxy.Health = latency < 300 ? "Excellent" : latency < 800 ? "Good" : "Fair";
                    return latency;
                }
                else
                {
                    proxy.LatencyMs = -1; // Timeout
                    proxy.Health = "Timeout / Unreachable";
                    return -1;
                }
            }
            catch (Exception ex)
            {
                proxy.LatencyMs = -1;
                proxy.Health = $"Error: {ex.Message}";
                return -1;
            }
        }
    }
}

