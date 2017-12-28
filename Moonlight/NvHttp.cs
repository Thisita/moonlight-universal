using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Windows.Storage;

namespace Moonlight
{
    public class NvHttp
    {
        public Guid Uuid { get; private set; }
        public HttpClient HttpClient { get; private set; }
        public string DeviceName { get; private set; }

        public NvHttp(Uri baseAddress)
        {
            HttpClient = new HttpClient
            {
                BaseAddress = baseAddress
            };
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

        public async Task<string> Cancel(Guid uniqueId)
        {
            return await HttpClient.GetStringAsync($"cancel?uniqueid={uniqueId}&uuid={Uuid}");
        }

        public async Task<string> Launch(Guid uniqueId, int appId, string mode, int additionalStates, int sops, string riKey, int riKeyId, int localAudioPlayMode, int surroundAudioInfo)
        {
            return await HttpClient.GetStringAsync($"launch?uniqueid={uniqueId}&uuid={Uuid}&appid={appId}&mode={mode}&additionalStates={additionalStates}&rikey={riKey}&rikeyid={riKeyId}&localAudioPlayMode={localAudioPlayMode}&surroundAudioInfo={surroundAudioInfo}");
        }

        public async Task<string> Pair(Guid uniqueId, string deviceName, int updateState, string phrase, string salt, string clientCert)
        {
            return await HttpClient.GetStringAsync($"serverinfo?uniqueid={uniqueId}&uuid={Uuid}&devicename={deviceName}&updateState={updateState}&phrase={phrase}&salt={salt}&clientcert={clientCert}");
        }

        public async Task<string> Resume(Guid uniqueId, string riKey, int riKeyId)
        {
            return await HttpClient.GetStringAsync($"resume?uniqueid={uniqueId}&uuid={Uuid}&rikey={riKey}&rikeyid{riKeyId}");
        }

        public async Task<NvServerInfo> ServerInfo()
        {
            using (Stream stream = await HttpClient.GetStreamAsync($"serverinfo"))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(NvServerInfo));
                    return serializer.Deserialize(reader) as NvServerInfo;
                }
            }
        }

        public async Task<NvServerInfo> ServerInfo(Guid uniqueId)
        {
            using (Stream stream = await HttpClient.GetStreamAsync($"serverinfo?uniqueid={uniqueId}&uuid={Uuid}"))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(NvServerInfo));
                    return serializer.Deserialize(reader) as NvServerInfo;
                }
            }
        }

        public async Task<NvPair> GetServerCert(Guid uniqueId, string salt, string clientCertificate)
        {
            using (Stream stream = await HttpClient.GetStreamAsync($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&phrase=getservercert&salt={salt}&clientcert={clientCertificate}"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                    return serializer.Deserialize(reader) as NvPair;
                }
            }
        }

        public async Task<NvPair> GetChallengeResponse(Guid uniqueId, string clientChallenge)
        {
            using (Stream stream = await HttpClient.GetStreamAsync($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&clientchallenge={clientChallenge}"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                    return serializer.Deserialize(reader) as NvPair;
                }
            }
        }

        public async Task<NvPair> GetServerChallengeResponse(Guid uniqueId, string serverChallengeResponse)
        {
            using (Stream stream = await HttpClient.GetStreamAsync($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&serverchallengeresp={serverChallengeResponse}"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                    return serializer.Deserialize(reader) as NvPair;
                }
            }
        }

        public async Task<NvPair> GetClientPairingSecret(Guid uniqueId, string clientPairingSecret)
        {
            using (Stream stream = await HttpClient.GetStreamAsync($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&clientpairingsecret={clientPairingSecret}"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                    return serializer.Deserialize(reader) as NvPair;
                }
            }
        }

        public async Task<NvPair> GetPairChallenge(Guid uniqueId)
        {
            using (Stream stream = await HttpClient.GetStreamAsync($"pair?uniqueid={uniqueId}&uuid={Uuid}&devicename={DeviceName}&updateState=1&phrase=pairchallenge"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(NvPair));
                    return serializer.Deserialize(reader) as NvPair;
                }
            }
        }

        public async Task<string> Unpair(Guid uniqueId)
        {
            return await HttpClient.GetStringAsync($"unpair?uniqueid={uniqueId}&uuid={Uuid}");
        }
    }
}
