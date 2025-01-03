﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ranger2
{
    public partial class ImagePanel : UserControl, 
                                      DirectoryContentsControl.IScrollIntoViewProvider,
                                      DirectoryContentsControl.IVisualOrderProvider,
                                      DirectoryContentsControl.IFocusOwner
    {
        private DragDropSelectionSupport m_dragDropSelection;

        public ImagePanel()
        {
            InitializeComponent();
            m_dragDropSelection = new(ListBoxInstance, DragSelectionCanvas, DragSelectionBorder);

            ListBoxInstance.Items.SortDescriptions.Add(new SortDescription(nameof(FileSystemObjectViewModel.NameSortValue), ListSortDirection.Ascending));

            DataContextChanged += (s, e) =>
            {
                if (DataContext is ViewModel viewModel)
                {
                    viewModel.SetScrollIntoViewProvider(this);
                    viewModel.SetFocusOwner(this);
                    viewModel.SetDragDropTarget(ListBoxInstance);
                    viewModel.SetVisualOrderProvider(this);
                }
            };
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item &&
                item.DataContext is FileSystemObjectViewModel itemViewModel)
            {
                (DataContext as ViewModel).OnCommonMouseDoubleClick(itemViewModel);
                e.Handled = true;
            }
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is DirectoryContentsControl.ViewModel viewModel)
            {
                ViewModel.OnCommonDrop(e, viewModel.CurrentPath, ListBoxInstance);
            }
        }

        public void ScrollIntoView(FileSystemObjectViewModel item)
        {
            ListBoxInstance.ScrollIntoView(item);
        }

        public void GrabFocus()
        {
            ListBoxInstance.Focus();
            Keyboard.Focus(ListBoxInstance);
        }

        public bool HasFocus() => ListBoxInstance.IsKeyboardFocused;

        private void ListBoxInstance_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (DataContext is ViewModel viewModel)
            {
                viewModel.SwitchFocus();
            }
        }

        public FileSystemObjectViewModel ItemAtVisualIndex(int index)
        {
            if (index >= 0 && ListBoxInstance.Items.Count < index)
            {
                return ListBoxInstance.Items[index] as FileSystemObjectViewModel;
            }

            return null;
        }

        public List<FileSystemObjectViewModel> GetVisualItems()
        {
            return DirectoryContentsControl.GetItemsAsViewModels(ListBoxInstance.Items);
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
                ViewModel.OnCommonDragOver(e, viewModel.CurrentPath, ListBoxInstance);
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
