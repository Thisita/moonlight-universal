using System.Xml.Serialization;

namespace Moonlight
{
    [XmlRoot(ElementName = "root")]
    public class NvLaunch
    {
        [XmlAttribute("protocol_version")]
        public string ProtocolVersion { get; set; }
        [XmlAttribute("query")]
        public string Query { get; set; }
        [XmlAttribute("status_code")]
        public int StatusCode { get; set; }
        [XmlAttribute("status_message")]
        public string StatusMessage { get; set; }
        public int DisplayHeight { get; set; }
        public int DisplayWidth { get; set; }
        public bool HdrMode { get; set; }
        public int RefreshRate { get; set; }
        [XmlElement(ElementName = "gamesession")]
        public string GameSession { get; set; }
    }
}