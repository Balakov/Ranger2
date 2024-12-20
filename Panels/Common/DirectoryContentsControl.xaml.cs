using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ranger2
{
    public interface IDirectoryWatcher
    {
        void EnableDirectoryWatcher(string directory);
        void DisableDirectoryWatcher();
    }

    public partial class DirectoryContentsControl : UserControl,
                                                    IDirectoryWatcher
    {
        public enum DirectoryListingType
        {
            None,
            Files,
            Images,
            Fonts
        }

        public enum ThumbnailSizeType
        {
            Small,
            Medium,
            Large
        }

        protected PanelContext m_context;
        protected UserSettings.FilePanelSettings m_settings;
        protected PathHistory m_pathHistory;

        private readonly FileSystemWatcher m_fileWatcher = new FileSystemWatcher();

        private ColumnDefinition m_linkedColumnDefinition;
        public ColumnDefinition LinkedColumnDefinition => m_linkedColumnDefinition;

        public bool IsCurrentPanel => (DataContext as ViewModel)?.IsCurrentPanel == true;
        public string CurrentPath => (DataContext as ViewModel)?.CurrentPath;
        public void HistoryBack() => (DataContext as ViewModel)?.HistoryBack();
        public void HistoryForward() => (DataContext as ViewModel)?.HistoryForward();

        public DirectoryContentsControl()
        {
            InitializeComponent();

            m_fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
            m_fileWatcher.Filter = "*";
            m_fileWatcher.Changed += OnFilesChanged;
            m_fileWatcher.Created += OnFilesChanged;
            m_fileWatcher.Renamed += OnFilesChanged;
            m_fileWatcher.Deleted += OnFilesChanged;

            Dispatcher.ShutdownStarted += (s, e) =>
            {
                DisableDirectoryWatcher();
                m_fileWatcher.Changed -= OnFilesChanged;
                m_fileWatcher.Created -= OnFilesChanged;
                m_fileWatcher.Renamed -= OnFilesChanged;
                m_fileWatcher.Deleted -= OnFilesChanged;
                m_fileWatcher.Dispose();
            };
        }

        public void EnableDirectoryWatcher(string directory)
        {
            if (!string.IsNullOrEmpty(directory) &&
                FileSystemEnumeration.DirectoryExists(directory))
            {
                try
                {
                    m_fileWatcher.Path = directory;
                    m_fileWatcher.EnableRaisingEvents = true;
                }
                catch { }
            }
        }

        public void DisableDirectoryWatcher()
        {
            m_fileWatcher.EnableRaisingEvents = false;
        }

        public void OnRegisteredWithLayout(PanelContext context,
                                           UserSettings.FilePanelSettings settings,
                                           ColumnDefinition linkedColumnDefinition)
        {
            m_context = context;
            m_settings = settings;
            m_linkedColumnDefinition = linkedColumnDefinition;
            m_pathHistory = new PathHistory(m_context.DirectoryChangeRequester);

            if (settings.ListingType.HasValue)
            {
                SetListingType(settings.ListingType.Value);
            }
            else
            {
                SetListingType(DirectoryListingType.Files);
            }
        }

        public DirectoryListingType ListingType
        {
            get
            {
                if (DataContext is FilesPanel.ViewModel)
                    return DirectoryListingType.Files;
                if (DataContext is ImagePanel.ViewModel)
                    return DirectoryListingType.Images;
                if (DataContext is FontPanel.ViewModel)
                    return DirectoryListingType.Fonts;
                return DirectoryListingType.None;
            }
        }

        public void SetListingType(DirectoryListingType type)
        {
            bool isCurrentPanel = false;
            if (DataContext is ViewModel oldViewModel)
            {
                isCurrentPanel = oldViewModel.IsCurrentPanel;
                // Unregister event handlers
                oldViewModel.IsCurrentPanel = false;
            }

            switch (type)
            {
                case DirectoryListingType.Files:
                    DataContext = new FilesPanel.ViewModel(m_context, m_settings, m_pathHistory, this);
                    break;
                case DirectoryListingType.Images:
                    DataContext = new ImagePanel.ViewModel(m_context, m_settings, m_pathHistory, this);
                    break;
                case DirectoryListingType.Fonts:
                    DataContext = new FontPanel.ViewModel(m_context, m_settings, m_pathHistory, this);
                    break;
                default:
                    DataContext = null;
                    break;
            }

            if (DataContext is ViewModel newPanelViewModel)
            {
                // Register event handlers
                newPanelViewModel.IsCurrentPanel = isCurrentPanel;

                if (type != DirectoryListingType.None)
                {
                    m_settings.ListingType = type;
                }
            }
        }

        public void SetThumbnailSize(ThumbnailSizeType size)
        {
            if (DataContext is ViewModel vm)
            {
                vm.SetThumbnailSize(size);
                m_settings.ThumbnailSize = size;
            }
        }

        public void OnSwitchFocus(bool hasFocus)
        {
            if (DataContext is ViewModel panelViewModel)
            {
                panelViewModel.IsCurrentPanel = hasFocus;
            }
        }

        private void OnSetFileMode(object sender, RoutedEventArgs e)
        {
            SetListingType(DirectoryListingType.Files);
        }

        private void OnSetImageMode(object sender, RoutedEventArgs e)
        {
            SetListingType(DirectoryListingType.Images);
        }

        private void OnSetFontMode(object sender, RoutedEventArgs e)
        {
            SetListingType(DirectoryListingType.Fonts);
        }

        private void OnSetThumbnailSizeSmall(object sender, RoutedEventArgs e)
        {
            SetThumbnailSize(ThumbnailSizeType.Small);
        }

        private void OnSetThumbnailSizeMedium(object sender, RoutedEventArgs e)
        {
            SetThumbnailSize(ThumbnailSizeType.Medium);
        }

        private void OnSetThumbnailSizeLarge(object sender, RoutedEventArgs e)
        {
            SetThumbnailSize(ThumbnailSizeType.Large);
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_context.PanelLayout.SwitchFocus(this);
        }
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            (DataContext as ViewModel)?.OnCommonPreviewKeyDown(e);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            (DataContext as ViewModel)?.OnCommonKeyDown(e);
        }

        public void SetPanelVisibility(bool visible)
        {
            SetListingType(visible ? m_settings.ListingType.Value : DirectoryListingType.None);
        }

        public void SetContentFromPath(string path, DirectoryListingType listingType)
        {
            m_context.PanelLayout.SwitchFocus(this);
            m_context.DirectoryChangeRequester.SetDirectory(path, null);
            SetListingType(listingType);
        }

        public void SetContentFromPanel(DirectoryContentsControl otherPanel)
        {
            SetContentFromPath(otherPanel.m_settings.Path, otherPanel.m_settings.ListingType.Value);
        }

        public void OnFilesChanged(object source, FileSystemEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                try
                {
                    Dispatcher.Invoke(() => { OnFilesChanged(source, e); });
                }
                catch { }
            }
            else
            {

                if (e.ChangeType == WatcherChangeTypes.Renamed)
                {
                    if (e is RenamedEventArgs renamedEventArgs)
                    {
                        (DataContext as ViewModel)?.OnFileSystemChange(e.FullPath, renamedEventArgs.OldFullPath, e.ChangeType);
                    }
                }
                else
                {
                    (DataContext as ViewModel)?.OnFileSystemChange(e.FullPath, null, e.ChangeType);
                }
            }
        }

        public static List<FileSystemObjectViewModel> GetItemsAsViewModels(ItemCollection collection)
        {
            List<FileSystemObjectViewModel> visualItems = new();
            foreach (object item in collection)
            {
                if (item is FileSystemObjectViewModel viewModel)
                    visualItems.Add(viewModel);
            }

            return visualItems;
        }

        private BreadcrumbsViewModel.PathPart GetBreadcrumb(object sender)
        {
            if (sender is Button button)
                return button.DataContext as BreadcrumbsViewModel.PathPart;
            else
                return null;
        }

        private void Breadcrumb_DragOver(object sender, DragEventArgs e) => GetBreadcrumb(sender)?.OnDragOver(sender, e);
        private void Breadcrumb_Drop(object sender, DragEventArgs e) => GetBreadcrumb(sender)?.OnDrop(sender, e);
        private void Breadcrumb_MouseUp(object sender, MouseButtonEventArgs e) => GetBreadcrumb(sender)?.OnMouseUp(sender, e);
        private void Breadcrumb_MouseDown(object sender, MouseButtonEventArgs e) => GetBreadcrumb(sender)?.OnMouseDown(sender, e);
        private void Breadcrumb_MouseMove(object sender, MouseEventArgs e) => GetBreadcrumb(sender)?.OnMouseMove(sender, e);
    }

    public class DirectoryContentsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            string templateName = null;

            if (item is FilesPanel.ViewModel)
            {
                templateName = "FilePanelDataTemplate";
            }
            else if (item is ImagePanel.ViewModel)
            {
                templateName = "ImagePanelDataTemplate";
            }
            else if (item is FontPanel.ViewModel)
            {
                templateName = "FontPanelDataTemplate";
            }

            if (!string.IsNullOrEmpty(templateName))
            {
                return (DataTemplate)((FrameworkElement)container).FindResource(templateName);
            }

            return null;
        }
    }
}
