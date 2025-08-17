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
                        {
                            FileSystemObjectViewModel newItem = null;
                            if (FileSystemEnumeration.DirectoryExists(path))
                            {
                                newItem = CreateDirectoryViewModel(path, m_viewMask);
                                if (newItem != null)
                                {
                                    m_files.Add(newItem);
                                    UpdateUIVisibility();
                                    UpdateStatusBar(driveSpaceChanged: true);
                                }
                            }
                            else if (FileSystemEnumeration.FileExists(path))
                            {
                                newItem = OnItemAdded(path);
                                UpdateUIVisibility();
                                UpdateStatusBar(driveSpaceChanged: true);
                            }

                            if (newItem != null && newItem.FullPath == m_pendingSelelectedPath)
                            {
                                ClearSelection();
                                newItem.IsSelected = true;
                                m_pendingSelelectedPath = null;
                            }
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
                            if (FileSystemEnumeration.DirectoryExists(path))
                            {
                                if (FileSystemObjectViewModel.DirectoryPassesViewFilter(path, m_viewMask, out var info))
                                {
                                    changedViewModel.UpdateView(info);
                                }
                            }
                            else if (FileSystemEnumeration.FileExists(path))
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

                            if (FileSystemEnumeration.DirectoryExists(path))
                            {
                                var directoryViewModel = CreateDirectoryViewModel(path, m_viewMask);
                                if (directoryViewModel != null)
                                {
                                    m_files.Add(directoryViewModel);
                                }
                            }
                            else if (FileSystemEnumeration.FileExists(path))
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

