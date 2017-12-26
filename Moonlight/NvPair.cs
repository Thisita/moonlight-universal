using System.Xml.Serialization;

namespace Moonlight
{
    [XmlRoot(ElementName = "root")]
    public class NvPair
    {
        [XmlElement(ElementName = "paired")]
        public int Paired { get; set; }
    }
}