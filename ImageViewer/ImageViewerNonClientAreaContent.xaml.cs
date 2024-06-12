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

            public ViewModel(ZoomBorder zoomBorder, ImageViewer.ViewModel imageViewModel)
            {
                ResetZoomCommand = DelegateCommand.Create(() => 
                {
                    zoomBorder?.Reset();
                    //imageViewModel.Stretch = Stretch.None;
                });
            }
        }

        public ImageViewerNonClientAreaContent(ZoomBorder zoomBorder, ImageViewer.ViewModel imageViewModel)
        {
            InitializeComponent();
            DataContext = new ViewModel(zoomBorder, imageViewModel);
        }
    }
}
