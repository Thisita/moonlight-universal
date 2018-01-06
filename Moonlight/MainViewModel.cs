using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Moonlight
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<NvStreamDevice> StreamDevices { get; } = new ObservableCollection<NvStreamDevice>();
        private Boolean _isSearching = false;
        public Boolean IsSearching
        {
            get { return _isSearching; }
            set
            {
                _isSearching = value;
                if(PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsSearching"));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}