using System.Windows.Controls;

namespace Chord.Views
{
    class RemoteSongContextMenu : ContextMenu
    {
        public delegate void EmptyEventHandler();
        public event EmptyEventHandler Download;

        public RemoteSongContextMenu()
        {
            Items.Add(new MenuItem { Header = "Download" });
            PreviewMouseLeftButtonUp += (obj, args) =>
            {
                MenuItem menuItem = (MenuItem)args.Source;
                if (menuItem.Header.ToString() == "Download")
                {
                    Download.Invoke();
                }
            };
        }
    }
}
