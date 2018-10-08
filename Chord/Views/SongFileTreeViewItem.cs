using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Chord.Views
{
    class SongFileTreeViewItem : TreeViewItem
    {
        public SongFileTreeViewItem(MainWindow mainWindow, string file)
        {
            FileInfo fileInfo = new FileInfo(file);
            Header = fileInfo.Name;
            var contextMenu = new SongNodeContextMenu();
            contextMenu.Delete += () =>
            {
                fileInfo.Delete();
                Task.Run(() => Dispatcher.Invoke(() => mainWindow.ScanSongs()));
            };
            ContextMenu = contextMenu;
        }
    }
}
