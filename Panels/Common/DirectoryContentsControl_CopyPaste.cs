using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public partial class ViewModel
        {
            public void HandleCopy(bool isCut)
            {
                if (TryGetSelectedFiles(out IEnumerable<FileSystemObjectViewModel> selectedViewModels))
                {
                    var selectedPaths = selectedViewModels.Select(x => new ClipboardManager.FileOperationPath() { FullPath = x.FullPath, Name = x.Name });
                    ClipboardManager.CopyPathsToClipboard(selectedPaths, isCut ? FileOperations.OperationType.Move
                                                                               : FileOperations.OperationType.Copy);
                }
                else
                {
                    // No files selected - copy the directory name to the clipboard as text
                    ClipboardManager.CopyPathsToClipboard([new ClipboardManager.FileOperationPath() { FullPath = CurrentPath }],
                                                          FileOperations.OperationType.Copy,
                                                          ClipboardManager.ClipboardDataTypes.Text);
                }
            }

            public void HandlePaste()
            {
                FileOperations.OperationType fileOp;
                IEnumerable<ClipboardManager.FileOperationPath> paths = ClipboardManager.GetPathsFromClipboard(out fileOp);
                OnDropOrPaste(paths, CurrentPath, fileOp, FileOperations.PasteOverSelfType.Allowed);
            }

            private static string FindCommonRoot(IEnumerable<ClipboardManager.FileOperationPath> paths)
            {
                string[] firstPathSplit = null;

                int lowestMatchIndex = int.MaxValue;
                foreach (ClipboardManager.FileOperationPath path in paths)
                {
                    string[] pathSplit = path.FullPath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
                    if (firstPathSplit == null)
                    {
                        // Assume all paths have a common root, so just pick the first path as the base to compare the others against
                        firstPathSplit = pathSplit;
                        lowestMatchIndex = firstPathSplit.Length - 1;
                    }
                    else
                    {
                        // If this path matches fewer parts than the previous one, make it the new common root.
                        int currentMatchIndex = 0;
                        for (int i = 0; i < pathSplit.Length && i < firstPathSplit.Length; i++)
                        {
                            if (pathSplit[i] == firstPathSplit[i])
                            {
                                currentMatchIndex++;
                            }
                        }

                        if (currentMatchIndex < lowestMatchIndex)
                        {
                            lowestMatchIndex = currentMatchIndex;
                        }
                    }
                }

                if (lowestMatchIndex > 0)
                {
                    return string.Join(Path.DirectorySeparatorChar.ToString(), firstPathSplit, 0, lowestMatchIndex) + Path.DirectorySeparatorChar;
                }
                else
                {
                    return null;
                }
            }

            private static string BuildDestinationPathForFileCopy(ClipboardManager.FileOperationPath path, string commonRoot, string destinationDirectory)
            {
                // Remove the common root
                string relativeDestination = path.FullPath.Substring(commonRoot.Length);
                string destinationPath = Path.Combine(destinationDirectory, relativeDestination);

                if (path.Name != null)
                {
                    string dirPart = Path.GetDirectoryName(destinationPath);
                    destinationPath = Path.Combine(dirPart, path.Name);
                }

                return destinationPath;
            }

            private static void OnDropOrPaste(IEnumerable<ClipboardManager.FileOperationPath> paths,
                                              string destinationDirectory,
                                              FileOperations.OperationType fileOp,
                                              FileOperations.PasteOverSelfType pasteOverSelf)
            {
                if (paths.Count() == 0)
                {
                    return;
                }

                string commonRoot = FindCommonRoot(paths);
                if(commonRoot == null)
                {
                    return;
                }

                List<string> copyFrom = new List<string>();
                List<string> copyTo = new List<string>();

                foreach (ClipboardManager.FileOperationPath path in paths)
                {
                    string destinationPath = BuildDestinationPathForFileCopy(path, commonRoot, destinationDirectory);

                    if (path.FullPath == destinationPath)
                    {
                        if (pasteOverSelf == FileOperations.PasteOverSelfType.Allowed)
                        {
                            // Duplicate the file or directory
                            bool isDirectory = FileSystemEnumeration.DirectoryExists(destinationPath);
                            bool isFile = FileSystemEnumeration.FileExists(destinationPath);

                            if (isDirectory || isFile)
                            {
                                for (int i = 1; i < 10; i++)
                                {
                                    string newDestinationPath = Path.Combine(Path.GetDirectoryName(destinationPath),
                                                                             Path.GetFileNameWithoutExtension(destinationPath) + " (" + i.ToString() + ")" + Path.GetExtension(destinationPath));
                                    if ((isFile && !FileSystemEnumeration.FileExists(newDestinationPath)) ||
                                        (isDirectory && !FileSystemEnumeration.DirectoryExists(newDestinationPath)))
                                    {
                                        copyFrom.Add(path.FullPath);
                                        copyTo.Add(newDestinationPath);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        copyFrom.Add(path.FullPath);
                        copyTo.Add(destinationPath);
                    }
                }

                if (copyFrom.Count > 0)
                {
                    // If the common root is in the %temp% directory, don't thread the operation. This is probably a compressed
                    // file extraction or other operation that's going to clean up temp files after we return from the DragDrop.

                    string tempDirectory = Path.GetTempPath();
                    string fullCommonRootPath = Path.GetFullPath(commonRoot);   // 7-Zip passes 8.3 version of temp directory

                    FileOperations.OperationBlockingBehaviour blockingBehaviour = fullCommonRootPath.StartsWith(tempDirectory) ? FileOperations.OperationBlockingBehaviour.BlockUntilComplete :
                                                                                                                                 FileOperations.OperationBlockingBehaviour.HandOffToThread;
                    if (fileOp == FileOperations.OperationType.Move)
                    {
                        FileOperations.MoveFiles(copyFrom, copyTo, blockingBehaviour);
                    }
                    else
                    {
                        FileOperations.CopyFiles(copyFrom, copyTo, blockingBehaviour);
                    }
                }
            }

        }
    }
}