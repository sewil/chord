using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace Chord
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: chorus search <query>");
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            }
            if (!Directory.Exists(ConfigurationManager.AppSettings["songsDirectory"]))
            {
                Console.WriteLine("Error: Configured songs directory does not exist. Set the Clone Hero songs directory path in chorus.exe.config.");
                Console.WriteLine("Exiting...");
                Environment.Exit(1);
            }
            if (args[0].ToLower() == "search")
            {
                int selectedIndex = 0;
                string query = args[1];
                int from = 0;
                var key = new ConsoleKey();
                bool rewrite = true;
                bool refetch = true;
                IList<Song> songs = new List<Song>();
                do
                {
                    if (refetch)
                    {
                        Console.WriteLine("Searching \"" + query + "\"...");
                        songs = APISearch(from, query).songs;
                    }
                    if (rewrite)
                    {
                        Console.Clear();
                        for (int i = 0; i < songs.Count; i++)
                        {
                            Song song = songs[i];
                            Console.WriteLine((i == selectedIndex ? "> " : "  ") + "\"" + song.name + "\" by " + song.artist + " in " + song.album + " (" + song.year + "), charter " + song.charter);
                        }
                        Console.WriteLine();
                        Console.WriteLine("(Esc) Exit\t(D) Download\t(P) Previous page\t(N) Next page\t\t[" + (from + 1) + "-" + (from + songs.Count) + "]");
                    }
                    rewrite = false;
                    refetch = false;
                    key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.D && songs.Count > 0)
                    {
                        Song selectedSong = songs[selectedIndex];
                        DownloadSong(selectedSong);
                        rewrite = true;
                    }
                    else if (key == ConsoleKey.N)
                    {
                        selectedIndex = 0;
                        from += 20;
                        rewrite = true;
                        refetch = true;
                    }
                    else if (key == ConsoleKey.P && from > 0)
                    {
                        from -= 20;
                        selectedIndex = 0;
                        refetch = true;
                        rewrite = true;
                    }
                    else if (key == ConsoleKey.DownArrow)
                    {
                        rewrite = true;
                        selectedIndex = mod(selectedIndex + 1, songs.Count);
                    }
                    else if (key == ConsoleKey.UpArrow)
                    {
                        rewrite = true;
                        selectedIndex = mod(selectedIndex - 1, songs.Count);
                    }
                } while (key != ConsoleKey.Escape);
            }
        }
        public static void DownloadSong(Song selectedSong)
        {
            string zip = Path.GetTempPath() + "chorus-song.zip";
            if (selectedSong.directLinks.archive != null)
            {
                Console.WriteLine("Downloading from \"" + selectedSong.directLinks.archive + "\"...");
                FileDownloader.DownloadFileFromURLToPath(selectedSong.directLinks.archive, zip);
            }
            else
            {
                Console.WriteLine("Downloading from \"" + selectedSong.link + "\"...");
                FileDownloader.DownloadFileFromURLToPath(selectedSong.link, zip);
            }
            MoveZipToSongsFolder(selectedSong, zip);
        }
        public static FileInfo DownloadFile(string url, string path)
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(url, path);
                return new FileInfo(path);
            }
        }
        public static Search APISearch(int from, string query)
        {
            query = Uri.EscapeDataString(query);
            using (var client = new WebClient())
            {
                try
                {
                    string apiURL = ConfigurationManager.AppSettings["apiURL"];
                    string responseString = client.DownloadString(apiURL + "/search/?query=" + query + "&from=" + from);
                    Search search = JsonConvert.DeserializeObject<Search>(responseString);
                    return search;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Exiting...");
                    Environment.Exit(0);
                    return null;
                }
            }
        }
        public static void MoveZipToSongsFolder(Song song, string zip)
        {
            string songsDirectory = ConfigurationManager.AppSettings["songsDirectory"];
            Console.WriteLine($"Extracting to \"{songsDirectory}\"...");
            UnarchiveZip(zip, songsDirectory + song.artist + " - " + song.name + "(" + song.charter + ")");
            Console.WriteLine("Cleaning up...");
            RemoveZip(zip);
        }
        public static void RemoveZip(string zip)
        {
            File.Delete(zip);
        }
        public static void UnarchiveZip(string zip, string toDirectory)
        {
            try
            {
                ZipFile.ExtractToDirectory(zip, toDirectory);
            }
            catch (Exception)
            {
                Process process = new Process();
                process.StartInfo.FileName = "unrar";
                process.StartInfo.Arguments = "x \"" + zip + "\" \"" + toDirectory + "\"";
                process.Start();
                process.WaitForExit();
            }
        }
        public static int mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
