using System;
using System.IO;
using System.Linq;

namespace Ranger2
{
    public partial class FilesPanel
    {
        public class ViewModel : DirectoryContentsControl.ViewModel
        {
            public class FileViewModel : FileSystemObjectViewModel
            {
                private readonly PanelContext m_context;

                public FileViewModel(string name,
                                     FileSystemObjectInfo info,
                                     PanelContext context,
                                     DirectoryContentsControl.ViewModel parentViewModel) : base(name, info, parentViewModel)
                {
                    m_context = context;
                    m_context.IconCache.QueueIconLoad(info.Path, IconCache.IconType.File, this);
                }

                public override bool CanRename => true;
                public override bool CanDelete => CanRename;

                public override void OnActivate()
                {
                    if (m_context.ImageCache.ShouldDecodeOnFileClick(FullPath))
                    {
                        var window = new ImageViewer(m_context.ImageCache, FullPath);
                        window.Show();
                        window.Activate();
                    }
                    else
                    {
                        FileOperations.ExecuteFile(FullPath);
                    }
                }
            }

            // ViewModel

            public override DirectoryContentsControl.DirectoryListingType ListingType => DirectoryContentsControl.DirectoryListingType.Files;
            public override void SetThumbnailSize(DirectoryContentsControl.ThumbnailSizeType size) { }
            public override bool ShowThumbnailSizeSelector => false;

            public ViewModel(PanelContext context,
                             UserSettings.FilePanelSettings settings,
                             PathHistory pathHistory,
                             IDirectoryWatcher directoryWatcher) : base(context, settings, pathHistory, directoryWatcher)
            {
                m_directoryScanner.OnDirectoryScanComplete += OnDirectoryScanComplete;
            }

            protected override void OnActivateSelectedItems()
            {
                m_visualOrderProvider?.GetVisualItems().FirstOrDefault(x => x.IsSelected)?.OnActivate();
            }

            protected override void OnDirectoryChanged(string path, string pathToSelect)
            {
                m_files.Clear();
                m_isLoading = true;
                UpdateUIVisibility();

                if (pathToSelect == null)
                {
                    pathToSelect = m_pathHistory.GetPreviouslySelectedDirectoryForPath(path);
                }

                m_directoryScanner.ScanDirectory(path, pathToSelect);
            }

            protected void OnDirectoryScanComplete(DirectoryScanner.ScanResult scanResult)
            {
                if (!scanResult.MatchesPath(m_settings.Path))
                    return;

                m_directoryWatcher.EnableDirectoryWatcher(m_settings.Path);

                try
                {
                    // Directories

                    foreach (var viewModel in CreateDirectoryViewModels(scanResult.Directories))
                    {
                        m_files.Add(viewModel);
                    }

                    // Files
                    {
                        foreach (var file in scanResult.Files)
                        {
                            OnItemAdded(file.Path);
                        }

                        SetSelectedFilename(scanResult.PathToSelect);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }

                m_isLoading = false;
                UpdateUIVisibility();
            }

            protected override FileSystemObjectViewModel OnItemAdded(string path)
            {
                try
                {
                    var fi = new FileInfo(path);

                    if (FileSystemObjectViewModel.FilePassesViewFilter(path, m_viewMask, out var info))
                    {
                        //bool isShortcut = Path.GetExtension(fi.FullName).ToLower() == ".lnk";
                        var fileViewModel = new FileViewModel(null, info, m_context, this);
                        m_files.Add(fileViewModel);
                        return fileViewModel;
                    }
                }
                catch
                {
                    // Ignore files we don't have permission to read
                }
                    
                return null;
            }
        }
        
    }
}
