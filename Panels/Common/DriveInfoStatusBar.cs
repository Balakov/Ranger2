using System;

namespace Ranger2
{
    public class DriveInfoStatusBar
    {
        private System.Windows.Threading.DispatcherTimer m_updateTimer = new();
        private readonly TimeSpan m_updateDelay = TimeSpan.FromMilliseconds(250);
        private bool m_updateFreeDriveSpace;
        private DirectoryContentsControl.ViewModel m_viewModel; // We assume all updates are only called fron a single view model.

        public DriveInfoStatusBar()
        {
            m_updateTimer.Interval = m_updateDelay;
            m_updateTimer.Tick += (s, e) =>
            {
                UpdateStatusInternal();
                m_updateTimer.Stop();
            };
        }

        public void UpdateStatus(bool updateFreeDriveSpace, DirectoryContentsControl.ViewModel vm)
        {
            if (updateFreeDriveSpace)
            {
                m_updateFreeDriveSpace = updateFreeDriveSpace;
            }

            m_viewModel = vm;
            m_updateTimer.Stop();
            m_updateTimer.Start();
        }

        private void UpdateStatusInternal()
        {
            if (m_updateFreeDriveSpace)
            {
                ulong currentDriveFreeSpace = FileOperations.GetDriveFreeBytes(m_viewModel.CurrentPath);
                m_viewModel.StatusBarDriveSpaceString = $"Free space {NumberFormatter.FormatBytes(currentDriveFreeSpace)}";
                m_updateFreeDriveSpace = false;
            }

            int selectedFileCount = 0;
            int selectedDirectoryCount = 0;
            ulong selectedFileSize = 0;

            int totalFileCount = 0;
            int totalDirectoryCount = 0;
            ulong totalFileSize = 0;

            foreach (var item in m_viewModel.Files)
            {
                if (item is DirectoryViewModel)
                {
                    totalDirectoryCount++;
                    if (item.IsSelected)
                    {
                        selectedDirectoryCount++;
                    }
                }
                else
                {
                    totalFileCount++;
                    totalFileSize += item.SizeSortValue;

                    if (item.IsSelected)
                    {
                        selectedFileCount++;
                        selectedFileSize += item.SizeSortValue;
                    }
                }
            }

            bool showSelectedInfo = selectedFileCount > 0;

            int fileCount = showSelectedInfo ? selectedFileCount : totalFileCount;
            ulong fileSize = showSelectedInfo ? selectedFileSize : totalFileSize;
            int directoryCount = showSelectedInfo ? selectedDirectoryCount : totalDirectoryCount;
            string selectedText = showSelectedInfo ? " selected" : string.Empty;
            
            string text = string.Empty;
            string sizeText = string.Empty;

            if (directoryCount > 0)
            {
                text += directoryCount == 1 ? "1 folder" : $"{directoryCount} folders";
            }

            if (fileCount > 0)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    text += " and ";
                }

                text += fileCount == 1 ? "1 file" : $"{fileCount} files";
            }

            if (fileCount > 0 || directoryCount > 0)
            {
                text += selectedText;

                if (fileCount > 0)
                {
                    string bytesText = NumberFormatter.FormatBytes(fileSize);
                    text += $" totalling {bytesText}";
                }
            }

            m_viewModel.StatusBarSelectedFilesString = text;
        }
    }
}
