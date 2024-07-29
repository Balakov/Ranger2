using HandyControl.Tools.Command;
using MediaDevices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Ranger2
{
    public partial class DrivesTree
    {
        public partial class ViewModel : Utility.ViewModelBase, IDirectoryChangeRequest
        {
            private Utility.EventHandlerSuppressor m_expandEventSuppressor = new();
            public bool ShouldEnableExpandEvent => m_expandEventSuppressor.ShouldEnableEventHandlers;

            private Utility.EventHandlerSuppressor m_setDirectoryEventSuppressor = new();
            public bool ShouldEnableSelectEvent => m_expandEventSuppressor.ShouldEnableEventHandlers;
            
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

                m_panelLayout.OnSwitchPanelFocus += (panel) =>
                {
                    m_setDirectoryEventSuppressor.DisableEventHandlers();
                    SetDirectory(panel.CurrentPath);
                    m_setDirectoryEventSuppressor.EnableEventHandlers();
                };

                CollapseTreeCommand = DelegateCommand.Create(() =>
                {
                    m_expandEventSuppressor.DisableEventHandlers();
                    CollapseTree(Directories, startLevel: 2);
                    m_expandEventSuppressor.EnableEventHandlers();
                });

                // Called from View
                SinglePanelCommand = DelegateCommand.Create(() => m_panelLayout.SetActivePanelCount(1));
                DualPanelCommand = DelegateCommand.Create(() => m_panelLayout.SetActivePanelCount(2));
                TriplePanelCommand = DelegateCommand.Create(() => m_panelLayout.SetActivePanelCount(3));

                var root = new DrivesTreeDirectoryViewModel("Computer", null, null, this);
                Directories.Add(root);

                DrivesTreeDirectoryViewModel firstReadyDrive = OnDrivesChanged();

                root.IsExpanded = true;

                if (firstReadyDrive != null)
                {
                    firstReadyDrive.IsExpanded = true;
                }
            }

            public DrivesTreeDirectoryViewModel OnDrivesChanged()
            {
                DrivesTreeDirectoryViewModel firstReadyDrive = null;

                Dictionary<string, DrivesTreeDirectoryViewModel> existingDrives = new();

                var rootDirectories = Directories.FirstOrDefault()?.Directories;
                if (rootDirectories != null)
                {
                    foreach (var drive in rootDirectories)
                    {
                        existingDrives.Add(drive.Path, drive);
                    }
                }

                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && !App.UserSettings.IgnoredDrives.Contains(drive.VolumeLabel))
                    {
                        string driveRootDirectory = drive.RootDirectory.FullName;

                        // Add new drives                        
                        if (!existingDrives.ContainsKey(driveRootDirectory))
                        {
                            var driveViewModel = new DrivesTreeDirectoryViewModel($"{drive.Name} ({drive.VolumeLabel})", drive.RootDirectory.FullName, null, this);
                            m_iconCache.QueueIconLoad(drive.RootDirectory.FullName, IconCache.IconType.FixedDrive, driveViewModel);

                            rootDirectories.Add(driveViewModel);

                            firstReadyDrive = firstReadyDrive ?? driveViewModel;
                        }
                        else
                        {
                            existingDrives.Remove(driveRootDirectory);
                        }
                    }
                }

                List<MediaDevice> mediaDevices = MediaDevice.GetDevices().ToList();
                foreach (var mediaDevice in mediaDevices)
                {
                    mediaDevice.Connect();
                    if (mediaDevice.DeviceType == DeviceType.Generic)
                    {
                        foreach (var drive in mediaDevice.GetDrives())
                        {
                            MediaDirectoryInfo driveRoot = drive.RootDirectory != null ? drive.RootDirectory
                                                                                       : mediaDevice.GetRootDirectory();
                            string driveName = !string.IsNullOrEmpty(drive.Name) ? drive.Name 
                                                                                 : mediaDevice.FriendlyName;

                            if (!existingDrives.ContainsKey(driveName))
                            {
                                var driveViewModel = new DrivesTreeDirectoryViewModel($"{driveName}", driveRoot.FullName, driveRoot, this);
                                m_iconCache.QueueIconLoad(null, IconCache.IconType.RemovableDrive, driveViewModel);
                        
                                rootDirectories.Add(driveViewModel);
                            }
                            else
                            {
                                existingDrives.Remove(mediaDevice.FriendlyName);
                            }
                        }
                    }
                    //mediaDevice.Disconnect();
                }

                foreach (var removedDrivePair in existingDrives)
                {
                    rootDirectories.Remove(removedDrivePair.Value);
                }

                return firstReadyDrive;
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

            public void SetDirectory(string path, string pathToSelect = null)
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
                        if (childDirectory.Path?.ToLower() == currentPath)
                        {
                            if (i == pathSplit.Length - 1)
                            {
                                // Don't expand the leaf directory automatically, just select it.
                                m_expandEventSuppressor.DisableEventHandlers();
                                UnSelectAll(Directories.First());
                                childDirectory.IsSelected = true;
                                m_expandEventSuppressor.EnableEventHandlers();
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

                if (m_setDirectoryEventSuppressor.ShouldEnableEventHandlers)
                {
                    OnDirectoryChanged?.Invoke(path, m_panelLayout.CurrentPanel?.CurrentPath, pathToSelect);
                }
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
