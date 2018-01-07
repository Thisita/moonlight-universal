using System.Xml.Serialization;

namespace Moonlight
{
    public class NvApplication
    {
        [XmlElement(ElementName = "AppInstallPath")]
        public string InstallPath { get; set; }
        [XmlElement(ElementName = "AppTitle")]
        public string Title { get; set; }
        public string CmsId { get; set; }
        public string Distributor { get; set; }
        public int ID { get; set; }
        [XmlElement(ElementName = "IsAppCollectorGame")]
        public int IsApplicationCollectorGame { get; set; }
        public int IsHdrSupported { get; set; }
        public int MaxControllersForSingleSession { get; set; }
        public string ShortName { get; set; }
        // SupportedSOPs
        public string UniqueId { get; set; }
        [XmlElement(ElementName = "simulateControllers")]
        public int SimulateControllers { get; set; }
        [XmlIgnore]
        public string BoxArt { get; set; }
    }
}