using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WindowsAntifyProxy.Models;

namespace WindowsAntifyProxy.Services
{
    public static class ProcessMonitorService
    {
        public static List<Process> GetProcessesForRule(RuleItem rule)
        {
            var results = new List<Process>();
            string processName = string.Empty;

            if (!string.IsNullOrWhiteSpace(rule.ExecutablePath))
            {
                try
                {
                    processName = Path.GetFileNameWithoutExtension(rule.ExecutablePath);
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(processName))
            {
                processName = rule.Name;
            }

            try
            {
                var procs = Process.GetProcessesByName(processName);
                results.AddRange(procs);
            }
            catch { }

            return results;
        }

        public static int StopRuleProcesses(RuleItem rule)
        {
            var procs = GetProcessesForRule(rule);
            int stopped = 0;

            foreach (var proc in procs)
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        // Try graceful window close first
                        proc.CloseMainWindow();
                        if (!proc.WaitForExit(600))
                        {
                            proc.Kill(entireProcessTree: true);
                        }
                        stopped++;
                    }
                }
                catch
                {
                    try
                    {
                        proc.Kill();
                        stopped++;
                    }
                    catch { }
                }
            }

            rule.IsRunning = false;
            rule.RunningPid = null;
            rule.RunningProcessCount = 0;
            return stopped;
        }

        public static async Task<Process?> RestartRuleProcessesAsync(RuleItem rule, AppSettings settings, bool bypassKillSwitch = false)
        {
            StopRuleProcesses(rule);

            // Wait brief moment for socket and file release
            await Task.Delay(800);

            return ProcessLauncher.LaunchRule(rule, settings, bypassKillSwitch: bypassKillSwitch);
        }

        public static void UpdateRuleLiveStatus(RuleItem rule)
        {
            var procs = GetProcessesForRule(rule);
            if (procs.Count > 0)
            {
                rule.IsRunning = true;
                rule.RunningProcessCount = procs.Count;
                rule.RunningPid = procs.FirstOrDefault()?.Id;
            }
            else
            {
                rule.IsRunning = false;
                rule.RunningProcessCount = 0;
                rule.RunningPid = null;
            }
        }
    }
}
