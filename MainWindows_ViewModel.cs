
namespace Ranger2
{
    public partial class MainWindow
    {
        public class ViewModel
        {
            public BookmarksPanel.ViewModel Bookmarks { get; }
            public IDirectoryChangeRequest DirectoryChangeRequester { get; }
            public IPanelLayout PanelLayout { get; }

            public void LoadBookmarks(UserSettings settings,
                                      IconCache iconCache,
                                      IDirectoryChangeRequest directoryChangeRequester,
                                      IPanelLayout panelLayout)
            {
                Bookmarks.LoadBookmarks(settings, iconCache, directoryChangeRequester, panelLayout);
            }

            public ViewModel(IDirectoryChangeRequest directoryChangeRequester, IPanelLayout panelLayout)
            {
                DirectoryChangeRequester = directoryChangeRequester;
                PanelLayout = panelLayout;

                BookmarksPanel.ViewModel.SetupDynamicProperties();
                Bookmarks = new();
            }
        }
    }
}
