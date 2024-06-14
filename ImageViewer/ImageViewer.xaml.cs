using System.Windows;
using System.Windows.Media;

namespace Ranger2
{
    public partial class ImageViewer
    {
        public class ViewModel : Utility.ViewModelBase, ImageCache.IImageLoadedNotification
        {
            private const string c_titlePrefix = "Ranger 2 - Image Viewer";
            private string m_filename;

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

            private Stretch m_stretch = Stretch.Uniform;
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

            public ViewModel(string filename)
            {
                m_filename = filename;
            }

            public void ImageLoaded(ImageSource image)
            {
                ImageSource = image;
                IsLoading = false;
                SetTitle();
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
        }

        public ImageViewer(ImageCache imageCache, string path)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            var viewModel = new ViewModel(System.IO.Path.GetFileName(path));

            NonClientAreaContent = new ImageViewerNonClientAreaContent(ZoomBorderInstance, viewModel);
            DataContext = viewModel;

            ZoomBorderInstance.OnImageViewerZoomChanged += (zoomLevel) =>
            {
                if (DataContext is ViewModel viewModel)
                {
                    viewModel.ZoomLevel = zoomLevel;
                }   
            };

            imageCache.QueueImageLoad(path, viewModel);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }
    }
}
