﻿using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public partial class ViewModel
        {
            private bool IsPreviousNavigationKey(Key key) => ListingType == DirectoryListingType.Images ? key == Key.Up || key == Key.Left 
                                                                                                        : key == Key.Up;
            private bool IsNextNavigationKey(Key key) => ListingType == DirectoryListingType.Images ? key == Key.Down || key == Key.Right
                                                                                                    : key == Key.Down;

            public void OnCommonKeyDown(KeyEventArgs e)
            {
                if (KeyboardUtilities.IsControlDown)
                {
                    if (e.Key == Key.I)
                    {
                        m_context.PanelLayout.SwitchCurrentContent();
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
                    HandleDelete();
                }
                else
                {
                    m_keySearch.OnKeypress(e.Key);
                }
            }

            public void OnCommonPreviewKeyDown(KeyEventArgs e)
            {
                // Allow shift through for multi-select.
                bool isNavigationKey = !KeyboardUtilities.IsAltDown && 
                                       !KeyboardUtilities.IsControlDown &&
                                       (IsPreviousNavigationKey(e.Key) ||  IsNextNavigationKey(e.Key));

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
                bool keyPrevious = IsPreviousNavigationKey(key);
                bool keyNext = IsNextNavigationKey(key);

                int previouslySelectedMinIndex = -1;
                int previouslySelectedMaxIndex = -1;

                bool clearSelection = !KeyboardUtilities.IsShiftDown;

                var files = m_visualOrderProvider?.GetVisualItems();

                for (int i = 0; i < files.Count; i++)
                {
                    if (files[i].IsSelected)
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
                                (keyNext && i != files.Count - 1))
                            {
                                files[i].IsSelected = false;
                            }
                        }
                        else
                        {
                            // Don't extend the selection downwards if we're cleaning it.
                            previouslySelectedMaxIndex = i;
                        }
                    }
                }

                if (previouslySelectedMinIndex == -1 && files.Count > 0)
                {
                    files.First().IsSelected = true;
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
                                files[previouslySelectedMinIndex - 1].IsSelected = true;
                                m_scrollIntoViewProvider?.ScrollIntoView(files[previouslySelectedMinIndex - 1]);
                            }
                        }
                        else if (keyNext)
                        {
                            if (previouslySelectedMaxIndex < files.Count - 1)
                            {
                                files[previouslySelectedMaxIndex + 1].IsSelected = true;
                                m_scrollIntoViewProvider?.ScrollIntoView(files[previouslySelectedMaxIndex + 1]);
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
                                    files[previouslySelectedMinIndex - 1].IsSelected = true;
                                    m_scrollIntoViewProvider?.ScrollIntoView(files[previouslySelectedMinIndex - 1]);
                                }
                            }
                            else if (m_lastKeyNavigationDirection == KeyNavigationDirection.Down)
                            {
                                files[previouslySelectedMaxIndex].IsSelected = false;
                            }
                        }
                        else if (keyNext)
                        {
                            if (m_lastKeyNavigationDirection == KeyNavigationDirection.Down)
                            {
                                if (previouslySelectedMaxIndex < files.Count - 1)
                                {
                                    files[previouslySelectedMaxIndex + 1].IsSelected = true;
                                    m_scrollIntoViewProvider?.ScrollIntoView(files[previouslySelectedMaxIndex + 1]);
                                }
                            }
                            else if (m_lastKeyNavigationDirection == KeyNavigationDirection.Up)
                            {
                                files[previouslySelectedMinIndex].IsSelected = false;
                            }
                        }
                    }
                }
            }

        }
    }
}