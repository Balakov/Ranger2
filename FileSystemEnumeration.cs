using System.Collections.Generic;
using System.IO;

namespace Ranger2
{
    public static class FileSystemEnumeration
    {
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public static IEnumerable<string> EnumerateFiles(string directory,
                                                         string searchPattern = "*",
                                                         SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return Directory.EnumerateFiles(directory, searchPattern, searchOption);
        }
        
        public static IEnumerable<string> EnumerateDirectories(string directory,
                                                               string searchPattern = "*", 
                                                               SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return Directory.EnumerateDirectories(directory, searchPattern, searchOption);
        }
    }
}
