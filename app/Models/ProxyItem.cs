using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace WindowsAntifyProxy.Models
{
    public class ProxyItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name = "Proxy";
        private string _type = "HTTP"; // HTTP, HTTPS, SOCKS5
        private string _host = "127.0.0.1";
        private int _port = 7897;
        private string? _username;
        private string? _password;
        private int? _latencyMs;
        private string _health = "Untested";
        private bool _isPrivate;

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

        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); OnPropertyChanged(nameof(Url)); OnPropertyChanged(nameof(TypeBadgeColor)); OnPropertyChanged(nameof(TypeTextColor)); }
        }

        public string Host
        {
            get => _host;
            set { _host = value; OnPropertyChanged(); OnPropertyChanged(nameof(ServerDisplay)); OnPropertyChanged(nameof(Url)); }
        }

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); OnPropertyChanged(nameof(ServerDisplay)); OnPropertyChanged(nameof(Url)); }
        }

        public string? Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string? Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public int? LatencyMs
        {
            get => _latencyMs;
            set
            {
                _latencyMs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LatencyDisplay));
                OnPropertyChanged(nameof(LatencyColor));
            }
        }

        public string Health
        {
            get => _health;
            set { _health = value; OnPropertyChanged(); }
        }

        public bool IsPrivate
        {
            get => _isPrivate;
            set { _isPrivate = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public string ServerDisplay => $"{Host}:{Port}";

        [JsonIgnore]
        public string Url
        {
            get
            {
                var scheme = Type.ToLower();
                if (scheme == "socks5") scheme = "socks5";
                if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
                {
                    return $"{scheme}://{Username}:{Password}@{Host}:{Port}";
                }
                return $"{scheme}://{Host}:{Port}";
            }
        }

        [JsonIgnore]
        public string LatencyDisplay
        {
            get
            {
                if (!LatencyMs.HasValue) return "—";
                if (LatencyMs.Value < 0) return "Timeout";
                return $"{LatencyMs.Value} ms";
            }
        }

        [JsonIgnore]
        public string LatencyColor
        {
            get
            {
                if (!LatencyMs.HasValue) return "#8E8E93";
                if (LatencyMs.Value < 0) return "#FF3B30";
                if (LatencyMs.Value < 250) return "#34C759";
                if (LatencyMs.Value < 750) return "#FF9500";
                return "#FF3B30";
            }
        }

        [JsonIgnore]
        public string TypeBadgeColor => Type.ToUpper() switch
        {
            "SOCKS5" => "#3B2358",
            "HTTP" => "#1E3A5F",
            "HTTPS" => "#143E2B",
            _ => "#2A2A2A"
        };

        [JsonIgnore]
        public string TypeTextColor => Type.ToUpper() switch
        {
            "SOCKS5" => "#C084FC",
            "HTTP" => "#60CDFF",
            "HTTPS" => "#4ADE80",
            _ => "#A1A1AA"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ProxyItem Clone()
        {
            return new ProxyItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = $"{Name} (Copy)",
                Type = Type,
                Host = Host,
                Port = Port,
                Username = Username,
                Password = Password,
                LatencyMs = LatencyMs,
                Health = Health,
                IsPrivate = IsPrivate
            };
        }
    }
}

