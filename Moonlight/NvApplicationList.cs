using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Moonlight
{
    [XmlRoot(ElementName = "root")]
    public class NvApplicationList
    {
        [XmlAttribute("protocol_version")]
        public string ProtocolVersion { get; set; }
        [XmlAttribute("query")]
        public string Query { get; set; }
        [XmlAttribute("status_code")]
        public int StatusCode { get; set; }
        [XmlAttribute("status_message")]
        public string StatusMessage { get; set; }

        public List<NvApplication> Applications { get; set; }
    }
}
