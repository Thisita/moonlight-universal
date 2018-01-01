using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Guid UniqueUuid { get; private set; }
        public HttpClient HttpClient { get; private set; }
        public string DeviceName { get; private set; }
        public Uri BaseAddress { get; private set; }

        public NvHttp(Uri baseAddress)
        {
            BaseAddress = baseAddress;
        }

        public async Task Initialize(CryptoProvider cryptoProvider)
        {
            HttpBaseProtocolFilter httpBaseProtocolFilter = new HttpBaseProtocolFilter();
            httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            httpBaseProtocolFilter.ClientCertificate = await cryptoProvider.GetClientSslCertificate();
            httpBaseProtocolFilter.AutomaticDecompression = true;
            HttpClient = new HttpClient(httpBaseProtocolFilter);

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("NvHttp.UniqueUuid"))
            {
                UniqueUuid = (Guid)localSettings.Values["NvHttp.UniqueUuid"];
            }
            else
            {
                UniqueUuid = Guid.NewGuid();
                localSettings.Values["NvHttp.UniqueUuid"] = UniqueUuid;
            }
            DeviceName = Dns.GetHostName();
        }

        public async Task<NvApplicationList> ApplicationList()
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"applist?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvApplicationList));
                return serializer.Deserialize(reader) as NvApplicationList;
            }
        }

        public async Task SaveBoxArt(Guid serverUuid, int applicationId)
        {
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile tempFile = await tempFolder.CreateFileAsync($"{serverUuid}-{applicationId}-boxart.png", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBufferAsync(tempFile, await HttpClient.GetBufferAsync(BuildUri($"appasset?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&appid={applicationId}&AssetType=2&AssetIdx=0")));
        }

        public async Task<string> Cancel()
        {
            return await HttpClient.GetStringAsync(BuildUri($"cancel?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}"));
        }

        public async Task<string> Launch(int appId, string mode, int additionalStates, int sops, string riKey, int riKeyId, int localAudioPlayMode, int surroundAudioInfo)
        {
            return await HttpClient.GetStringAsync(BuildUri($"launch?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&appid={appId}&mode={mode}&additionalStates={additionalStates}&rikey={riKey}&rikeyid={riKeyId}&localAudioPlayMode={localAudioPlayMode}&surroundAudioInfo={surroundAudioInfo}"));
        }

        public async Task<string> Pair(string deviceName, int updateState, string phrase, string salt, string clientCert)
        {
            return await HttpClient.GetStringAsync(BuildUri($"serverinfo?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&devicename={deviceName}&updateState={updateState}&phrase={phrase}&salt={salt}&clientcert={clientCert}"));
        }

        public async Task<string> Resume(string riKey, int riKeyId)
        {
            return await HttpClient.GetStringAsync(BuildUri($"resume?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&rikey={riKey}&rikeyid{riKeyId}"));
        }
        
        public async Task<NvServerInfo> ServerInfo()
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"serverinfo?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvServerInfo));
                return serializer.Deserialize(reader) as NvServerInfo;
            }
        }

        public async Task<NvPair> GetServerCert(string salt, string clientCertificate)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&devicename={DeviceName}&updateState=1&phrase=getservercert&salt={salt}&clientcert={clientCertificate}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<NvPair> GetChallengeResponse(string clientChallenge)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&devicename={DeviceName}&updateState=1&clientchallenge={clientChallenge}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<NvPair> GetServerChallengeResponse(string serverChallengeResponse)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&devicename={DeviceName}&updateState=1&serverchallengeresp={serverChallengeResponse}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<NvPair> GetClientPairingSecret(string clientPairingSecret)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&devicename={DeviceName}&updateState=1&clientpairingsecret={clientPairingSecret}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<NvPair> GetPairChallenge()
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"pair?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&devicename={DeviceName}&updateState=1&phrase=pairchallenge"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                return serializer.Deserialize(reader) as NvPair;
            }
        }

        public async Task<string> Unpair()
        {
            return await HttpClient.GetStringAsync(BuildUri($"unpair?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}"));
        }

        private Uri BuildUri(string suffix)
        {
            return new Uri(BaseAddress + suffix);
        }
    }
}
