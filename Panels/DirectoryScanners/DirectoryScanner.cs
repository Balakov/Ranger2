using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Ranger2
{
    public class DirectoryScanner
    {
        public delegate void OnDirectoryScanCompleteDelegate(ScanResult scanResult);
        public event OnDirectoryScanCompleteDelegate OnDirectoryScanComplete;
        
        protected void InvokeOnDirectoryScanComplete(ScanResult scanResult) => OnDirectoryScanComplete?.Invoke(scanResult);

        public class ScanResult
        {
            public struct Filename
            {
                public readonly string Path;
                public readonly string Name;

                public Filename(string path, string name)
                {
                    Path = path;
                    Name = name;
                }
            }

            public readonly string ScannedPath;
            public readonly IEnumerable<Filename> Files;
            public readonly IEnumerable<string> Directories;

            public ScanResult(string scannedPath, IEnumerable<Filename> files, IEnumerable<string> directories)
            {
                ScannedPath = scannedPath?.TrimEnd('\\');
                Files = files;
                Directories = directories;
            }

            public static ScanResult Empty() => new ScanResult(null, [], []);

            public bool MatchesPath(string currentPath)
            {
                if (ScannedPath == null)
                    return true;

                string cleanScannedPath = ScannedPath?.TrimEnd('\\');
                string cleanCurrent = currentPath.TrimEnd('\\');

                return string.Equals(cleanCurrent, cleanScannedPath, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public virtual void ScanDirectory(string directory, IEnumerable<string> allowedExtensions = null)
        {
            if (string.IsNullOrEmpty(directory))
            {
                OnDirectoryScanComplete?.Invoke(ScanResult.Empty());
                return;
            }

            if (File.Exists(directory))
            {
                directory = Path.GetDirectoryName(directory);
            }

            if (!directory.StartsWith(@"\\"))
            {
                if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                    !directory.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    directory += Path.DirectorySeparatorChar;
                }
            }

            BackgroundWorker backgroundThread = new BackgroundWorker();

            backgroundThread.DoWork += (s, e) =>
            {
                bool isRootUNCPath = false;
                bool isRootUNCOrShare = false;

                if (!directory.StartsWith(@"\\"))
                {
                    if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                        !directory.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                    {
                        directory += Path.DirectorySeparatorChar;
                    }
                }
                else
                {
                    // If this is the first part of a UNC path it's not a real directory, it's a share - we need to skip some checks
                    string[] UNCParts = directory.Split('\\');
                    if (UNCParts.Length <= 4)
                    {
                        isRootUNCOrShare = true;

                        if (UNCParts.Length == 3)
                        {
                            isRootUNCPath = true;
                        }
                    }
                }

                if (!isRootUNCOrShare)
                {
                    try
                    {
                        // Fix any relative paths
                        directory = Path.GetFullPath(directory);
                    }
                    catch (Exception)
                    {
                        e.Result = ScanResult.Empty();
                        return;
                    }

                    if (!Directory.Exists(directory))
                    {
                        e.Result = ScanResult.Empty();
                        return;
                    }
                }

                try
                {
                    List<ScanResult.Filename> files = new();
                    List<string> directories = new();

                    if (isRootUNCPath)
                    {
                        var networkShares = Trinet.Networking.ShareCollection.GetShares(directory);
                        List<string> sharePaths = new List<string>();
                        foreach (Trinet.Networking.Share networkShare in networkShares)
                        {
                            if (!networkShare.NetName.EndsWith("$") && (networkShare.ShareType.HasFlag(Trinet.Networking.ShareType.Device) ||
                                                                        networkShare.ShareType.HasFlag(Trinet.Networking.ShareType.Disk)))
                            {
                                // Don't use ToString() as the TriNet code seems to add a random number of leading slashes.
                                directories.Add(networkShare.Server + '\\' + networkShare.NetName);
                            }
                        }
                    }
                    else
                    {
                        directories.AddRange(Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly));

                        bool isRoot = directory.Length < 4;

                        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
                        {
                            bool fileAllowed = true;

                            if (allowedExtensions != null)
                            {
                                string fileExtension = Path.GetExtension(file).ToLower();
                                fileAllowed = allowedExtensions.Any(x => x == fileExtension);
                            }

                            if (fileAllowed)
                            {
                                files.Add(new ScanResult.Filename(file, null));
                            }
                        }
                    }

                    files.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

                    e.Result = new ScanResult(directory, files, directories);
                }
                catch (Exception)
                {
                    e.Result = ScanResult.Empty();
                }
            };

            backgroundThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler((s, e) =>
            {
                InvokeOnDirectoryScanComplete(e.Result as ScanResult);
            });

            backgroundThread.RunWorkerAsync();
        }
    }
}