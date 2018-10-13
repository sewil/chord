using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Chord.Windows.Dialogs;
using IniParser.Model;

namespace Chord.Views
{
    public enum NodeType
    {
        Directory, File
    }

    class SongNodeTreeViewItem : TreeViewItem
    {
        public string Identifier { get; set; }
        public MainWindow MainWindow { get; set; }
        public FileInfo FileInfo { get; set; }
        public DirectoryInfo DirectoryInfo { get; set; }
        public NodeType NodeType { get; set; }

        public SongNodeTreeViewItem(MainWindow mainWindow, string path, NodeType nodeType)
        {
            Identifier = path;
            if (nodeType == NodeType.File)
            {
                FileInfo = new FileInfo(path);
                Header = FileInfo.Name;
            }
            else if (nodeType == NodeType.Directory)
            {
                DirectoryInfo = new DirectoryInfo(path);
                Header = DirectoryInfo.Name;

                PreviewMouseRightButtonDown += (sender, e) =>
                {
                    TreeViewItem treeViewItem = MainWindow.VisualUpwardSearch(e.OriginalSource as DependencyObject);
                    if (treeViewItem != null)
                    {
                        treeViewItem.Focus();
                        e.Handled = true;
                    }
                };
            }

            MainWindow = mainWindow;
            var contextMenu = new SongNodeContextMenu();
            contextMenu.PreviewMouseLeftButtonUp += (obj, args) =>
            {
                MenuItem menuItem = (MenuItem)args.Source;
                if (menuItem.Name == "delete")
                {
                    try
                    {
                        if (nodeType == NodeType.File)
                        {
                            FileInfo.Delete();
                        }
                        else if (nodeType == NodeType.Directory)
                        {
                            DirectoryInfo.Delete(true);
                        }
                    }
                    catch (IOException e)
                    {
                        MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Task.Run(() => Dispatcher.Invoke(() => mainWindow.ScanSongs()));
                }
                else if (menuItem.Name == "renameSong")
                {
                    RenameSong();
                }
            };
            ContextMenu = contextMenu;
        }

        public void RenameSong()
        {
            string songIni = null;
            if (NodeType == NodeType.Directory)
            {
                songIni = Directory.GetFiles(DirectoryInfo.FullName, "song.ini", SearchOption.AllDirectories)?.FirstOrDefault();
            }
            else if (NodeType == NodeType.File && FileInfo.Name == "song" && FileInfo.Extension == "ini")
            {
                songIni = FileInfo.FullName;
            }
            if (songIni == null)
            {
                MessageBox.Show("File 'song.ini' not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IniData songIniData;
            try
            {
                songIniData = App.fileIniParser.ReadFile(songIni);
                if (songIniData["song"] == null)
                {
                    throw new System.Exception("Section 'song' missing.");
                }
            }
            catch (System.Exception exception)
            {
                MessageBox.Show("Error reading song ini data: " + exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RenameSongDialog renameSongDialog = new RenameSongDialog(songIniData["song"]["name"]);
            renameSongDialog.Show();
            renameSongDialog.RenameButton.Click += (obj, e) =>
            {
                string newName = renameSongDialog.SongNameTextBox.Text;
                songIniData["song"]["name"] = newName;
                App.fileIniParser.WriteFile(songIni, songIniData, System.Text.Encoding.UTF8);
                renameSongDialog.Close();
                MainWindow.ScanSongs();
            };
        }
    }
}
