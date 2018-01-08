using System;
using System.ComponentModel;

namespace Moonlight
{
    public class StreamViewModel : INotifyPropertyChanged
    {
        private Boolean _isConnecting = true;
        public Boolean IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                _isConnecting = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsConnecting"));
            }
        }
        private string _connectionStatus;
        public string ConnectionStatus
        {
            get { return _connectionStatus; }
            set
            {
                _connectionStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnectionStatus"));
            }
        }
        private NvLaunch _launch;
        public NvLaunch Launch
        {
            get { return _launch; }
            set
            {
                _launch = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Launch"));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}