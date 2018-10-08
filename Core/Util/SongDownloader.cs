using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Chord.Core.Util
{
    public static class SongDownloader
    {
        public static void DownloadSong(string songsDirectory, string link, string artist, string name, string charter, Action<string> status)
        {
            string zip = Path.GetTempPath() + "chord-song.zip";
            FileDownloader.DownloadFileFromURLToPath(link, zip);
            MoveZipToSongsFolder(songsDirectory, artist, name, charter, zip, status);
        }

        public static void MoveZipToSongsFolder(string songsDirectory, string artist, string name, string charter, string zip, Action<string> status)
        {
            UnarchiveZip(zip, Path.Combine(songsDirectory, artist + " - " + name + " (" + charter + ")"), status);
            RemoveZip(zip, status);
        }

        public static void RemoveZip(string zip, Action<string> status)
        {
            status.Invoke("Cleaning up...");
            File.Delete(zip);
        }

        public static void UnarchiveZip(string zip, string toDirectory, Action<string> status)
        {
            status.Invoke("Extracting...");
            if (!Directory.Exists(toDirectory))
            {
                Directory.CreateDirectory(toDirectory);
            }
            try
            {
                ZipFile.ExtractToDirectory(zip, toDirectory);
            }
            catch
            {
                status.Invoke("System extraction failed, trying WinRAR...");
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "unrar";
                    process.StartInfo.Arguments = "x \"" + zip + "\" \"" + toDirectory + "\"";
                    process.Start();
                    process.WaitForExit();
                }
                catch (Win32Exception)
                {
                    throw new Win32Exception("Error extracting archive. Make sure WinRAR is installed and added to PATH.");
                }
            }
        }
    }
}
