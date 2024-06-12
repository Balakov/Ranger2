using System.Collections.Generic;

namespace Ranger2
{
    public class PathHistory
    {
        public enum HistoryUpdateType
        {
            UpdateHistory,
            NoHistoryUpdate
        }

        private List<string> m_paths = new ();
        private Dictionary<string, string> m_previouslySelectedDirectories = new();
        private int m_currentIndex = -1;
        private readonly char[] m_separators = [System.IO.Path.DirectorySeparatorChar, 
                                                System.IO.Path.AltDirectorySeparatorChar];

        private IDirectoryChangeRequest m_directoryChangeRequestor;

        private bool m_pathPushEnabled = true;

        public PathHistory(IDirectoryChangeRequest directoryChangeRequestor)
        {
            m_directoryChangeRequestor = directoryChangeRequestor;
        }

        public void EnablePathChangeCapture()
        {
            m_directoryChangeRequestor.OnDirectoryChanged += OnDirectoryChanged;
        }

        public void DisablePathChangeCapture()
        {
            m_directoryChangeRequestor.OnDirectoryChanged -= OnDirectoryChanged;
        }

        private void OnDirectoryChanged(string path, string previousPath)
        {
            SetPreviouslySelectedDirectoryForPath(previousPath, path);
            PushPath(path);
        }

        public void PushPath(string path)
        {
            if (m_pathPushEnabled)
            {
                if (m_currentIndex != m_paths.Count - 1)
                {
                    // Clear the history in front of this entry
                    m_paths.RemoveRange(m_currentIndex + 1, (m_paths.Count - 1) - m_currentIndex);
                }

                m_paths.Add(path);
                m_currentIndex = m_paths.Count - 1;
            }
        }

        public void Back()
        {
            if (m_currentIndex == -1)
                return;

            if (m_currentIndex > 0)
            {
                m_currentIndex--;
            }

            m_pathPushEnabled = false;
            m_directoryChangeRequestor.SetDirectory(m_paths[m_currentIndex]);
            m_pathPushEnabled = true;
        }

        public void Forward()
        {
            if (m_currentIndex == -1)
                return;

            if (m_currentIndex < m_paths.Count - 1)
            {
                m_currentIndex++;
            }

            m_pathPushEnabled = false;
            m_directoryChangeRequestor.SetDirectory(m_paths[m_currentIndex]);
            m_pathPushEnabled = true;
        }

        public void SetPreviouslySelectedDirectoryForPath(string path, string lastSelectedDirectory)
        {
            if (path != null)
            {
                path = path.TrimEnd(m_separators);
                lastSelectedDirectory = lastSelectedDirectory.TrimEnd(m_separators);

                if (!m_previouslySelectedDirectories.ContainsKey(path))
                {
                    m_previouslySelectedDirectories.Add(path, lastSelectedDirectory);
                }
                else
                {
                    m_previouslySelectedDirectories[path] = lastSelectedDirectory;
                }
            }
        }

        public string GetPreviouslySelectedDirectoryForPath(string path)
        {
            if (path != null)
            {
                path = path.TrimEnd(m_separators);

                if (m_previouslySelectedDirectories.ContainsKey(path))
                {
                    return m_previouslySelectedDirectories[path];
                }
            }

            return null;
        }
    }
}
