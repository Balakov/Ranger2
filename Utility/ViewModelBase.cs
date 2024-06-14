using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ranger2.Utility
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool OnPropertyChanged<T>(ref T variable, T value, [CallerMemberName] string viewModelPropertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(variable, value))
            {
                variable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(viewModelPropertyName));

                return true;
            }

            return false;
        }

        protected void OnPropertyChanged([CallerMemberName] string viewModelPropertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(viewModelPropertyName));
        }
    }
}
