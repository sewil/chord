﻿using System;
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
            FileInfo file = FileDownloader.DownloadFile(link, status);
            var fileType = FileTypeDetector.DetectFileType(file.FullName);

            name = FileCleanString(name);
            artist = FileCleanString(artist);
            charter = FileCleanString(charter);

            string outputName = artist + " - " + name + (!string.IsNullOrWhiteSpace(charter) ? " (" + charter + ")" : "");

            if (fileType == "ZIP" || fileType == "7Z" || fileType == "RAR")
            {
                MoveZipToSongsFolder(songsDirectory, outputName, file.FullName, status);
            }
            else if (fileType == "SNGPKG")
            {
                string outDir = Path.Combine(songsDirectory, outputName);
                DecodeSngFile(file.FullName, outDir, status);
            }
            else if (fileType == "CON")
            {
                var converter = new CONConverter(status);
                string outDir = Path.Combine(songsDirectory, outputName);
                converter.Convert(file.FullName, outDir);
            }
            else
            {
                File.Move(file.FullName, Path.Combine(songsDirectory, outputName + file.Extension));
            }
        }
        public static void DecodeSngFile(string fileName, string outDir, Action<string> status)
        {
            var tmpOut = Path.Combine(fileName + ".tmp");
            Directory.CreateDirectory(tmpOut);
            File.Move(fileName, Path.Combine(tmpOut, Path.GetFileName(fileName)));
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "SngCli.exe",
                    Arguments = $"decode -i \"{tmpOut}\" -o \"{tmpOut}\"",
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit();
            Directory.Move(Path.Combine(tmpOut, Path.GetFileNameWithoutExtension(fileName)), outDir);
            Directory.Delete(tmpOut, true);
            status.Invoke("Cleaning up...");
        }
        public static void MoveZipToSongsFolder(string songsDirectory, string outputName, string zip, Action<string> status)
        {
            UnarchiveZip(zip, Path.Combine(songsDirectory, outputName), status);
            RemoveFile(zip, status);
        }

        private static string FileCleanString(string value)
        {
            if (value == null) return null;
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(value, invalidRegStr, "_");
        }

        public static void RemoveFile(string file, Action<string> status)
        {
            status.Invoke("Cleaning up...");
            File.Delete(file);
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
                        FileName = unrar ? "unrar" : "winrar",
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
