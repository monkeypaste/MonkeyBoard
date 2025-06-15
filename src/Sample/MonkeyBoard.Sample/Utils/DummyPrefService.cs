using MonkeyBoard.Common;
using MonkeyBoard.Sample;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace MonkeyBoard.Sample {
    public class DummyPrefService : ISharedPrefService {


        Dictionary<PrefKeys, object> _transientPrefLookup;
        Dictionary<PrefKeys, object> TransientPrefLookup {
            get {
                if (_transientPrefLookup == null) {
                    _transientPrefLookup = [];//SharedPrefWrapper.DefPrefValLookup;
                }
                return _transientPrefLookup;
            }
        }

        public T GetPrefValue<T>(PrefKeys prefKey) {
            if (TransientPrefLookup.TryGetValue(prefKey, out object valObj) &&
                valObj is SliderPrefProps sliderProps) {
                return (T)(object)sliderProps.Default;
            }
            return (T)(object)valObj;
        }

        public void SetPrefValue<T>(PrefKeys prefKey, T newValue) {
            object oldVal = TransientPrefLookup[prefKey];
            TransientPrefLookup[prefKey] = newValue;
            PreferencesChanged?.Invoke(this, null);
        }

        public event EventHandler<PreferencesChangedEventArgs> PreferencesChanged;

        public Task RestoreDefaultsAsync() {
            throw new NotImplementedException();
        }

        public void RestoreDefaults() {
            throw new NotImplementedException();
        }
    }
}

