using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Ranger2
{
    public static class ClipboardManager
    {
        [Serializable]
        public class FileOperationPath
        {
            public string FullPath;
            public string Name;
        }

        [Serializable]
        public class FileOperationPathList
        {
            public List<FileOperationPath> Paths = new List<FileOperationPath>();
            public FileOperations.OperationType FileOperation;
        }

        [Flags]
        public enum ClipboardDataTypes
        {
            Data = 1 << 0,
            Files = 1 << 1,
            Text = 1 << 2
        }

        public static IDataObject PathsToDataObject(IEnumerable<FileOperationPath> paths, FileOperations.OperationType fileOp, ClipboardDataTypes dataTypes)
        {
            if (paths.Count() == 0)
            {
                return null;
            }

            FileOperationPathList pathList = new FileOperationPathList()
            {
                Paths = paths.ToList(),
                FileOperation = fileOp
            };

            StringBuilder sb = new StringBuilder();
            StringCollection fileDroplist = new StringCollection();

            foreach (FileOperationPath path in paths)
            {
                sb.AppendLine(path.FullPath);
                fileDroplist.Add(path.FullPath);
            }

            DataObject dataObject = new DataObject();

            if (dataTypes.HasFlag(ClipboardDataTypes.Data))
            {
                dataObject.SetData(pathList);
            }

            if (dataTypes.HasFlag(ClipboardDataTypes.Files))
            {
                dataObject.SetFileDropList(fileDroplist);
            }

            if (dataTypes.HasFlag(ClipboardDataTypes.Text))
            {
                dataObject.SetText(sb.ToString());
            }

            return dataObject;
        }

        public static void CopyPathsToClipboard(IEnumerable<FileOperationPath> paths,
                                                FileOperations.OperationType fileOp,
                                                ClipboardDataTypes dataTypes = ClipboardDataTypes.Data | ClipboardDataTypes.Files | ClipboardDataTypes.Text)
        {
            var dataObject = PathsToDataObject(paths, fileOp, dataTypes);
            if (dataObject != null)
            {
                Clipboard.SetDataObject(dataObject);
            }
        }

        public static IEnumerable<FileOperationPath> GetPathsFromClipboard(out FileOperations.OperationType fileOp)
        {
            fileOp = FileOperations.OperationType.Copy;

            IDataObject clipData = Clipboard.GetDataObject();
            if (clipData != null)
            {
                if (clipData.GetDataPresent(typeof(FileOperationPathList)))
                {
                    FileOperationPathList opList = clipData.GetData(typeof(FileOperationPathList)) as FileOperationPathList;
                    if (opList != null)
                    {
                        fileOp = opList.FileOperation;
                        return opList.Paths;
                    }
                }
            }

            if (Clipboard.ContainsFileDropList())
            {
                StringCollection clipPaths = Clipboard.GetFileDropList();

                string[] paths = new string[clipPaths.Count];
                clipPaths.CopyTo(paths, 0);
                return paths.Select(x => new FileOperationPath() { FullPath = x });
            }

            if (Clipboard.ContainsText())
            {
                // See if this is a list of files and dirs
                List<FileOperationPath> paths = new();
                StringReader sr = new StringReader(Clipboard.GetText());
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        line = line.Trim();
                        if (File.Exists(line) || Directory.Exists(line))
                        {
                            paths.Add(new FileOperationPath() { FullPath = line });
                        }
                    }
                }

                return paths;
            }

            return new List<FileOperationPath>();
        }
    }

}
