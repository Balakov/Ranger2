using System.Linq;
using System.Windows;

namespace Ranger2
{
    public partial class BookmarksPanel
    {
        public BookmarksPanel()
        {
            InitializeComponent();
        }

        private void BookmarksPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var data = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (DataContext is ViewModel viewModel)
                {
                    var pathToAdd = data.FirstOrDefault();

                    if (!string.IsNullOrEmpty(pathToAdd))
                    {
                        viewModel.AddBookmark(null, pathToAdd, openInExplorer: false, bookmarkGroup: App.UserSettings.ActiveBookmarkGroup);
                    }
                }
            }
        }

        private void BookmarksPanel_DragOver(object sender, DragEventArgs e)
        {
            var data = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (data != null)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
    }
}
