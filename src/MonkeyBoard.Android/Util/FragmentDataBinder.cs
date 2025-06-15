using Android.OS;

namespace MonkeyBoard.Android {
    public class FragmentDataBinder<T> : Binder {
        // from https://stackoverflow.com/a/37774966/105028
        public T BoundData { get; private set; }

        public FragmentDataBinder(T data) {
            BoundData = data;
        }
    }
}
