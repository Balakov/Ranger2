using System.IO;
using System.Windows;

namespace Ranger2.Dialogs
{
    public partial class RenameDialog : Window
    {
        private readonly string m_basePath;
        private readonly string m_originalPath;
        private readonly bool m_allowSameName;

        public string RenamedFile => Path.Combine(m_basePath, TextBoxInstance.Text);

        public RenameDialog(string filePath, string title, bool allowSameName)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            m_originalPath = filePath;
            m_basePath = Path.GetDirectoryName(filePath);
            m_allowSameName = allowSameName;

            TextBoxInstance.Text = Path.GetFileName(filePath); ;
            TextBoxInstance.SelectAll();

            DuplicateWarning.Visibility = Visibility.Collapsed;
            TextBoxInstance.TextChanged += TextBoxInstance_TextChanged;

            TitleTextBox.Text = title;

            OKButtonInstance.IsEnabled = allowSameName;
        }

        private void TextBoxInstance_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (RenamedFile == m_originalPath && !m_allowSameName)
            {
                DuplicateWarning.Visibility = Visibility.Collapsed;
                OKButtonInstance.IsEnabled = false;
            }
            else if (File.Exists(RenamedFile))
            {
                DuplicateWarning.Visibility = Visibility.Visible;
                DuplicateWarning.ToolTip = "There is already an existing file with that name.";
                OKButtonInstance.IsEnabled = false;
            }
            else if (Directory.Exists(RenamedFile))
            {
                DuplicateWarning.Visibility = Visibility.Visible;
                DuplicateWarning.ToolTip = "There is already an existing directory with that name.";
                OKButtonInstance.IsEnabled = false;
            }
            else
            {
                DuplicateWarning.Visibility = Visibility.Collapsed;
                OKButtonInstance.IsEnabled = !string.IsNullOrEmpty(TextBoxInstance.Text);
            }
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
