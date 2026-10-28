# Windows Antify Proxy

A native Windows desktop utility for scoping proxy environment variables (`HTTP_PROXY`, `HTTPS_PROXY`, `ALL_PROXY`, and `NO_PROXY`) exclusively to designated applications (such as Antigravity), preventing proxy variables from leaking into the global Windows operating system.

---

## Overview

Configuring global proxy variables in Windows modifies user or system registry environments, forcing all command-line utilities, background daemons, and multiplayer games to route through the proxy. When the local proxy client terminates, network connectivity across these unrelated tools breaks.

Windows Antify Proxy provides process-level isolation:
- Scopes proxy variables strictly to the Process Environment Block (PEB) of target executables.
- Keeps Windows Registry and system-wide environment variables untouched.
- Operates entirely in user space without requiring administrative privileges, TUN/TAP virtual network adapters, or kernel drivers.

---

## Features

- **Per-Application Environment Isolation**: Injects `HTTP_PROXY`, `HTTPS_PROXY`, and custom variables exclusively into selected application processes.
- **Multi-Protocol Support**: Compatible with HTTP, HTTPS, and SOCKS5 proxy protocols (Clash, Clash Verge, v2ray, Xray, Charles, Fiddler).
- **Desktop Shortcut Generator**: Produces native Windows `.lnk` shortcuts embedded with the original executable icon for single-click isolated launching.
- **Real-Time Process Monitoring**: Continuously tracks target PID states, providing quick stop, start, and restart controls.
- **Windows Explorer Integration**: Optional right-click context menu to launch any executable with isolated proxy variables.
- **Windows 11 Fluent Interface**: Built natively with WPF, featuring a dark interface and responsive in-window drawers.

---

## Repository Structure

```
main repo project/
|-- app/                    # Native .NET 8 WPF Desktop Application
|   |-- Converters/         # UI data converters
|   |-- Models/             # Configuration, Rule, and Proxy models
|   |-- Services/           # Process launcher, monitor, and shell integration
|   |-- App.xaml            # Application entry point and theme resources
|   |-- MainWindow.xaml     # Fluent UI layout and navigation
|   |-- WindowsAntifyProxy.csproj
|-- docs/                   # Technical Documentation
|   |-- overview.md         # Problem definition and feature summary
|   |-- architecture.md     # Process Environment Block internals and Win32 APIs
|   |-- configuration.md    # JSON schema and settings storage
|   |-- api-reference.md    # Internal services and models reference
|-- LICENSE                 # MIT License
|-- .gitignore
|-- README.md
```

---

## Building from Source

### Prerequisites

- Windows 10 (version 1809 or higher) or Windows 11 (64-bit)
- .NET 8.0 SDK (Desktop Runtime)

### Building the Desktop Application

1. Navigate to the `app` directory:
   ```powershell
   cd app
   ```

2. Restore dependencies and build:
   ```powershell
   dotnet build -c Release
   ```

3. Publish a standalone single-file binary:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ../release
   ```

---

## Documentation

Comprehensive documentation is available in the `docs` directory:

- [Project Overview](docs/overview.md)
- [Architecture & Internals](docs/architecture.md)
- [Configuration & Settings](docs/configuration.md)
- [API & Code Reference](docs/api-reference.md)

---

## Contributing and Issues

Contributions, bug reports, and suggestions are welcome.
Please submit issues via GitHub:
https://github.com/mahmoud-abdalla-eg/windows-antify-proxy/issues

---

## License

This project is licensed under the MIT License.
Inspired by Antify for macOS.
