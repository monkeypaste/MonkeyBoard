using System.Xml;
using System.Xml.Serialization;

namespace MonkeyBoard.Builder {
    [XmlRoot(ElementName = "string")]
    public class AdResString {

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

}