using System.Collections.Generic;
using System.Xml.Serialization;

namespace Moonlight
{
    [XmlRoot(ElementName = "root")]
    public class NvServerInfo
    {
        public enum NvPairStatus
        {
            [XmlEnum("0")]
            Unpaired,
            [XmlEnum("1")]
            Paired,
            [XmlEnum("2")]
            WrongPin,
            [XmlEnum("3")]
            Failed,
            [XmlEnum("4")]
            AlreadyInProgress
        };

        [XmlAttribute("protocol_version")]
        public string ProtocolVersion { get; set; }
        [XmlAttribute("query")]
        public string Query { get; set; }
        [XmlAttribute("status_code")]
        public int StatusCode { get; set; }
        [XmlAttribute("status_message")]
        public string StatusMessage { get; set; }

        public int AuthenticationType { get; set; }
        public string ConnectionState { get; set; }
        public int CurrentClient { get; set; }
        public string GfeVersion { get; set; }
        public string GsVersion { get; set; }
        public int HttpsPort { get; set; }
        public string LocalIp { get; set; }
        public List<string> LocalIPs { get; set; }
        public int LoginState { get; set; }
        public long MaxLumaPixelsH264 { get; set; }
        public long MaxLumaPixelsHEVC { get; set; }
        public int Mode { get; set; }
        public NvPairStatus PairStatus { get; set; }
        public int ServerCompatibility { get; set; }
        public int ServerCodecModeSupport { get; set; }
        public int ServerColorSpaceSupport { get; set; }
        public List<NvDisplayMode> SupportedDisplayMode { get; set; }
        [XmlElement(ElementName = "accountId")]
        public string AccountId { get; set; }
        [XmlElement(ElementName = "appversion")]
        public string AppVersion { get; set; }
        [XmlElement(ElementName = "currentgame")]
        public int CurrentGame { get; set; }
        [XmlElement(ElementName = "gamelistid")]
        public string GameListId { get; set; }
        [XmlElement(ElementName = "gputype")]
        public string GpuType { get; set; }
        [XmlElement(ElementName = "hostname")]
        public string HostName { get; set; }
        [XmlElement(ElementName = "mac")]
        public string Mac { get; set; }
        [XmlElement(ElementName = "numofapps")]
        public int NumOfApps { get; set; }
        [XmlElement(ElementName = "resyncSuccessful")]
        public int ResyncSuccessful { get; set; }
        [XmlElement(ElementName = "state")]
        public string State { get; set; }
        [XmlElement(ElementName = "uniqueid")]
        public string UniqueId { get; set; }
    }
}