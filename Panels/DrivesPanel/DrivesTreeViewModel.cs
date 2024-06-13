using HandyControl.Tools.Command;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Ranger2
{
    public sealed partial class DrivesTree
    {
        private partial class ViewModel : Utility.UndoableViewModelBase, IDirectoryChangeRequest
        {
            private IconCache m_iconCache;
            private IPanelLayout m_panelLayout;

            public ObservableCollection<DrivesTreeDirectoryViewModel> Directories { get; set; } = new();

            public ICommand CollapseTreeCommand { get; }
            public ICommand SinglePanelCommand { get; }
            public ICommand DualPanelCommand { get; }
            public ICommand TriplePanelCommand { get; }

            public event OnDirectoryChangedDelegate OnDirectoryChanged;

            public ViewModel(IconCache iconCache, IPanelLayout panelLayout)
            {
                m_iconCache = iconCache;
                m_panelLayout = panelLayout;

                CollapseTreeCommand = DelegateCommand.Create(() =>
                {
                    DisableEventHandlers();
                    CollapseTree(Directories, startLevel: 2);
                    EnableEventHandlers();
                });

                // Called from View
                SinglePanelCommand = DelegateCommand.Create(() => m_panelLayout.SetActivePanelCount(1));
                DualPanelCommand = DelegateCommand.Create(() => m_panelLayout.SetActivePanelCount(2));
                TriplePanelCommand = DelegateCommand.Create(() => m_panelLayout.SetActivePanelCount(3));

                var root = new DrivesTreeDirectoryViewModel("Computer", null, this);
                Directories.Add(root);

                DrivesTreeDirectoryViewModel firstReadyDrive = null;

                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        var driveViewModel = new DrivesTreeDirectoryViewModel($"{drive.Name} ({drive.VolumeLabel})", drive.RootDirectory.FullName, this);
                        m_iconCache.QueueIconLoad(drive.RootDirectory.FullName, IconCache.IconType.Drive, driveViewModel);

                        root.Directories.Add(driveViewModel);

                        firstReadyDrive = firstReadyDrive ?? driveViewModel;
                    }
                }

                root.IsExpanded = true;

                if (firstReadyDrive != null)
                {
                    firstReadyDrive.IsExpanded = true;
                }
            }

            private void CollapseTree(IEnumerable<DrivesTreeDirectoryViewModel> nodes, int startLevel, int currentLevel = 0)
            {
                foreach (var node in nodes)
                {
                    CollapseTree(node.Directories, startLevel, currentLevel + 1);

                    if (currentLevel >= startLevel && node.IsExpanded)
                    {
                        node.IsExpanded = false;
                    }
                }
            }

            public void SetDirectory(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return;

                // Expand the nodes of the tree up to the selected directory
                DrivesTreeDirectoryViewModel currentDirectory = Directories.First();

                var pathSplit = path.ToLower().Split([Path.DirectorySeparatorChar, 
                                                      Path.AltDirectorySeparatorChar]);

                string currentPath = string.Empty;

                for (int i=0; i < pathSplit.Length; i++)
                {
                    string pathPart = pathSplit[i];

                    if (string.IsNullOrEmpty(currentPath))
                    {
                        currentPath = pathPart + Path.DirectorySeparatorChar;
                    }
                    else
                    {
                        currentPath = Path.Combine(currentPath, pathPart);
                    }

                    foreach (var childDirectory in currentDirectory.Directories)
                    {
                        if (childDirectory.Path.ToLower() == currentPath)
                        {
                            if (i == pathSplit.Length - 1)
                            {
                                // Don't expand the leaf directory automatically, just select it.
                                DisableEventHandlers();
                                UnSelectAll(Directories.First());
                                childDirectory.IsSelected = true;
                                EnableEventHandlers();
                            }
                            else
                            {
                                childDirectory.IsExpanded = true;
                            }

                            currentDirectory = childDirectory;
                            break;
                        }
                    }
                }

                OnDirectoryChanged?.Invoke(path, m_panelLayout.CurrentPanel?.CurrentPath);
            }

            private void UnSelectAll(DrivesTreeDirectoryViewModel root)
            {
                root.IsSelected = false;
                foreach (var child in root.Directories)
                {
                    UnSelectAll(child);
                }
            }

            public void SetDirectoryToParent()
            {
                string currentPath = m_panelLayout.CurrentPanel?.CurrentPath;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    string newPath = Path.GetFullPath(Path.Combine(currentPath, ".."));
                    if (newPath != null)
                    {
                        SetDirectory(newPath);
                    }
                }
            }

        }
    }
}
