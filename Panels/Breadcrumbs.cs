using System;
using System.IO;
using System.Collections.ObjectModel;

namespace Ranger2
{
    public class BreadcrumbsViewModel : Utility.ViewModelBase
    {
        public class PathPart
        {
            public string Path { get; }
            public string Name { get; }
            public bool IsEnabled { get; }

            public PathPart(string path, string name, bool isEnabled)
            {
                Path = path;
                Name = name;
                IsEnabled = isEnabled;
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
