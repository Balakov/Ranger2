using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public partial class ViewModel
        {
            public void OnCommonKeyDown(KeyEventArgs e)
            {
                if (KeyboardUtilities.IsControlDown)
                {
                    if (e.Key == Key.Left)
                    {
                        m_context.PanelLayout.DuplicateCurrentContent(DuplicateContentDirection.Left);
                    }
                    else if (e.Key == Key.Right)
                    {
                        m_context.PanelLayout.DuplicateCurrentContent(DuplicateContentDirection.Right);
                    }
                    else if (e.Key == Key.A)
                    {
                        foreach (var file in m_files)
                        {
                            file.IsSelected = true;
                        }
                    }
                    else if (e.Key == Key.C || e.Key == Key.X)
                    {
                        HandleCopy(e.Key == Key.X);
                    }
                    else if (e.Key == Key.V)
                    {
                        HandlePaste();
                    }
                }
                else if (e.Key == Key.Enter)
                {
                    OnActivateSelectedItems();
                }
                else if (e.Key == Key.Back)
                {
                    m_context.DirectoryChangeRequester.SetDirectoryToParent();
                }
                else if (e.Key == Key.F5)
                {
                    OnDirectoryChanged(m_currentDirectory, null);
                }
                else if (e.Key == Key.F2)
                {
                    if (TryGetSelectedFile(out var file) && file.CanRename)
                    {
                        string title = (file is DirectoryViewModel) ? "Rename Directory" : "Rename File";
                        var dialog = new Dialogs.RenameDialog(file.FullPath, title, allowSameName: false);
                        if (dialog.ShowDialog() == true)
                        {
                            FileOperations.RenameFiles(new List<string> { file.FullPath }, new List<string> { dialog.RenamedFile });
                        }
                    }
                }
                else if (e.Key == Key.F7)
                {
                    AddNewFile();
                }
                else if (e.Key == Key.F8)
                {
                    AddNewFolder();
                }
                else if (e.Key == Key.Delete)
                {
                    if (TryGetSelectedFiles(out var files))
                    {
                        var deletableFiles = files.Where(x => x.CanDelete).Select(x => x.FullPath).ToList();
                        if (deletableFiles.Any())
                        {
                            // Shift will delete permanently
                            bool toRecycleBin = !KeyboardUtilities.IsShiftDown;
                            FileOperations.DeleteFiles(deletableFiles, toRecycleBin);
                        }
                    }
                }
                else
                {
                    m_keySearch.OnKeypress(e.Key);
                }
            }

            public void OnCommonPreviewKeyDown(KeyEventArgs e)
            {
                bool isNavigationKey = !KeyboardUtilities.AnyModifiersDown && 
                                       (e.Key == Key.Down || 
                                        e.Key == Key.Up || 
                                        e.Key == Key.Left || 
                                        e.Key == Key.Right);

                if (isNavigationKey)
                {
                    // No need to do anything if its already handled or it's a repeated key down event   
                    if (e.Handled)
                        return;

                    OnNavigationKeyDown(e.Key);
                    e.Handled = true;
                }
            }

            private void OnNavigationKeyDown(Key key)
            {
                bool keyPrevious = key == Key.Up || key == Key.Left;
                bool keyNext = key == Key.Down || key == Key.Right;

                int previouslySelectedMinIndex = -1;
                int previouslySelectedMaxIndex = -1;

                bool clearSelection = !KeyboardUtilities.IsShiftDown;

                for (int i = 0; i < m_files.Count; i++)
                {
                    if (m_files[i].IsSelected)
                    {
                        if (previouslySelectedMinIndex == -1)
                        {
                            previouslySelectedMinIndex = i;
                            previouslySelectedMaxIndex = i;
                        }

                        if (clearSelection)
                        {
                            // Don't deselect if we're not going to actually move the selection 
                            // as it causes a flicker.
                            if ((keyPrevious && i != 0) ||
                                (keyNext && i != m_files.Count - 1))
                            {
                                m_files[i].IsSelected = false;
                            }
                        }
                        else
                        {
                            // Don't extend the selection downwards if we're cleaning it.
                            previouslySelectedMaxIndex = i;
                        }
                    }
                }

                if (previouslySelectedMinIndex == -1 && m_files.Count > 0)
                {
                    m_files.First().IsSelected = true;
                }
                else
                {
                    // Only one item is selected.
                    if (previouslySelectedMaxIndex == previouslySelectedMinIndex)
                    {
                        if (keyPrevious)
                        {
                            if (previouslySelectedMinIndex > 0)
                            {
                                m_files[previouslySelectedMinIndex - 1].IsSelected = true;
                                m_scrollIntoViewProvider.ScrollIntoView(m_files[previouslySelectedMinIndex - 1]);
                            }
                        }
                        else if (keyNext)
                        {
                            if (previouslySelectedMaxIndex < m_files.Count - 1)
                            {
                                m_files[previouslySelectedMaxIndex + 1].IsSelected = true;
                                m_scrollIntoViewProvider.ScrollIntoView(m_files[previouslySelectedMaxIndex + 1]);
                            }
                        }

                        m_lastKeyNavigationDirection = (keyPrevious) ? KeyNavigationDirection.Up : KeyNavigationDirection.Down;
                    }
                    else
                    {
                        // If last direction was up and we're going up, add to min.
                        // If last direction was up and we're going down, subtract from min selection.
                        if (keyPrevious)
                        {
                            if (m_lastKeyNavigationDirection == KeyNavigationDirection.Up)
                            {
                                if (previouslySelectedMinIndex > 0)
                                {
                                    m_files[previouslySelectedMinIndex - 1].IsSelected = true;
                                    m_scrollIntoViewProvider.ScrollIntoView(m_files[previouslySelectedMinIndex - 1]);
                                }
                            }
                            else if (m_lastKeyNavigationDirection == KeyNavigationDirection.Down)
                            {
                                m_files[previouslySelectedMaxIndex].IsSelected = false;
                            }
                        }
                        else if (keyNext)
                        {
                            if (m_lastKeyNavigationDirection == KeyNavigationDirection.Down)
                            {
                                if (previouslySelectedMaxIndex < m_files.Count - 1)
                                {
                                    m_files[previouslySelectedMaxIndex + 1].IsSelected = true;
                                    m_scrollIntoViewProvider.ScrollIntoView(m_files[previouslySelectedMaxIndex + 1]);
                                }
                            }
                            else if (m_lastKeyNavigationDirection == KeyNavigationDirection.Up)
                            {
                                m_files[previouslySelectedMinIndex].IsSelected = false;
                            }
                        }
                    }
                }
            }

        }
    }
}