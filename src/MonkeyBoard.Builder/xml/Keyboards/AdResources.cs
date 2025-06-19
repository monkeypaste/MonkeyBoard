using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MonkeyBoard.Builder {
    [XmlRoot(ElementName = "resources")]
    public class AdResources {
        public static AdResources Deserialize(string path) {
            try {
                string xml = File.ReadAllText(path);
                xml = xml.Replace("android:", string.Empty).Replace("ask:", string.Empty);
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                XmlSerializer serializer = new XmlSerializer(typeof(AdResources));

                using (var reader = new System.Xml.XmlTextReader(ms) { Namespaces = false }) {
                    var resc = (AdResources)serializer.Deserialize(reader);
                    if (resc == null) {
                        return null;
                    }
                    return resc;
                }
            }
            catch { }
            return null;
        }

        [XmlElement(ElementName = "string")]
        public List<AdResString> ResString { get; set; }
    }

}