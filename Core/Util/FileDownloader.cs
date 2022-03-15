﻿using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace Chord.Core.Util
{
    public static class FileDownloader
    {
        private const string GOOGLE_DRIVE_DOMAIN = "drive.google.com";
        private const string GOOGLE_DRIVE_DOMAIN2 = "https://drive.google.com";

        // Normal example: FileDownloader.DownloadFileFromURLToPath( "http://example.com/file/download/link", @"C:\file.txt" );
        // Drive example: FileDownloader.DownloadFileFromURLToPath( "http://drive.google.com/file/d/FILEID/view?usp=sharing", @"C:\file.txt" );
        public static FileInfo DownloadFileFromURLToPath(string url, string path)
        {
            if (url.StartsWith(GOOGLE_DRIVE_DOMAIN) || url.StartsWith(GOOGLE_DRIVE_DOMAIN2))
                return DownloadGoogleDriveFileFromURLToPath(url, path);
            else
                return DownloadFileFromURLToPath(url, path, null);
        }

        private static FileInfo DownloadFileFromURLToPath(string url, string path, WebClient webClient)
        {
            if (webClient == null)
            {
                using (webClient = new WebClient())
                {
                    webClient.DownloadFile(url, path);
                    return new FileInfo(path);
                }
            }
            else
            {
                webClient.DownloadFile(url, path);
                return new FileInfo(path);
            }
        }

        private static FileInfo DownloadGoogleDriveFileFromURLToPath(string url, string path)
        {
            string folders = "drive/folders/";
            if (url.IndexOf(folders) > 0)
            {
                int index = url.IndexOf(folders) + folders.Length;
                string folderId = url.Substring(index, url.Length - index);
                DriveService service = GoogleDriveUtil.Authorize();
                var files = GoogleDriveUtil.ListFiles(service, folderId);
                string tempSongsDirectory = Path.Combine(Path.GetTempPath(), "chord-songs");
                if (!Directory.Exists(tempSongsDirectory))
                {
                    Directory.CreateDirectory(tempSongsDirectory);
                }
                foreach (var file in files)
                {
                    string fileUrl = "https://drive.google.com/uc?id=" + file.Id + "&export=download";
                    string tempLocalFile = Path.Combine(tempSongsDirectory, file.Name);
                    DownloadGoogleDriveFileFromURLToPath(fileUrl, tempLocalFile);
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                ZipFile.CreateFromDirectory(tempSongsDirectory, path);
                Directory.Delete(tempSongsDirectory, true);
                return new FileInfo(path);
            }
            else
            {
                url = GetGoogleDriveDownloadLinkFromUrl(url);
                using (CookieAwareWebClient webClient = new CookieAwareWebClient())
                {
                    FileInfo downloadedFile;

                    for (int i = 0; i < 2; i++)
                    {
                        downloadedFile = DownloadFileFromURLToPath(url, path, webClient);

                        if (downloadedFile.Length > 60000)
                        {
                            return downloadedFile;
                        }

                        string content;
                        using (var reader = downloadedFile.OpenText())
                        {
                            char[] header = new char[20];
                            int readCount = reader.ReadBlock(header, 0, 20);
                            if (readCount < 20 || !(new string(header).Contains("<!DOCTYPE html>")))
                            {
                                return downloadedFile;
                            }

                            content = reader.ReadToEnd();
                        }

                        int linkIndex = content.LastIndexOf("href=\"/uc?");
                        if (linkIndex < 0)
                        {
                            return downloadedFile;
                        }

                        linkIndex += 6;
                        int linkEnd = content.IndexOf('"', linkIndex);
                        if (linkEnd < 0)
                        {
                            return downloadedFile;
                        }

                        url = "https://drive.google.com" + content.Substring(linkIndex, linkEnd - linkIndex).Replace("&amp;", "&");
                    }

                    downloadedFile = DownloadFileFromURLToPath(url, path, webClient);

                    return downloadedFile;
                }
            }
        }

        // Handles 4 kinds of links (they can be preceeded by https://):
        // - drive.google.com/open?id=FILEID
        // - drive.google.com/file/d/FILEID/view?usp=sharing
        // - drive.google.com/uc?id=FILEID&export=download
        public static string GetGoogleDriveDownloadLinkFromUrl(string url)
        {
            int index = url.IndexOf("id=");
            int closingIndex;
            if (index > 0)
            {
                index += 3;
                closingIndex = url.IndexOf('&', index);
                if (closingIndex < 0)
                    closingIndex = url.Length;
            }
            else
            {
                index = url.IndexOf("file/d/");
                if (index < 0) // url is not in any of the supported forms
                    throw new ArgumentException("Invalid URL specified.");

                index += 7;

                closingIndex = url.IndexOf('/', index);
                if (closingIndex < 0)
                {
                    closingIndex = url.IndexOf('?', index);
                    if (closingIndex < 0)
                        closingIndex = url.Length;
                }
            }

            return string.Format("https://drive.google.com/uc?id={0}&export=download", url.Substring(index, closingIndex - index));
        }
    }

    // Web client used for Google Drive
    public class CookieAwareWebClient : WebClient
    {
        private class CookieContainer
        {
            Dictionary<string, string> _cookies;

            public string this[Uri url]
            {
                get
                {
                    string cookie;
                    if (_cookies.TryGetValue(url.Host, out cookie))
                        return cookie;

                    return null;
                }
                set
                {
                    _cookies[url.Host] = value;
                }
            }

            public CookieContainer()
            {
                _cookies = new Dictionary<string, string>();
            }
        }

        private CookieContainer cookies;

        public CookieAwareWebClient() : base()
        {
            cookies = new CookieContainer();
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);

            if (request is HttpWebRequest)
            {
                ((HttpWebRequest)request).UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                string cookie = cookies[address];
                if (cookie != null)
                    ((HttpWebRequest)request).Headers.Set("cookie", cookie);
            }

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            WebResponse response = base.GetWebResponse(request, result);

            string[] cookies = response.Headers.GetValues("Set-Cookie");
            if (cookies != null && cookies.Length > 0)
            {
                string cookie = "";
                foreach (string c in cookies)
                    cookie += c;

                this.cookies[response.ResponseUri] = cookie;
            }

            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            try
            {
                WebResponse response = base.GetWebResponse(request);
                string[] cookies = response.Headers.GetValues("Set-Cookie");
                if (cookies != null && cookies.Length > 0)
                {
                    string cookie = "";
                    foreach (string c in cookies)
                        cookie += c;

                    this.cookies[response.ResponseUri] = cookie;
                }

                return response;
            }
            catch (WebException exception)
            {
                if (exception.Response == null || !exception.Response.ContentType.Contains("application/json")) throw;
                // Is JSON response, check for drive api error
                using (var stream = exception.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    try
                    {
                        DriveAPIError driveErr = JsonConvert.DeserializeObject<DriveAPIError>(json);
                        throw new Exception(driveErr.error.message);
                    }
                    catch (JsonSerializationException) { throw; }
                }
            }
        }
    }
}

public struct DriveAPIError
{
    public struct Error
    {
        public struct Errors
        {
            public string domain;
            public string reason;
            public string message;
            public string extendedHelp;
        }
        public Errors[] errors;
        public int code;
        public string message;
    }
    public Error error;
}
