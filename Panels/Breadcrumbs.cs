using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Input;
using System.IO;

namespace Ranger2
{
    public class BreadcrumbsViewModel : Utility.ViewModelBase
    {
        public class PathPart
        {
            private bool m_isDragging;
            private Point m_dragStartPosition;
            private CancellationTokenSource m_dragCancellationToken;

            public string Path { get; }
            public string Name { get; }
            public bool IsEnabled { get; }

            public PathPart(string path, string name, bool isEnabled)
            {
                Path = path;
                Name = name;
                IsEnabled = isEnabled;
            }

            // Drag & drop handling

            public void OnDragOver(object sender, DragEventArgs e)
            {
                if (sender is Button button &&
                    button.DataContext is PathPart pathPart)
                {
                    DirectoryContentsControl.ViewModel.OnCommonDragOver(e, pathPart.Path, button);
                }
            }

            public void OnDrop(object sender, DragEventArgs e)
            {
                if (sender is Button button &&
                    button.DataContext is PathPart pathPart)
                {
                    DirectoryContentsControl.ViewModel.OnCommonDrop(e, pathPart.Path, button);
                }
            }

            public void OnMouseUp(object sender, MouseButtonEventArgs e)
            {
                if (m_isDragging)
                {
                    m_isDragging = false;
                    e.Handled = true;
                    var button = sender as Button;
                    button.ReleaseMouseCapture();
                }

                m_dragCancellationToken?.Cancel();
            }

            public void OnMouseDown(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton != MouseButton.Left)
                    return;

                m_isDragging = false;
                m_dragStartPosition = e.GetPosition(sender as IInputElement);

                m_dragCancellationToken?.Cancel();
                m_dragCancellationToken = new CancellationTokenSource();
            }

            public void OnMouseMove(object sender, MouseEventArgs e)
            {
                if(e.LeftButton == MouseButtonState.Pressed)
                {
                    var mousePosition = e.GetPosition(sender as IInputElement);
                    Vector diff = m_dragStartPosition - mousePosition;

                    if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance ||
                        m_isDragging)
                    {
                        m_isDragging = true;

                        if (sender is Button button &&
                            button.DataContext is PathPart pathPart)
                        {
                            var fileOp = new ClipboardManager.FileOperationPath()
                            {
                                FullPath = pathPart.Path.TrimEnd('\\')
                            };

                            var dataObject = ClipboardManager.PathsToDataObject([fileOp],
                                                                                FileOperations.OperationType.Copy,
                                                                                ClipboardManager.ClipboardDataTypes.Data | ClipboardManager.ClipboardDataTypes.Files);
                            if (dataObject != null)
                            {
                                DragDrop.DoDragDrop(button, dataObject, DragDropEffects.Copy | DragDropEffects.Move);
                            }
                        }
                    }
                }
            }
        }

        private ObservableCollection<PathPart> m_pathParts = new();
        public ObservableCollection<PathPart> PathParts
        {
            get => m_pathParts;
            set => OnPropertyChanged(ref m_pathParts, value);
        }

        public void SetPath(string directory)
        {
            m_pathParts.Clear();

            if (!string.IsNullOrEmpty(directory))
            {
                string[] parts;
                if (directory.StartsWith(@"\\"))
                {
                    parts = directory.Substring(2).Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                    parts[0] = @"\\" + parts[0];
                }
                else
                {
                    parts = directory.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                }


                string combinedPath = "";

                for(int i=0; i < parts.Length; i++)
                {
                    combinedPath += parts[i] + Path.DirectorySeparatorChar;
                    //m_pathParts.Add(new PathPart(combinedPath, parts[i], i != parts.Length-1));
                    m_pathParts.Add(new PathPart(combinedPath, parts[i], isEnabled: true));
                }
            }
        }
    }
}
