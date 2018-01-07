using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Moonlight
{
    public class NvHttp
    {
        private HttpClient HttpClient { get; set; }
        public Guid UniqueUuid { get; private set; }
        public string DeviceName { get; private set; }
        public Uri BaseAddress { get; private set; }

        public NvHttp(Uri baseAddress, Certificate clientCertificate)
        {
            BaseAddress = baseAddress;
            UniqueUuid = Configuration.UniqueUuid;
            DeviceName = Configuration.DeviceName;
            HttpBaseProtocolFilter httpBaseProtocolFilter = new HttpBaseProtocolFilter
            {
                ClientCertificate = clientCertificate,
                AutomaticDecompression = true
            };
            httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            HttpClient = new HttpClient(httpBaseProtocolFilter);
        }

        public async Task<List<NvApplication>> ApplicationList(Guid serverUuid)
        {
            NvApplicationList applicationList;
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"applist?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}"))))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvApplicationList));
                applicationList = serializer.Deserialize(reader) as NvApplicationList;
            }
            List<Task> saveBoxArtTasks = new List<Task>();
            foreach (NvApplication application in applicationList.Applications)
            {
                saveBoxArtTasks.Add(SaveBoxArt(serverUuid, application));
            }
            await Task.WhenAll(saveBoxArtTasks);
            return applicationList.Applications;
        }

        private async Task SaveBoxArt(Guid serverUuid, NvApplication application)
        {
            try
            {
                StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile tempFile = await tempFolder.CreateFileAsync($"{serverUuid}-{application.ID}-boxart.png", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBufferAsync(tempFile, await HttpClient.GetBufferAsync(BuildUri($"appasset?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&appid={application.ID}&AssetType=2&AssetIdx=0")));
                application.BoxArt = tempFile.Path;
            }
            catch (COMException ex)
            {
                application.BoxArt = " ";
                Debug.WriteLine("Failed to get box art for {0}: {1}", application.Title, ex.Message);
            }
        }

        public async Task<string> Cancel()
        {
            return await HttpClient.GetStringAsync(BuildUri($"cancel?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}"));
        }

        public async Task<NvGameSession> Launch(int appId, string mode, int additionalStates, int sops, string riKey, int riKeyId, int localAudioPlayMode, int surroundAudioInfo)
        {
            using (TextReader reader = new StringReader(await HttpClient.GetStringAsync(BuildUri($"launch?uniqueid={UniqueUuid}&uuid={Guid.NewGuid()}&appid={appId}&mode={mode}&additionalStates={additionalStates}&rikey={riKey}&rikeyid={riKeyId}&localAudioPlayMode={localAudioPlayMode}&surroundAudioInfo={surroundAudioInfo}"))))
            {
                Debug.WriteLine(reader.ReadToEnd());
                XmlSerializer serializer = new XmlSerializer(typeof(NvServerInfo));
                return serializer.Deserialize(reader) as NvGameSession;
            }
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
