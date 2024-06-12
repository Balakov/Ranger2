using System.Linq;

namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public partial class ViewModel
        {
            public bool SelectClosestFile(string searchString, KeySearch.SearchLimitType limitType)
            {
                FileSystemObjectViewModel matchingItem = null;

                switch (limitType)
                {
                    case KeySearch.SearchLimitType.None:
                        matchingItem = m_files.FirstOrDefault(x => x.Name.ToLower().StartsWith(searchString.ToLower()));
                        break;
                    case KeySearch.SearchLimitType.Files:
                        matchingItem = m_files.FirstOrDefault(x => !(x is DirectoryViewModel) &&
                                                                   x.Name.ToLower().StartsWith(searchString.ToLower()));
                        break;
                    case KeySearch.SearchLimitType.Directories:
                        matchingItem = m_files.FirstOrDefault(x => (x is DirectoryViewModel) &&
                                                                   x.Name.ToLower().StartsWith(searchString.ToLower()));
                        break;
                }

                if (matchingItem != null)
                {
                    DisableEventHandlers();

                    foreach (var file in Files)
                    {
                        file.IsSelected = false;
                    }

                    matchingItem.IsSelected = true;
                    m_scrollIntoViewProvider?.ScrollIntoView(matchingItem);
                    EnableEventHandlers();

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}