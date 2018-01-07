using System;
using System.Net;
using Windows.Storage;

namespace Moonlight
{
    public class Configuration
    {
        private static readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        public static Guid UniqueUuid
        {
            get
            {
                if (_localSettings.Values.ContainsKey("NvHttp.UniqueUuid"))
                {
                    return (Guid)_localSettings.Values["NvHttp.UniqueUuid"];
                }
                else
                {
                    Guid newValue = Guid.NewGuid();
                    _localSettings.Values["NvHttp.UniqueUuid"] = newValue;
                    return newValue;
                }
            }
        }

        public static bool LocalAudio
        {
            get
            {
                if (_localSettings.Values.ContainsKey("LocalAudio"))
                {
                    return (bool)_localSettings.Values["LocalAudio"];
                }
                else
                {
                    _localSettings.Values["LocalAudio"] = false;
                    return false;
                }
            }
            set
            {
                _localSettings.Values["LocalAudio"] = value;
            }
        }

        public static string DeviceName
        {
            get
            {
                if (_localSettings.Values.ContainsKey("DeviceName"))
                {
                    return (string)_localSettings.Values["DeviceName"];
                }
                else
                {
                    string newValue = Dns.GetHostName();
                    _localSettings.Values["DeviceName"] = newValue;
                    return newValue;
                }
            }
        }
    }
}
