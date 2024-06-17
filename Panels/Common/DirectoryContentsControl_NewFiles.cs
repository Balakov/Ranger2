using System.IO;

namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public partial class ViewModel
        {
            public void AddNewFile()
            {
                string newFilePath = Path.Combine(CurrentPath, "New File.txt");
                int i = 0;

                while (FileSystemEnumeration.FileExists(newFilePath))
                {
                    i++;
                    if (i > 99)
                    {
                        return;
                    }

                    newFilePath = Path.Combine(CurrentPath, $"New File {i}.txt");
                }

                var dialog = new Dialogs.RenameDialog(newFilePath, "New File", allowSameName: true);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        File.WriteAllText(dialog.RenamedFile, string.Empty);
                        m_pendingSelelectedPath = dialog.RenamedFile;
                    }
                    catch { }
                }
            }

            public void AddNewFolder()
            {
                string newFolderPath = Path.Combine(CurrentPath, "New Folder");
                int i = 0;

                while (FileSystemEnumeration.DirectoryExists(newFolderPath))
                {
                    i++;
                    if (i > 99)
                    {
                        return;
                    }

                    newFolderPath = Path.Combine(CurrentPath, $"New Folder {i}");
                }

                var dialog = new Dialogs.RenameDialog(newFolderPath, "New Folder", allowSameName: true);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        Directory.CreateDirectory(dialog.RenamedFile);
                        m_pendingSelelectedPath = dialog.RenamedFile;
                    }
                    catch { }
                }
            }

            public void AddNewShortcut(string path, string sourcePath)
            {
                vbAccelerator.Components.Shell.ShellLink link = new();
                link.ShortCutFile = path;
                link.Target = sourcePath;
                link.Save();
            }

        }
    }
}