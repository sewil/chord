﻿using Chord.Core.API.Chorus.Models;
using Chord.Core.Util;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
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
                    string link = FileDownloader.GetDriveUrl(song.driveFileId ?? song.parentFolderId, song.driveFileId == null);
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
                    catch (Exception e)
                    {
                        string message;
                        if (e is WebException)
                        {
                            message = e.InnerException?.Message ?? e.Message;
                        }
                        else
                        {
                            message = "Link could not be downloaded.";
                        }
                        downloadFailed = true;
                        Dispatcher.Invoke(() =>
                        {
                            var result = MessageBox.Show(string.Format("{0} Open in browser instead?", message), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
