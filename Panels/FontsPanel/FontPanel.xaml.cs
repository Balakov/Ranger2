using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ranger2
{
    public interface IInvalidateFontPreview
    {
        void InvalidateFontPreview();
    }

    public partial class FontPanel : UserControl,
                                     DirectoryContentsControl.IScrollIntoViewProvider
    {
        private DragDropSelectionSupport m_dragDropSelection;

        public FontPanel()
        {
            InitializeComponent();
            m_dragDropSelection = new(ListViewInstance, DragSelectionCanvas, DragSelectionBorder);

            DataContextChanged += (s, e) =>
            {
                if (DataContext is ViewModel viewModel)
                {
                    viewModel.SetScrollIntoViewProvider(this);
                    viewModel.SetDragDropTarget(ListViewInstance);
                }
            };
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem listViewItem &&
                listViewItem.DataContext is FileSystemObjectViewModel viewModel)
            {
                (DataContext as ViewModel).OnCommonMouseDoubleClick(viewModel);
            }
        }

        private void ListViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ListViewItem listViewItem &&
                listViewItem.DataContext is FileSystemObjectViewModel viewModel)
            {
                (DataContext as ViewModel).OnCommonItemKeyDown(e, viewModel);
            }
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListView listView &&
                DataContext is DirectoryContentsControl.ViewModel viewModel)
            {
                viewModel.OnCommonDrop(e, ListViewInstance);
            }
        }

        private void OnPaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            if (sender is SKElement element &&
                element.DataContext is ViewModel.FontViewModel fontViewModel &&
                fontViewModel.Paint != null)
            {
                // the the canvas and properties
                var canvas = e.Surface.Canvas;

                // make sure the canvas is blank
                canvas.Clear(SKColors.Transparent);

                fontViewModel.SetPaintColourFromTheme();

                canvas.DrawText("The Quick Brown Fox Jumps Over The Lazy Dog", 0, 30, fontViewModel.Paint);
            }
        }

        private void SKElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is SKElement element &&
                element.DataContext is ViewModel.FontViewModel fontViewModel)
            {
                fontViewModel.RegisterSKElement(element);
            }
        }

        public void ScrollIntoView(FileSystemObjectViewModel item)
        {
            ListViewInstance.ScrollIntoView(item);
        }

        private void ListView_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ViewModel viewModel)
            {
                viewModel.OnItemsSelected(e.AddedItems);
            }
        }

        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            base.OnDragOver(e);

            if (DataContext is ViewModel viewModel)
            {
                viewModel.OnCommonDragOver(e);
            }
        }
    }

    public class FontPanelTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            string templateName = null;

            if (item is FontPanel.ViewModel.FontViewModel)
            {
                templateName = "FontTemplate";
            }
            else if (item is DirectoryViewModel)
            {
                templateName = "DCCFileSystemObjectTemplate";
            }

            if (!string.IsNullOrEmpty(templateName))
            {
                return (DataTemplate)((FrameworkElement)container).FindResource(templateName);
            }

            return null;
        }
    }
}
