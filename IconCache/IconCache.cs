using System.Collections.Generic;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;

namespace Ranger2
{
    public class IconCache
    {
        public interface IIconLoadedNotification
        {
            void IconLoaded(ImageSource icon);
        }

        public enum IconType
        {
            File,
            Directory,
            Drive
        }

        private class QueuedIcon
        {
            public string m_path;
            public IIconLoadedNotification m_owner;
            public IconType m_iconType;
            private System.Windows.Threading.Dispatcher m_dispatcher;

            public QueuedIcon(string path, IconType iconType, IIconLoadedNotification owner, System.Windows.Threading.Dispatcher dispatcher)
            {
                m_dispatcher = dispatcher;
                m_path = path;
                m_iconType = iconType;
                m_owner = owner;
            }

            public void SetIcon(ImageSource icon)
            {
                icon.Freeze();

                if (!m_dispatcher.CheckAccess())
                {
                    m_dispatcher.Invoke(() => { SetIcon(icon); });
                }
                else
                {
                    m_owner.IconLoaded(icon);
                }
            }
        }

        private ConcurrentDictionary<string, BitmapSource> m_fileExtensionCache = new();

        private System.Windows.Threading.Dispatcher m_dispatcher;
        private Queue<QueuedIcon> m_iconQueue = new();
        private bool m_queueRunning = false;

        public static BitmapSource m_defaultFileIcon;
        public static BitmapSource m_defaultDirectoryIcon;
        public static BitmapSource m_defaultDriveIcon;

        public IconCache(System.Windows.Threading.Dispatcher dispatcher)
        {
            m_dispatcher = dispatcher;

            using (var icon = ShellIcons.GetStockIcon(Shell32.SHSTOCKICONID.SIID_DOCNOASSOC, out int iIconFile))
            {
                m_defaultFileIcon = GetBitmapSourceFromIcon(icon);
                m_defaultFileIcon.Freeze();
            }

            using (var icon = ShellIcons.GetStockIcon(Shell32.SHSTOCKICONID.SIID_FOLDER, out int iIconDirectory))
            {
                m_defaultDirectoryIcon = GetBitmapSourceFromIcon(icon);
                m_defaultDirectoryIcon.Freeze();
            }

            using (var icon = ShellIcons.GetStockIcon(Shell32.SHSTOCKICONID.SIID_DRIVEFIXED, out int iIconDrive))
            {
                m_defaultDriveIcon = GetBitmapSourceFromIcon(icon);
                m_defaultDriveIcon.Freeze();
            }
        }

        public void QueueIconLoad(string path, IconType iconType, IIconLoadedNotification owner)
        {
            switch (iconType)
            {
                case IconType.File:
                    string fileExtension = System.IO.Path.GetExtension(path);
                    if (m_fileExtensionCache.TryGetValue(fileExtension, out var bitmapSource))
                    {
                        owner.IconLoaded(bitmapSource);
                        return;
                    }
                    else
                    {
                        owner.IconLoaded(m_defaultFileIcon);
                    }
                    break;
                case IconType.Directory:
                    owner.IconLoaded(m_defaultDirectoryIcon);
                    break;
                case IconType.Drive:
                    owner.IconLoaded(m_defaultDriveIcon);
                    break;
            }

            lock (m_iconQueue)
            {
                m_iconQueue.Enqueue(new QueuedIcon(path, iconType, owner, m_dispatcher));

                if (!m_queueRunning)
                {
                    m_queueRunning = true;
                    ThreadPool.UnsafeQueueUserWorkItem((_) => ProcessQueue(), null);
                }
            }
        }

        private void ProcessQueue()
        {
            while (true)
            {
                QueuedIcon iconToProcess = null;
                
                lock (m_iconQueue)
                {
                    if (!m_iconQueue.Any())
                    {
                        m_queueRunning = false;
                        break;
                    }

                    iconToProcess = m_iconQueue.Dequeue();
                }

                try
                {
                    if (iconToProcess != null)
                    {
                        switch (iconToProcess.m_iconType)
                        {
                            case IconType.File:
                                BitmapSource icon = GetIconForFile(iconToProcess.m_path);
                                string fileExtension = System.IO.Path.GetExtension(iconToProcess.m_path);
                                if (!m_fileExtensionCache.ContainsKey(fileExtension))
                                {
                                    m_fileExtensionCache.TryAdd(fileExtension, icon);
                                }
                                iconToProcess.SetIcon(icon);
                                break;
                            case IconType.Directory:
                                iconToProcess.SetIcon(GetIconForDirectory(iconToProcess.m_path));
                                break;
                            case IconType.Drive:
                                iconToProcess.SetIcon(GetIconForDrive(iconToProcess.m_path));
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch
                {
                    //ThreadPool.UnsafeQueueUserWorkItem((_) => ProcessQueue(), null);
                    throw;
                }
            }
        }

        private BitmapSource GetBitmapSourceFromIcon(Icon icon) => Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        private BitmapSource GetIconForDrive(string path)
        {
            return GetIconForPath(path, isDirectory: true, isOverlay: false);
        }

        private BitmapSource GetIconForFile(string path)
        {
            return GetIconForPath(path, isDirectory: false, isOverlay: false);
        }

        private BitmapSource GetIconForDirectory(string path)
        {
            return GetIconForPath(path, isDirectory: true, isOverlay: false);
        }

        private BitmapSource GetIconForPath(string path, bool isDirectory, bool isOverlay)
        {
            using (var icon = ShellIcons.GetFileIcon(path, ShellIcons.IconSize.Small, isOverlay, isDirectory, out int iIcon))
            {
                return GetBitmapSourceFromIcon(icon);
            }
        }
    }
}
