using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ranger2
{
    public interface IImageViewerFullScreenRequestProcessor
    {
        void RequestFullscreen(bool enabled);
    }

    public partial class ImageViewer : ImageCache.IImageLoadedNotification,
                                       IImageViewerFullScreenRequestProcessor
    {
        public class ViewModel : Utility.ViewModelBase
        {
            private const string c_titlePrefix = "Ranger 2 - Image Viewer";
            private string m_filename;
            private FrameworkElement m_containerElement;
            private IImageViewerFullScreenRequestProcessor m_fullscreenRequest;

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

            public ViewModel(string filename, 
                             FrameworkElement containerElement, 
                             IImageViewerFullScreenRequestProcessor fullscreenRequest)
            {
                m_filename = filename;
                m_containerElement = containerElement;
                m_fullscreenRequest = fullscreenRequest;
            }

            public void ImageLoaded(ImageSource image, ZoomBorder zoomBorder)
            {
                ImageSource = image;
                IsLoading = false;
                
                if (image != null)
                {
                    SetTitle();
                    zoomBorder.ScaleToFit(ViewAreaWidth, ViewAreaHeight, image.Width, image.Height);
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
                    title += $"{m_imageSource.Width} x {m_imageSource.Height} - {m_zoomLevel}%";
                }

                Title = title;
            }

            public void RequestFullScreen(bool enabled) => m_fullscreenRequest.RequestFullscreen(enabled);
        }

        public ImageViewer(ImageCache imageCache, string path)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            var viewModel = new ViewModel(System.IO.Path.GetFileName(path), GridInstance, this);

            NonClientAreaContent = new ImageViewerNonClientAreaContent(ZoomBorderInstance, viewModel);
            DataContext = viewModel;

            ZoomBorderInstance.OnImageViewerZoomChanged += (zoomLevel) =>
            {
                if (DataContext is ViewModel viewModel)
                {
                    viewModel.ZoomLevel = zoomLevel;
                }   
            };

            imageCache.QueueImageLoad(path, this);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
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

        public void ImageLoaded(ImageSource image)
        {
            if (DataContext is ViewModel viewModel)
            {
                viewModel.ImageLoaded(image, ZoomBorderInstance);
            }
        }

        public void RequestFullscreen(bool enabled)
        {
            IsFullScreen = enabled;
            Focus();
            Keyboard.Focus(this);
        }
    }
}
