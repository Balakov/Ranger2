using HandyControl.Tools.Command;
using Ranger2.DynamicViewModelProperties;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace Ranger2
{
    public partial class BookmarksPanel
    {
        public class BookmarkViewModel : Utility.ViewModelBase, IconCache.IIconLoadedNotification
        {
            private static DynamicPropertyManager<BookmarkViewModel> s_dynamicPropertyManager = new();

            private BookmarkContext m_context;
            private UserSettings.Bookmark m_bookmark;
            private ViewModel m_parentViewModel;

            private bool m_isFile;
            
            public string Path
            {
                get => m_bookmark.Path;
                set
                {
                    if (Path != value)
                    {
                        m_bookmark.Path = value;
                        OnPropertyChanged(nameof(Path));
                        m_context.IconCache.QueueIconLoad(m_bookmark.Path, m_isFile ? IconCache.IconType.File : IconCache.IconType.Directory, this);
                    }
                }
            }
            
            public string Name
            {
                get => m_bookmark.Name;
                set
                {
                    if (Name != value)
                    {
                        m_bookmark.Name = value;
                        OnPropertyChanged(nameof(Name));
                    }
                }
            }

            public bool OpenInExplorer
            {
                get => m_bookmark.OpenInExplorer;
                set
                {
                    if (OpenInExplorer != value)
                    {
                        m_bookmark.OpenInExplorer = value;
                        OnPropertyChanged(nameof(OpenInExplorer));
                    }
                }
            }

            private ImageSource m_icon;
            public ImageSource Icon
            {
                get => m_icon;
                set => OnPropertyChanged(ref m_icon, value);
            }

            public ICommand ActivateBookmarkCommand { get; }
            public ICommand DeleteBookmarkCommand { get; }
            public ICommand EditBookmarkCommand { get; }
            public ICommand OrganiseBookmarksCommand { get; }

            public BookmarkViewModel(UserSettings.Bookmark bookmark, BookmarkContext context, ViewModel parentViewModel)
            {
                m_bookmark = bookmark;
                m_isFile = IsFile(bookmark.Path);
                m_context = context;
                m_parentViewModel = parentViewModel;

                context.IconCache.QueueIconLoad(m_bookmark.Path, m_isFile ? IconCache.IconType.File : IconCache.IconType.Directory, this);

                ActivateBookmarkCommand = DelegateCommand.Create(Activate);
                DeleteBookmarkCommand = DelegateCommand.Create(Delete);
                EditBookmarkCommand = DelegateCommand.Create(EditBookmark);
                OrganiseBookmarksCommand = DelegateCommand.Create(() => m_parentViewModel.OrganiseBookmarks());
            }

            private bool IsFile(string path)
            {
                return path.StartsWith("\\\\") ? false : FileSystemEnumeration.FileExists(m_bookmark.Path);
            }

            public static void SetupDynamicProperties()
            {
                for (int i = 0; i < 4; i++)
                {
                    s_dynamicPropertyManager.Properties.Add(DynamicPropertyManager<BookmarkViewModel>.CreateProperty<BookmarkViewModel, bool>($"IsInBookmarkGroup{i}",
                                                                                                                                              OnGetDynamicProperty,
                                                                                                                                              OnSetDynamicProperty,
                                                                                                                                              null));
                }
            }

            public void IconLoaded(ImageSource icon)
            {
                Icon = icon;
            }

            private void Activate()
            {
                if (OpenInExplorer)
                {
                    FileOperations.ExecuteWithExplorer(m_bookmark.Path);
                }
                else
                {
                    string directory = m_isFile ? System.IO.Path.GetDirectoryName(m_bookmark.Path) : m_bookmark.Path;
                    m_context.DirectoryChangeRequester.SetDirectory(directory, m_bookmark.Path);
                }
            }

            private void Delete()
            {
                m_context.BookmarkDeleter.DeleteBookmark(this);
            }

            private void EditBookmark()
            {
                var dialog = new EditBookmarkDialog(m_bookmark);
                if(dialog.ShowDialog() == true)
                {
                    m_isFile = IsFile(dialog.RenamedBookmarkPath);
                    Name = dialog.RenamedBookmark;
                    Path = dialog.RenamedBookmarkPath;
                }
            }

            public static bool OnGetDynamicProperty(BookmarkViewModel vm, string key)
            {
                int bookmarkGroup = int.Parse(key.Substring(key.Length - 1, 1));
                return vm.m_bookmark.BookmarkGroups.Any(x => x.Group == bookmarkGroup);
            }

            public static void OnSetDynamicProperty(BookmarkViewModel vm, string key, bool value)
            {
                int bookmarkGroup = int.Parse(key.Substring(key.Length - 1, 1));

                if (value)
                {
                    if (!vm.m_bookmark.BookmarkGroups.Any(x => x.Group == bookmarkGroup))
                    {
                        vm.m_bookmark.BookmarkGroups.Add(new UserSettings.BookmarkGroupEntry() { Group = bookmarkGroup });
                    }
                }
                else
                {
                    vm.m_bookmark.BookmarkGroups.RemoveAll(x => x.Group == bookmarkGroup);
                    vm.m_context.BookmarkGroupRefresher.RefreshActiveBookmarkGroup();
                }
            }

        }
    }
}