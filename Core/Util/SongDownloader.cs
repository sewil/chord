using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Chord.Core.Util
{
    public static class SongDownloader
    {
        public static void DownloadSong(string songsDirectory, string link, string artist, string name, string charter, Action<string> status)
        {
            FileInfo zip = FileDownloader.DownloadFile(link);
            MoveZipToSongsFolder(songsDirectory, artist, name, charter, zip.FullName, status);
        }

        public static void MoveZipToSongsFolder(string songsDirectory, string artist, string name, string charter, string zip, Action<string> status)
        {
            name = FileCleanString(name);
            artist = FileCleanString(artist);
            charter = FileCleanString(charter);
            UnarchiveZip(zip, Path.Combine(songsDirectory, artist + " - " + name + (!string.IsNullOrWhiteSpace(charter) ? " (" + charter + ")" : "")), status);
            RemoveZip(zip, status);
        }

        private static string FileCleanString(string value)
        {
            if (value == null) return null;
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(value, invalidRegStr, "_");
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
                WinrarExtract(zip, toDirectory, status, true);
            }
        }
        private static void WinrarExtract(string zip, string toDirectory, Action<string> status, bool unrar)
        {
            try
            {
                var fileInfo = new FileInfo(zip);
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = unrar ? "unrar.exe" : "winrar.exe",
                        Arguments = "x \"" + zip + "\" \"" + toDirectory + "\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                if (output.Contains("No files to extract"))
                {
                    if (unrar)
                    {
                        status.Invoke("unrar.exe extraction failed. Trying winrar.exe...");
                        WinrarExtract(zip, toDirectory, status, false);
                    }
                    else
                    {
                        throw new Win32Exception(string.Format("Error extracting archive with extension \"{0}\".", fileInfo.Extension));
                    }
                }
            }
            catch (Win32Exception) // unrar.exe or winrar.exe not found
            {
                throw new Win32Exception("Error extracting archive. Make sure WinRAR is installed and added to PATH.");
            }
        }
    }
}
