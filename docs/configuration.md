# Configuration & Storage

This document describes the configuration schema, settings directory, and profile structure used by Windows Antify Proxy.

---

## Storage Location

All configuration settings are stored in JSON format in the current user's roaming application data directory:

```
%APPDATA%\WindowsAntifyProxy\settings.json
```

Typically resolves to:
```
C:\Users\<Username>\AppData\Roaming\WindowsAntifyProxy\settings.json
```

---

## Schema Overview

The configuration structure is represented by the `AppSettings` class:

```json
{
  "IsProxyActive": true,
  "AutoEnforceProxy": true,
  "ExplorerContextMenu": false,
  "SelectedProxyId": "default-clash",
  "Proxies": [
    {
      "Id": "default-clash",
      "Name": "Clash / Verge (7897)",
      "Host": "127.0.0.1",
      "Port": 7897,
      "Protocol": "HTTP",
      "IsDefault": true
    }
  ],
  "Rules": [
    {
      "Id": "antigravity-rule",
      "Name": "Antigravity",
      "Subtitle": "C:\\Users\\<User>\\AppData\\Local\\Programs\\antigravity\\Antigravity.exe",
      "ExecutablePath": "C:\\Users\\<User>\\AppData\\Local\\Programs\\antigravity\\Antigravity.exe",
      "Arguments": "",
      "Type": "App",
      "Action": "Proxy",
      "ProxyName": "Clash / Verge (7897)",
      "IsEnabled": true,
      "CustomVariables": [
        {
          "Key": "HTTP_PROXY",
          "Value": "http://127.0.0.1:7897",
          "IsEnabled": true
        },
        {
          "Key": "HTTPS_PROXY",
          "Value": "http://127.0.0.1:7897",
          "IsEnabled": true
        },
        {
          "Key": "NO_PROXY",
          "Value": "localhost,127.0.0.1",
          "IsEnabled": true
        }
      ]
    }
  ],
  "Profiles": [
    {
      "Id": "default-profile",
      "Name": "Default Profile",
      "Description": "Standard developer configuration"
    }
  ]
}
```

---

## Rule Properties

- **`Name`**: Display name for the rule in the user interface.
- **`ExecutablePath`**: Absolute path to the executable file (`.exe`, `.bat`, or `.cmd`).
- **`Arguments`**: Optional command-line arguments passed to the application at launch.
- **`Action`**:
  - `"Proxy"`: Injects active proxy environment variables.
  - `"Direct"`: Clears proxy variables and sets `NO_PROXY=*`.
- **`CustomVariables`**: Collection of key-value pairs representing custom environment variables to be injected into the target process.

---

## Supported Proxy Protocols

Windows Antify Proxy supports all standard protocols handled by environment variables:
- **HTTP**: `http://<host>:<port>`
- **HTTPS**: `http://<host>:<port>` or `https://<host>:<port>`
- **SOCKS5**: `socks5://<host>:<port>` or `socks5h://<host>:<port>`
