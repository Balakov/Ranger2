using HandyControl.Tools.Command;
using System.Windows.Input;
using System.Windows.Media;

namespace Ranger2
{
    public partial class ImageViewerNonClientAreaContent
    {
        public class ViewModel : Utility.ViewModelBase
        {
            public ICommand ResetZoomCommand { get; set; }
            public ICommand SetSmoothScalingCommand { get; set; }
            public ICommand SetNearestNeighbourScalingCommand { get; set; }
            public ICommand FullscreenCommand { get; set; }
            public ICommand FitToViewCommand { get; set; }

            public ViewModel(ZoomBorder zoomBorder, ImageViewer.ViewModel imageViewModel)
            {
                ResetZoomCommand = DelegateCommand.Create(() => 
                {
                    zoomBorder?.Reset();
                    //imageViewModel.Stretch = Stretch.None;
                });

                SetSmoothScalingCommand = DelegateCommand.Create(() => 
                {
                    zoomBorder?.Reset();
                    imageViewModel.ScalingMode = BitmapScalingMode.HighQuality;
                    App.UserSettings.ImageViewerScalingMode = imageViewModel.ScalingMode;
                });

                SetNearestNeighbourScalingCommand = DelegateCommand.Create(() => 
                {
                    zoomBorder?.Reset();
                    imageViewModel.ScalingMode = BitmapScalingMode.NearestNeighbor;
                    App.UserSettings.ImageViewerScalingMode = imageViewModel.ScalingMode;
                });

                FullscreenCommand = DelegateCommand.Create(() =>
                {
                    imageViewModel.RequestFullScreen(true);
                });

                FitToViewCommand = DelegateCommand.Create(() =>
                {
                    imageViewModel.ScaleToFit();
                });

                imageViewModel.ScalingMode = App.UserSettings.ImageViewerScalingMode ?? BitmapScalingMode.HighQuality;
            }
        }

        public ImageViewerNonClientAreaContent(ZoomBorder zoomBorder, ImageViewer.ViewModel imageViewModel)
        {
            InitializeComponent();
            DataContext = new ViewModel(zoomBorder, imageViewModel);
        }
    }
}
