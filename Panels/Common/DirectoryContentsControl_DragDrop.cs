﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public partial class ViewModel
        {
            public void OnCommonDrop(DragEventArgs e, UIElement dropTarget)
            {
                var effect = GetDesiredDragDropOperation(e);

                DroppedFiles droppedData = PathsFromDragDropSource(e.Data, effect);

                if (droppedData.Files != null && 
                    droppedData.Files.Count() > 0)
                {
                    // Copy the files to the current directory unless we dragged onto a specific directory
                    FileSystemObjectViewModel dropItem = (dropTarget.InputHitTest(e.GetPosition(dropTarget)) as FrameworkElement)?.DataContext as FileSystemObjectViewModel;
                        
                    string destinationPath = (dropItem is DirectoryViewModel dirViewModel) ? dirViewModel.FullPath : m_currentDirectory;
                    OnDropOrPaste(droppedData.Files, destinationPath, droppedData.FileOp, FileOperations.PasteOverSelfType.NotAllowed);
                }
            }

            private DragDropEffects GetDesiredDragDropOperation(DragEventArgs e)
            {
                if (e.Data.GetDataPresent(typeof(ClipboardManager.FileOperationPathList)) ||
                    e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    DroppedFiles droppedData = PathsFromDragDropSource(e.Data, e.Effects);
                    
                    if (droppedData.Files != null &&
                        droppedData.Files.Count() > 0)
                    {
                        string currentPathRoot = Path.GetPathRoot(CurrentPath);

                        // If all files are on the same drive, or ALT is pressed, move the files.
                        bool shouldMove = droppedData.Files.All((x) =>
                        {
                            return Path.GetPathRoot(x.FullPath) == currentPathRoot;
                        });

                        if (KeyboardUtilities.IsAltDown)
                        {
                            shouldMove = !shouldMove;
                        }

                        return shouldMove ? DragDropEffects.Move : DragDropEffects.Copy;
                    }
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor"))
                {
                    return DragDropEffects.Copy;
                }
                    
                return DragDropEffects.None;
            }

            public void OnCommonDragOver(DragEventArgs e)
            {
                e.Effects = GetDesiredDragDropOperation(e);
                e.Handled = true;
            }

            private class DroppedFiles
            {
                public IEnumerable<ClipboardManager.FileOperationPath> Files;
                public FileOperations.OperationType FileOp;
            }

            private DroppedFiles PathsFromDragDropSource(IDataObject data, DragDropEffects effect)
            {
                if (data.GetDataPresent(typeof(ClipboardManager.FileOperationPathList)))
                {
                    var pathList = (ClipboardManager.FileOperationPathList)data.GetData(typeof(ClipboardManager.FileOperationPathList));

                    return new DroppedFiles()
                    {
                        Files = pathList.Paths,
                        FileOp = (effect == DragDropEffects.Move) ? FileOperations.OperationType.Move : FileOperations.OperationType.Copy
                    };
                }
                else if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    return new DroppedFiles()
                    {
                        Files = ((string[])data.GetData(DataFormats.FileDrop)).Select(x => new ClipboardManager.FileOperationPath() { FullPath = x }),
                        FileOp = (effect == DragDropEffects.Move) ? FileOperations.OperationType.Move : FileOperations.OperationType.Copy
                    };
                }
                else if (data.GetDataPresent("FileGroupDescriptor"))
                {
                    // https://stackoverflow.com/questions/33019385/c-sharp-drag-and-drop-attached-file-from-outlook-email
                    //
                    // First step here is to get the filename of the attachment and build a full-path name so we can store it
                    // in the temporary folder

                    MemoryStream memoryStream = (MemoryStream)data.GetData("FileGroupDescriptor");
                    byte[] fileGroupDescriptor = new byte[memoryStream.Length];
                    memoryStream.Read(fileGroupDescriptor, 0, (int)memoryStream.Length);
                    // used to build the filename from the FileGroupDescriptor block
                    StringBuilder fileName = new StringBuilder();
                    // this trick gets the filename of the passed attached file
                    for (int i = 76; fileGroupDescriptor[i] != 0; i++)
                    {
                        fileName.Append(Convert.ToChar(fileGroupDescriptor[i]));
                    }
                    memoryStream.Close();

                    // Second step:  we have the file name, now we need to get the actual raw data for the attached file and copy it to disk so we work on it.
                    MemoryStream dataMemoryStream = (MemoryStream)data.GetData("FileContents", true);
                    byte[] fileBytes = new byte[dataMemoryStream.Length];
                    dataMemoryStream.Read(fileBytes, 0, (int)dataMemoryStream.Length);

                    // Create a file and save the raw data to it
                    string filePath = Path.Combine(Path.GetTempPath(), fileName.ToString());
                    File.WriteAllBytes(filePath, fileBytes);

                    FileInfo tempFile = new FileInfo(filePath);

                    if (tempFile.Exists)
                    {
                        return new DroppedFiles()
                        {
                            Files = [new ClipboardManager.FileOperationPath() { FullPath = filePath }],
                            FileOp = FileOperations.OperationType.Move  // Force move so we delete the temp file
                        };
                    }
                }

                return new DroppedFiles();
            }

        }
    }
}