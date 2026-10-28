# Project Overview

Windows Antify Proxy is a native Windows utility designed to provide per-application proxy isolation. It scopes proxy environment variables (`HTTP_PROXY`, `HTTPS_PROXY`, `ALL_PROXY`, and `NO_PROXY`) strictly to selected processes, preventing proxy settings from affecting the entire operating system.

---

## The Problem with Global Proxy Settings

Under standard Windows operation, configuring a proxy either requires modifying Windows Internet Options (system-wide) or setting User Environment Variables in the Windows Registry (`HKCU\Environment`).

This leads to several issues:
1. **System-wide Pollution**: Setting `HTTP_PROXY` or `HTTPS_PROXY` globally forces all command-line tools (Git, npm, pip, curl, PowerShell), background services, and third-party software through the proxy.
2. **Broken Connectivity on Proxy Termination**: When your proxy client (e.g., Clash, v2ray, or Charles) is closed or crashes, any tool attempting to use the lingering environment variable will fail to connect.
3. **Gaming & Low-Latency Disruption**: Multiplayer games and latency-critical network applications may trigger anti-cheat bans or experience connection failures when global proxy variables or TUN virtual network adapters are present.
4. **Kernel & Driver Overhead**: TUN/TAP drivers require administrative rights, install virtual network adapters, and can cause kernel instability.

---

## The Solution: Process-Level Isolation

Windows Antify Proxy takes an application-isolated approach:
- **Zero Kernel Drivers**: Operates entirely in user space. No TUN/TAP drivers or administrative privileges are required.
- **Process Environment Block (PEB) Scoping**: When launching an application (such as Antigravity), proxy variables are injected directly into that process's environment block. No Windows registry or system environment variables are modified.
- **Multi-Protocol & Custom Variables**: Supports HTTP, HTTPS, and SOCKS5 proxy endpoints with arbitrary environment variable key-value overrides for fine-grained configuration.
- **Single-Click Shortcuts**: Native Windows desktop shortcuts (`.lnk`) can be generated for any rule, preserving the target application's real icon while wrapping launch parameters.

---

## Technical Specifications

- **Operating System**: Windows 10 (Version 1809 or later), Windows 11 (64-bit)
- **Framework**: .NET 8.0 (Windows Desktop Runtime / WPF)
- **Architecture**: x64 Native Executable
- **Default Scoped Target**: Antigravity (`%LOCALAPPDATA%\Programs\antigravity\Antigravity.exe`)
- **Default Proxy Endpoint**: `http://127.0.0.1:7897` (Clash / Clash Verge / v2ray)
