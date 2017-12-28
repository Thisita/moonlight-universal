using System.Xml.Serialization;

namespace Moonlight
{
    public class NvApplication
    {
        public int ID { get; set; }
        [XmlElement(ElementName = "AppTitle")]
        public string Title { get; set; }
        public int IsHdrSupported { get; set; }
        [XmlIgnore]
        public string BoxArt { get; set; }
    }
}