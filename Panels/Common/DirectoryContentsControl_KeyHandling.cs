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
                else if (e.Key == Key.Back)
                {
                    m_context.DirectoryChangeRequester.SetDirectoryToParent();
                }
                else if (e.Key == Key.Oem8)
                {
                    m_context.PanelLayout.CycleFocus(KeyboardUtilities.IsShiftDown ? PanelCycleDirection.Left : PanelCycleDirection.Right);
                }
                else if (e.Key == Key.F5)
                {
                    OnDirectoryChanged(m_currentDirectory);
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

        }
    }
}