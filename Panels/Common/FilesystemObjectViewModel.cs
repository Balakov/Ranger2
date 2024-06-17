using HandyControl.Tools.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;

namespace Ranger2
{
    public abstract class FileSystemObjectViewModel : Utility.ViewModelBase, IconCache.IIconLoadedNotification
    {
        public class FileSystemObjectInfo
        {
            public readonly string Path;
            public readonly FileAttributes Attribs;
            public readonly ulong Size;
            public readonly DateTime LastWriteTime;
            public readonly bool IsGreyedOut;
            public readonly bool IsDirectory;

            public FileSystemObjectInfo(string path, FileAttributes attribs, ulong size, DateTime lastWriteTime, bool isDirectory, bool isGreyedOut)
            {
                Path = path;
                Attribs = attribs;
                Size = size;
                LastWriteTime = lastWriteTime;
                IsGreyedOut = isGreyedOut;
                IsDirectory = isDirectory;
            }
        }

        public static class Sentinels
        {
            public const FileAttributes InvalidAttributes = 0;
            public static readonly DateTime InvalidDate = DateTime.MinValue;
        }

        private string m_fullPath;
        public string FullPath 
        {
            get => m_fullPath;
            set => OnPropertyChanged(ref m_fullPath, value);
        }

        private string m_name;
        public string Name 
        {
            get => m_name;
            set => OnPropertyChanged(ref m_name, value);
        }

        private string m_nameSortValue;
        public string NameSortValue
        {
            get => m_nameSortValue;
            set => OnPropertyChanged(ref m_nameSortValue, value);
        }

        private string m_sizeString;
        public string SizeString 
        {
            get => m_sizeString;
            set => OnPropertyChanged(ref m_sizeString, value);
        }

        private ulong m_sizeSortValue;
        public ulong SizeSortValue
        {
            get => m_sizeSortValue;
            set => OnPropertyChanged(ref m_sizeSortValue, value);
        }

        private string m_attribsString;
        public string AttribsString 
        {
            get => m_attribsString;
            set => OnPropertyChanged(ref m_attribsString, value);
        }

        private string m_dateString;
        public string DateString 
        {
            get => m_dateString;
            set => OnPropertyChanged(ref m_dateString, value);
        }

        private DateTime m_dateSortValue;
        public DateTime DateSortValue
        {
            get => m_dateSortValue;
            set => OnPropertyChanged(ref m_dateSortValue, value);
        }

        private bool m_isGreyedOut;
        public bool IsGreyedOut 
        {
            get => m_isGreyedOut;
            set => OnPropertyChanged(ref m_isGreyedOut, value);
        }

        private ImageSource m_icon;
        public ImageSource Icon
        {
            get => m_icon;
            set => OnPropertyChanged(ref m_icon, value);
        }

        private bool m_isSelected;
        public bool IsSelected
        {
            get => m_isSelected;
            set => OnPropertyChanged(ref m_isSelected, value);
        }

        protected DirectoryContentsControl.ViewModel m_parentViewModel;

        public abstract bool CanRename { get; }
        public abstract bool CanDelete { get; }

        public ICommand MenuOpenCommand { get; private set; }
        public ICommand MenuOpenWithCommand { get; private set; }
        public ICommand MenuOpenInExplorerCommand { get; private set; }
        public ICommand MenuEditCommand { get; private set; }
        public ICommand MenuCommandPropmtCommand { get; private set; }
        public ICommand MenuCopyFileListCommand { get; private set; }
        public ICommand MenuNewFolderCommand { get; private set; }
        public ICommand MenuNewFileCommand { get; private set; }
        public ICommand MenuShortcutNewCommand { get; private set; }
        public ICommand MenuCopyCommand { get; private set; }
        public ICommand MenuCutCommand { get; private set; }
        public ICommand MenuPasteCommand { get; private set; }
        public ICommand MenuPropertiesCommand { get; private set; }

        public FileSystemObjectViewModel(string name, FileSystemObjectInfo info, DirectoryContentsControl.ViewModel parentViewModel)
        {
            m_parentViewModel = parentViewModel;

            if (name == null && info.Path.StartsWith("\\\\"))
            {
                // Path.GetFileName fails on UNC paths
                string[] split = info.Path.TrimEnd('\\').Split('\\');
                Name = split[split.Length - 1];
            }
            else
            {
                Name = name ?? Path.GetFileName(info.Path);
            }

            FullPath = info.Path;

            NameSortValue = (info.IsDirectory ? "A" : "Z") + Name;
            SizeSortValue = info.Size;
            DateSortValue = info.IsDirectory ? info.LastWriteTime - TimeSpan.FromDays(365 * 100) : info.LastWriteTime;

            UpdateView(info);
            CreateCommands();
        }

        public static bool FilePassesViewFilter(string path, ViewFilter.ViewMask viewMask, out FileSystemObjectInfo info)
        {
            FileInfo fi = new FileInfo(path);

            FileAttributes fileAttribs = Sentinels.InvalidAttributes;
            ulong fileSize = 0;
            DateTime lastWriteTime = Sentinels.InvalidDate;

            try
            {
                fileAttribs = fi.Attributes;
                fileSize = (ulong)fi.Length;
                lastWriteTime = fi.LastWriteTime;
            }
            catch { }

            if (ViewFilter.FilterViewByAttributes(fileAttribs, viewMask, fi.Name.StartsWith("."), out var isGreyedOut))
            {
                info = new FileSystemObjectInfo(path, fileAttribs, fileSize, lastWriteTime, isDirectory: false, isGreyedOut);
                return true;
            }
            else
            {
                info = null;
                return false;
            }
        }

        public static bool DirectoryPassesViewFilter(string path, ViewFilter.ViewMask viewMask, out FileSystemObjectInfo info)
        {
            DirectoryInfo di = new DirectoryInfo(path);

            FileAttributes fileAttribs = Sentinels.InvalidAttributes;
            DateTime lastWriteTime = Sentinels.InvalidDate;

            try
            {
                fileAttribs = di.Attributes;
                lastWriteTime = di.LastWriteTime;
            }
            catch { }

            if (ViewFilter.FilterViewByAttributes(fileAttribs, viewMask, di.Name.StartsWith("."), out var isGreyedOut))
            {
                info = new FileSystemObjectInfo(path, fileAttribs, 0, lastWriteTime, isDirectory: true, isGreyedOut);
                return true;
            }
            else
            {
                info = null;
                return false;
            }
        }

        public void UpdateView(FileSystemObjectInfo info)
        {
            AttribsString = AttribsToString(info.Attribs);
            IsGreyedOut = info.IsGreyedOut;

            if (info.IsDirectory)
            {
                SizeString = "<Folder>";
            }
            else
            {
                SizeString = NumberFormatter.FormatBytes(info.Size);
            }

            if (info.LastWriteTime != Sentinels.InvalidDate)
            {
                DateString = info.LastWriteTime.ToString("dd/MM/yyyy HH:mm:ss");
            }
        }

        public void IconLoaded(ImageSource icon)
        {
            Icon = icon;
        }

        public virtual void OnActivate()
        {
        }

        protected static string AttribsToString(FileAttributes attribs)
        {
            StringBuilder sb = new StringBuilder(3);

            if (attribs.HasFlag(FileAttributes.ReadOnly)) sb.Append('R');
            if (attribs.HasFlag(FileAttributes.Hidden)) sb.Append('H');
            if (attribs.HasFlag(FileAttributes.System)) sb.Append('S');

            return sb.ToString();
        }

        private void CreateCommands()
        {
            MenuOpenCommand = DelegateCommand.Create(() =>
            {
                OnActivate();
            });

            MenuOpenWithCommand = DelegateCommand.Create(() =>
            {
                // Do NOT add quotes around the path. This makes it not work.
                FileOperations.ExecuteFile("rundll32.exe", "shell32.dll, OpenAs_RunDLL " + FullPath, quoteArgs: false);
            });

            MenuOpenInExplorerCommand = DelegateCommand.Create(() =>
            {
                string directory = FileSystemEnumeration.DirectoryExists(FullPath) ? FullPath : m_parentViewModel.CurrentPath;
                FileOperations.ExecuteWithExplorer(directory);
            });

            MenuCommandPropmtCommand = DelegateCommand.Create(() =>
            {
                string directory = FileSystemEnumeration.DirectoryExists(FullPath) ? FullPath : m_parentViewModel.CurrentPath;
                FileOperations.ExecuteFile("cmd.exe", null, false, directory);
            });

            MenuEditCommand = DelegateCommand.Create(() =>
            {
                if (FileSystemEnumeration.FileExists(FullPath))
                {
                    if (App.UserSettings.DefaultEditorPath == null || !FileSystemEnumeration.FileExists(App.UserSettings.DefaultEditorPath))
                    {
                        var dialog = new Microsoft.Win32.OpenFileDialog();
                        dialog.Filter = App.UserSettings.DefaultEditorPath;
                        dialog.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        dialog.Multiselect = false;
                        dialog.Title = "Choose Default Editor";
                        dialog.Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*";

                        if (dialog.ShowDialog(System.Windows.Application.Current.MainWindow) == true)
                        {
                            App.UserSettings.DefaultEditorPath = dialog.FileName;
                        }
                    }

                    if (App.UserSettings.DefaultEditorPath != null && FileSystemEnumeration.FileExists(App.UserSettings.DefaultEditorPath))
                    {
                        List<string> selectedPaths = new List<string>();
                        FileOperations.ExecuteFile(App.UserSettings.DefaultEditorPath, FullPath);
                    }
                }
            });

            MenuCopyFileListCommand = DelegateCommand.Create(() =>
            {
                string directory = FileSystemEnumeration.DirectoryExists(FullPath) ? FullPath : m_parentViewModel.CurrentPath;

                try
                {
                    var files = FileSystemEnumeration.EnumerateFiles(directory, "*", SearchOption.AllDirectories);
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
                m_parentViewModel.AddNewFolder();
            });

            MenuNewFileCommand = DelegateCommand.Create(() => 
            {
                m_parentViewModel.AddNewFile();
            });

            MenuShortcutNewCommand = DelegateCommand.Create(() => 
            {
                string directory = Path.GetDirectoryName(FullPath);
                string fileName = Path.GetFileNameWithoutExtension(FullPath) + " - Shortcut.lnk";
                string destination = Path.Combine(directory, fileName);

                if (!FileSystemEnumeration.FileExists(destination))
                {
                    m_parentViewModel.AddNewShortcut(destination, FullPath);
                }
            });

            MenuCopyCommand = DelegateCommand.Create(() => 
            {
                m_parentViewModel.HandleCopy(isCut: false);
            });

            MenuCutCommand = DelegateCommand.Create(() => 
            {
                m_parentViewModel.HandleCopy(isCut: true);
            });

            MenuPasteCommand = DelegateCommand.Create(() => 
            {
                m_parentViewModel.HandlePaste();
            });

            MenuPropertiesCommand = DelegateCommand.Create(() => 
            {
                m_parentViewModel.ShowFileProperties();
            });
        }
    }

    // Directory View Model

    public class DirectoryViewModel : FileSystemObjectViewModel
    {
        private readonly IDirectoryChangeRequest m_directoryChangeRequester;

        public DirectoryViewModel(FileSystemObjectInfo info,
                                  IDirectoryChangeRequest directoryChangeRequester,
                                  IconCache iconCache,
                                  DirectoryContentsControl.ViewModel parentViewModel) : base(null, info, parentViewModel)
        {
            m_directoryChangeRequester = directoryChangeRequester;
            iconCache.QueueIconLoad(info.Path, IconCache.IconType.Directory, this);
        }

        public override void OnActivate()
        {
            m_directoryChangeRequester.SetDirectory(FullPath, null);
        }

        public override bool CanRename => true;
        public override bool CanDelete => CanRename;
    }
}
