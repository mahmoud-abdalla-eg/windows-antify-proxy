using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsAntifyProxy.Models
{
    public class ConfigurationProfile : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name = "Work";
        private bool _isActive = true;
        private string _defaultAction = "Direct";
        private bool _wifiAutoSwitch;
        private string? _wifiSsid;
        private List<string> _ruleIds = new();

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public string DefaultAction
        {
            get => _defaultAction;
            set { _defaultAction = value; OnPropertyChanged(); }
        }

        public bool WifiAutoSwitch
        {
            get => _wifiAutoSwitch;
            set { _wifiAutoSwitch = value; OnPropertyChanged(); }
        }

        public string? WifiSsid
        {
            get => _wifiSsid;
            set { _wifiSsid = value; OnPropertyChanged(); }
        }

        public List<string> RuleIds
        {
            get => _ruleIds;
            set { _ruleIds = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

