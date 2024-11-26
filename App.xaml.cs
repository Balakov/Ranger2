using HandyControl.Themes;
using System;
using System.Windows;
using System.IO;
using System.Threading;

namespace Ranger2
{
    public partial class App : Application
    {
        private static App s_instance;
        public static App Instance => s_instance;

        private UserSettings m_userSettings = null;
        public static UserSettings UserSettings => ((App)Current).m_userSettings;

        public delegate void OnThemeChangedDelegate();
        public event OnThemeChangedDelegate OnThemeChanged;

        private Mutex m_mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "MyAppName";
            bool createdNew;

            m_mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
            {
                //app is already running! Exiting the application
                Application.Current.Shutdown();
            }

            base.OnStartup(e);

            s_instance = this;

            string userSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YellowDroid", "Ranger2", "UserSettings.xml");
            m_userSettings = UserSettings.Load(userSettingsPath);

            if (m_userSettings.DarkMode)
            {
                UpdateSkin(darkMode: true);
            }
        }

        public void UpdateSkin(bool darkMode)
        {
            ThemeManager.Current.ApplicationTheme = darkMode ? ApplicationTheme.Dark : ApplicationTheme.Light;
            m_userSettings.DarkMode = darkMode;
            OnThemeChanged?.Invoke();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            m_userSettings?.Save();

            base.OnExit(e);
        }

    }
}
