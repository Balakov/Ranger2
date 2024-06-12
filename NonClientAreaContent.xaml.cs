using HandyControl.Themes;
using HandyControl.Tools.Command;
using System.Windows;
using System.Windows.Input;

namespace Ranger2
{
    public partial class NonClientAreaContent
    {
        public class ViewModel : Utility.ViewModelBase
        {
            public ICommand SetDarkModeCommand { get; set; }
            public ICommand SetLightModeCommand { get; set; }

            private bool m_isLightMode;
            public bool IsLightMode 
            {
                get => m_isLightMode;
                set => OnPropertyChanged(ref m_isLightMode, value); 
            }

            public ViewModel()
            {
                SetDarkModeCommand = DelegateCommand.Create(() => ChangeSkin(darkMode: true));
                SetLightModeCommand = DelegateCommand.Create(() => ChangeSkin(darkMode: false));

                App.Instance.OnThemeChanged += OnThemeChanged;
            }

            private void OnThemeChanged()
            {
                IsLightMode = ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light;
            }

            private void ChangeSkin(bool darkMode)
            {
                ((App)Application.Current).UpdateSkin(darkMode);
            }
        }

        public NonClientAreaContent()
        {
            InitializeComponent();
            DataContext = new ViewModel();
        }
    }
}
