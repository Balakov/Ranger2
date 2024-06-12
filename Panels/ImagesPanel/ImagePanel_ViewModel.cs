using System;
using System.Windows.Media;

namespace Ranger2
{
    public partial class ImagePanel
    {
        public class ViewModel : DirectoryContentsControl.ViewModel
        {
            public class ImageViewModel : FileSystemObjectViewModel, ImageCache.IImageLoadedNotification
            {
                private ImageSource m_imageSource;
                public ImageSource ImageSource
                {
                    get => m_imageSource;
                    set => OnPropertyChanged(ref m_imageSource, value);
                }

                private int m_width;
                public int Width
                {
                    get => m_width;
                    set => OnPropertyChanged(ref m_width, value);
                }

                private int m_height;
                public int Height
                {
                    get => m_height;
                    set => OnPropertyChanged(ref m_height, value);
                }

                private bool m_isLoading;
                public bool IsLoading
                {
                    get => m_isLoading;
                    set => OnPropertyChanged(ref m_isLoading, value);
                }

                public override bool CanRename => true;
                public override bool CanDelete => CanRename;

                public ImageViewModel(FileSystemObjectInfo info,
                                      int imageWidth,
                                      int imageHeight,
                                      ImageCache imageCache,
                                      bool fromChangeEvent,
                                      DirectoryContentsControl.ViewModel parentViewModel) : base(null, info, parentViewModel)
                {
                    float aspect = (float)imageWidth / (float)imageHeight;
                    Width = 200;
                    Height = (int)(200.0f / aspect);

                    IsLoading = true;

                    imageCache.QueueThumbnailLoad(info.Path,
                                                  c_desiredThumbnailWidth,
                                                  info.LastWriteTime,
                                                  this,
                                                  fromChangeEvent ? TimeSpan.FromMilliseconds(100)
                                                                  : null);
                }

                public void ImageLoaded(ImageSource image)
                {
                    ImageSource = image;
                    IsLoading = false;

                    if (image != null)
                    {
                        if (image.Width != m_width || image.Height != m_width)
                        {
                            Width = (int)image.Width;
                            Height = (int)image.Height;
                        }
                    }
                }
            }

            private const int c_desiredThumbnailWidth = 200;

            public override DirectoryContentsControl.DirectoryListingType ListingType => DirectoryContentsControl.DirectoryListingType.Images;

            public ViewModel(PanelContext context,
                             UserSettings.FilePanelSettings settings,
                             PathHistory pathHistory,
                             IDirectoryWatcher directoryWatcher) : base(context, settings, pathHistory, directoryWatcher)
            {
                m_directoryScanner.OnDirectoryScanComplete += OnDirectoryScanComplete;
            }

            public void ViewImage(ImageViewModel imageViewModel)
            {
                var window = new ImageViewer(m_context.ImageCache, imageViewModel.FullPath);
                window.Show();
            }

            protected override void OnActivateItem(FileSystemObjectViewModel viewModel)
            {
                if (viewModel is ImageViewModel imageViewModel)
                {
                    ViewImage(imageViewModel);
                }
                else if (viewModel is DirectoryViewModel directoryViewModel)
                {
                    directoryViewModel.OnActivate();
                }
            }

            protected override void OnDirectoryChanged(string path)
            {
                m_files.Clear();
                m_isLoading = true;
                UpdateUIVisibility();
                m_directoryScanner.ScanDirectory(path);
            }

            private void OnDirectoryScanComplete(DirectoryScanner.ScanResult scanResult)
            {
                if (!scanResult.MatchesPath(m_settings.Path))
                    return;

                m_directoryWatcher.EnableDirectoryWatcher(m_settings.Path);

                // Directories

                foreach (var viewModel in CreateDirectoryViewModels(scanResult.Directories))
                {
                    m_files.Add(viewModel);
                }

                // Files

                foreach (var file in scanResult.Files)
                {
                    OnItemAddedInternal(file.Path, fromChangeEvent: false);
                }

                m_isLoading = false;
                UpdateUIVisibility();
            }

            protected override void OnItemAdded(string path)
            {
                OnItemAddedInternal(path, fromChangeEvent: true);
            }

            private void OnItemAddedInternal(string path, bool fromChangeEvent)
            {
                if (m_context.ImageCache.CanDecode(path))
                {
                    var (w, h) = m_context.ImageCache.GetBounds(path);

                    if (FileSystemObjectViewModel.FilePassesViewFilter(path, m_viewMask, out var info))
                    {
                        var viewModel = new ImageViewModel(info, w, h, m_context.ImageCache, fromChangeEvent, this);
                        m_files.Add(viewModel);
                    }
                }
            }
        }
    }
}
        