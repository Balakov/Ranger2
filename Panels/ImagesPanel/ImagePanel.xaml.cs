using System.Windows;
using System.Windows.Controls;

namespace Ranger2
{
    public partial class ImagePanel : UserControl, 
                                      DirectoryContentsControl.IScrollIntoViewProvider
    {
        private DragDropSelectionSupport m_dragDropSelection;
        private ViewModel m_viewModel = null;

        public ImagePanel()
        {
            InitializeComponent();
            m_dragDropSelection = new(ListBoxInstance, DragSelectionCanvas, DragSelectionBorder);

            DataContextChanged += (s, e) =>
            {
                m_viewModel = DataContext as ViewModel;
                m_viewModel?.SetScrollIntoViewProvider(this);
            };
        }

        private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item &&
                item.DataContext is FileSystemObjectViewModel itemViewModel)
            {
                (DataContext as ViewModel).OnCommonMouseDoubleClick(itemViewModel);
            }
        }

        private void ListBoxItem_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender is ListViewItem listViewItem &&
                listViewItem.DataContext is FileSystemObjectViewModel viewModel)
            {
                (DataContext as ViewModel).OnCommonItemKeyDown(e, viewModel);
            }
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is DirectoryContentsControl.ViewModel viewModel)
            {
                viewModel.OnCommonDrop(e, ListBoxInstance);
            }
        }

        public void ScrollIntoView(FileSystemObjectViewModel item)
        {
            ListBoxInstance.ScrollIntoView(item);
        }

        private void ListBox_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is DirectoryContentsControl.ViewModel viewModel)
            {
                viewModel.OnItemsSelected(e.AddedItems);
            }
        }

        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            base.OnDragOver(e);

            if (DataContext is DirectoryContentsControl.ViewModel viewModel)
            {
                viewModel.OnCommonDragOver(e);
            }
        }
    }

    public class ImagePanelTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            string templateName = null;

            if (item is ImagePanel.ViewModel.ImageViewModel)
            {
                templateName = "ImageTemplate";
            }
            else if (item is DirectoryViewModel)
            {
                templateName = "DCCFixedWidthFileSystemObjectTemplate";
            }

            if (!string.IsNullOrEmpty(templateName))
            {
                return (DataTemplate)((FrameworkElement)container).FindResource(templateName);
            }

            return null;
        }
    }
}
