<div align="center">

<img src="assets/logo.png" alt="Windows Antify Proxy Logo" width="120" height="120" />

# Win Antify Proxy (Windows Antify Proxy)

**专为 Windows 打造的单进程网络代理与环境变量隔离管理工具**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-blue.svg?logo=windows&logoColor=white)](https://github.com/mahmoud-abdalla-eg/windows-antify-proxy)
[![Framework](https://img.shields.io/badge/.NET-8.0%20WPF-purple.svg?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Version](https://img.shields.io/badge/Version-v1.0.0-emerald.svg)](https://github.com/mahmoud-abdalla-eg/windows-antify-proxy/releases)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/mahmoud-abdalla-eg/windows-antify-proxy/pulls)

---

### [English Document](README.md) · [简体中文说明](README_CN.md) · [搜索关键词优化](#-搜索关键词优化-seo-keywords)

---

</div>

<br/>

<div align="center">
  <img src="assets/app-screenshot.png" alt="Windows Antify Proxy 实时遥测仪表盘截图" width="860" />
  <br/><br/>
  <details>
    <summary><b>📸 点击展开查看规则管理与进程控制界面 (Rules View)</b></summary>
    <br/>
    <img src="assets/rules-screenshot.png" alt="Windows Antify Proxy 规则管理界面截图" width="860" />
  </details>
</div>

<br/>

---

## 📖 项目简介

**Win Antify Proxy** (Windows Antify Proxy) 是一款专为 Windows 10 与 Windows 11 设计的原生桌面代理环境隔离工具。它的核心使命是：**将代理环境变量（`HTTP_PROXY`、`HTTPS_PROXY`、`ALL_PROXY`、`NO_PROXY` 及 SOCKS5）严格限制在指定的应用进程中**（例如 **Antigravity**、**Cursor**、**VS Code**、**Git**、**Python**、命令行终端或任意特定 `.exe` 程序）。

本项目致敬并参考了 macOS 平台的著名工具 [Antify](https://antifyapp.com/en/) ([cyberlesterr/antify-feedback](https://github.com/cyberlesterr/antify-feedback))，致力于为 Windows 开发者打造优雅、无污染、无内核驱动负担的单进程代理分流体验。

---

## 💥 彻底解决三大核心痛点

平时使用 Clash、Clash Verge、v2rayN、Sing-box 等代理客户端进行软件开发与 AI 辅助编程时，经常面临如下难题：

### 1. 全局系统环境变量污染与误封风险
- 很多教程引导用户在 Windows 高级系统设置中配置用户或系统变量 `HTTP_PROXY=http://127.0.0.1:7890`，导致整个系统的全部联网进程无差别走代理。
- **网游误串流导致高延迟或被封号**：主流竞技网游（无畏契约/Valorant、英雄联盟/LOL、CS2、Apex 英雄、绝地求生等）一旦检测到未知代理 IP 或出现代理延迟抖动，极易触发游戏反作弊系统误封（Anti-Cheat Ban）。
- **局域网与本地开发服务瘫痪**：访问 `localhost:3000`、`127.0.0.1:8000` 或公司内网私有源、微服务时，常因全局代理拦截而报错无法连接。

### 2. TUN 虚拟网卡模式的沉重负担
- TUN 模式必须安装虚拟网卡并在系统内核层劫持所有网络数据包，导致 CPU 与内存开销升高、笔记本发热耗电。
- 极易与 WSL2、Docker Desktop、Hyper-V 以及企业级安全 VPN（如 Cisco AnyConnect、WireGuard、Fortinet）发生网卡驱动冲突与路由死锁。

### 3. Win Antify Proxy 的完美解法
- **零 TUN 网卡驱动、零全局变量污染**。
- 纯用户态运行：启动目标应用时，仅在其独立的子进程环境块中注入代理参数。
- 目标开发程序稳定走高速代理，操作系统其余程序、竞技网游和内网直连服务保持 100% 原始网络状态，互不干扰！

---

## 🌟 核心功能特性

- 🛡️ **单进程级环境隔离（Per-Process Isolation）：**
  将 `HTTP_PROXY`、`HTTPS_PROXY`、`ALL_PROXY` 仅注入至选定的目标软件，不污染 Windows 注册表和系统全局变量。
- ⚡ **免 TUN / 零内核驱动冲突：**
  纯用户空间运行，无需安装 WinTUN/TAP 虚拟网卡驱动，不占系统底层网络适配器，杜绝游戏反作弊误报与网络卡死。
- 🛑 **独家 Kill Switch 防泄漏断网保护：**
  启动应用前主动探测本地代理端口（默认 `127.0.0.1:7897`，支持自定义）是否正常监听。若代理服务未开启，自动拦截启动，防止真实 IP 裸奔泄露。
- 🎯 **随时可用的进程管理与控制：**
  - **`▶ 立即启动`：** 顶部窗口标题栏与每条规则行常驻，一触即发。
  - **`⏹ 一键停止`：** 无需翻找 Windows 任务管理器，一键优雅终止目标进程。
  - **`🔄 一键重启`：** 修改代理配置后，一键自动应用新环境并重启软件。
  - **实时进程监控：** 动态显示当前运行状态及系统 PID（`● 运行中 (PID: xxxx)` / `○ 已停止`）。
- 🚀 **一键生成独立桌面 / 开始菜单快捷方式：**
  交互式对话框支持为应用生成指向 **桌面**、**开始菜单** 或 **任务栏** 的专属快捷方式，双击图标即可直接以隔离代理模式启动！
- 🌐 **多协议兼容与自定义环境变量：**
  全面兼容 HTTP、HTTPS、SOCKS5 代理协议，并支持自由增加任意环境变量（如 `NODE_EXTRA_CA_CERTS`、`GIT_SSL_NO_VERIFY`、自定义 API Key 等）。
- 🎨 **原生 Windows 11 Fluent 现代深色界面：**
  基于 .NET 8 WPF 构建，采用流畅的 Fluent 亚克力质感与深色主题，内置代理 Ping 延迟测试与侧边抽屉面板。
- 📦 **绿色单文件便携免安装：**
  支持直接编译发布单文件绿色版 `.exe`，双击即用，随身携带，不写系统冗余残留。

---

## 📊 三大代理方案横向对比

| 功能特性对比 | Win Antify Proxy | 全局系统代理 / 系统环境变量 | TUN 虚拟网卡接管模式 |
| :--- | :---: | :---: | :---: |
| **单应用精准代理** | ✅ **100% 进程级独立隔离** | ❌ 全局生效，殃及其他程序 | ⚠️ 需要复杂配置分流规则 |
| **网络游戏防封号/低延迟** | ✅ **完全直连，绝不串流** | ❌ 极易误走代理导致封号 | ⚠️ 易被游戏反作弊模块拦截 |
| **本地服务 / 局域网内网** | ✅ **原生直连，零破坏** | ❌ 经常被代理劫持导致报错 | ⚠️ 需要手动配置绕过 IP 段 |
| **虚拟网卡 / 驱动负担** | ✅ **零驱动，纯用户态** | ✅ 无驱动 | ❌ 需安装 TAP/TUN 虚拟网卡 |
| **Kill Switch 断网防泄漏** | ✅ **内置主动端口探活** | ❌ 无此保护机制 | ⚠️ 依赖客户端具体实现 |
| **一键生成独立桌面图标** | ✅ **原生支持生成快捷方式** | ❌ 不支持 | ❌ 不支持 |
| **进程实时监控 (PID/启停)** | ✅ **内置实时监控** | ❌ 不支持 | ❌ 不支持 |

---

## 🚀 源码编译与运行

```powershell
# 1. 克隆代码仓库
git clone https://github.com/mahmoud-abdalla-eg/windows-antify-proxy.git
cd windows-antify-proxy/app

# 2. 编译发布单文件可执行文件
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ../release
```

运行程序后：
1. **配置本地代理：** 默认代理端口为 `127.0.0.1:7897`（Clash / Clash Verge 默认端口），点击 **Ping** 验证连接。
2. **添加或勾选目标程序：** 默认预设并自动扫描检测 **Antigravity**，亦可自由添加 Cursor、VS Code、Git 等。
3. **一键启动：** 点击顶部标题栏或规则卡片中的 **`▶ 立即启动`** 即可在隔离网络环境中运行。

---

## 💻 命令行自动化使用

```powershell
# 通过命令行直接启动指定的规则应用：
WindowsAntifyProxy.exe --launch-rule "Antigravity"

# 跳过 Kill Switch 代理端口探活检查直接启动：
WindowsAntifyProxy.exe --launch-rule "Antigravity" --bypass-killswitch

# 启动并向目标应用传递额外的运行参数：
WindowsAntifyProxy.exe --launch-rule "Antigravity" --args "--new-window"
```

---

## 📁 项目目录结构

```
windows-antify-proxy/
├── app/                              # 原生 .NET 8 WPF 桌面应用源码
│   ├── Converters/                   # UI 数据绑定转换器
│   ├── Models/                       # AppSettings, RuleItem, ProxyItem 等模型
│   ├── Services/                     # ProcessLauncher, ShortcutService, ProcessMonitor
│   ├── MainWindow.xaml               # Windows 11 Fluent 暗色 UI 与抽屉层
│   ├── MainWindow.xaml.cs            # 进程状态追踪与启停主逻辑
│   └── WindowsAntifyProxy.csproj     # 工程编译配置文件
├── docs/                             # 深度技术文档
│   ├── overview.md                   # 项目概述与设计动机
│   ├── architecture.md               # 进程环境块 (PEB) 底层原理
│   ├── configuration.md              # 配置 JSON 存储格式说明
│   └── api-reference.md              # 内部服务与接口说明
├── assets/                           # 高清截图、Logo 与图标资源
├── LICENSE                           # MIT 开源协议
├── README.md                         # 国际中英双语总文档
└── README_CN.md                      # 简体中文专属详尽文档
```

---

## 🏷️ 搜索关键词优化 (SEO Keywords)

为了让全球及国内开发者在 GitHub、搜索引擎（百度、谷歌、Bing 等）通过中英文关键词快速检索到本工具，收录以下精准关键词：

### 🇨🇳 中文关键词
`Windows代理`, `Windows代理工具`, `单应用代理`, `应用分流代理`, `进程代理`, `局部代理`, `环境变量代理`, `Antigravity 代理`, `Antigravity Windows`, `Antify Windows版`, `Clash 指定应用代理`, `Clash Verge 分流`, `Clash 进程代理`, `免 TUN 模式代理`, `防止游戏误代理`, `防游戏串流封号`, `英雄联盟代理避让`, `无畏契约代理避让`, `CS2 代理避让`, `开发工具代理`, `AI 编程代理`, `Cursor 代理`, `VSCode 代理`, `Git 代理设置`, `指定进程走代理`, `避免全局代理污染`, `Kill Switch 防泄漏`, `本地代理端口探活`, `Windows 进程级代理隔离`, `单软件独立代理`.

### 🇬🇧 English Keywords
`windows proxy`, `per-process proxy`, `per-app proxy windows`, `isolate proxy windows`, `antify windows`, `antify for windows`, `antigravity proxy`, `antigravity`, `antigravity tool`, `http_proxy`, `https_proxy`, `all_proxy`, `socks5 proxy windows`, `clash windows per app`, `clash verge per-process`, `v2ray per app`, `sing-box windows proxy`, `selective proxy`, `transparent proxy windows`, `environment variable proxy`, `proxy isolation`, `no tun proxy`, `anti cheat safe proxy`, `kill switch proxy windows`, `developer proxy tool`, `cursor proxy`, `vscode proxy`, `git proxy windows`.

---

## 📚 深入技术文档

欢迎查阅 [`docs/`](docs/) 目录下的深入设计文档：
- 📖 [项目整体概述 (Overview)](docs/overview.md)
- 🏗️ [进程环境块底层架构 (Architecture)](docs/architecture.md)
- ⚙️ [配置存储格式说明 (Configuration)](docs/configuration.md)
- 🔌 [代码与 API 参考 (API Reference)](docs/api-reference.md)

---

## 💬 交流与问题反馈

如果遇到问题、有新需求建议或发现了 Bug：
- 🐛 **提交反馈：** [GitHub Issues](https://github.com/mahmoud-abdalla-eg/windows-antify-proxy/issues)
- 💡 **Pull Requests：** 欢迎提交 PR 为项目添砖加瓦！

---

## 📜 开源许可与鸣谢

- **开源协议：** 本项目采用 [MIT 许可证](LICENSE) 开源。
- **灵感致敬：** 概念设计与交互灵感致敬 macOS 平台 [Antify](https://antifyapp.com/en/) 项目作者 [@cyberlesterr](https://github.com/cyberlesterr/antify-feedback)。
- **项目作者：** [Mahmoud Abdalla](https://github.com/mahmoud-abdalla-eg)

<div align="center">
  <sub>为全球开发者与极客用心打造 · 如果本项目对你有帮助，欢迎在 GitHub 点个 ⭐ Star 支持一下！</sub>
</div>
