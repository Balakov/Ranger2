using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Ranger2
{
    public partial class OrganiseBookmarksDialog : Window
    {
        public class ViewModel : Utility.ViewModelBase
        {
            public string GroupName { get; }

            public ViewModel(string name)
            {
                GroupName = name;
            }
        }

        public IEnumerable<UserSettings.Bookmark> SortedBookmarks => ListBoxInstance.Items.Cast<UserSettings.Bookmark>();
        public string GroupName => GroupNameTextBox.Text;

        public OrganiseBookmarksDialog(IEnumerable<UserSettings.Bookmark> bookmarks, string groupName)
        {
            InitializeComponent();

            foreach (var bookmark in bookmarks)
            {
                ListBoxInstance.Items.Add(bookmark);
            }

            ListBoxInstance.SelectedIndex = ListBoxInstance.Items.Count - 1;

            if (int.TryParse(groupName, out int unused))
            {
                TitleTextBlock.Text = $"Bookmark Group {groupName}";
            }
            else
            {
                TitleTextBlock.Text = $"Bookmark Group \"{groupName}\"";
            }

            GroupNameTextBox.Text = groupName;

            DataContext = new ViewModel(groupName);
        }

        private void OKButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelection(1);
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelection(-1);
        }

        private void MoveSelection(int direction)
        {
            var selectedBookmark = ListBoxInstance.SelectedItem as UserSettings.Bookmark;
            int selectedBookmarkIndex = ListBoxInstance.SelectedIndex;
            ListBoxInstance.Items.RemoveAt(selectedBookmarkIndex);

            int newIndex = selectedBookmarkIndex + direction;
            ListBoxInstance.Items.Insert(newIndex, selectedBookmark);
            ListBoxInstance.SelectedIndex = newIndex;
        }

        private void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpArrowInstance.IsEnabled = ListBoxInstance.SelectedIndex > 0;
            DownArrowInstance.IsEnabled = ListBoxInstance.SelectedIndex < ListBoxInstance.Items.Count - 1;
        }
    }
}
