using Chord.Core.API.Chorus;
using Chord.Core.API.Chorus.Models;
using Chord.Properties;
using Chord.Views;
using Chord.Windows;
using IniParser;
using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;
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
        static IniDataParser iniParser = new IniDataParser(new IniParserConfiguration
        {
            CaseInsensitive = true,
            AllowDuplicateKeys = true,
            SkipInvalidLines = true
        });
        FileIniDataParser fileIniParser = new FileIniDataParser(iniParser);

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
            APIComboBox.Items.Add(new APIComboBoxItem(APIType.Chorus, "Chorus") { IsSelected = true });
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

        private SongDirectoryTreeViewItem ScanDirectory(string directory)
        {
            SongDirectoryTreeViewItem treeViewItem = new SongDirectoryTreeViewItem(this, directory);
            string[] files = Directory.GetFiles(directory);

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Name == "song" && fileInfo.Extension == ".ini")
                {
                    KeyDataCollection iniData = fileIniParser.ReadFile(directory)["song"];
                    string name = iniData["artist"] + " - " + iniData["name"];
                }

                treeViewItem.Items.Add(new SongFileTreeViewItem(this, file));
            }

            string[] subDirectories = Directory.GetDirectories(directory);
            foreach (string subDirectory in subDirectories)
            {
                SongDirectoryTreeViewItem subTreeViewItem = ScanDirectory(subDirectory);
                treeViewItem.Items.Add(subTreeViewItem);
            }

            return treeViewItem;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            APIComboBoxItem selectedItem = (APIComboBoxItem)APIComboBox.SelectedItem;

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
                        IList<Song> songs = ChorusAPI.Search(0, searchQuery).songs;
                        Dispatcher.Invoke(() =>
                        {
                            RemoteSongList.Items.Clear();
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
                            SearchButton.Content = "Search";
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
    }
}
