using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ranger2
{
    public partial class FilesPanel : UserControl,
                                      DirectoryContentsControl.IScrollIntoViewProvider
    {
        private DragDropSelectionSupport m_dragDropSelection;

        public FilesPanel()
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

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView _ListView = sender as ListView;
            GridView _GridView = _ListView?.View as GridView;

            if (_GridView != null && _ListView.ActualWidth > 0)
            {
                var _ActualWidth = _ListView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
                for (Int32 i = 1; i < _GridView.Columns.Count; i++)
                {
                    _ActualWidth = _ActualWidth - _GridView.Columns[i].ActualWidth;
                }

                if (_ActualWidth >= 0)
                {
                    _GridView.Columns[0].Width = _ActualWidth;
                }
            }
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
            if (DataContext is DirectoryContentsControl.ViewModel viewModel)
            {
                viewModel.OnCommonDrop(e, ListViewInstance);
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

    }
}
