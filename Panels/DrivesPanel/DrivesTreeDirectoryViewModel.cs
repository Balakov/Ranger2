using MediaDevices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace Ranger2
{
    public partial class DrivesTree
    {
        public partial class ViewModel
        {
            public class DrivesTreeDirectoryViewModel : Utility.ViewModelBase, IconCache.IIconLoadedNotification
            {
                private ViewModel m_parentViewModel;
                private ViewFilter.ViewMask m_viewMask = 0;
                private MediaDirectoryInfo m_mediaDeviceDrive;

                public string Name { get; set; }
                public string Path { get; set; }

                private ImageSource m_icon;
                public ImageSource Icon
                {
                    get => m_icon;
                    set => OnPropertyChanged(ref m_icon, value);
                }

                private bool m_hasScannedSubdirectories;
                public bool HasScannedSubdirectories
                {
                    get => m_hasScannedSubdirectories;
                    set => OnPropertyChanged(ref m_hasScannedSubdirectories, value);
                }

                private bool m_isExpanded;
                public bool IsExpanded
                {
                    get => m_isExpanded;
                    set
                    {
                        if (OnPropertyChanged(ref m_isExpanded, value) && 
                            value == true && 
                            m_parentViewModel.ShouldEnableExpandEvent)
                        {
                            OnExpand();
                        }
                    }
                }

                private bool m_isSelected;
                public bool IsSelected
                {
                    get => m_isSelected;
                    set
                    {
                        if (OnPropertyChanged(ref m_isSelected, value) && 
                            m_parentViewModel.ShouldEnableSelectEvent)
                        {
                            OnSelect();
                        }
                    }
                }

                public ObservableCollection<DrivesTreeDirectoryViewModel> Directories { get; set; } = new();

                public static DrivesTreeDirectoryViewModel DummyNode(string name = null) => new DrivesTreeDirectoryViewModel(name ?? "Dummy", null, null, null);

                public DrivesTreeDirectoryViewModel(string name, string path, MediaDirectoryInfo mediaDeviceDrive, ViewModel parentViewModel)
                {
                    m_parentViewModel = parentViewModel;
                    m_mediaDeviceDrive = mediaDeviceDrive;

                    Name = name;
                    Path = path;

                    if (!string.IsNullOrEmpty(path))
                    {
                        Directories.Add(DummyNode());
                    }
                }

                private void OnExpand()
                {
                    if (!HasScannedSubdirectories && !string.IsNullOrEmpty(Path))
                    {
                        HasScannedSubdirectories = true;

                        string newDir = Path;
                        Directories.Clear();   // Remove the dummy

                        // Evaulate all of the directories in the new path
                        try
                        {
                            IEnumerable<string> directories;

                            if (m_mediaDeviceDrive != null)
                            {
                                //m_mediaDevice.Connect(MediaDeviceAccess.GenericRead);
                                directories = m_mediaDeviceDrive.EnumerateDirectories().Select(x => x.FullName);
                                //m_mediaDevice.Disconnect();
                            }
                            else
                            {
                                directories = FileSystemEnumeration.EnumerateDirectories(newDir);
                            }

                            foreach (string dir in directories.OrderBy(x => x))
                            {
                                try
                                {
                                    string leafName = System.IO.Path.GetFileName(dir);

                                    FileAttributes attributes = new DirectoryInfo(dir).Attributes;
                                    if ((int)attributes == -1)
                                    {
                                        attributes = FileAttributes.Normal;
                                    }

                                    if (ViewFilter.FilterViewByAttributes(attributes, m_viewMask, leafName.StartsWith("."), out var greyedOut))
                                    {
                                        var directoryViewModel = new DrivesTreeDirectoryViewModel(leafName, dir, null, m_parentViewModel);
                                        m_parentViewModel.m_iconCache.QueueIconLoad(dir, IconCache.IconType.Directory, directoryViewModel);
                                        Directories.Add(directoryViewModel);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var directoryViewModel = DummyNode(ex.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var directoryViewModel = DummyNode(ex.Message);
                        }
                    }
                }

                private void OnSelect()
                {
                    if (!string.IsNullOrEmpty(Path))
                    {
                        m_parentViewModel.SetDirectory(Path);
                    }
                }

                public void IconLoaded(ImageSource icon)
                {
                    Icon = icon;
                }
            }
        }
    }
}

            