using System.Xml.Serialization;

namespace Moonlight
{
    [XmlRoot(ElementName = "root")]
    public class NvPair
    {
        [XmlAttribute("protocol_version")]
        public string ProtocolVersion { get; set; }
        [XmlAttribute("query")]
        public string Query { get; set; }
        [XmlAttribute("status_code")]
        public int StatusCode { get; set; }
        [XmlAttribute("status_message")]
        public string StatusMessage { get; set; }

        [XmlElement(ElementName = "challengeresponse")]
        public string ChallengeResponse { get; set; }
        [XmlElement(ElementName = "encodedcipher")]
        public string EncodedCipher { get; set; }
        [XmlElement(ElementName = "isBusy")]
        public bool IsBusy { get; set; }
        [XmlElement(ElementName = "paired")]
        public bool Paired { get; set; }
        [XmlElement(ElementName = "pairingsecret")]
        public string PairingSecret { get; set; }
        [XmlElement(ElementName = "plaincert")]
        public string PlainCertificate { get; set; }
    }
}