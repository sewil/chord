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
            GamePathTextBox.Text = Settings.Default.GamePath;
            var selectedAPI = string.IsNullOrWhiteSpace(Settings.Default.SelectedAPI) ? "Chorus" : Settings.Default.SelectedAPI;
            APIComboBox.Items.Add(new APIComboBoxItem(APIType.Chorus, "Chorus") { IsSelected = selectedAPI == "Chorus" });
            APIComboBox.Items.Add(new APIComboBoxItem(APIType.RhythmVerse, "RhythmVerse") { IsSelected = selectedAPI == "RhythmVerse" });
            if (!string.IsNullOrWhiteSpace(Settings.Default.SongsDirectory))
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

        private APIType lastProvider;
        private string lastQuery;
        private int page;
        private string GetSearchButtonText()
        {
            return SearchQueryTextBox.Text == lastQuery ? $"Search more" : "Search";
        }
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            APIComboBoxItem selectedItem = (APIComboBoxItem)APIComboBox.SelectedItem;
            var paginate = selectedItem.APIType == lastProvider && SearchQueryTextBox.Text == lastQuery;
            if (paginate)
            {
                page++;
            }
            else
            {
                page = 1;
                lastQuery = SearchQueryTextBox.Text;
                lastProvider = selectedItem.APIType;
            }

            if (selectedItem == null)
            {
                MessageBox.Show("No provider selected.");
                return;
            }

            if (selectedItem.APIType == APIType.Chorus)
            {
                SearchButton.IsEnabled = false;
                SearchButton.Content = "Searching...";
                string searchQuery = SearchQueryTextBox.Text;
                Task.Run(() =>
                {
                    try
                    {
                        IList<Song> songs = ChorusAPI.Search(page, searchQuery).data;
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
                    catch (Exception exception)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show(exception.Message));
                    }
                    finally
                    {
                        Dispatcher.Invoke(() =>
                        {
                            SearchButton.IsEnabled = true;
                            SearchButton.Content = GetSearchButtonText();
                        });
                    }
                });
            }
            else if (selectedItem.APIType == APIType.RhythmVerse)
            {
                SearchButton.IsEnabled = false;
                SearchButton.Content = "Searching...";
                string searchQuery = SearchQueryTextBox.Text;
                Task.Run(() =>
                {
                    try
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
                    catch (Exception exception)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show(exception.Message));
                    }
                    finally
                    {
                        Dispatcher.Invoke(() =>
                        {
                            SearchButton.IsEnabled = true;
                            SearchButton.Content = GetSearchButtonText();
                        });
                    }
                });
            }
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
            Dispatcher.Invoke(() =>
            {
                SearchButton.Content = GetSearchButtonText();
            });
        }
    }
}
