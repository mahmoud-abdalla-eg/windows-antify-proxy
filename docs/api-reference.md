# API & Code Reference

This document provides a technical reference for core services, models, and utility classes in the Windows Antify Proxy solution.

---

## Namespaces

- **`WindowsAntifyProxy.Models`**: Data contracts, observable objects, and persistence models.
- **`WindowsAntifyProxy.Services`**: Core logic for process launching, health monitoring, icon extraction, configuration persistence, and Windows Shell integration.
- **`WindowsAntifyProxy.Converters`**: WPF value converters for UI bindings.

---

## Core Services

### `ProcessLauncher`
Location: `Services/ProcessLauncher.cs`

Responsible for starting, restarting, and terminating target applications with isolated environment blocks.

```csharp
public static class ProcessLauncher
{
    // Launches target executable with custom environment variables
    public static Process? LaunchWithEnvironment(
        RuleItem rule, 
        AppSettings settings);

    // Kills all active Win32 processes matching the rule's executable path
    public static void TerminateRuleProcesses(RuleItem rule);

    // Restarts running processes under the current rule configuration
    public static void RestartRuleProcesses(RuleItem rule, AppSettings settings);
}
```

---

### `ProcessMonitorService`
Location: `Services/ProcessMonitorService.cs`

Tracks execution state and live process identifiers (PIDs) for configured application rules.

```csharp
public static class ProcessMonitorService
{
    // Updates RuleItem.IsRunning and RuleItem.ActivePids
    public static void UpdateRuleLiveStatus(RuleItem rule);

    // Determines if any process matching the executable path is running
    public static bool IsProcessRunning(string executablePath);
}
```

---

### `ProxyTesterService`
Location: `Services/ProxyTesterService.cs`

Performs TCP socket connectivity checks to verify whether a proxy endpoint is active and listening.

```csharp
public static class ProxyTesterService
{
    // Attempts a non-blocking TCP socket connection to host:port within timeout
    public static Task<bool> IsPortListeningAsync(string host, int port, int timeoutMs = 1000);

    // Measures connection latency in milliseconds (-1 on failure)
    public static Task<long> TestProxyLatencyAsync(string host, int port, int timeoutMs = 2000);
}
```

---

### `ConfigService`
Location: `Services/ConfigService.cs`

Manages configuration persistence, default rule seeding (Antigravity detection), and profile serialization.

```csharp
public static class ConfigService
{
    // Loads settings from %APPDATA%\WindowsAntifyProxy\settings.json
    public static AppSettings LoadSettings();

    // Serializes AppSettings to JSON with indentation
    public static void SaveSettings(AppSettings settings);

    // Path to the settings JSON file
    public static string SettingsFilePath { get; }
}
```

---

### `ShortcutService`
Location: `Services/ShortcutService.cs`

Generates Windows `.lnk` shortcuts on the Desktop, in the Start Menu, or prepared for Taskbar pinning with custom launch parameters and application icons.

```csharp
public static class ShortcutService
{
    // Creates a desktop shortcut
    public static string CreateDesktopShortcut(RuleItem rule);

    // Creates a Start Menu shortcut and attempts programmatic Pin to Start
    public static string CreateStartMenuShortcut(RuleItem rule);

    // Creates a shortcut and highlights it in Explorer for 1-click Pin to Taskbar
    public static string CreateTaskbarShortcut(RuleItem rule);

    // Creates shortcuts across Desktop, Start Menu, and highlights for Taskbar
    public static string CreateEverywhereShortcuts(RuleItem rule);
}
```

---

### `ShellContextMenuService`
Location: `Services/ContextMenuService.cs`

Manages Windows File Explorer right-click integration via `HKEY_CURRENT_USER\Software\Classes\*\shell`.

```csharp
public static class ShellContextMenuService
{
    // Registers right-click menu entry
    public static void RegisterContextMenu();

    // Unregisters right-click menu entry
    public static void UnregisterContextMenu();
}
```

---

### `IconHelper`
Location: `Services/IconHelper.cs`

Extracts high-resolution icons from native Windows PE binaries (`.exe`, `.dll`, `.ico`) and converts them to WPF `ImageSource` objects.

```csharp
public static class IconHelper
{
    public static ImageSource? GetIconForFile(string filePath);
}
```

---

## Core Models

### `RuleItem` (`INotifyPropertyChanged`)
Location: `Models/RuleItem.cs`

| Property | Type | Description |
| :--- | :--- | :--- |
| `Name` | `string` | Display name of the application rule |
| `ExecutablePath` | `string` | Absolute path to the executable |
| `Arguments` | `string` | CLI arguments |
| `Action` | `string` | `"Proxy"` or `"Direct"` |
| `IsEnabled` | `bool` | Active state toggle |
| `IsRunning` | `bool` | Real-time process running state |
| `CustomVariables` | `ObservableCollection<EnvironmentVariableItem>` | Environment variables |

### `ProxyItem` (`INotifyPropertyChanged`)
Location: `Models/ProxyItem.cs`

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `string` | Unique identifier |
| `Name` | `string` | Display label (e.g., `"Clash / Verge"`) |
| `Host` | `string` | Hostname or IP (`"127.0.0.1"`) |
| `Port` | `int` | Port number (`7897`) |
| `Protocol` | `string` | `"HTTP"`, `"HTTPS"`, `"SOCKS5"` |
| `IsDefault` | `bool` | Whether this is the default proxy |
