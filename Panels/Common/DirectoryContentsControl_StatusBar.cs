
namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public partial class ViewModel
        {
            /*
            private void UpdateStatusBar(bool updateFreeDriveSpace)
            {
                if (updateFreeDriveSpace)
                {
                    m_currentDriveFreeSpace = FileOperations.GetDriveFreeBytes(CurrentPath);
                }

                DriveSpaceStatusLabel.Text = string.Format("Free Space: {0:N0} bytes", m_currentDriveFreeSpace);

                int fileCount = 0;
                int directoryCount = 0;
                long totalSelectedFileSize = 0;
                foreach (ListViewItem lvi in FileListView.SelectedItems)
                {
                    if (lvi.Tag is FileTag)
                    {
                        fileCount++;
                        totalSelectedFileSize += (lvi.Tag as FileTag).FileInfo?.Length ?? 0;
                    }
                    else if (lvi.Tag is DirectoryTag)
                    {
                        directoryCount++;
                    }
                }

                string fileText = string.Empty;
                if (fileCount > 0)
                {
                    string filePluralText = fileCount == 1 ? "file" : "files";
                    string sumText = fileCount == 1 ? "of" : "totalling";
                    string bytesText = totalSelectedFileSize == 1 ? "byte" : "bytes";

                    fileText = $"{fileCount} {filePluralText} {sumText} {totalSelectedFileSize:N0} {bytesText}";
                }

                string folderText = string.Empty;
                if (directoryCount > 0)
                {
                    folderText = directoryCount == 1 ? "1 folder" : $"{directoryCount} folders";
                }

                SelectionStatusLabel.Text = (fileCount > 0 || directoryCount > 0) ? string.Format($"Selected: {folderText} {fileText}") : "";
            }
            */
        }
    }
}