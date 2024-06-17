using HandyControl.Tools.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public interface IScrollIntoViewProvider
        {
            void ScrollIntoView(FileSystemObjectViewModel item);
            void GrabFocus();
        }

        public abstract partial class ViewModel : Utility.ViewModelBase,
                                                  KeySearch.IVisualSearchProvider
        {
            private enum KeyNavigationDirection { Down, Up };

            private string m_currentDirectory;
            private KeySearch m_keySearch;
            private KeyNavigationDirection m_lastKeyNavigationDirection = KeyNavigationDirection.Down;

            private UIElement m_dragDropTarget;
            public void SetDragDropTarget(UIElement element) => m_dragDropTarget = element;

            private IScrollIntoViewProvider m_scrollIntoViewProvider;
            public void SetScrollIntoViewProvider(IScrollIntoViewProvider scrollIntoViewProvider) => m_scrollIntoViewProvider = scrollIntoViewProvider;

            protected PanelContext m_context;
            protected DirectoryScanner m_directoryScanner = new DirectoryScanner();
            protected UserSettings.FilePanelSettings m_settings;
            protected ViewFilter.ViewMask m_viewMask = ViewFilter.ViewMask.ShowHidden | ViewFilter.ViewMask.ShowSystem;
            protected IDirectoryWatcher m_directoryWatcher;
            protected PathHistory m_pathHistory;
            protected bool m_isLoading;

            protected ObservableCollection<FileSystemObjectViewModel> m_files { get; } = new();

            public IEnumerable<FileSystemObjectViewModel> Files => m_files;

            public bool ShowLoadingUI => m_isLoading;
            public bool ShowFilesUI => !m_isLoading && m_files.Count > 0;
            public bool ShowNoFilesUI => !m_isLoading && m_files.Count == 0;

            public ICommand MenuCommandPropmtCommand { get; private set; }
            public ICommand MenuCopyFileListCommand { get; private set; }
            public ICommand MenuNewFolderCommand { get; private set; }
            public ICommand MenuNewFileCommand { get; private set; }

            public BreadcrumbsViewModel Breadcrumbs { get; } = new();
            public ICommand OnBreadcrumbClickedCommand { get; }
            public DriveInfoStatusBar m_statusBar = new DriveInfoStatusBar();

            // Path to select after adding a new file or directory
            private string m_pendingSelelectedPath = null;

            private string m_statusBarDriveSpaceString;
            public string StatusBarDriveSpaceString
            {
                get => m_statusBarDriveSpaceString;
                set => OnPropertyChanged(ref m_statusBarDriveSpaceString, value);
            }

            private string m_statusBarSelectedFilesString;
            public string StatusBarSelectedFilesString
            {
                get => m_statusBarSelectedFilesString;
                set => OnPropertyChanged(ref m_statusBarSelectedFilesString, value);
            }

            private bool m_isCurrentPanel;
            public bool IsCurrentPanel
            {
                get => m_isCurrentPanel;
                set
                {
                    if (OnPropertyChanged(ref m_isCurrentPanel, value))
                    {
                        var dirChange = m_context.DirectoryChangeRequester;

                        if (value)
                        {
                            dirChange.OnDirectoryChanged += OnDirectoryChangedInternal;
                            m_pathHistory.EnablePathChangeCapture();
                        }
                        else
                        {
                            dirChange.OnDirectoryChanged -= OnDirectoryChangedInternal;
                            m_pathHistory.DisablePathChangeCapture();
                        }
                    }
                }
            }

            public string CurrentPath => m_settings.Path;

            public void HistoryBack() => m_pathHistory.Back();
            public void HistoryForward() => m_pathHistory.Forward();

            // Derived class overrides
            public abstract DirectoryListingType ListingType { get; }
            protected abstract void OnDirectoryChanged(string path, string pathToSelect);
            protected abstract void OnActivateSelectedItems();
            protected abstract FileSystemObjectViewModel OnItemAdded(string path);

            protected ViewModel(PanelContext context,
                                UserSettings.FilePanelSettings settings,
                                PathHistory history,
                                IDirectoryWatcher directoryWatcher)
            {
                m_context = context;
                m_settings = settings;
                m_pathHistory = history;
                m_directoryWatcher = directoryWatcher;

                if (!string.IsNullOrEmpty(settings.Path))
                {
                    OnDirectoryChangedInternal(settings.Path, null, m_pathHistory.GetPreviouslySelectedDirectoryForPath(settings.Path));
                    m_pathHistory.PushPath(settings.Path);
                }

                OnBreadcrumbClickedCommand = DelegateCommand.Create((o) =>
                {
                    string path = o as string;
                    m_context.DirectoryChangeRequester.SetDirectory(path, m_pathHistory.GetPreviouslySelectedDirectoryForPath(path));
                });

                m_keySearch = new(this);

                CreateCommands();
            }

            protected void UpdateUIVisibility()
            {
                OnPropertyChanged(nameof(ShowLoadingUI));
                OnPropertyChanged(nameof(ShowFilesUI));
                OnPropertyChanged(nameof(ShowNoFilesUI));
            }

            protected void UpdateStatusBar(bool driveSpaceChanged)
            {
                m_statusBar.UpdateStatus(driveSpaceChanged, this);
            }

            private void OnDirectoryChangedInternal(string path, string previousPath, string pathToSelect)
            {
                if (m_currentDirectory != path || !string.IsNullOrEmpty(pathToSelect))
                {
                    m_currentDirectory = path;
                    m_settings.Path = path;

                    Breadcrumbs.SetPath(path);
                    UpdateStatusBar(true);

                    OnDirectoryChanged(path, pathToSelect);
                }
            }

            protected void SetSelectedFilename(string path)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    var fileToSelect = m_files.FirstOrDefault(x => x.FullPath == path);
                    if (fileToSelect != null)
                    {
                        fileToSelect.IsSelected = true;
                        m_scrollIntoViewProvider.ScrollIntoView(fileToSelect);
                        m_scrollIntoViewProvider.GrabFocus();
                    }
                }
            }

            private bool TryFindFile(string path, out FileSystemObjectViewModel fileViewModel)
            {
                fileViewModel = m_files.FirstOrDefault(x => x.FullPath == path);
                return fileViewModel != null;
            }

            public void RefreshDirectory()
            {
                OnDirectoryChanged(m_currentDirectory, null);
            }

            public void OnCommonMouseDoubleClick(FileSystemObjectViewModel viewModel)
            {
                OnActivateSelectedItems();
            }

            private bool TryGetSelectedFiles(out IEnumerable<FileSystemObjectViewModel> viewModels)
            {
                viewModels = Files.Where(x => x.IsSelected).ToList();
                return viewModels.Any();
            }

            private bool TryGetSelectedFile(out FileSystemObjectViewModel viewModel)
            {
                var selectedFiles = Files.Where(x => x.IsSelected);
                if (selectedFiles.Count() == 1)
                {
                    viewModel = selectedFiles.First();
                    return true;
                }
                else
                {
                    viewModel = null;
                    return false;
                }
            }

            public void OnItemsSelected(System.Collections.IList addedItems)
            {
                UpdateStatusBar(false);
            }

            protected DirectoryViewModel CreateDirectoryViewModel(string directory, ViewFilter.ViewMask viewMask)
            {
                try
                {
                    if (FileSystemObjectViewModel.DirectoryPassesViewFilter(directory, viewMask, out var info))
                    {
                        var directoryViewModel = new DirectoryViewModel(info,
                                                                        m_context.DirectoryChangeRequester,
                                                                        m_context.IconCache,
                                                                        this);
                        return directoryViewModel;
                    }
                }
                catch
                {
                    // We may not be able to create info objects for private network shares 
                }

                return null;
            }

            protected IEnumerable<DirectoryViewModel> CreateDirectoryViewModels(IEnumerable<string> directories)
            {
                List<DirectoryViewModel> viewModels = new();

                foreach (string subdirectory in directories)
                {
                    var directoryViewModel = CreateDirectoryViewModel(subdirectory, m_viewMask);
                    if (directoryViewModel != null)
                    {
                        viewModels.Add(directoryViewModel);
                    }
                }

                return viewModels;
            }

            private void CreateCommands()
            {
                MenuCommandPropmtCommand = DelegateCommand.Create(() =>
                {
                    FileOperations.ExecuteFile("cmd.exe", null, false, CurrentPath);
                });

                MenuCopyFileListCommand = DelegateCommand.Create(() =>
                {
                    try
                    {
                        var files = FileSystemEnumeration.EnumerateFiles(CurrentPath, "*", SearchOption.AllDirectories);
                        if (files.Any())
                        {
                            ClipboardManager.CopyPathsToClipboard(files.Select(x => new ClipboardManager.FileOperationPath() { FullPath = x }),
                                                                  FileOperations.OperationType.Copy,
                                                                  ClipboardManager.ClipboardDataTypes.Text);
                        }
                    }
                    catch { }
                });

                MenuNewFolderCommand = DelegateCommand.Create(() =>
                {
                    AddNewFolder();
                });

                MenuNewFileCommand = DelegateCommand.Create(() =>
                {
                    AddNewFile();
                });
            }

            public void ShowFileProperties()
            {
                var selectedFileViewModels = m_files.Where(x => x.IsSelected);
                int numSelected = selectedFileViewModels.Count();

                if (numSelected == 1)
                {
                    FileOperations.ShowFileProperties(selectedFileViewModels.First().FullPath);
                }
                else if (numSelected > 1)
                {
                    FileOperations.ShowMultiFileProperties(selectedFileViewModels.Select(x => x.FullPath).ToList());
                }
            }
        }

    }
}
