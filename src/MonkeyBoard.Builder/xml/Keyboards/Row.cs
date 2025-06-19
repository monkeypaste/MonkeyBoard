using MonkeyPaste.Common;
using MonkeyBoard.Common;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace MonkeyBoard.Builder {
    [XmlRoot(ElementName = "Row")]
    public class Row : KeyboardFileBase {

        [XmlAttribute(AttributeName = "keyboardMode")]
        public string KeyboardMode { get; set; }
        
        [XmlAttribute(AttributeName = "keyHeight")]
        public string KeyHeight { get; set; }

        [XmlElement(ElementName = "Key")]
        public List<Key> Key { get; set; }

        public bool IsProvidedNumbersRow {
            get {
                return  KeyHeight.ToStringOrEmpty().Contains("key_short_height") || Key.Any(y => y.KeyHeight != null && y.KeyHeight.Contains("key_short_height"));
            }
        }
        public override string ToString() {
            return string.Join(",",Key.Select(x=>x.PrimaryCharacter));
        }
        public Row Clone() {
            var r = new Row() {
                KeyboardMode = KeyboardMode,
                KeyHeight = KeyHeight,
                Key = Key.Select(x => x.Clone()).ToList()
            };
            return r;
        }

        public RowFormat ToRowFormat() {
            return new RowFormat() {
                keys = Key.Select(x => x.ToKeyFormat()).ToList()
            };
        }
    }
}