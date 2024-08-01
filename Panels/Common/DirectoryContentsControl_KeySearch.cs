using System.Collections.Generic;
using System.Linq;

namespace Ranger2
{
    public partial class DirectoryContentsControl
    {
        public partial class ViewModel
        {
            public bool SelectClosestFile(string search, int searchIndex)
            {
                search = search.ToLower();

                List<FileSystemObjectViewModel> matches = m_visualOrderProvider.GetVisualItems().Where(x => x.Name.ToLower().StartsWith(search)).ToList();

                if (matches.Any())
                {
                    FileSystemObjectViewModel choosenMatch = null;

                    if (searchIndex == 0)
                    {
                        choosenMatch = matches.First();
                    }
                    else
                    {
                        choosenMatch = matches[searchIndex % matches.Count];
                    }

                    foreach (FileSystemObjectViewModel file in m_files)
                    {
                        file.IsSelected = file == choosenMatch;
                    }

                    m_scrollIntoViewProvider?.ScrollIntoView(choosenMatch);
                }

                return false;
            }
        }
    }
}