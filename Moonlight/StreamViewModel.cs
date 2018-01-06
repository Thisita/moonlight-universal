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
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsConnecting"));
                }
            }
        }
        private string _connectionStatus;
        public string ConnectionStatus
        {
            get { return _connectionStatus; }
            set
            {
                _connectionStatus = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ConnectionStatus"));
                }
            }
        }
        private NvGameSession _gameSession;
        public NvGameSession GameSession
        {
            get { return _gameSession; }
            set
            {
                _gameSession = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("GameSession"));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}