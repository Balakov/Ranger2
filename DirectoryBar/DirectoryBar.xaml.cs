using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Ranger2
{
    public partial class DirectoryBar : UserControl
    {
        //

        public IDirectoryChangeRequest DirectoryChangeRequester
        {
            get { return (IDirectoryChangeRequest)GetValue(DirectoryChangeRequesterProperty); }
            set { SetValue(DirectoryChangeRequesterProperty, value); }
        }

        public static readonly DependencyProperty DirectoryChangeRequesterProperty = DependencyProperty.Register(nameof(DirectoryChangeRequester),
                                                                                                                 typeof(IDirectoryChangeRequest),
                                                                                                                 typeof(DirectoryBar),
                                                                                                                 new PropertyMetadata(null, OnDependencyPropertyChange));

        //

        public IPanelLayout PanelLayout
        {
            get { return (IPanelLayout)GetValue(PanelLayoutProperty); }
            set { SetValue(PanelLayoutProperty, value); }
        }

        public static readonly DependencyProperty PanelLayoutProperty = DependencyProperty.Register(nameof(PanelLayout),
                                                                                                    typeof(IPanelLayout),
                                                                                                    typeof(DirectoryBar),
                                                                                                    new PropertyMetadata(null, OnDependencyPropertyChange));

        //

        private static void OnDependencyPropertyChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DirectoryBar directoryBar)
            {
                directoryBar.OnDependencyPropertyChange();
            }
        }

        public DirectoryBar()
        {
            InitializeComponent();
        }

        private void OnDependencyPropertyChange()
        {
            if (DirectoryChangeRequester != null &&
                PanelLayout != null)
            {
                DirectoryChangeRequester.OnDirectoryChanged -= OnDirectoryChanged;
                PanelLayout.OnSwitchPanelFocus -= OnActivePanelChanged;

                DirectoryChangeRequester.OnDirectoryChanged += OnDirectoryChanged;
                PanelLayout.OnSwitchPanelFocus += OnActivePanelChanged;

                OnActivePanelChanged(PanelLayout.CurrentPanel);
            }
        }

        private void OnActivePanelChanged(DirectoryContentsControl panel)
        {
            OnDirectoryChanged(panel.CurrentPath, null, null);
        }

        private void OnDirectoryChanged(string path, string previousPath, string pathToSelect)
        {
            TextBoxInstance.Text = !string.IsNullOrEmpty(pathToSelect) ? pathToSelect : path;
        }

        private void TextBoxInstance_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter &&
                !string.IsNullOrEmpty(TextBoxInstance.Text))
            {
                string path = TextBoxInstance.Text;
                string selectedFile = null;

                if (FileSystemEnumeration.FileExists(path))
                {
                    selectedFile = path;
                    path = Path.GetDirectoryName(path);
                }

                DirectoryChangeRequester.SetDirectory(path, selectedFile);
            }
        }

        private void TextBoxInstance_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    TextBoxInstance.Text = files[0];
                }
            }
        }

        private void TextBoxInstance_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = false;
        }

        private void TextBoxInstance_OnPreviewDragOver(object sender, DragEventArgs e)
        {
            // Disable default TextBox drag and drop behaviour.
            // https://stackoverflow.com/questions/4281857/wpf-drag-and-drop-to-a-textbox
            e.Handled = true;
        }
    }
}
