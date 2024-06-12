
namespace Ranger2
{
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private PanelLayout m_panelLayout;

        public MainWindow()
        {
            InitializeComponent();

            UserSettings userSettings = App.UserSettings;

            NonClientAreaContent = new NonClientAreaContent();

            Loaded += (s, e) =>
            {
                var iconCache = new IconCache(this.Dispatcher);
                var imageCache = new ImageCache(this.Dispatcher);
                var fontPreviewCache = new FontPreviewCache(this.Dispatcher);


                DrivesTreeInstance.LinkedColumnDefinition = DrivesPanelColumnDefinition;

                m_panelLayout = new(DrivesTreeInstance, [Panel1, Panel2, Panel3]);
                
                DrivesTreeInstance.OnRegisteredWithLayout(m_panelLayout);
                
                IDirectoryChangeRequest directoryChangeRequester = DrivesTreeInstance.GetDirectoryChangeRequester();

                var panelContext = new PanelContext(directoryChangeRequester,
                                                    m_panelLayout,
                                                    iconCache,
                                                    imageCache,
                                                    fontPreviewCache,
                                                    userSettings);

                Panel1.OnRegisteredWithLayout(panelContext, userSettings.Panel1Settings, Panel1ColumnDefinition);
                Panel2.OnRegisteredWithLayout(panelContext, userSettings.Panel2Settings, Panel2ColumnDefinition);
                Panel3.OnRegisteredWithLayout(panelContext, userSettings.Panel3Settings, Panel3ColumnDefinition);

                LayoutUIFromSettings(userSettings);

                SizeChanged += (s, e) => OnLayoutChanged();
                LocationChanged += (s, e) => OnLayoutChanged();
                StateChanged += (s, e) => OnLayoutChanged();

                var viewModel = new ViewModel(directoryChangeRequester, m_panelLayout);
                viewModel.LoadBookmarks(userSettings, iconCache, directoryChangeRequester, m_panelLayout);
                DataContext = viewModel;

                m_panelLayout.SwitchFocus(Panel1);
            };
        }

        private void OnLayoutChanged()
        {
            UserSettings userSettings = App.UserSettings;

            userSettings.WindowX = (int)this.Left;
            userSettings.WindowY = (int)this.Top;

            userSettings.WindowMaximised = WindowState == System.Windows.WindowState.Maximized;

            if (WindowState == System.Windows.WindowState.Normal)
            {
                userSettings.WindowWidth = (int)this.Width;
                userSettings.WindowHeight = (int)this.Height;
            }
        }

        private void LayoutUIFromSettings(UserSettings userSettings)
        {
            m_panelLayout.SetActivePanelCount(userSettings.ActivePanelCount);

            try
            {
                if (userSettings.WindowX.HasValue)
                    this.Left = userSettings.WindowX.Value;
                if (userSettings.WindowY.HasValue)
                    this.Top = userSettings.WindowY.Value;

                if (userSettings.WindowWidth.HasValue)
                    this.Width = userSettings.WindowWidth.Value;
                if (userSettings.WindowHeight.HasValue)
                    this.Height = userSettings.WindowHeight.Value;

                if (userSettings.WindowMaximised.HasValue)
                    this.WindowState = userSettings.WindowMaximised.Value ? System.Windows.WindowState.Maximized
                                                                          : System.Windows.WindowState.Normal;
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }
}
