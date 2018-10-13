using System.Windows.Controls;

namespace Chord.Views
{
    class SongNodeContextMenu : ContextMenu
    {
        public SongNodeContextMenu()
        {
            Items.Add(new MenuItem { Header = "Rename song", Name = "renameSong" });
            Items.Add(new MenuItem { Header = "Delete", Name = "delete" });
        }
    }
}
