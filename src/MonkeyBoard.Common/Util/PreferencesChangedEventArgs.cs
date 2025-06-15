using System;
using System.Collections.Generic;

namespace MonkeyBoard.Common {
    public class PreferencesChangedEventArgs : EventArgs {
        public Dictionary<PrefKeys,(object oldValue,object newValue)> ChangedPrefLookup { get;}
        public PreferencesChangedEventArgs(Dictionary<PrefKeys, (object oldValue, object newValue)> changes) {
            ChangedPrefLookup = changes;
        }
        public PreferencesChangedEventArgs(PrefKeys prefKey, object oldVal,object newVal) {
            ChangedPrefLookup = [];
            ChangedPrefLookup.Add(prefKey, (oldVal, newVal));
        }
    }
}
