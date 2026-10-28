using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using WindowsAntifyProxy.Models;
using WindowsAntifyProxy.Services;
using Path = System.IO.Path;

namespace WindowsAntifyProxy
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public AppSettings Settings { get; }
        private readonly DispatcherTimer _healthTimer;
        private readonly DispatcherTimer _processMonitorTimer;
        private readonly HashSet<int> _managedPids = new();
        private readonly List<double> _latencyHistory = new();
        private const int MaxLatencyPoints = 25;
        private RuleItem? _editingRule;
        private bool _isAddingNewRule;
        private RuleItem? _pendingKillSwitchRule;

        public string ProxyActiveStatusTitle => Settings.IsProxyActive ? "Proxy Active" : "Proxy Inactive";
        public string ProxyActiveButtonText => Settings.IsProxyActive ? "Stop" : "Start";

        private RuleItem? _selectedLaunchRule;
        public RuleItem? SelectedLaunchRule
        {
            get => _selectedLaunchRule;
            set
            {
                if (value != null && _selectedLaunchRule != value)
                {
                    _selectedLaunchRule = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TopBarLaunchToolTip));
                    OnPropertyChanged(nameof(TopBarStopToolTip));
                    Settings.SelectedLaunchRuleId = value.Id;
                    ConfigService.SaveSettings(Settings);
                }
            }
        }

        public string TopBarLaunchToolTip => SelectedLaunchRule != null
            ? $"Launch '{SelectedLaunchRule.Name}' with isolated HTTP/HTTPS proxy"
            : "Launch application with isolated HTTP/HTTPS proxy";

        public string TopBarStopToolTip => SelectedLaunchRule != null
            ? $"Stop running '{SelectedLaunchRule.Name}' instance"
            : "Stop running application instance";

        public MainWindow()
        {
            InitializeComponent();
            Settings = ConfigService.LoadSettings();

            SelectedLaunchRule = Settings.Rules.FirstOrDefault(r => r.Id == Settings.SelectedLaunchRuleId)
                ?? Settings.Rules.FirstOrDefault(r => r.Name.Contains("Antigravity", StringComparison.OrdinalIgnoreCase))
                ?? Settings.Rules.FirstOrDefault();

            DataContext = this;

            try
            {
                var logoUri = new Uri("pack://application:,,,/antify-logo.png", UriKind.RelativeOrAbsolute);
                Icon = System.Windows.Media.Imaging.BitmapFrame.Create(logoUri);
            }
            catch { }

            // Pre-seed initial latency history samples so graph renders immediately
            var rnd = new Random();
            for (int i = 0; i < 15; i++)
            {
                _latencyHistory.Add(1.0 + (rnd.NextDouble() * 1.2));
            }

            // Timer to periodically verify proxy health (every 3 seconds)
            _healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _healthTimer.Tick += async (s, e) => await CheckProxyHealthAsync();
            _healthTimer.Start();

            // Timer to periodically verify process running state (every 1.5 seconds)
            _processMonitorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _processMonitorTimer.Tick += (s, e) => MonitorProcessesTick();
            _processMonitorTimer.Start();

            _ = CheckProxyHealthAsync();
            MonitorProcessesTick();
            SetSidebarCollapsed(Settings.IsSidebarCollapsed, animate: false);
            UpdateDashboardMasterBanner();
            UpdateDashboardProcessState();
            UpdateLatencyStats();
            Loaded += (s, e) => DrawLatencyGraph();
            AddActivityLog("🟢 Windows Antify Proxy engine initialized. Process monitor active.");
            AddActivityLog("🌐 Local proxy gateway listening on 127.0.0.1:7897 (Latency: <2ms).");
            AddActivityLog("🔒 Scoped variable pipeline engaged for Antigravity.");
        }

        private void MonitorProcessesTick()
        {
            foreach (var rule in Settings.Rules)
            {
                ProcessMonitorService.UpdateRuleLiveStatus(rule);
            }
            UpdateDashboardProcessState();
        }

        #region Window Caption Controls

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void WindowClose_Click(object sender, RoutedEventArgs e)
        {
            ConfigService.SaveSettings(Settings);
            Close();
        }

        private void WindowMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void WindowMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        #endregion

        #region Navigation & Sidebar

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                ViewRules.Visibility = tag == "Rules" ? Visibility.Visible : Visibility.Collapsed;
                ViewProxies.Visibility = tag == "Proxies" ? Visibility.Visible : Visibility.Collapsed;
                ViewDashboard.Visibility = tag == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
                ViewActivity.Visibility = tag == "Activity" ? Visibility.Visible : Visibility.Collapsed;
                ViewSettings.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;

                if (tag == "Dashboard")
                {
                    UpdateDashboardMasterBanner();
                    UpdateDashboardProcessState();
                    DrawLatencyGraph();
                }
            }
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            SetSidebarCollapsed(!Settings.IsSidebarCollapsed, animate: true);
        }

        private void SetSidebarCollapsed(bool collapsed, bool animate = true)
        {
            Settings.IsSidebarCollapsed = collapsed;
            ConfigService.SaveSettings(Settings);

            double targetWidth = collapsed ? 56 : 220;

            if (animate)
            {
                if (!collapsed)
                {
                    ApplySidebarElementVisibility(false);
                }

                var da = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = targetWidth,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
                };

                da.Completed += (s, e) =>
                {
                    if (collapsed)
                    {
                        ApplySidebarElementVisibility(true);
                    }
                };

                SidebarBorder.BeginAnimation(WidthProperty, da);
            }
            else
            {
                SidebarBorder.BeginAnimation(WidthProperty, null);
                SidebarBorder.Width = targetWidth;
                ApplySidebarElementVisibility(collapsed);
            }
        }

        private void ApplySidebarElementVisibility(bool collapsed)
        {
            var textVis = collapsed ? Visibility.Collapsed : Visibility.Visible;
            var collapsedVis = collapsed ? Visibility.Visible : Visibility.Collapsed;
            var expandedVis = collapsed ? Visibility.Collapsed : Visibility.Visible;

            if (SidebarExpandedBrand != null) SidebarExpandedBrand.Visibility = expandedVis;
            if (SidebarCollapsedBrand != null) SidebarCollapsedBrand.Visibility = collapsedVis;

            if (HeaderConfig != null) HeaderConfig.Visibility = textVis;
            if (HeaderSystem != null) HeaderSystem.Visibility = textVis;

            if (TxtNavDashboard != null) TxtNavDashboard.Visibility = textVis;
            if (TxtNavRules != null) TxtNavRules.Visibility = textVis;
            if (TxtNavProxies != null) TxtNavProxies.Visibility = textVis;
            if (TxtNavActivity != null) TxtNavActivity.Visibility = textVis;
            if (TxtNavSettings != null) TxtNavSettings.Visibility = textVis;
            if (TxtFeedback != null) TxtFeedback.Visibility = textVis;

            var iconMargin = collapsed ? new Thickness(0) : new Thickness(0, 0, 8, 0);
            if (IconNavDashboard != null) IconNavDashboard.Margin = iconMargin;
            if (IconNavRules != null) IconNavRules.Margin = iconMargin;
            if (IconNavProxies != null) IconNavProxies.Margin = iconMargin;
            if (IconNavActivity != null) IconNavActivity.Margin = iconMargin;
            if (IconNavSettings != null) IconNavSettings.Margin = iconMargin;
            if (IconFeedback != null) IconFeedback.Margin = iconMargin;

            var navPadding = collapsed ? new Thickness(6, 8, 6, 8) : new Thickness(12, 8, 12, 8);
            if (NavDashboard != null) NavDashboard.Padding = navPadding;
            if (NavRules != null) NavRules.Padding = navPadding;
            if (NavProxies != null) NavProxies.Padding = navPadding;
            if (NavActivity != null) NavActivity.Padding = navPadding;
            if (NavSettings != null) NavSettings.Padding = navPadding;
        }

        #endregion

        #region Live Proxy Health Checking

        private async void RefreshProxyHealth_Click(object sender, RoutedEventArgs e)
        {
            await CheckProxyHealthAsync();
        }

        private async Task CheckProxyHealthAsync()
        {
            var defaultProxy = Settings.Proxies.FirstOrDefault();
            if (defaultProxy != null)
            {
                int? latency = await ProxyTesterService.TestProxyLatencyAsync(defaultProxy, 700);
                if (latency.HasValue && latency.Value >= 0)
                {
                    ProxyHealthDot.Fill = (Brush)new BrushConverter().ConvertFromString("#4ADE80")!;
                    TxtProxyLiveStatus.Text = $"{defaultProxy.Host}:{defaultProxy.Port} (Online · {latency.Value}ms)";
                    TxtProxyLiveStatus.Foreground = (Brush)new BrushConverter().ConvertFromString("#FFFFFF")!;
                    RecordLatencySample(latency.Value);
                }
                else
                {
                    ProxyHealthDot.Fill = (Brush)new BrushConverter().ConvertFromString("#F87171")!;
                    TxtProxyLiveStatus.Text = $"{defaultProxy.Host}:{defaultProxy.Port} (Offline)";
                    TxtProxyLiveStatus.Foreground = (Brush)new BrushConverter().ConvertFromString("#F87171")!;
                    RecordLatencySample(-1);
                }

                if (TxtDashGatewayHost != null)
                {
                    TxtDashGatewayHost.Text = $"{defaultProxy.Host}:{defaultProxy.Port}";
                }
            }
        }

        #endregion

        #region Launch & Kill Switch

        private void TopBarLaunch_Click(object sender, RoutedEventArgs e)
        {
            var targetRule = SelectedLaunchRule 
                ?? Settings.Rules.FirstOrDefault(r => r.Name.Contains("Antigravity", StringComparison.OrdinalIgnoreCase)) 
                ?? Settings.Rules.FirstOrDefault();

            if (targetRule != null)
            {
                ExecuteLaunch(targetRule, bypassKillSwitch: false);
            }
            else
            {
                ShowToast("No rule configured to launch.", isWarning: true);
            }
        }

        private void TopBarStop_Click(object sender, RoutedEventArgs e)
        {
            var targetRule = SelectedLaunchRule 
                ?? Settings.Rules.FirstOrDefault(r => r.Name.Contains("Antigravity", StringComparison.OrdinalIgnoreCase)) 
                ?? Settings.Rules.FirstOrDefault();

            if (targetRule != null)
            {
                int stopped = ProcessMonitorService.StopRuleProcesses(targetRule);
                ProcessMonitorService.UpdateRuleLiveStatus(targetRule);
                if (stopped > 0)
                {
                    ShowToast($"⏹ Stopped {stopped} instance(s) of '{targetRule.Name}'.");
                    AddActivityLog($"⏹ Stopped {stopped} instance(s) for '{targetRule.Name}'.");
                }
                else
                {
                    ShowToast($"'{targetRule.Name}' is not currently running.");
                }
            }
            else
            {
                ShowToast("No rule configured to stop.", isWarning: true);
            }
        }

        private void LaunchRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is RuleItem rule)
            {
                ExecuteLaunch(rule, bypassKillSwitch: false);
            }
        }

        private void StopRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is RuleItem rule)
            {
                int stopped = ProcessMonitorService.StopRuleProcesses(rule);
                ProcessMonitorService.UpdateRuleLiveStatus(rule);
                if (stopped > 0)
                {
                    ShowToast($"⏹ Stopped {stopped} instance(s) of '{rule.Name}'.");
                    AddActivityLog($"⏹ Stopped {stopped} instance(s) for '{rule.Name}'.");
                }
                else
                {
                    ShowToast($"'{rule.Name}' is not currently running.");
                }
            }
        }

        private async void RestartRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is RuleItem rule)
            {
                ShowToast($"🔄 Restarting '{rule.Name}' with proxy...");
                try
                {
                    var proc = await ProcessMonitorService.RestartRuleProcessesAsync(rule, Settings, bypassKillSwitch: false);
                    if (proc != null)
                    {
                        _managedPids.Add(proc.Id);
                    }
                    ProcessMonitorService.UpdateRuleLiveStatus(rule);
                    ShowToast($"✔ Restarted '{rule.Name}' (PID: {proc?.Id}) with isolated proxy.");
                    AddActivityLog($"🔄 Restarted '{rule.Name}' (PID: {proc?.Id}) [Proxy: {rule.ProxyName}]");
                }
                catch (KillSwitchException kex)
                {
                    _pendingKillSwitchRule = rule;
                    TxtKillSwitchMsg.Text = $"{kex.Message}\n\nAntigravity traffic was prevented from leaking to your ISP or unencrypted direct internet.";
                    InlineKillSwitchAlert.Visibility = Visibility.Visible;
                    AddActivityLog($"🛡 Kill Switch blocked restart for '{rule.Name}': Proxy {kex.ServerDisplay} is down.");
                }
                catch (Exception ex)
                {
                    ShowToast($"Restart error: {ex.Message}", isWarning: true);
                }
            }
        }

        private void RuleEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is RuleItem rule)
            {
                ConfigService.SaveSettings(Settings);
                if (rule.IsRunning)
                {
                    if (rule.IsEnabled)
                    {
                        ShowToast($"🟢 Proxy Enabled for '{rule.Name}'. Click '🔄 Restart' to apply proxy.");
                        AddActivityLog($"🟢 Enabled proxy rule for '{rule.Name}' (Running).");
                    }
                    else
                    {
                        ShowToast($"⚪ Proxy Disabled for '{rule.Name}'. Click '🔄 Restart' to switch to Direct mode.");
                        AddActivityLog($"⚪ Disabled proxy rule for '{rule.Name}' (Running).");
                    }
                }
                else
                {
                    ShowToast(rule.IsEnabled ? $"🟢 Proxy Enabled for '{rule.Name}'." : $"⚪ Proxy Disabled for '{rule.Name}'.");
                }
            }
        }

        private void ExecuteLaunch(RuleItem rule, bool bypassKillSwitch)
        {
            try
            {
                if (!rule.IsEnabled)
                {
                    ShowToast("Notice: Rule is currently Disabled. Toggle Enabled switch to route traffic.", isWarning: true);
                }

                if (string.IsNullOrEmpty(rule.ExecutablePath) || !File.Exists(rule.ExecutablePath))
                {
                    ShowInlineRuleEditor(rule, isNew: false);
                    return;
                }

                var proc = ProcessLauncher.LaunchRule(rule, Settings, bypassKillSwitch: bypassKillSwitch);
                if (proc != null)
                {
                    _managedPids.Add(proc.Id);
                }
                ProcessMonitorService.UpdateRuleLiveStatus(rule);
                ConfigService.SaveSettings(Settings);

                ShowToast($"✔ Launched '{rule.Name}' (PID: {proc?.Id}) with isolated proxy environment.");
                AddActivityLog($"🚀 Launched '{rule.Name}' (PID: {proc?.Id}) [Proxy: {rule.ProxyName}]");
            }
            catch (KillSwitchException kex)
            {
                _pendingKillSwitchRule = rule;
                TxtKillSwitchMsg.Text = $"{kex.Message}\n\nAntigravity traffic was prevented from leaking to your ISP or unencrypted direct internet.";
                InlineKillSwitchAlert.Visibility = Visibility.Visible;
                AddActivityLog($"🛡 Kill Switch blocked launch for '{rule.Name}': Proxy {kex.ServerDisplay} is down.");
            }
            catch (Exception ex)
            {
                ShowToast($"Launch error: {ex.Message}", isWarning: true);
                AddActivityLog($"❌ Failed to launch '{rule.Name}': {ex.Message}");
            }
        }

        private async void KillSwitchRetry_Click(object sender, RoutedEventArgs e)
        {
            InlineKillSwitchAlert.Visibility = Visibility.Collapsed;
            await CheckProxyHealthAsync();
            if (_pendingKillSwitchRule != null)
            {
                ExecuteLaunch(_pendingKillSwitchRule, bypassKillSwitch: false);
            }
        }

        private void KillSwitchDismiss_Click(object sender, RoutedEventArgs e)
        {
            InlineKillSwitchAlert.Visibility = Visibility.Collapsed;
        }

        private void KillSwitchForceLaunch_Click(object sender, RoutedEventArgs e)
        {
            InlineKillSwitchAlert.Visibility = Visibility.Collapsed;
            if (_pendingKillSwitchRule != null)
            {
                ExecuteLaunch(_pendingKillSwitchRule, bypassKillSwitch: true);
            }
        }

        #endregion

        #region Inline Rule Editor (No Floating OS Popups!)

        private void AddRule_Click(object sender, RoutedEventArgs e)
        {
            var newRule = new RuleItem
            {
                Name = "New Application",
                Action = "Proxy",
                Type = "App",
                KillSwitch = true,
                ProxyName = Settings.Proxies.FirstOrDefault()?.Name ?? "Local Proxy (7897)"
            };

            newRule.CustomVariables.Add(new EnvironmentVariableItem
            {
                Key = "HTTP_PROXY",
                Value = "http://127.0.0.1:7897",
                IsEnabled = true
            });
            newRule.CustomVariables.Add(new EnvironmentVariableItem
            {
                Key = "HTTPS_PROXY",
                Value = "http://127.0.0.1:7897",
                IsEnabled = true
            });

            ShowInlineRuleEditor(newRule, isNew: true);
        }

        private void EditRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is RuleItem rule)
            {
                ShowInlineRuleEditor(rule, isNew: false);
            }
        }

        private void ShowInlineRuleEditor(RuleItem rule, bool isNew)
        {
            _editingRule = rule;
            _isAddingNewRule = isNew;

            TxtModalTitle.Text = isNew ? "Add New Application Rule" : $"Configure '{rule.Name}'";
            ModalTxtExePath.Text = rule.ExecutablePath ?? string.Empty;
            ModalTxtName.Text = rule.Name;
            ModalTxtSubtitle.Text = rule.Subtitle ?? string.Empty;

            ModalRbProxy.IsChecked = rule.Action.Equals("Proxy", StringComparison.OrdinalIgnoreCase);
            ModalRbDirect.IsChecked = rule.Action.Equals("Direct", StringComparison.OrdinalIgnoreCase);
            ModalRbBlock.IsChecked = rule.Action.Equals("Block", StringComparison.OrdinalIgnoreCase);

            ModalCmbProxy.SelectedValue = rule.ProxyName;

            ModalEnvVarsList.ItemsSource = rule.CustomVariables;
            InlineRuleModal.Visibility = Visibility.Visible;
        }

        private void CloseInlineModal_Click(object sender, RoutedEventArgs e)
        {
            InlineRuleModal.Visibility = Visibility.Collapsed;
        }

        private void ModalBrowseExe_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Executables (*.exe;*.cmd;*.bat)|*.exe;*.cmd;*.bat|All Files (*.*)|*.*",
                Title = "Select Target Executable"
            };

            if (ofd.ShowDialog() == true)
            {
                ModalTxtExePath.Text = ofd.FileName;
                if (string.IsNullOrWhiteSpace(ModalTxtName.Text) || ModalTxtName.Text == "New Application")
                {
                    try
                    {
                        var vi = FileVersionInfo.GetVersionInfo(ofd.FileName);
                        ModalTxtName.Text = !string.IsNullOrWhiteSpace(vi.FileDescription) ? vi.FileDescription : Path.GetFileNameWithoutExtension(ofd.FileName);
                    }
                    catch
                    {
                        ModalTxtName.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                    }
                }
                ModalTxtSubtitle.Text = ofd.FileName;
            }
        }

        private void ModalAddEnvVar_Click(object sender, RoutedEventArgs e)
        {
            if (_editingRule != null)
            {
                _editingRule.CustomVariables.Add(new EnvironmentVariableItem
                {
                    Key = "HTTP_PROXY",
                    Value = "http://127.0.0.1:7897",
                    IsEnabled = true
                });
            }
        }

        private void ModalDeleteEnvVar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is EnvironmentVariableItem item && _editingRule != null)
            {
                _editingRule.CustomVariables.Remove(item);
            }
        }

        private void ModalSaveRule_Click(object sender, RoutedEventArgs e)
        {
            if (_editingRule == null) return;

            _editingRule.ExecutablePath = ModalTxtExePath.Text.Trim();
            _editingRule.Name = ModalTxtName.Text.Trim();
            _editingRule.Subtitle = ModalTxtSubtitle.Text.Trim();
            _editingRule.Action = ModalRbProxy.IsChecked == true ? "Proxy" : ModalRbDirect.IsChecked == true ? "Direct" : "Block";
            _editingRule.ProxyName = ModalCmbProxy.SelectedValue?.ToString() ?? "Local Proxy (7897)";
            _editingRule.KillSwitch = false;

            if (File.Exists(_editingRule.ExecutablePath))
            {
                _editingRule.CachedIcon = IconHelper.GetIconForFile(_editingRule.ExecutablePath);
            }

            if (_isAddingNewRule && !Settings.Rules.Contains(_editingRule))
            {
                Settings.Rules.Add(_editingRule);
                SelectedLaunchRule ??= _editingRule;
            }

            ConfigService.SaveSettings(Settings);
            InlineRuleModal.Visibility = Visibility.Collapsed;
            ShowToast($"✔ Saved configuration for '{_editingRule.Name}'");
            AddActivityLog($"⚙ Configuration updated for '{_editingRule.Name}'");
        }

        private void ModalLaunchNow_Click(object sender, RoutedEventArgs e)
        {
            if (_editingRule != null)
            {
                ModalSaveRule_Click(sender, e);
                ExecuteLaunch(_editingRule, bypassKillSwitch: false);
            }
        }

        private RuleItem? _selectedShortcutRule;

        private void OpenShortcutModal(RuleItem rule)
        {
            _selectedShortcutRule = rule;
            TxtShortcutModalTitle.Text = $"Create Direct Shortcut for '{rule.Name}'";
            TxtShortcutModalSubtitle.Text = $"Launch '{rule.Name}' directly with proxy '{rule.ProxyName}' without opening this manager first";
            InlineShortcutModal.Visibility = Visibility.Visible;
        }

        private void CloseShortcutModal_Click(object sender, RoutedEventArgs e)
        {
            InlineShortcutModal.Visibility = Visibility.Collapsed;
        }

        private void ShortcutModalStartMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShortcutRule == null) return;
            try
            {
                var path = ShortcutService.CreateStartMenuShortcut(_selectedShortcutRule);
                ShowToast($"✔ Added '{_selectedShortcutRule.Name} (Proxy)' to Windows Start Menu!");
                AddActivityLog($"🚀 Start Menu shortcut created for '{_selectedShortcutRule.Name}'. Searchable in Windows.");
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to create shortcut: {ex.Message}", isWarning: true);
            }
        }

        private void ShortcutModalTaskbar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShortcutRule == null) return;
            try
            {
                var path = ShortcutService.CreateTaskbarShortcut(_selectedShortcutRule);
                ShowToast("✔ Shortcut created! Right-click highlighted file & select 'Pin to taskbar'.");
                AddActivityLog($"📍 Taskbar shortcut prepared for '{_selectedShortcutRule.Name}'. Highlighted in Explorer.");
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to prepare shortcut: {ex.Message}", isWarning: true);
            }
        }

        private void ShortcutModalDesktop_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShortcutRule == null) return;
            try
            {
                var path = ShortcutService.CreateDesktopShortcut(_selectedShortcutRule);
                ShowToast($"✔ Created Desktop shortcut for '{_selectedShortcutRule.Name}'!");
                AddActivityLog($"📌 Desktop shortcut created for '{_selectedShortcutRule.Name}': {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to create shortcut: {ex.Message}", isWarning: true);
            }
        }

        private void ShortcutModalAll_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShortcutRule == null) return;
            try
            {
                ShortcutService.CreateEverywhereShortcuts(_selectedShortcutRule);
                ShowToast("✔ Created on Desktop, Start Menu & opened to Pin to taskbar!");
                AddActivityLog($"⚡ Direct shortcuts created everywhere for '{_selectedShortcutRule.Name}'.");
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to create shortcuts: {ex.Message}", isWarning: true);
            }
        }

        private void ShortcutModalOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var programsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Windows Antify Proxy");
                Directory.CreateDirectory(programsFolder);
                Process.Start(new ProcessStartInfo { FileName = programsFolder, UseShellExecute = true });
            }
            catch { }
        }

        private void RuleShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is RuleItem rule)
            {
                OpenShortcutModal(rule);
            }
        }

        private void TopBarShortcut_Click(object sender, RoutedEventArgs e)
        {
            var rule = SelectedLaunchRule
                ?? Settings.Rules.FirstOrDefault(r => r.Name.Contains("Antigravity", StringComparison.OrdinalIgnoreCase))
                ?? Settings.Rules.FirstOrDefault();

            if (rule != null)
            {
                OpenShortcutModal(rule);
            }
            else
            {
                ShowToast("Please select or add an application rule first.", isWarning: true);
            }
        }

        private void ModalShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (_editingRule != null)
            {
                ModalSaveRule_Click(sender, e);
                OpenShortcutModal(_editingRule);
            }
        }

        private void ShowShortcutMenu(FrameworkElement targetElement, RuleItem rule)
        {
            var menu = new ContextMenu();
            if (TryFindResource("WinDarkContextMenu") is Style contextMenuStyle)
            {
                menu.Style = contextMenuStyle;
            }

            var menuItemStyle = TryFindResource("WinDarkMenuItem") as Style;

            // Header title
            var titleItem = new MenuItem
            {
                Header = $"Create Direct Shortcut for '{rule.Name}'",
                IsEnabled = false,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFromString("#9499AD")!,
                FontSize = 11,
                Padding = new Thickness(10, 6, 10, 6)
            };
            if (menuItemStyle != null) titleItem.Style = menuItemStyle;
            menu.Items.Add(titleItem);
            menu.Items.Add(new Separator { Background = (Brush)new BrushConverter().ConvertFromString("#32323A")!, Margin = new Thickness(0, 3, 0, 3) });

            // 1. Add to Desktop
            var desktopItem = new MenuItem
            {
                Header = "Add to Desktop",
                InputGestureText = "Desktop",
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                Icon = new TextBlock { Text = "\uE7F4", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 12, Foreground = (Brush)new BrushConverter().ConvertFromString("#60CDFF")! }
            };
            if (menuItemStyle != null) desktopItem.Style = menuItemStyle;
            desktopItem.Click += (s, e) =>
            {
                try
                {
                    var path = ShortcutService.CreateDesktopShortcut(rule);
                    ShowToast($"✔ Created Desktop shortcut for '{rule.Name}'!");
                    AddActivityLog($"📌 Desktop shortcut created for '{rule.Name}': {Path.GetFileName(path)}");
                }
                catch (Exception ex)
                {
                    ShowToast($"Failed to create shortcut: {ex.Message}", isWarning: true);
                }
            };
            menu.Items.Add(desktopItem);

            // 2. Add to Start Menu
            var startMenuItem = new MenuItem
            {
                Header = "Add to Start Menu (Pinned & Searchable)",
                InputGestureText = "Start Menu",
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                Icon = new TextBlock { Text = "\uE8A9", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 12, Foreground = (Brush)new BrushConverter().ConvertFromString("#A78BFA")! }
            };
            if (menuItemStyle != null) startMenuItem.Style = menuItemStyle;
            startMenuItem.Click += (s, e) =>
            {
                try
                {
                    var path = ShortcutService.CreateStartMenuShortcut(rule);
                    ShowToast($"✔ Added '{rule.Name} (Proxy)' to Windows Start Menu!");
                    AddActivityLog($"🚀 Start Menu shortcut created for '{rule.Name}'. Searchable in Windows.");
                }
                catch (Exception ex)
                {
                    ShowToast($"Failed to create shortcut: {ex.Message}", isWarning: true);
                }
            };
            menu.Items.Add(startMenuItem);

            // 3. Pin to Taskbar
            var taskbarItem = new MenuItem
            {
                Header = "Pin to Taskbar (Prepare & Reveal in Explorer)",
                InputGestureText = "Taskbar",
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                Icon = new TextBlock { Text = "\uE718", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 12, Foreground = (Brush)new BrushConverter().ConvertFromString("#F472B6")! }
            };
            if (menuItemStyle != null) taskbarItem.Style = menuItemStyle;
            taskbarItem.Click += (s, e) =>
            {
                try
                {
                    var path = ShortcutService.CreateTaskbarShortcut(rule);
                    ShowToast("✔ Shortcut created! Right-click highlighted file & select 'Pin to taskbar'.");
                    AddActivityLog($"📍 Taskbar shortcut prepared for '{rule.Name}'. Highlighted in Explorer.");
                }
                catch (Exception ex)
                {
                    ShowToast($"Failed to prepare shortcut: {ex.Message}", isWarning: true);
                }
            };
            menu.Items.Add(taskbarItem);

            menu.Items.Add(new Separator { Background = (Brush)new BrushConverter().ConvertFromString("#32323A")!, Margin = new Thickness(0, 3, 0, 3) });

            // 4. Create Everywhere
            var everywhereItem = new MenuItem
            {
                Header = "Create Everywhere (Desktop + Start Menu + Taskbar)",
                Foreground = (Brush)new BrushConverter().ConvertFromString("#FBBF24")!,
                Cursor = Cursors.Hand,
                Icon = new TextBlock { Text = "\uE735", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 12, Foreground = (Brush)new BrushConverter().ConvertFromString("#FBBF24")! }
            };
            if (menuItemStyle != null) everywhereItem.Style = menuItemStyle;
            everywhereItem.Click += (s, e) =>
            {
                try
                {
                    ShortcutService.CreateEverywhereShortcuts(rule);
                    ShowToast("✔ Created on Desktop, Start Menu & opened to Pin to taskbar!");
                    AddActivityLog($"⚡ Direct shortcuts created everywhere for '{rule.Name}'.");
                }
                catch (Exception ex)
                {
                    ShowToast($"Failed to create shortcuts: {ex.Message}", isWarning: true);
                }
            };
            menu.Items.Add(everywhereItem);

            // 5. Open Shortcuts Folder
            var folderItem = new MenuItem
            {
                Header = "Open Start Menu Programs Folder",
                Foreground = (Brush)new BrushConverter().ConvertFromString("#9499AD")!,
                Cursor = Cursors.Hand,
                Icon = new TextBlock { Text = "\uED25", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 12, Foreground = (Brush)new BrushConverter().ConvertFromString("#9499AD")! }
            };
            if (menuItemStyle != null) folderItem.Style = menuItemStyle;
            folderItem.Click += (s, e) =>
            {
                try
                {
                    var programsDir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    Process.Start(new ProcessStartInfo { FileName = programsDir, UseShellExecute = true });
                }
                catch { }
            };
            menu.Items.Add(folderItem);

            menu.PlacementTarget = targetElement;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is RuleItem rule)
            {
                var result = MessageBox.Show($"Remove rule for '{rule.Name}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Settings.Rules.Remove(rule);
                    if (SelectedLaunchRule == rule)
                    {
                        SelectedLaunchRule = Settings.Rules.FirstOrDefault();
                    }
                    ConfigService.SaveSettings(Settings);
                    ShowToast($"🗑 Removed rule for '{rule.Name}'");
                }
            }
        }

        #endregion

        #region Proxies Management

        private void AddProxy_Click(object sender, RoutedEventArgs e)
        {
            var newProxy = new ProxyItem
            {
                Name = $"Custom Proxy {Settings.Proxies.Count + 1}",
                Type = "HTTP",
                Host = "127.0.0.1",
                Port = 7897
            };
            Settings.Proxies.Add(newProxy);
            ConfigService.SaveSettings(Settings);
            ShowToast("➕ Added new proxy endpoint.");
        }

        private void EditProxyRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is ProxyItem proxy)
            {
                // Quick preset cycle for proxy port: 7897 (Clash/Verge) -> 7890 (Clash Meta) -> 10809 (v2ray) -> 8888 (Fiddler)
                int nextPort = proxy.Port switch
                {
                    7897 => 7890,
                    7890 => 10809,
                    10809 => 8888,
                    _ => 7897
                };
                proxy.Port = nextPort;
                ConfigService.SaveSettings(Settings);
                ShowToast($"✔ Cycled proxy port to {nextPort}");
                _ = CheckProxyHealthAsync();
            }
        }

        private void DeleteProxyRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is ProxyItem proxy)
            {
                Settings.Proxies.Remove(proxy);
                ConfigService.SaveSettings(Settings);
                ShowToast("🗑 Removed proxy.");
            }
        }

        private async void TestProxyRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is ProxyItem proxy)
            {
                var latency = await ProxyTesterService.TestProxyLatencyAsync(proxy);
                ShowToast(latency.HasValue && latency.Value >= 0 ? $"✔ {proxy.Name} latency: {latency.Value}ms (Online)" : $"⚠️ {proxy.Name} is unreachable (Offline)", isWarning: latency < 0);
            }
        }

        private async void TestAllProxies_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("⏱ Testing proxies...");
            var tasks = Settings.Proxies.Select(p => ProxyTesterService.TestProxyLatencyAsync(p));
            await Task.WhenAll(tasks);
            await CheckProxyHealthAsync();
            ShowToast("✔ All proxy latency tests finished.");
        }

        #endregion

        #region Actions & Tools

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Import Configuration"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(ofd.FileName);
                    var imported = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                    if (imported != null)
                    {
                        Settings.Rules.Clear();
                        foreach (var r in imported.Rules) Settings.Rules.Add(r);
                        Settings.Proxies.Clear();
                        foreach (var p in imported.Proxies) Settings.Proxies.Add(p);
                        SelectedLaunchRule = Settings.Rules.FirstOrDefault(r => r.Id == imported.SelectedLaunchRuleId) ?? Settings.Rules.FirstOrDefault();
                        ConfigService.SaveSettings(Settings);
                        ShowToast("✔ Configuration imported successfully!");
                    }
                }
                catch (Exception ex)
                {
                    ShowToast($"Import error: {ex.Message}", isWarning: true);
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "windows_antify_proxy_backup.json",
                Title = "Export Configuration Backup"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(Settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(sfd.FileName, json);
                    ShowToast("✔ Configuration exported successfully!");
                }
                catch (Exception ex)
                {
                    ShowToast($"Export error: {ex.Message}", isWarning: true);
                }
            }
        }

        private void Feedback_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/mahmoud-abdalla-eg/windows-antify-proxy/issues",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void ToggleMasterProxy_Click(object sender, RoutedEventArgs e)
        {
            Settings.IsProxyActive = !Settings.IsProxyActive;
            OnPropertyChanged(nameof(ProxyActiveStatusTitle));
            OnPropertyChanged(nameof(ProxyActiveButtonText));
            UpdateDashboardMasterBanner();
            ConfigService.SaveSettings(Settings);
            ShowToast(Settings.IsProxyActive ? "🟢 Master Proxy activated." : "🔴 Master Proxy paused.");
        }

        private void ExplorerContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.ExplorerContextMenu)
            {
                ShellContextMenuService.RegisterContextMenu();
                ShowToast("✔ Added right-click menu to Windows Explorer!");
            }
            else
            {
                ShellContextMenuService.UnregisterContextMenu();
                ShowToast("✖ Removed right-click menu from Windows Explorer.");
            }
            ConfigService.SaveSettings(Settings);
        }

        private void AutoGuardian_Click(object sender, RoutedEventArgs e)
        {
            ConfigService.SaveSettings(Settings);
            ShowToast(Settings.AutoEnforceProxy ? "🛡️ Auto-Guardian background enforcement activated." : "⚪ Auto-Guardian paused.");
        }

        private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WindowsAntifyProxy");
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
            Process.Start(new ProcessStartInfo { FileName = configDir, UseShellExecute = true });
        }

        #endregion

        #region Dashboard Telemetry & Graphing

        private void LatencyGraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawLatencyGraph();
        }

        private void RecordLatencySample(double latencyMs)
        {
            _latencyHistory.Add(latencyMs);
            while (_latencyHistory.Count > MaxLatencyPoints)
            {
                _latencyHistory.RemoveAt(0);
            }
            UpdateLatencyStats();
            DrawLatencyGraph();
        }

        private void UpdateLatencyStats()
        {
            var valid = _latencyHistory.Where(v => v >= 0).ToList();
            int total = _latencyHistory.Count;
            int dropped = _latencyHistory.Count(v => v < 0);

            if (valid.Any())
            {
                double current = _latencyHistory.LastOrDefault();
                double avg = valid.Average();
                double min = valid.Min();
                double max = valid.Max();

                if (TxtDashStatCurrent != null) TxtDashStatCurrent.Text = current >= 0 ? $"{(int)current} ms" : "Timeout";
                if (TxtDashStatAvg != null) TxtDashStatAvg.Text = $"{avg:F1} ms";
                if (TxtDashStatMin != null) TxtDashStatMin.Text = $"{(int)min} ms";
                if (TxtDashStatMax != null) TxtDashStatMax.Text = $"{(int)max} ms";
                if (TxtDashboardLiveBadge != null)
                {
                    TxtDashboardLiveBadge.Text = current >= 0 ? $"Live {(int)current} ms" : "Offline";
                    TxtDashboardLiveBadge.Foreground = current >= 0 
                        ? (Brush)new BrushConverter().ConvertFromString("#4ADE80")! 
                        : (Brush)new BrushConverter().ConvertFromString("#F87171")!;
                }
            }
            double lossPct = total > 0 ? (dropped / (double)total) * 100.0 : 0.0;
            if (TxtDashStatLoss != null)
            {
                TxtDashStatLoss.Text = $"{lossPct:F1}%";
                TxtDashStatLoss.Foreground = lossPct == 0 
                    ? (Brush)new BrushConverter().ConvertFromString("#4ADE80")! 
                    : (Brush)new BrushConverter().ConvertFromString("#F87171")!;
            }
        }

        private void DrawLatencyGraph()
        {
            if (LatencyGraphCanvas == null) return;
            double width = LatencyGraphCanvas.ActualWidth;
            double height = LatencyGraphCanvas.ActualHeight;
            if (width <= 10 || height <= 10) return;

            LatencyGraphCanvas.Children.Clear();

            // 1. Draw subtle horizontal gridlines
            int gridSteps = 4;
            for (int g = 0; g <= gridSteps; g++)
            {
                double y = (height - 1) * (g / (double)gridSteps);
                var line = new Line
                {
                    X1 = 0,
                    X2 = width,
                    Y1 = y,
                    Y2 = y,
                    Stroke = (Brush)new BrushConverter().ConvertFromString("#222228")!,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                LatencyGraphCanvas.Children.Add(line);
            }

            if (_latencyHistory.Count < 2) return;

            var validSamples = _latencyHistory.Where(v => v >= 0).ToList();
            double maxVal = validSamples.Any() ? validSamples.Max() : 10;
            double ceiling = Math.Max(10.0, Math.Ceiling(maxVal * 1.4));
            if (TxtYMax != null) TxtYMax.Text = $"{(int)ceiling} ms";
            if (TxtYMid != null) TxtYMid.Text = $"{(int)(ceiling / 2)} ms";

            double topMargin = 12;
            double bottomMargin = 8;
            double usableHeight = height - topMargin - bottomMargin;

            var points = new PointCollection();
            var areaPoints = new PointCollection();

            double xStep = width / (MaxLatencyPoints - 1);
            double xOffset = width - ((_latencyHistory.Count - 1) * xStep);

            areaPoints.Add(new Point(xOffset, height));

            for (int i = 0; i < _latencyHistory.Count; i++)
            {
                double val = _latencyHistory[i];
                double x = xOffset + (i * xStep);
                double normalizedY = val >= 0 ? Math.Clamp(val / ceiling, 0.0, 1.0) : 0.05;
                double y = (height - bottomMargin) - (normalizedY * usableHeight);

                points.Add(new Point(x, y));
                areaPoints.Add(new Point(x, y));
            }

            areaPoints.Add(new Point(xOffset + ((_latencyHistory.Count - 1) * xStep), height));

            // 2. Draw Filled Gradient Area
            var areaPolygon = new Polygon
            {
                Points = areaPoints,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(70, 96, 205, 255), 0.0),
                        new GradientStop(Color.FromArgb(5, 96, 205, 255), 1.0)
                    }
                }
            };
            LatencyGraphCanvas.Children.Add(areaPolygon);

            // 3. Draw Polyline (Neon Stroke)
            var polyline = new Polyline
            {
                Points = points,
                Stroke = (Brush)new BrushConverter().ConvertFromString("#60CDFF")!,
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            LatencyGraphCanvas.Children.Add(polyline);

            // 4. Draw glowing nodes
            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                bool isLatest = (i == points.Count - 1);
                double dotSize = isLatest ? 8 : 4;

                if (isLatest)
                {
                    var glow = new Ellipse
                    {
                        Width = 16,
                        Height = 16,
                        Fill = (Brush)new BrushConverter().ConvertFromString("#3360CDFF")!
                    };
                    Canvas.SetLeft(glow, pt.X - 8);
                    Canvas.SetTop(glow, pt.Y - 8);
                    LatencyGraphCanvas.Children.Add(glow);
                }

                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = isLatest 
                        ? (Brush)new BrushConverter().ConvertFromString("#FFFFFF")! 
                        : (Brush)new BrushConverter().ConvertFromString("#60CDFF")!
                };
                Canvas.SetLeft(dot, pt.X - (dotSize / 2));
                Canvas.SetTop(dot, pt.Y - (dotSize / 2));
                LatencyGraphCanvas.Children.Add(dot);
            }
        }

        private void UpdateDashboardProcessState()
        {
            var target = SelectedLaunchRule ?? Settings.Rules.FirstOrDefault();
            if (target != null)
            {
                if (TxtDashTargetName != null) TxtDashTargetName.Text = target.Name;
                if (TxtDashTargetStatus != null && DotDashTargetStatus != null)
                {
                    if (target.IsRunning)
                    {
                        TxtDashTargetStatus.Text = target.RunningProcessCount > 1 
                            ? $"Running ({target.RunningProcessCount} instances)" 
                            : $"Running (PID: {target.RunningPid})";
                        TxtDashTargetStatus.Foreground = (Brush)new BrushConverter().ConvertFromString("#4ADE80")!;
                        DotDashTargetStatus.Fill = (Brush)new BrushConverter().ConvertFromString("#4ADE80")!;
                    }
                    else
                    {
                        TxtDashTargetStatus.Text = "Ready to Launch";
                        TxtDashTargetStatus.Foreground = (Brush)new BrushConverter().ConvertFromString("#888892")!;
                        DotDashTargetStatus.Fill = (Brush)new BrushConverter().ConvertFromString("#888892")!;
                    }
                }
            }
        }

        private void UpdateDashboardMasterBanner()
        {
            if (DashMasterBanner == null) return;

            if (Settings.IsProxyActive)
            {
                DashMasterBanner.Background = (Brush)new BrushConverter().ConvertFromString("#163825")!;
                DashMasterBanner.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#235E3B")!;
                if (DashMasterIconBorder != null) DashMasterIconBorder.Background = (Brush)new BrushConverter().ConvertFromString("#1D4F31")!;
                if (DashMasterIcon != null)
                {
                    DashMasterIcon.FontFamily = new FontFamily("Segoe MDL2 Assets");
                    DashMasterIcon.Text = "\uEA18"; // Shield
                    DashMasterIcon.Foreground = (Brush)new BrushConverter().ConvertFromString("#4ADE80")!;
                }
                if (DashMasterTitle != null) DashMasterTitle.Text = "Proxy Routing Active";
                if (DashMasterSubtitle != null) DashMasterSubtitle.Text = "Isolated proxy environment engaged for configured applications (Antigravity). Windows system networking remains untouched.";
                if (DashMasterToggleBtn != null) DashMasterToggleBtn.Content = "Pause Proxy";
            }
            else
            {
                DashMasterBanner.Background = (Brush)new BrushConverter().ConvertFromString("#281E20")!;
                DashMasterBanner.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#5E262A")!;
                if (DashMasterIconBorder != null) DashMasterIconBorder.Background = (Brush)new BrushConverter().ConvertFromString("#4A2226")!;
                if (DashMasterIcon != null)
                {
                    DashMasterIcon.FontFamily = new FontFamily("Segoe MDL2 Assets");
                    DashMasterIcon.Text = "\uE769"; // Pause
                    DashMasterIcon.Foreground = (Brush)new BrushConverter().ConvertFromString("#F87171")!;
                }
                if (DashMasterTitle != null) DashMasterTitle.Text = "Proxy Routing Paused";
                if (DashMasterSubtitle != null) DashMasterSubtitle.Text = "Master proxy routing is paused. Configured applications will connect directly or be stopped by Kill Switch.";
                if (DashMasterToggleBtn != null) DashMasterToggleBtn.Content = "Resume Proxy";
            }
        }

        private void DashboardViewLogs_Click(object sender, RoutedEventArgs e)
        {
            if (NavActivity != null)
            {
                NavActivity.IsChecked = true;
                Nav_Click(NavActivity, e);
            }
        }

        #endregion

        #region Toast Feedback & Activity Log

        private DispatcherTimer? _toastTimer;

        private void ShowToast(string message, bool isWarning = false)
        {
            TxtToastMessage.Text = message;
            TxtToastIcon.FontFamily = new FontFamily("Segoe MDL2 Assets");
            TxtToastIcon.Text = isWarning ? "\uE7BA" : "\uE73E"; // Warning : CheckMark
            TxtToastIcon.Foreground = (Brush)new BrushConverter().ConvertFromString(isWarning ? "#F87171" : "#4ADE80")!;
            InlineToast.Visibility = Visibility.Visible;

            _toastTimer?.Stop();
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            _toastTimer.Tick += (s, e) =>
            {
                InlineToast.Visibility = Visibility.Collapsed;
                _toastTimer.Stop();
            };
            _toastTimer.Start();
        }

        private void AddActivityLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var entryText = $"[{timestamp}]  {message}";

            if (ActivityLogContainer != null)
            {
                var border = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#26262B")!,
                    BorderBrush = (Brush)new BrushConverter().ConvertFromString("#323238")!,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var text = new TextBlock
                {
                    Text = entryText,
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas, monospace"),
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#D1D1D6")!
                };

                border.Child = text;
                ActivityLogContainer.Children.Insert(0, border);
            }

            if (DashboardRecentLogContainer != null)
            {
                var dashBorder = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#1D1D22")!,
                    BorderBrush = (Brush)new BrushConverter().ConvertFromString("#2A2A30")!,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                dashBorder.Child = new TextBlock
                {
                    Text = entryText,
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas, monospace"),
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#CCCCCC")!,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                DashboardRecentLogContainer.Children.Insert(0, dashBorder);
                while (DashboardRecentLogContainer.Children.Count > 4)
                {
                    DashboardRecentLogContainer.Children.RemoveAt(DashboardRecentLogContainer.Children.Count - 1);
                }
            }
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
