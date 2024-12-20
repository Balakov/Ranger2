using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ranger2
{
    public partial class FilesPanel : UserControl,
                                      DirectoryContentsControl.IScrollIntoViewProvider,
                                      DirectoryContentsControl.IVisualOrderProvider,
                                      DirectoryContentsControl.IFocusOwner
    {
        private DragDropSelectionSupport m_dragDropSelection;

        public FilesPanel()
        {
            InitializeComponent();
            m_dragDropSelection = new(ListViewInstance, DragSelectionCanvas, DragSelectionBorder);

            ListViewInstance.Items.SortDescriptions.Add(new SortDescription(nameof(FileSystemObjectViewModel.NameSortValue), ListSortDirection.Ascending));

            DataContextChanged += (s, e) =>
            {
                if (DataContext is ViewModel viewModel)
                {
                    viewModel.SetScrollIntoViewProvider(this);
                    viewModel.SetFocusOwner(this);
                    viewModel.SetDragDropTarget(ListViewInstance);
                    viewModel.SetVisualOrderProvider(this);
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
                e.Handled = true;
            }
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is DirectoryContentsControl.ViewModel viewModel)
            {
                ViewModel.OnCommonDrop(e, viewModel.CurrentPath, ListViewInstance);
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

        public void ScrollIntoView(FileSystemObjectViewModel item)
        {
            ListViewInstance.ScrollIntoView(item);
        }

        public void GrabFocus()
        {
            ListViewInstance.Focus();
            Keyboard.Focus(ListViewInstance);
        }

        public bool HasFocus() => ListViewInstance.IsKeyboardFocused;

        private void ListViewInstance_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (DataContext is ViewModel viewModel)
            {
                viewModel.SwitchFocus();
            }
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

    }
}
