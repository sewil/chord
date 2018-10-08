using System.Windows.Controls;

namespace Chord.Views
{
    class SongNodeContextMenu : ContextMenu
    {
        public delegate void EmptyEventHandler();
        public event EmptyEventHandler Delete;

        public SongNodeContextMenu()
        {
            Items.Add(new MenuItem { Header = "Delete" });
            PreviewMouseLeftButtonUp += (obj, args) =>
            {
                MenuItem menuItem = (MenuItem)args.Source;
                if (menuItem.Header.ToString() == "Delete")
                {
                    Delete.Invoke();
                }
            };
        }
    }
}
