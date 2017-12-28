using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Moonlight
{
    public class NvHttp
    {
        public Guid Uuid { get; private set; }
        public HttpClient HttpClient { get; private set; }
        public string DeviceName { get; private set; }
        public Uri BaseAddress { get; private set; }

        public NvHttp(Uri baseAddress)
        {
            HttpBaseProtocolFilter httpBaseProtocolFilter = new HttpBaseProtocolFilter();
            httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            HttpClient = new HttpClient(httpBaseProtocolFilter);
            BaseAddress = baseAddress;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if(localSettings.Values.ContainsKey("NvHttp.Uuid"))
            {
                Uuid = (Guid)localSettings.Values["NvHttp.Uuid"];
            }
            else
            {
                Uuid = Guid.NewGuid();
                localSettings.Values["NvHttp.Uuid"] = Uuid;
            }
            DeviceName = Dns.GetHostName();
        }

        public async Task<NvApplicationList> ApplicationList(Guid uniqueId)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"applist?uniqueid={uniqueId}&uuid={Uuid}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvApplicationList));
                return serializer.Deserialize(reader) as NvApplicationList;
            }
        }

        public async Task SaveBoxArt(Guid uniqueId, int applicationId)
        {
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile tempFile = await tempFolder.CreateFileAsync($"{uniqueId}-{applicationId}-boxart.png", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBufferAsync(tempFile, await HttpClient.GetBufferAsync(BuildUri($"appasset?uniqueid={uniqueId}&uuid={Uuid}&appid={applicationId}&AssetType=2&AssetIdx=0")));
        }

        public async Task<string> Cancel(Guid uniqueId)
        {
            return await HttpClient.GetStringAsync(BuildUri($"cancel?uniqueid={uniqueId}&uuid={Uuid}"));
        }

        public async Task<string> Launch(Guid uniqueId, int appId, string mode, int additionalStates, int sops, string riKey, int riKeyId, int localAudioPlayMode, int surroundAudioInfo)
        {
            return await HttpClient.GetStringAsync(BuildUri($"launch?uniqueid={uniqueId}&uuid={Uuid}&appid={appId}&mode={mode}&additionalStates={additionalStates}&rikey={riKey}&rikeyid={riKeyId}&localAudioPlayMode={localAudioPlayMode}&surroundAudioInfo={surroundAudioInfo}"));
        }

        public async Task<string> Pair(Guid uniqueId, string deviceName, int updateState, string phrase, string salt, string clientCert)
        {
            return await HttpClient.GetStringAsync(BuildUri($"serverinfo?uniqueid={uniqueId}&uuid={Uuid}&devicename={deviceName}&updateState={updateState}&phrase={phrase}&salt={salt}&clientcert={clientCert}"));
        }

        public async Task<string> Resume(Guid uniqueId, string riKey, int riKeyId)
        {
            return await HttpClient.GetStringAsync(BuildUri($"resume?uniqueid={uniqueId}&uuid={Uuid}&rikey={riKey}&rikeyid{riKeyId}"));
        }

        public async Task<NvServerInfo> ServerInfo()
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"serverinfo"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvServerInfo));
                return serializer.Deserialize(reader) as NvServerInfo;
            }
        }

        public async Task<NvServerInfo> ServerInfo(Guid uniqueId)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"serverinfo?uniqueid={uniqueId}&uuid={Uuid}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvServerInfo));
                return serializer.Deserialize(reader) as NvServerInfo;
            }
        }

        public async Task<NvPair> GetServerCert(Guid uniqueId, string salt, string clientCertificate)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&phrase=getservercert&salt={salt}&clientcert={clientCertificate}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<NvPair> GetChallengeResponse(Guid uniqueId, string clientChallenge)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&clientchallenge={clientChallenge}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<NvPair> GetServerChallengeResponse(Guid uniqueId, string serverChallengeResponse)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&serverchallengeresp={serverChallengeResponse}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<NvPair> GetClientPairingSecret(Guid uniqueId, string clientPairingSecret)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&clientpairingsecret={clientPairingSecret}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<NvPair> GetPairChallenge(Guid uniqueId)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&phrase=pairchallenge"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<string> Unpair(Guid uniqueId)
        {
            return await HttpClient.GetStringAsync(BuildUri($"unpair?uniqueid={uniqueId}&uuid={Uuid}"));
        }

        private Uri BuildUri(string suffix)
        {
            return new Uri(BaseAddress + suffix);
        }
    }
}
