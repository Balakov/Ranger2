using HandyControl.Tools.Command;
using Ranger2.DynamicViewModelProperties;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Ranger2
{
    public partial class BookmarksPanel
    {
        public interface IBookmarkDeleter
        {
            void DeleteBookmark(BookmarkViewModel bookmark);
        }

        public interface IBookmarkGroupRefresher
        {
            void RefreshActiveBookmarkGroup();
        }

        public class BookmarkContext
        {
            public readonly IconCache IconCache;
            public readonly IDirectoryChangeRequest DirectoryChangeRequester;
            public readonly IPanelLayout PanelLayout;
            public readonly IBookmarkDeleter BookmarkDeleter;
            public readonly IBookmarkGroupRefresher BookmarkGroupRefresher;

            public BookmarkContext(IconCache iconCache,
                                   IDirectoryChangeRequest directoryChangeRequester,
                                   IPanelLayout panelLayout,
                                   IBookmarkDeleter bookmarkDeleter,
                                   IBookmarkGroupRefresher bookmarkGroupSwitcher)
            {
                IconCache = iconCache;
                DirectoryChangeRequester = directoryChangeRequester;
                PanelLayout = panelLayout;
                BookmarkDeleter = bookmarkDeleter;
                BookmarkGroupRefresher = bookmarkGroupSwitcher;
            }
        }

        private static DynamicPropertyManager<ViewModel> s_dynamicPropertyManager = new();

        public class ViewModel : Utility.ViewModelBase, 
                                 IBookmarkDeleter, 
                                 IBookmarkGroupRefresher
        {
            private BookmarkContext m_context;
            private UserSettings m_settings;

            private ObservableCollection<BookmarkViewModel> m_toolbarBookmarks = new();
            public ObservableCollection<BookmarkViewModel> ToolbarBookmarks
            {
                get => m_toolbarBookmarks;
                set => OnPropertyChanged(ref m_toolbarBookmarks, value);
            }

            private int m_activeBookmarkGroup;
            public int ActiveBookmarkGroup
            {
                get => m_activeBookmarkGroup;
                set => OnPropertyChanged(ref m_activeBookmarkGroup, value);
            }

            public ICommand SwitchBookmarkToGroupCommand { get; }
            public ICommand NavigateBackwards { get; }
            public ICommand NavigateForwards { get; }
            public ICommand NavigateToParent { get; }
            public ICommand RecycleBinCommand { get; }

            public ViewModel()
            {
                SwitchBookmarkToGroupCommand = DelegateCommand.Create((o) => SetActiveBookmarkGroup(int.Parse(o.ToString())));

                BookmarkViewModel.SetupDynamicProperties();

                NavigateBackwards = DelegateCommand.Create(() => m_context.PanelLayout.CurrentPanel?.HistoryBack());
                NavigateForwards = DelegateCommand.Create(() => m_context.PanelLayout.CurrentPanel?.HistoryForward());
                NavigateToParent = DelegateCommand.Create(() => m_context.DirectoryChangeRequester.SetDirectoryToParent());
                RecycleBinCommand = DelegateCommand.Create(() => System.Diagnostics.Process.Start("explorer.exe", "shell:RecycleBinFolder"));
            }

            public static void SetupDynamicProperties()
            {
                for (int i = 0; i < 6; i++)
                {
                    s_dynamicPropertyManager.Properties.Add(DynamicPropertyManager<ViewModel>.CreateProperty<ViewModel, bool>($"IsActiveBookmarkGroup{i}", 
                                                                                                                              OnGetIsActiveBookmarkGroup, 
                                                                                                                              null));
                    s_dynamicPropertyManager.Properties.Add(DynamicPropertyManager<ViewModel>.CreateProperty<ViewModel, string>($"BookmarkGroupName{i}",
                                                                                                                                OnGetBookmarkGroupName,
                                                                                                                                null));
                }
            }

            public static bool OnGetIsActiveBookmarkGroup(ViewModel vm, string key)
            {
                int bookmarkGroup = int.Parse(key.Substring(key.Length - 1, 1));
                return vm.m_activeBookmarkGroup == bookmarkGroup;
            }

            public static string OnGetBookmarkGroupName(ViewModel vm, string key)
            {
                int bookmarkGroup = int.Parse(key.Substring(key.Length - 1, 1));
                return vm.GetBookmarkGroupName(bookmarkGroup);
            }

            public string GetBookmarkGroupName(int bookmarkGroupIndex)
            {
                return m_settings.BookmarkGroups.FirstOrDefault(x => x.Group == bookmarkGroupIndex)?.Name ?? (bookmarkGroupIndex + 1).ToString();
            }

            public void LoadBookmarks(UserSettings settings, IconCache iconCache, IDirectoryChangeRequest directoryChangeRequester, IPanelLayout panelLayout)
            {
                m_settings = settings;
                m_context = new(iconCache, directoryChangeRequester, panelLayout, this, this);

                // Backward compat.
                foreach (var bookmark in settings.Bookmarks.Where(x => x.BookmarkGroups.Count == 0))
                {
                    bookmark.BookmarkGroups.Add(new UserSettings.BookmarkGroupEntry() { Group = 0 });
                }

                SetActiveBookmarkGroup(settings.ActiveBookmarkGroup);
            }

            public void AddBookmark(string name, string path, bool openInExplorer, int bookmarkGroup)
            {
                if (!m_toolbarBookmarks.Any(x => x.Path.ToLower() == path.ToLower()))
                {
                    var newBookmark = new UserSettings.Bookmark()
                    {
                        Name = name ?? System.IO.Path.GetFileName(path),
                        Path = path,
                        OpenInExplorer = openInExplorer,
                        BookmarkGroups = new() { new UserSettings.BookmarkGroupEntry() { Group = m_activeBookmarkGroup, SortOrder = 99 } }  // Add to end
                    };

                    m_settings.Bookmarks.Add(newBookmark);

                    if (bookmarkGroup == m_activeBookmarkGroup)
                    {
                        m_toolbarBookmarks.Add(new BookmarkViewModel(newBookmark, m_context, this));
                    }
                }
            }
            
            public void DeleteBookmark(BookmarkViewModel bookmark)
            {
                m_toolbarBookmarks.Remove(bookmark);
                m_settings.Bookmarks.RemoveAll(x => x.Path == bookmark.Path);
            }

            public void RefreshActiveBookmarkGroup() => SetActiveBookmarkGroup(m_activeBookmarkGroup);

            public void SetActiveBookmarkGroup(int group)
            {
                m_activeBookmarkGroup = group;
                m_settings.ActiveBookmarkGroup = group;

                m_toolbarBookmarks.Clear();

                List<(UserSettings.Bookmark bookmark, int sortOrder)> bookmarksForGroup = new();

                foreach (var bookmark in GetSortedBookmarks(group))
                {
                    m_toolbarBookmarks.Add(new BookmarkViewModel(bookmark, m_context, this));
                }

                for (int i = 0; i < 6; i++)
                {
                    OnPropertyChanged($"IsActiveBookmarkGroup{i}");
                    OnPropertyChanged($"BookmarkGroupName{i}");
                }
            }

            private IEnumerable<UserSettings.Bookmark> GetSortedBookmarks(int group)
            {
                List<(UserSettings.Bookmark bookmark, int sortOrder)> bookmarksForGroup = new();

                foreach (var bookmark in m_settings.Bookmarks.Where(x => x.BookmarkGroups.Any(x => x.Group == group)))
                {
                    int sortOrderForGroup = bookmark.BookmarkGroups.First(x => x.Group == group).SortOrder;
                    bookmarksForGroup.Add(new(bookmark, sortOrderForGroup));
                }

                return bookmarksForGroup.OrderBy(x => x.sortOrder).Select(x => x.bookmark);
            }

            public void OrganiseBookmarks()
            {
                var dialog = new OrganiseBookmarksDialog(GetSortedBookmarks(m_activeBookmarkGroup), GetBookmarkGroupName(m_activeBookmarkGroup));
                if (dialog.ShowDialog() == true)
                {
                    int currentSortOrder = 0;
                    foreach (var bookmark in dialog.SortedBookmarks)
                    {
                        var group = bookmark.BookmarkGroups.First(x => x.Group == m_activeBookmarkGroup);
                        if (group == null)
                        {
                            group = new UserSettings.BookmarkGroupEntry() { Group = m_activeBookmarkGroup };
                            bookmark.BookmarkGroups.Add(group);
                        }

                        group.SortOrder = currentSortOrder;

                        currentSortOrder++;
                    }

                    if (!string.IsNullOrEmpty(dialog.GroupName))
                    {
                        var existingBookmarkGroup = m_settings.BookmarkGroups.FirstOrDefault(x => x.Group == m_activeBookmarkGroup);
                        if (existingBookmarkGroup == null)
                        {
                            existingBookmarkGroup = new UserSettings.BookmarkGroup() { Group = m_activeBookmarkGroup };
                            m_settings.BookmarkGroups.Add(existingBookmarkGroup);
                        }

                        existingBookmarkGroup.Name = dialog.GroupName;
                    }

                    SetActiveBookmarkGroup(m_activeBookmarkGroup);
                }
            }

        }
    }
}
