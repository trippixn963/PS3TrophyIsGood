using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PS3TrophiesIsPerfect.ViewModels
{
    /// <summary>Minimal INotifyPropertyChanged base for view models.</summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value))
                return;
            field = value;
            Raise(name);
        }

        protected void Raise([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
