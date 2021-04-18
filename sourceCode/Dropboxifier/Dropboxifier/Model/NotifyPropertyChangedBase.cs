using System.ComponentModel;

namespace Dropboxifier.Model
{
    /// <summary>
    /// Basic INotifyPropertyChanged implementation that includes an OnPropertyChanged helper method
    /// </summary>
    public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }
    }
}
