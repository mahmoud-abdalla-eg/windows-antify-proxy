using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace WindowsAntifyProxy.Models
{
    public class RuleItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N");
        private bool _isEnabled = true;
        private string _name = "App";
        private string _subtitle = "app.exe";
        private string _executablePath = string.Empty;
        private string? _arguments;
        private string? _workingDirectory;
        private string _type = "App"; // App, Path
        private string _action = "Proxy"; // Proxy, Direct, Block
        private string _proxyName = "Proxy A";
        private string? _proxyId;
        private bool _killSwitch;
        private string _lastMatch = "—";
        private bool _runAsAdmin;
        private string? _iconBase64;
        private ObservableCollection<EnvironmentVariableItem> _customVariables = new();

        // Live process tracking
        private bool _isRunning;
        private int? _runningPid;
        private int _runningProcessCount;

        [JsonIgnore]
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RunningStatusText));
                OnPropertyChanged(nameof(RunningStatusColor));
                OnPropertyChanged(nameof(LaunchButtonText));
            }
        }

        [JsonIgnore]
        public int? RunningPid
        {
            get => _runningPid;
            set
            {
                _runningPid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RunningStatusText));
            }
        }

        [JsonIgnore]
        public int RunningProcessCount
        {
            get => _runningProcessCount;
            set
            {
                _runningProcessCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RunningStatusText));
            }
        }

        [JsonIgnore]
        public string RunningStatusText => _isRunning
            ? (_runningProcessCount > 1 ? $"● Running ({_runningProcessCount} procs)" : $"● Running (PID {_runningPid})")
            : "○ Stopped";

        [JsonIgnore]
        public string RunningStatusColor => _isRunning ? "#4ADE80" : "#6E6E78";

        [JsonIgnore]
        public string LaunchButtonText => _isRunning ? "Launch More" : "Launch Now";

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Subtitle
        {
            get => _subtitle;
            set { _subtitle = value; OnPropertyChanged(); }
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set { _executablePath = value; OnPropertyChanged(); }
        }

        public string? Arguments
        {
            get => _arguments;
            set { _arguments = value; OnPropertyChanged(); }
        }

        public string? WorkingDirectory
        {
            get => _workingDirectory;
            set { _workingDirectory = value; OnPropertyChanged(); }
        }

        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public string Action
        {
            get => _action;
            set
            {
                _action = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActionColor));
                OnPropertyChanged(nameof(ActionText));
            }
        }

        public string ProxyName
        {
            get => _proxyName;
            set { _proxyName = value; OnPropertyChanged(); }
        }

        public string? ProxyId
        {
            get => _proxyId;
            set { _proxyId = value; OnPropertyChanged(); }
        }

        public bool KillSwitch
        {
            get => _killSwitch;
            set { _killSwitch = value; OnPropertyChanged(); }
        }

        public string LastMatch
        {
            get => _lastMatch;
            set { _lastMatch = value; OnPropertyChanged(); }
        }

        public bool RunAsAdmin
        {
            get => _runAsAdmin;
            set { _runAsAdmin = value; OnPropertyChanged(); }
        }

        public string? IconBase64
        {
            get => _iconBase64;
            set { _iconBase64 = value; OnPropertyChanged(); }
        }

        public ObservableCollection<EnvironmentVariableItem> CustomVariables
        {
            get => _customVariables;
            set { _customVariables = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public ImageSource? CachedIcon { get; set; }

        [JsonIgnore]
        public string ActionColor => Action.ToLower() switch
        {
            "proxy" => "#60CDFF",
            "direct" => "#4ADE80",
            "block" => "#F87171",
            _ => "#9E9E9E"
        };

        [JsonIgnore]
        public string ActionText => Action switch
        {
            "Proxy" => "Proxy",
            "Direct" => "Direct",
            "Block" => "Block",
            _ => Action
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public RuleItem Clone()
        {
            var clone = new RuleItem
            {
                Id = Guid.NewGuid().ToString("N"),
                IsEnabled = IsEnabled,
                Name = $"{Name} (Copy)",
                Subtitle = Subtitle,
                ExecutablePath = ExecutablePath,
                Arguments = Arguments,
                WorkingDirectory = WorkingDirectory,
                Type = Type,
                Action = Action,
                ProxyName = ProxyName,
                ProxyId = ProxyId,
                KillSwitch = KillSwitch,
                LastMatch = "—",
                RunAsAdmin = RunAsAdmin,
                IconBase64 = IconBase64,
                CachedIcon = CachedIcon
            };

            foreach (var variable in CustomVariables)
            {
                clone.CustomVariables.Add(variable.Clone());
            }

            return clone;
        }

        public override string ToString() => Name;
    }
}

