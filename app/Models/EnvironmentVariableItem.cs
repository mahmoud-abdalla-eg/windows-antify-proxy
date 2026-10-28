using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsAntifyProxy.Models
{
    public class EnvironmentVariableItem : INotifyPropertyChanged
    {
        private string _key = string.Empty;
        private string _value = string.Empty;
        private bool _isEnabled = true;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public EnvironmentVariableItem Clone()
        {
            return new EnvironmentVariableItem
            {
                Key = Key,
                Value = Value,
                IsEnabled = IsEnabled
            };
        }
    }
}

