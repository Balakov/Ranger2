using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ranger2
{
    public interface IImageViewerFullScreenRequestProcessor
    {
        void RequestFullscreen(bool enabled);
    }

    public partial class ImageViewer : IImageViewerFullScreenRequestProcessor
    {
        public class ViewModel : Utility.ViewModelBase,
                                 ImageCache.IImageLoadedNotification
        {
            private const string c_titlePrefix = "Ranger 2 - Image Viewer";
            private string m_path;
            private string m_filename;
            private FrameworkElement m_containerElement;
            private IImageViewerFullScreenRequestProcessor m_fullscreenRequest;
            private ZoomBorder m_zoomBorder;
            private ImageCache m_imageCache;
            private DirectoryScanner m_directoryScanner = new();

            private string m_title = c_titlePrefix;
            public string Title
            {
                get => m_title;
                set => OnPropertyChanged(ref m_title, value);
            }

            private int m_zoomLevel = 100;
            public int ZoomLevel
            {
                get => m_zoomLevel;
                set 
                {
                    if (OnPropertyChanged(ref m_zoomLevel, value))
                    {
                        SetTitle();
                    }
                }
            }

            private bool m_isLoading = true;
            public bool IsLoading
            {
                get => m_isLoading;
                set => OnPropertyChanged(ref m_isLoading, value);
            }

            private ImageSource m_imageSource;
            public ImageSource ImageSource
            {
                get => m_imageSource;
                set => OnPropertyChanged(ref m_imageSource, value);
            }

            private Stretch m_stretch = Stretch.None;
            public Stretch Stretch
            {
                get => m_stretch;
                set => OnPropertyChanged(ref m_stretch, value);
            }

            private BitmapScalingMode m_scalingMode = BitmapScalingMode.HighQuality;
            public BitmapScalingMode ScalingMode
            {
                get => m_scalingMode;
                set => OnPropertyChanged(ref m_scalingMode, value);
            }

            public double ViewAreaWidth => m_containerElement.ActualWidth;
            public double ViewAreaHeight => m_containerElement.ActualHeight;

            public ViewModel(string path, 
                             FrameworkElement containerElement, 
                             IImageViewerFullScreenRequestProcessor fullscreenRequest,
                             ZoomBorder zoomBorder,
                             ImageCache imageCache)
            {
                m_containerElement = containerElement;
                m_fullscreenRequest = fullscreenRequest;
                m_zoomBorder = zoomBorder;
                m_imageCache = imageCache;
                
                m_zoomBorder.OnImageViewerZoomChanged += (zoomLevel) => ZoomLevel = zoomLevel;

                LoadImage(path);
            }

            public void LoadImage(string path)
            {
                m_path = path;
                m_filename = Path.GetFileName(m_path);
                IsLoading = true;
                SetTitle();
                m_imageCache.QueueImageLoad(path, this);
            }

            public void ImageLoaded(ImageSource image)
            {
                ImageSource = image;
                IsLoading = false;
                
                SetTitle();

                if (image != null)
                {
                    m_zoomBorder.ScaleToFit(ViewAreaWidth, ViewAreaHeight, image.Width, image.Height);
                }
            }

            private void SetTitle()
            {
                string title = Title = $"{c_titlePrefix} - {m_filename} - ";

                if (m_isLoading)
                {
                    title += "Loading";
                }
                else
                {
                    if (m_imageSource != null)
                    {
                        title += $"{m_imageSource?.Width} x {m_imageSource?.Height} - {m_zoomLevel}%";
                    }
                    else
                    {
                        title += "Load Failed";
                    }
                }

                Title = title;
            }


            public void NextImage()
            {
                ScanFiles(SearchDirection.Down);
            }

            public void PreviousImage()
            {
                ScanFiles(SearchDirection.Up);
            }

            private enum SearchDirection { Up, Down }
            private SearchDirection m_currentSearchDirection;

            private void ScanFiles(SearchDirection direction)
            {
                string directory = Path.GetDirectoryName(m_path);
                m_currentSearchDirection = direction;
                m_directoryScanner.OnDirectoryScanComplete += OnDirectoryScanComplete;
                m_directoryScanner.ScanDirectory(m_path, null);
            }

            private void OnDirectoryScanComplete(DirectoryScanner.ScanResult scanResult)
            {
                m_directoryScanner.OnDirectoryScanComplete -= OnDirectoryScanComplete;

                List<DirectoryScanner.ScanResult.Filename> files = scanResult.Files.ToList();
                DirectoryScanner.ScanResult.Filename previous = null;
                bool takeNext = false;

                foreach (var file in scanResult.Files)
                {
                    if (file.Path == m_path)
                    {
                        if (m_currentSearchDirection == SearchDirection.Up)
                        {
                            if (previous != null)
                            {
                                LoadImage(previous.Path);
                            }
                            
                            break;
                        }
                        else
                        {
                            takeNext = true;
                        }
                    }
                    else
                    {
                        if (m_imageCache.CanDecode(file.Path))
                        {
                            if (takeNext)
                            {
                                LoadImage(file.Path);
                                break;
                            }

                            previous = file;
                        }
                    }
                }
            }

            public void ScaleToFit()
            {
                if (m_imageSource != null)
                {
                    m_zoomBorder?.ScaleToFit(ViewAreaWidth,
                                             ViewAreaHeight,
                                             m_imageSource.Width,
                                             m_imageSource.Height);
                }
            }

            public void RequestFullScreen(bool enabled) => m_fullscreenRequest.RequestFullscreen(enabled);
        }

        private ViewModel m_viewModel;

        public ImageViewer(ImageCache imageCache, string path)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            m_viewModel = new ViewModel(path, GridInstance, this, ZoomBorderInstance, imageCache);
            DataContext = m_viewModel;

            NonClientAreaContent = new ImageViewerNonClientAreaContent(ZoomBorderInstance, m_viewModel);

            // Ensure the window is activated using the dispatcher
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Activate();
                Focus();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Up)
            {
                m_viewModel.PreviousImage();
            }
            else if (e.Key == Key.Right || e.Key == Key.Down)
            {
                m_viewModel.NextImage();
            }
            if (e.Key == Key.F12)
            {
                RequestFullscreen(!IsFullScreen);
            }
            else if (e.Key == Key.Escape)
            {
                if (IsFullScreen)
                {
                    RequestFullscreen(false);
                }
                else
                {
                    Close();
                }
            }
        }

        public void RequestFullscreen(bool enabled)
        {
            IsFullScreen = enabled;

            if (!enabled)
            {
                m_viewModel.ScaleToFit();
            }
        }
    }
}
