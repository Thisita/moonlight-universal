using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Moonlight
{
    public class NvHttp
    {
        public Guid Uuid { get; private set; }
        public HttpClient HttpClient { get; private set; }

        public NvHttp(Uri baseAddress)
        {
            HttpClient = new HttpClient
            {
                BaseAddress = baseAddress
            };
            Uuid = Guid.NewGuid();
        }

        public NvHttp(Uri baseAddress, Guid uuid)
        {
            HttpClient = new HttpClient
            {
                BaseAddress = baseAddress
            };
            Uuid = uuid;
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
            using(Stream stream = await HttpClient.GetStreamAsync($"serverinfo"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvServerInfo));
                return serializer.Deserialize(stream) as NvServerInfo;
            }
        }

        public async Task<NvServerInfo> ServerInfo(Guid uniqueId)
        {
            using (Stream stream = await HttpClient.GetStreamAsync($"serverinfo?uniqueid={uniqueId}&uuid={Uuid}?uniqueid={uniqueId}&uuid={Uuid}"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NvServerInfo));
                return serializer.Deserialize(stream) as NvServerInfo;
            }
        }

        public async Task<NvPair> GetServerCert(byte[] salt, string v)
        {
            throw new NotImplementedException();
        }

        public async Task<string> Unpair(Guid uniqueId)
        {
            return await HttpClient.GetStringAsync($"unpair?uniqueid={uniqueId}&uuid={Uuid}");
        }
    }
}
