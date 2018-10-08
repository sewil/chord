using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Chord.Views
{
    class SongDirectoryTreeViewItem : TreeViewItem
    {
        public SongDirectoryTreeViewItem(MainWindow mainWindow, string directory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            Header = directoryInfo.Name;
            var contextMenu = new SongNodeContextMenu();
            contextMenu.Delete += () =>
            {
                directoryInfo.Delete(true);
                Task.Run(() => Dispatcher.Invoke(() => mainWindow.ScanSongs()));
            };
            PreviewMouseRightButtonDown += (sender, e) =>
            {
                TreeViewItem treeViewItem = MainWindow.VisualUpwardSearch(e.OriginalSource as DependencyObject);
                if (treeViewItem != null)
                {
                    treeViewItem.Focus();
                    e.Handled = true;
                }
            };

            ContextMenu = contextMenu;
        }
    }
}
