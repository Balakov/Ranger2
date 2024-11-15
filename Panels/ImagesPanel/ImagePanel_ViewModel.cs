using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Xps.Packaging;

namespace Ranger2
{
    public partial class ImagePanel
    {
        public class ViewModel : DirectoryContentsControl.ViewModel
        {
            public class ImageViewModel : FileSystemObjectViewModel, 
                                          ImageCache.IImageLoadedNotification
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
                                      int thumbnailWidth,
                                      ImageCache imageCache,
                                      bool fromChangeEvent,
                                      DirectoryContentsControl.ViewModel parentViewModel) : base(null, info, parentViewModel)
                {
                    float aspect = (float)imageWidth / (float)imageHeight;
                    Width = thumbnailWidth;
                    Height = (int)(thumbnailWidth / aspect);

                    IsLoading = true;

                    imageCache.QueueThumbnailLoad(info.Path,
                                                  thumbnailWidth,
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

            private int m_thumbnailWidth = 200;
            public int ThumbnailWidth
            {
                get => m_thumbnailWidth;
                set
                {
                    if (OnPropertyChanged(ref m_thumbnailWidth, value))
                    {
                        OnPropertyChanged(nameof(ImageContainerColumnWidth));
                        OnPropertyChanged(nameof(ImageFileNameWidth));
                        RefreshDirectory();
                    }
                }
            }

            public override DirectoryContentsControl.DirectoryListingType ListingType => DirectoryContentsControl.DirectoryListingType.Images;
            public override bool ShowThumbnailSizeSelector => true;

            private int ThumbnailSizeToWidth(DirectoryContentsControl.ThumbnailSizeType size)
            {
                switch (size)
                {
                    default:
                    case DirectoryContentsControl.ThumbnailSizeType.Small:
                        return 200;
                    case DirectoryContentsControl.ThumbnailSizeType.Medium:
                        return 300;
                    case DirectoryContentsControl.ThumbnailSizeType.Large:
                        return 400;
                }
            }

            public override void SetThumbnailSize(DirectoryContentsControl.ThumbnailSizeType size)
            {
                ThumbnailSize = size;
                ThumbnailWidth = ThumbnailSizeToWidth(size);
            }

            private DirectoryContentsControl.ThumbnailSizeType  m_thumbnailSize;
            public DirectoryContentsControl.ThumbnailSizeType ThumbnailSize
            {
                get => m_thumbnailSize;
                set => OnPropertyChanged(ref m_thumbnailSize, value);
            }

            public int ImageContainerColumnWidth => m_thumbnailWidth + 16;
            public int ImageFileNameWidth => m_thumbnailWidth;

            public ViewModel(PanelContext context,
                             UserSettings.FilePanelSettings settings,
                             PathHistory pathHistory,
                             IDirectoryWatcher directoryWatcher) : base(context, settings, pathHistory, directoryWatcher)
            {
                m_directoryScanner.OnDirectoryScanComplete += OnDirectoryScanComplete;
                m_thumbnailWidth = ThumbnailSizeToWidth(settings.ThumbnailSize ?? DirectoryContentsControl.ThumbnailSizeType.Small);
            }

            public void ViewImage(ImageViewModel imageViewModel)
            {
                var window = new ImageViewer(m_context.ImageCache, imageViewModel.FullPath);
                window.Show();
                window.Activate();
            }

            protected override void OnActivateSelectedItems()
            {
                var viewModel = m_visualOrderProvider?.GetVisualItems().FirstOrDefault(x => x.IsSelected);

                if (viewModel is ImageViewModel imageViewModel)
                {
                    ViewImage(imageViewModel);
                }
                else if (viewModel is DirectoryViewModel directoryViewModel)
                {
                    directoryViewModel.OnActivate();
                }
            }

            protected override void OnDirectoryChanged(string path, string pathToSelect)
            {
                m_files.Clear();
                m_isLoading = true;
                UpdateUIVisibility();
                m_directoryScanner.ScanDirectory(path, pathToSelect);
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

                SetSelectedFilename(scanResult.PathToSelect);

                m_isLoading = false;
                UpdateUIVisibility();
            }

            protected override FileSystemObjectViewModel OnItemAdded(string path)
            {
                return OnItemAddedInternal(path, fromChangeEvent: true);
            }

            private FileSystemObjectViewModel OnItemAddedInternal(string path, bool fromChangeEvent)
            {
                if (m_context.ImageCache.CanDecode(path))
                {
                    var (w, h) = m_context.ImageCache.GetBounds(path);

                    if (FileSystemObjectViewModel.FilePassesViewFilter(path, m_viewMask, out var info))
                    {
                        var viewModel = new ImageViewModel(info, w, h, m_thumbnailWidth, m_context.ImageCache, fromChangeEvent, this);
                        m_files.Add(viewModel);
                        return viewModel;
                    }
                }

                return null;
            }
        }
    }
}
        