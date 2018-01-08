using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Moonlight
{
    public class ApplicationsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<NvApplication> Applications { get; } = new ObservableCollection<NvApplication>();
        private NvStreamDevice _streamDevice;
        public NvStreamDevice StreamDevice
        {
            get { return _streamDevice; }
            set
            {
                _streamDevice = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StreamDevice"));
            }
        }
        private Boolean _isSearching = false;
        public Boolean IsSearching
        {
            get { return _isSearching; }
            set
            {
                _isSearching = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSearching"));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}