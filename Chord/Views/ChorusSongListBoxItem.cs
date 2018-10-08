using Chord.Core.API.Chorus.Models;
using Chord.Core.Util;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Chord.Views
{
    class ChorusSongListBoxItem : ListBoxItem
    {
        public ChorusSongListBoxItem(MainWindow mainWindow, Song song)
        {
            Content = song.artist + " - " + song.name + " (" + song.charter + ")";
            RemoteSongContextMenu contextMenu = new RemoteSongContextMenu();
            contextMenu.Download += () =>
            {
                string songsDirectory = mainWindow.SongsDirectory.Text;
                mainWindow.StatusLabel.Content = "Downloading...";
                Task.Run(() =>
                {
                    string link = song.directLinks.archive ?? song.link;
                    bool downloadFailed = false;
                    try
                    {
                        SongDownloader.DownloadSong(songsDirectory, link, song.artist, song.name, song.charter, (status) =>
                        {
                            Dispatcher.Invoke(() => mainWindow.StatusLabel.Content = status);
                        });
                    }
                    catch (Win32Exception exception)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                        downloadFailed = true;
                    }
                    catch (Exception)
                    {
                        downloadFailed = true;
                        Dispatcher.Invoke(() =>
                        {
                            var result = MessageBox.Show("Link could not be downloaded. Open in browser instead?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (result == MessageBoxResult.Yes)
                            {
                                Process.Start(link);
                            }
                        });
                    }
                    Dispatcher.Invoke(() =>
                    {
                        mainWindow.ScanSongs();
                        if (!downloadFailed)
                        {
                            mainWindow.StatusLabel.Content = "Download complete.";
                        }
                    });
                });
            };
            ContextMenu = contextMenu;
        }
    }
}
