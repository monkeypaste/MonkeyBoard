
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MonkeyBoard.Common {
    public abstract class ViewModelBase : INotifyPropertyChanged {

        static List<ViewModelBase> _allFrames = [];
        public static IReadOnlyList<ViewModelBase> All =>
            _allFrames;

        protected ViewModelBase() {
            _allFrames.Add(this);
        }
        ~ViewModelBase() {
            _allFrames.Remove(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null, [CallerFilePath] string path = null, [CallerMemberName] string memName = null, [CallerLineNumber] int line = 0) {
            if (PropertyChanged == null ||
                propertyName == null) {
                return;
            }
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public abstract class TreeViewModelBase : ViewModelBase {

        public virtual TreeViewModelBase Parent { get; protected set; }
    }
}
