using System.IO;
using System.Windows;

namespace Ranger2
{
    public partial class EditBookmarkDialog : Window
    {
        public string RenamedBookmark => NameTextBoxInstance.Text;
        public string RenamedBookmarkPath => PathTextBoxInstance.Text;

        public EditBookmarkDialog(UserSettings.Bookmark bookmark)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            NameTextBoxInstance.Text = bookmark.Name;
            NameTextBoxInstance.SelectAll();
            NameTextBoxInstance.TextChanged += TextBoxInstance_TextChanged;

            PathTextBoxInstance.Text = bookmark.Path;
            PathTextBoxInstance.TextChanged += TextBoxInstance_TextChanged;
        }

        private void TextBoxInstance_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            OKButtonInstance.IsEnabled = !string.IsNullOrEmpty(NameTextBoxInstance.Text) && 
                                         !string.IsNullOrEmpty(PathTextBoxInstance.Text) &&
                                         FileSystemEnumeration.FileExists(PathTextBoxInstance.Text) ||
                                         FileSystemEnumeration.DirectoryExists(PathTextBoxInstance.Text);
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
    }
}
