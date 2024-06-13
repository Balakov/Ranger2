using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Ranger2
{
    public class DragDropSelectionSupport
    {
        [DllImport("user32.dll")]
        static extern uint GetDoubleClickTime();

        private ListBox m_listBox;
        private Canvas m_dragSelectionCanvas;
        private Border m_dragSelectionBorder;

        private Point m_dragStartPositionLocal;
        private Point m_dragStartPositionGlobal;
        private bool m_scrollActive = false;
        private bool m_dragSelectionEnabled = false;
        private bool m_dragSelectionActive = false;
        private DateTime m_lastClickTime;

        public DragDropSelectionSupport(ListBox listBox, Canvas dragSelectionBox, Border dragSelectionBorder)
        {
            m_listBox = listBox;
            m_dragSelectionCanvas = dragSelectionBox;
            m_dragSelectionBorder = dragSelectionBorder;

            m_listBox.PreviewMouseLeftButtonDown += PreviewMouseLeftButtonDown;
            m_listBox.PreviewMouseLeftButtonUp += PreviewMouseLeftButtonUp;
            m_listBox.MouseMove += MouseMove;
            m_dragSelectionBorder.MouseMove += MouseMove;
            m_dragSelectionBorder.PreviewMouseLeftButtonUp += PreviewMouseLeftButtonUp;
        }

        private bool TryGetClickedOnViewModel(Point mousePosition, out FileSystemObjectViewModel viewModel)
        {
            FrameworkElement element = m_listBox.InputHitTest(mousePosition) as FrameworkElement;
            if (element != null)
            {
                if (element.DataContext is FileSystemObjectViewModel dataContextViewModel)
                {
                    viewModel = dataContextViewModel;
                    return true;
                }
            }

            viewModel = null;
            return false;
        }

        private bool IsMultiSelectKeyPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) ||
                   Keyboard.IsKeyDown(Key.RightShift) ||
                   Keyboard.IsKeyDown(Key.LeftCtrl) ||
                   Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private bool IsClickOnScrollbar(Point mousePosition)
        {
            HitTestResult r = VisualTreeHelper.HitTest(m_listBox, mousePosition);

            if (r != null && r.VisualHit != null)
            {
                return IsVisualChildOfType(r.VisualHit, typeof(System.Windows.Controls.Primitives.ScrollBar));
            }

            return false;
        }

        private bool IsClickOnHeader(Point mousePosition)
        {
            HitTestResult r = VisualTreeHelper.HitTest(m_listBox, mousePosition);

            if (r != null && r.VisualHit != null)
            {
                return IsVisualChildOfType(r.VisualHit, typeof(GridViewColumnHeader));
            }

            return false;
        }
        
        private bool IsVisualChildOfType(DependencyObject element, Type type)
        {
            if (element != null)
            {
                var current = element;
                do
                {
                    //Debug.Log(current.GetType().ToString());
                    if (current.GetType() == type)
                    {
                        return true;
                    }

                    current = VisualTreeHelper.GetParent(current);
                }
                while (current != null);
            }

            return false;
        }

        private void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                if (IsClickOnScrollbar(e.GetPosition(m_listBox)))
                {
                    m_scrollActive = true;
                }
                else if (IsClickOnHeader(e.GetPosition(m_listBox)))
                {
                }
                else
                {
                    if (TryGetClickedOnViewModel(e.GetPosition(m_listBox), out var viewModel))
                    {
                        if (!IsMultiSelectKeyPressed())
                        {
                            DateTime clickTime = DateTime.UtcNow;

                            if (!viewModel.IsSelected)
                            {
                                Keyboard.Focus(listBox);
                                listBox.Focus();
                                listBox.UnselectAll();
                                viewModel.IsSelected = true;
                            }
                            else
                            {
                                // Was this a double click?
                                if ((clickTime - m_lastClickTime).TotalMilliseconds > GetDoubleClickTime())
                                {
                                    e.Handled = true;
                                }
                            }
                    
                            m_lastClickTime = clickTime;
                        }
                    }
                    else
                    {
                        // Region drag?
                        m_dragSelectionEnabled = true;
                        e.Handled = true;
                    }

                    m_dragStartPositionLocal = e.GetPosition(m_listBox);
                    m_dragStartPositionGlobal = e.GetPosition(null);
                }
            }
        }

        private List<ListBoxItem> FindListBoxItems(Visual visual, List<ListBoxItem> items = null)
        {
            if (items == null)
            {
                items = new();
            }

            if (visual is ListBoxItem listBoxItem)
            {
                items.Add(listBoxItem);
                return items;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                // Retrieve child visual at specified index value.
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(visual, i);
                FindListBoxItems(childVisual, items);
            }

            return items;
        }

        private void PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                if (!m_scrollActive)
                {
                    if (m_dragSelectionActive)
                    {
                        m_dragSelectionActive = false;
                        m_listBox.ReleaseMouseCapture();
                        ApplyDragSelectionRect(listBox);
                    }
                    else
                    {
                        if (TryGetClickedOnViewModel(e.GetPosition(m_listBox), out var viewModel))
                        {
                            if (!IsMultiSelectKeyPressed())
                            {
                                if (viewModel.IsSelected)
                                {
                                    Keyboard.Focus(listBox);
                                    listBox.Focus();
                                    listBox.UnselectAll();
                                    viewModel.IsSelected = true;
                                }
                            }
                        }
                        else
                        {
                            listBox.UnselectAll();
                        }
                    }
                }

                m_scrollActive = false;
                m_dragSelectionEnabled = false;
            }
        }

        private void MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed &&
                !m_scrollActive &&
                sender is ListBox listBox)
            {
                if (m_dragSelectionActive)
                {
                    Point mousePositionLocal = e.GetPosition(m_listBox);
                    UpdateDragSelectionRect(m_dragStartPositionLocal, mousePositionLocal);
                    e.Handled = true;
                }
                else
                {
                    Point mousePositionGlobal = e.GetPosition(null);
                    Vector diff = m_dragStartPositionGlobal - mousePositionGlobal;

                    if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        if (m_dragSelectionEnabled)
                        {
                            m_dragSelectionActive = true;
                            m_listBox.CaptureMouse();

                            if (!IsMultiSelectKeyPressed())
                            {
                                listBox.UnselectAll();
                            }

                            Point mousePositionLocal = e.GetPosition(m_listBox);
                            InitDragSelectionRect(m_dragStartPositionLocal, mousePositionLocal);
                        }
                        else
                        {
                            if (listBox.SelectedItems.Count > 0)
                            {
                                List<ClipboardManager.FileOperationPath> files = new();
                                foreach (FileSystemObjectViewModel itemViewModel in listBox.SelectedItems)
                                {
                                    files.Add(new ClipboardManager.FileOperationPath() { FullPath = itemViewModel.FullPath, Name = itemViewModel.Name });
                                }

                                var dataObject = ClipboardManager.PathsToDataObject(files, FileOperations.OperationType.Copy, ClipboardManager.ClipboardDataTypes.Data |
                                                                                                                                               ClipboardManager.ClipboardDataTypes.Files);
                                if (dataObject != null)
                                {
                                    DragDrop.DoDragDrop(listBox, dataObject, DragDropEffects.Copy | DragDropEffects.Move);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void InitDragSelectionRect(Point pt1, Point pt2)
        {
            UpdateDragSelectionRect(pt1, pt2);
            m_dragSelectionCanvas.Visibility = Visibility.Visible;
        }

        private void UpdateDragSelectionRect(Point pt1, Point pt2)
        {
            double x, y, width, height;

            if (pt2.X < pt1.X)
            {
                x = pt2.X;
                width = pt1.X - pt2.X;
            }
            else
            {
                x = pt1.X;
                width = pt2.X - pt1.X;
            }

            if (pt2.Y < pt1.Y)
            {
                y = pt2.Y;
                height = pt1.Y - pt2.Y;
            }
            else
            {
                y = pt1.Y;
                height = pt2.Y - pt1.Y;
            }

            //
            // Update the coordinates of the rectangle used for drag selection.
            //
            Canvas.SetLeft(m_dragSelectionBorder, x);
            Canvas.SetTop(m_dragSelectionBorder, y);
            m_dragSelectionBorder.Width = width;
            m_dragSelectionBorder.Height = height;
        }

        private void ApplyDragSelectionRect(ListBox listBox)
        {
            if (listBox != null)
            {
                double x = Canvas.GetLeft(m_dragSelectionBorder);
                double y = Canvas.GetTop(m_dragSelectionBorder);
                double width = m_dragSelectionBorder.Width;
                double height = m_dragSelectionBorder.Height;
                Rect dragRect = new Rect(x, y, width, height);

                // Find and select all the list box items.
                var listBoxItems = FindListBoxItems(listBox);

                foreach (ListBoxItem item in listBoxItems)
                {
                    var itemRect = VisualTreeHelper.GetDescendantBounds(item);
                    var offset = item.TransformToAncestor(listBox).Transform(new Point(0, 0));
                    itemRect.X = offset.X;
                    itemRect.Y = offset.Y;

                    if (dragRect.IntersectsWith(itemRect))
                    {
                        listBox.SelectedItems.Add(item.DataContext as FileSystemObjectViewModel);
                    }
                }
            }

            m_dragSelectionCanvas.Visibility = Visibility.Collapsed;
        }
    }
}
