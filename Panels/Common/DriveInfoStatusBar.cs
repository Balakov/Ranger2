using System;

namespace Ranger2
{
    public class DriveInfoStatusBar
    {
        private System.Windows.Threading.DispatcherTimer m_updateTimer = new();
        private readonly TimeSpan m_updateDelay = TimeSpan.FromMilliseconds(500);
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
                m_viewModel.StatusBarDriveSpaceString = $"Free space {currentDriveFreeSpace:N0} bytes";
                m_updateFreeDriveSpace = false;
            }

            int fileCount = 0;
            int directoryCount = 0;
            long totalSelectedFileSize = 0;

            foreach (var item in m_viewModel.Files)
            {
                if (item.IsSelected)
                {
                    if (item is DirectoryViewModel)
                    {
                        directoryCount++;
                    }
                    else
                    {
                        fileCount++;
                        totalSelectedFileSize += item.SizeSortValue;
                    }
                }
            }

            string text = string.Empty;
            string totalSizeText = string.Empty;

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
                text += " selected";

                if (fileCount > 0)
                {
                    string bytesText = totalSelectedFileSize == 1 ? "byte" : "bytes";
                    text += $" totalling {totalSelectedFileSize:N0} {bytesText}";
                }
            }

            m_viewModel.StatusBarSelectedFilesString = text;
        }
    }
}
