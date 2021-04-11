using System.ComponentModel;
using System.Threading;

namespace Tunetoon.Utilities
{
    public class AsyncNotifyPropertyChanged : INotifyPropertyChanged
    {
        // Very hacky, this first gets set with the UI context
        private static readonly SynchronizationContext UiContext = SynchronizationContext.Current;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var sc = SynchronizationContext.Current;
            if (sc != UiContext)
            {
                UiContext.Post(d => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)), null);
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
