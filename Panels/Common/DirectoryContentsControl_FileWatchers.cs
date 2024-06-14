using System.IO;

namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public partial class ViewModel
        {
            public void OnFileSystemChange(string path, string oldPath, WatcherChangeTypes changeType)
            {
                switch (changeType)
                {
                    case WatcherChangeTypes.Created:
                        if (Directory.Exists(path))
                        {
                            var directoryViewModel = CreateDirectoryViewModel(path, m_viewMask);
                            if (directoryViewModel != null)
                            {
                                m_files.Add(directoryViewModel);
                                UpdateUIVisibility();
                                UpdateStatusBar(driveSpaceChanged: true);
                            }
                        }
                        else if (File.Exists(path))
                        {
                            OnItemAdded(path);
                            UpdateUIVisibility();
                            UpdateStatusBar(driveSpaceChanged: true);
                        }
                        break;
                    case WatcherChangeTypes.Deleted:
                        if (TryFindFile(path, out var deletedFileViewModel))
                        {
                            m_files.Remove(deletedFileViewModel);
                            UpdateUIVisibility();
                            UpdateStatusBar(driveSpaceChanged: true);
                        }
                        break;
                    case WatcherChangeTypes.Changed:
                        if (TryFindFile(path, out var changedViewModel))
                        {
                            if (Directory.Exists(path))
                            {
                                if (FileSystemObjectViewModel.DirectoryPassesViewFilter(path, m_viewMask, out var info))
                                {
                                    changedViewModel.UpdateView(info);
                                }
                            }
                            else if (File.Exists(path))
                            {
                                if (FileSystemObjectViewModel.FilePassesViewFilter(path, m_viewMask, out var info))
                                {
                                    changedViewModel.UpdateView(info);
                                }
                            }
                        }
                        break;
                    case WatcherChangeTypes.Renamed:
                        if (TryFindFile(oldPath, out var renamedViewModel))
                        {
                            m_files.Remove(renamedViewModel);

                            if (Directory.Exists(path))
                            {
                                var directoryViewModel = CreateDirectoryViewModel(path, m_viewMask);
                                if (directoryViewModel != null)
                                {
                                    m_files.Add(directoryViewModel);
                                }
                            }
                            else if (File.Exists(path))
                            {
                                OnItemAdded(path);
                            }
                        }
                        break;
                    //break;
                    case WatcherChangeTypes.All:
                        // Rescan the entire directory
                        OnDirectoryChanged(m_currentDirectory, null);
                        break;
                }
            }
        }
    }
}

