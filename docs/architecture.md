# Architecture and Internals

This document details the internal design, process management model, and Windows API integrations used by Windows Antify Proxy.

---

## 1. Process Environment Block (PEB) Injection

In Microsoft Windows, every Win32 process maintains an environment block within its Process Environment Block (PEB). Child processes inherit the environment block of their parent by default, but this block can be explicitly customized at creation time.

Windows Antify Proxy uses `System.Diagnostics.ProcessStartInfo` to populate environment variables before calling `Process.Start()`:

```csharp
var psi = new ProcessStartInfo
{
    FileName = rule.ExecutablePath,
    Arguments = rule.Arguments,
    WorkingDirectory = Path.GetDirectoryName(rule.ExecutablePath) ?? string.Empty,
    UseShellExecute = false // Required for custom environment block passing
};

// Clear any inherited system-wide proxy variables
psi.EnvironmentVariables.Remove("HTTP_PROXY");
psi.EnvironmentVariables.Remove("HTTPS_PROXY");
psi.EnvironmentVariables.Remove("ALL_PROXY");
psi.EnvironmentVariables.Remove("NO_PROXY");

// Apply scoped environment variables defined for this rule
foreach (var env in rule.CustomVariables.Where(v => v.IsEnabled))
{
    psi.EnvironmentVariables[env.Key] = env.Value;
}

Process.Start(psi);
```

### Key Differences from System Scoping

| Attribute | Global System Variable | Windows Antify Proxy |
| :--- | :--- | :--- |
| **Storage Location** | `HKCU\Environment` or `HKLM\...` | Transient Process Memory (RAM) |
| **Persistence** | Permanent until registry edit | Terminated when process exits |
| **Process Reach** | Every process started by the user | Exclusively the target process |
| **System Pollution** | High (affects Git, CLI, games) | None (0% system leakage) |
| **Admin Required** | Often required for system scope | Never required |

---

## 2. Process Lifetime and State Monitoring

Windows Antify Proxy tracks running target instances in real time using `ProcessMonitorService`:

1. **Periodic Tick Loop**: A background `DispatcherTimer` queries active Win32 processes by executable base name every 1.5 seconds.
2. **Path Matching**: Retrieved process IDs (`Process.GetProcessesByName`) have their module file paths inspected to ensure exact matching against the configured rule's `ExecutablePath`.
3. **PID Tracking & Status Updates**:
   - Matches update the rule's `IsRunning` state and populate the active PID list.
   - UI badges reflect `Running (PID: 12345)` or `Stopped`.
4. **Clean Termination**: When the user triggers process termination, `ProcessLauncher.TerminateRuleProcesses(rule)` sends a polite exit signal before forcing termination via `Process.Kill()`.

---

## 3. Windows Shell Integration

### Desktop Shortcut Creation
Windows Antify Proxy generates Windows `.lnk` shortcuts using the Windows Script Host (`IWshRuntimeLibrary`) COM interface:
- **Target**: Points directly to `WindowsAntifyProxy.exe`.
- **Arguments**: `--launch "<RuleName>"`.
- **Icon Location**: Dynamically extracted from the target application binary using Win32 `ExtractIconExW` to ensure native visual fidelity.

### Explorer Context Menu
When enabled in Settings, the application registers an Explorer context menu via `HKCU\Software\Classes\*\shell\WindowsAntifyProxy`:
- Provides a right-click entry: *"Launch with Antify Proxy"*.
- Passes the clicked file path as an argument to Windows Antify Proxy, launching the application with active proxy variables.
- Modifies strictly user-level registry keys (`HKEY_CURRENT_USER`), avoiding administrative privilege elevation.
