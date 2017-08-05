
namespace SmartProperties
{
	using System;
	using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged, IDisposable
    {
        public NotifyPropertyChangedBase()
        {
            this.PropertyModel = PropertyModel.Create(this, this.OnPropertyChanged);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private PropertyModel PropertyModel { get; }

        protected virtual void OnPropertyChanged([CallerMemberName]string property = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public void Dispose()
        {
            this.PropertyModel.Dispose();
        }
    }
}
