using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.Generic;
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
                                     DirectoryContentsControl.IScrollIntoViewProvider,
                                     DirectoryContentsControl.IVisualOrderProvider
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
                    viewModel.SetVisualOrderProvider(this);
                }
            };
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem listViewItem &&
                listViewItem.DataContext is FileSystemObjectViewModel viewModel)
            {
                (DataContext as ViewModel).OnCommonMouseDoubleClick(viewModel);
                e.Handled = true;
            }
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListView listView &&
                DataContext is DirectoryContentsControl.ViewModel viewModel)
            {
                ViewModel.OnCommonDrop(e, viewModel.CurrentPath, ListViewInstance);
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

        public void GrabFocus()
        {
            ListViewInstance.Focus();
            Keyboard.Focus(ListViewInstance);
        }

        public FileSystemObjectViewModel ItemAtVisualIndex(int index)
        {
            if (index >= 0 && ListViewInstance.Items.Count < index)
            {
                return ListViewInstance.Items[index] as FileSystemObjectViewModel;
            }

            return null;
        }

        public List<FileSystemObjectViewModel> GetVisualItems()
        {
            return DirectoryContentsControl.GetItemsAsViewModels(ListViewInstance.Items);
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
                ViewModel.OnCommonDragOver(e, viewModel.CurrentPath, ListViewInstance);
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
