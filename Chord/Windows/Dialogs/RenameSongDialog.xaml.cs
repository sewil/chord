using System.Windows;

namespace Chord.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for TextDialog.xaml
    /// </summary>
    public partial class RenameSongDialog : Window
    {
        public RenameSongDialog(string currentSongName)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            SongNameTextBox.Text = currentSongName;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
