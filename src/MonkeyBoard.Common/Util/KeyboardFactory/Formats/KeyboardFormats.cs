
using MonkeyPaste.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonkeyBoard.Common {


    public class KeyboardCollectionManifest {
        public string culture { get; set; }
        public string collectionUri { get; set; }
    }
    public class KeyboardCollectionFormat {
        [JsonIgnore]
        public string culture { get; set; } // set passively by file dir
        public List<KeyboardFormat> keyboards { get; set; } = [];
        public KeyboardCollectionFormat Clone() {
            return new() {
                culture = culture,
                keyboards = keyboards.Select(x => x.Clone()).ToList()
            };
        }
    }
    public class KeyboardFormat {
        [JsonIgnore]
        public KeyboardCollectionFormat Parent { get; set; }
        [JsonIgnore]
        public string FilePath { get; set; }

        public string guid { get; set; }
        public List<RowFormat> rows { get; set; } = [];
        public List<RowFormat> landscapeRows { get; set; } = [];
        public List<string> letterGroups { get; set; }
        public string label { get; set; }
        public string description { get; set; }
        public bool isDefault { get; set; }
        public bool isNumPad { get; set; }

        public void FinishInit(KeyboardCollectionFormat parent, string path) {
            Parent = parent;
            FilePath = path;
            rows.ForEach(x => x.Parent = this);
            foreach (var r in rows) {
                r.keys.ForEach(x => x.Parent = r);
            }
        }
        public override string ToString() {
            return label.ToStringOrEmpty();
        }
        public KeyboardFormat Clone() {
            return new KeyboardFormat() {
                FilePath = FilePath,
                guid = guid,
                rows = rows.Select(x => x.Clone()).ToList(),
                landscapeRows = landscapeRows.Select(x => x.Clone()).ToList(),
                letterGroups = letterGroups.ToList(),
                label = label,
                description = description,
                isDefault = isDefault,
                isNumPad = isNumPad
            };
        }
    }
    public class RowFormat {
        [JsonIgnore]
        public KeyboardFormat Parent { get; set; }
        public List<KeyFormat> keys { get; set; } = [];
        public override string ToString() {
            return string.Join("|", keys.Select(x=>x.ToString()));
        }
        public RowFormat Clone() {
            return new RowFormat() {
                keys = keys.Select(x => x.Clone()).ToList()
            };
        }
    }
    public class KeyFormat {
        [JsonIgnore]
        public RowFormat Parent { get; set; }
        public string primaryValues { get; set; }
        public string shiftValues { get; set; }
        public int row { get; set; }
        public int column { get; set; }
        public override string ToString() {
            return primaryValues.ToStringOrEmpty();
        }
        public KeyFormat Clone() {
            return new KeyFormat() {
                primaryValues = primaryValues,
                shiftValues = shiftValues,
                row = row,
                column = column
            };
        }
    }
}