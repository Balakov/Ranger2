using System.Management;
using System.Windows;
using System.Windows.Controls;

namespace Ranger2
{
    public sealed partial class DrivesTree : UserControl
    {
        private bool m_suppressRequestBringIntoView;
        private IPanelLayout m_layoutLayout;
        private readonly ManagementEventWatcher m_driveWatcher = new ManagementEventWatcher();

        public ColumnDefinition LinkedColumnDefinition { get; set; }

        public DrivesTree()
        {
            InitializeComponent();

            // Drive monitor
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3");
            m_driveWatcher.EventArrived += DriveWatcher_EventArrived;
            m_driveWatcher.Query = query;
            m_driveWatcher.Start();

            Dispatcher.ShutdownStarted += (s, e) =>
            {
                m_driveWatcher.Stop();
            };
        }

        private void DriveWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => { DriveWatcher_EventArrived(sender, e); });
            }
            else
            {
                if (DataContext is ViewModel viewModel)
                {
                    viewModel.OnDrivesChanged();
                }
            }
        }

        public IDirectoryChangeRequest GetDirectoryChangeRequester() => DataContext as ViewModel;

        public void OnRegisteredWithLayout(IPanelLayout layoutPanel)
        {
            m_layoutLayout = layoutPanel;
            DataContext = new ViewModel(new IconCache(this.Dispatcher), m_layoutLayout);
        }

        // Prevent automatic horizontal scrolling.
        // https://stackoverflow.com/questions/3225940/prevent-automatic-horizontal-scroll-in-treeview/34269542#42238409

        private void TreeViewItem_RequestBringIntoView(object sender, System.Windows.RequestBringIntoViewEventArgs e)
        {
            // Ignore re-entrant calls
            if (m_suppressRequestBringIntoView)
                return;

            // Cancel the current scroll attempt
            e.Handled = true;

            // Call BringIntoView using a rectangle that extends into "negative space" to the left of our
            // actual control. This allows the vertical scrolling behaviour to operate without adversely
            // affecting the current horizontal scroll position.
            m_suppressRequestBringIntoView = true;

            TreeViewItem tvi = sender as TreeViewItem;
            if (tvi != null)
            {
                var height = ((FrameworkElement)e.OriginalSource).ActualHeight;
                Rect newTargetRect = new Rect(double.NegativeInfinity, 0, double.PositiveInfinity, height);
                tvi.BringIntoView(newTargetRect);
            }

            m_suppressRequestBringIntoView = false;
        }

        // Correctly handle programmatically selected items
        private void OnSelected(object sender, RoutedEventArgs e)
        {
            ((TreeViewItem)sender).BringIntoView();
            e.Handled = true;
        }
    }
}
