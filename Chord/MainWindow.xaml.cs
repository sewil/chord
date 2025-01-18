using Chord.Core.API.Chorus;
using Chord.Core.API.Chorus.Models;
using Chord.Core.API.RhythmVerse;
using Chord.Properties;
using Chord.Views;
using Chord.Windows;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chord
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string selectedNode;

        public MainWindow()
        {
            InitializeComponent();
            string latestCommitHash = ConfigurationManager.AppSettings["LatestCommitHash"];
            string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            Uri releasesURL = new Uri(ConfigurationManager.AppSettings["ReleasesURL"]);
            GitHubHyperlink.NavigateUri = releasesURL;
            GitHubHyperlink.Inlines.Add(new Run(latestCommitHash + "@" + version));
            GitHubHyperlink.RequestNavigate += (sender, e) =>
            {
                Process.Start(e.Uri.ToString());
            };
            CreditsLink.RequestNavigate += (sender, e) =>
            {
                CreditsWindow window = new CreditsWindow();
                window.Show();
            };
            var selectedAPI = string.IsNullOrWhiteSpace(Settings.Default.SelectedAPI) ? "Chorus" : Settings.Default.SelectedAPI;
            APIComboBox.Items.Add(new APIComboBoxItem(APIType.Chorus, "Chorus") { IsSelected = selectedAPI == "Chorus" });
            APIComboBox.Items.Add(new APIComboBoxItem(APIType.RhythmVerse, "RhythmVerse") { IsSelected = selectedAPI == "RhythmVerse" });
            LocateSongsDirectory();
            UpdateSongsDirectory();
            LocateGamePath();
            Dispatcher.Invoke(() =>
            {
                GamePathTextBox.Text = Settings.Default.GamePath;
            });
        }

        private void LocateGamePath()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.GamePath))
            {
                var gamePath = Path.Combine("C:\\", "Program Files", "Clone Hero", "Clone Hero.exe");
                if (File.Exists(gamePath))
                {
                    Settings.Default.GamePath = gamePath;
                    Settings.Default.Save();
                }
            }
        }

        private void LocateSongsDirectory()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.SongsDirectory))
            {
                string songsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Clone Hero", "Songs");
                if (Directory.Exists(songsDirectory))
                {
                    Settings.Default.SongsDirectory = songsDirectory;
                    Settings.Default.Save();
                }
            }
        }

        private void UpdateSongsDirectory()
        {
            var songsDirectory = Settings.Default.SongsDirectory;
            if (!string.IsNullOrWhiteSpace(songsDirectory) && Directory.Exists(songsDirectory))
            {
                SongsDirectory.Text = Settings.Default.SongsDirectory;
                Task.Run(() =>
                {
                    Dispatcher.Invoke(() => ScanSongs());
                });
            }
            else
            {
                SongList.Visibility = Visibility.Collapsed;
                SongListPlaceholderLabel.Visibility = Visibility.Visible;
            }
        }

        public void ScanSongs()
        {
            StatusLabel.Content = "Scanning songs directory...";
            try
            {
                TreeViewItem treeViewItem = ScanDirectory(SongsDirectory.Text);
                SongListPlaceholderLabel.Visibility = Visibility.Collapsed;
                SongList.Visibility = Visibility.Visible;
                Settings.Default.SongsDirectory = SongsDirectory.Text;
                Settings.Default.Save();
                treeViewItem.ContextMenu = null;
                treeViewItem.IsExpanded = true;
                SongList.Items.Clear();
                SongList.Items.Add(treeViewItem);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                StatusLabel.Content = "";
            }
        }

        private SongNodeTreeViewItem ScanDirectory(string directory)
        {
            SongNodeTreeViewItem treeViewItem = new SongNodeTreeViewItem(this, directory, NodeType.Directory);
            string[] files = Directory.GetFiles(directory);

            foreach (string file in files)
            {
                treeViewItem.Items.Add(new SongNodeTreeViewItem(this, file, NodeType.File));
            }

            string[] subDirectories = Directory.GetDirectories(directory);
            foreach (string subDirectory in subDirectories)
            {
                SongNodeTreeViewItem subTreeViewItem = ScanDirectory(subDirectory);
                if (subTreeViewItem.IsExpanded)
                {
                    treeViewItem.IsExpanded = true;
                }
                treeViewItem.Items.Add(subTreeViewItem);
            }

            treeViewItem.Expanded += SongNodeTreeViewItem_Expanded;

            if (treeViewItem.Identifier == selectedNode)
            {
                treeViewItem.IsExpanded = true;
            }

            return treeViewItem;
        }

        private void SongNodeTreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            selectedNode = ((SongNodeTreeViewItem)e.OriginalSource).Identifier;
        }

        private int page;
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData(false);
        }

        private void LoadData(bool paginate)
        {
            APIComboBoxItem selectedItem = (APIComboBoxItem)APIComboBox.SelectedItem;
            if (paginate)
            {
                page++;
            }
            else
            {
                page = 1;
            }

            if (selectedItem == null)
            {
                MessageBox.Show("No provider selected.");
                return;
            }

            SearchButton.IsEnabled = false;
            LoadMoreButton.IsEnabled = false;
            string searchQuery = SearchQueryTextBox.Text;
            if (paginate)
            {
                LoadMoreButton.Content = "Loading...";
            }
            else
            {
                SearchButton.Content = "Searching...";
            }
            Task.Run(() =>
            {
                try
                {
                    if (selectedItem.APIType == APIType.Chorus)
                    {
                        var songs = ChorusAPI.Search(page, searchQuery).data;
                        Dispatcher.Invoke(() =>
                        {
                            if (!paginate)
                            {
                                RemoteSongList.Items.Clear();
                            }
                            foreach (Song song in songs)
                            {
                                RemoteSongList.Items.Add(new ChorusSongListBoxItem(this, song));
                            }
                        });
                    }
                    else
                    {
                        var songs = RhythmVerseAPI.Search(page, searchQuery).Data.Songs;
                        Dispatcher.Invoke(() =>
                        {
                            if (!paginate)
                            {
                                RemoteSongList.Items.Clear();
                            }
                            foreach (var song in songs)
                            {
                                RemoteSongList.Items.Add(new RhythmVerseSongListBoxItem(this, song));
                            }
                        });
                    }
                    Dispatcher.Invoke(() =>
                    {
                        LoadMoreButton.Visibility = Visibility.Visible;
                        LoadMoreButton.IsEnabled = true;
                    });
                }
                catch (Exception exception)
                {
                    page = Math.Max(page - 1, 1);
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(exception.Message);
                        LoadMoreButton.IsEnabled = false;
                    });
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        SearchButton.IsEnabled = true;
                        SearchButton.Content = "Search";
                        LoadMoreButton.Content = "Load more";
                    });
                }
            });
        }

        private void SearchQueryTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchButton_Click(sender, e);
            }
        }

        public static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return source as TreeViewItem;
        }

        private void SongsDirectoryTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Task.Run(() => Dispatcher.Invoke(() => ScanSongs()));
            }
        }

        private void ShowInExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(SongsDirectory.Text);
        }

        private void OpenGameButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(GamePathTextBox.Text);
        }

        private void GamePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Default.GamePath = GamePathTextBox.Text;
            Settings.Default.Save();
        }

        private void APIComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            APIComboBoxItem selectedItem = (APIComboBoxItem)APIComboBox.SelectedItem;

            Settings.Default.SelectedAPI = selectedItem.Content.ToString();
            Settings.Default.Save();
        }

        private void SearchQueryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData(true);
        }
    }
}
