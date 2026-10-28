using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsAntifyProxy.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        private bool _isProxyActive = true;
        private string _activeConfigurationId = string.Empty;
        private bool _runOnStartup;
        private bool _minimizeToTray = true;
        private bool _explorerContextMenu = true;
        private bool _autoEnforceProxy = true;
        private bool _isSidebarCollapsed;
        private string? _selectedLaunchRuleId;
        private ObservableCollection<ProxyItem> _proxies = new();
        private ObservableCollection<RuleItem> _rules = new();
        private ObservableCollection<ConfigurationProfile> _configurations = new();

        public bool IsProxyActive
        {
            get => _isProxyActive;
            set { _isProxyActive = value; OnPropertyChanged(); }
        }

        public string ActiveConfigurationId
        {
            get => _activeConfigurationId;
            set { _activeConfigurationId = value; OnPropertyChanged(); }
        }

        public bool RunOnStartup
        {
            get => _runOnStartup;
            set { _runOnStartup = value; OnPropertyChanged(); }
        }

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set { _minimizeToTray = value; OnPropertyChanged(); }
        }

        public bool ExplorerContextMenu
        {
            get => _explorerContextMenu;
            set { _explorerContextMenu = value; OnPropertyChanged(); }
        }

        public bool AutoEnforceProxy
        {
            get => _autoEnforceProxy;
            set { _autoEnforceProxy = value; OnPropertyChanged(); }
        }

        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set { _isSidebarCollapsed = value; OnPropertyChanged(); }
        }

        public string? SelectedLaunchRuleId
        {
            get => _selectedLaunchRuleId;
            set { _selectedLaunchRuleId = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ProxyItem> Proxies
        {
            get => _proxies;
            set { _proxies = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RuleItem> Rules
        {
            get => _rules;
            set { _rules = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ConfigurationProfile> Configurations
        {
            get => _configurations;
            set { _configurations = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

