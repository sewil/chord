﻿using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Chord.Core.Util
{
    public static class FileDownloader
    {
        private const string GOOGLE_DRIVE_DOMAIN = @"(https?://)?drive\.google\.com";
        private const string MEDIAFIRE_DOMAIN = @"(https?://)?(www\.)?mediafire\.com";

        public static FileInfo DownloadFile(string url, Action<string> status)
        {
            if (Regex.IsMatch(url, GOOGLE_DRIVE_DOMAIN))
                return DownloadFromDrive(url, status);
            else if (Regex.IsMatch(url, MEDIAFIRE_DOMAIN))
                return DownloadMediafire(url);
            else
                return DownloadDirect(url);
        }

        public static string GetDriveUrl(string id, bool isFolder)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (isFolder)
            {
                return string.Format("http://drive.google.com/drive/folders/{0}", id);
            }
            else
            {
                return string.Format("http://drive.google.com/file/d/{0}/view?usp=sharing", id);
            }
        }

        public static FileInfo DownloadMediafire(string url)
        {
            var htmlFile = new FileInfo(Path.Combine(Path.GetTempPath(), "chord-song.tmp"));
            var client = new MediafireSDK.DataClient();
            client.DownloadFile(url, htmlFile.Directory.FullName, htmlFile.FullName).Wait();
            var html = File.ReadAllText(htmlFile.FullName);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var downloadA = doc.GetElementbyId("downloadButton");
            var downloadLink = downloadA.GetAttributeValue("href", "");
            return DownloadDirect(downloadLink);
        }

        public static FileInfo DownloadDirect(string url)
        {
            string path = Path.Combine(Path.GetTempPath(), "chord-song.tmp");
            using (var webClient = new WebClient())
            {
                webClient.DownloadFile(url, path);
                var contentType = webClient.ResponseHeaders[HttpResponseHeader.ContentType];
                if (contentType == "text/html; charset=utf-8")
                {
                    throw new WebException("Invalid download content type.");
                }
                return new FileInfo(path);
            }
        }

        private static FileInfo DownloadFromDrive(string url, Action<string> status)
        {
            string folders = @"drive\/(u\/\d+\/)?folders\/(.+)";
            var match = Regex.Match(url, folders);
            if (match.Success)
            {
                int index = match.Groups[match.Groups.Count - 1].Index;
                string folderId = url.Substring(index, url.Length - index);
                DriveService service = GoogleDriveUtil.GetService();
                var files = GoogleDriveUtil.ListFiles(service, folderId);
                string tempSongsDirectory = Path.Combine(Path.GetTempPath(), "chord-songs");
                if (!Directory.Exists(tempSongsDirectory))
                {
                    Directory.CreateDirectory(tempSongsDirectory);
                }
                foreach (var file in files)
                {
                    string tempLocalFile = Path.Combine(tempSongsDirectory, file.Name);
                    GoogleDriveUtil.DownloadFile(service, file.Id, tempLocalFile, status);
                }
                var path = Path.Combine(Path.GetTempPath(), "chord-song.tmp");
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
                string fileId = GetDriveFileId(url);
                var service = GoogleDriveUtil.GetService();
                var file = GoogleDriveUtil.GetFile(service, fileId, out FilesResource.GetRequest getRequest);
                var path = Path.Combine(Path.GetTempPath(), "chord-song" + Path.GetExtension(file.Name));
                GoogleDriveUtil.DownloadFile(getRequest, path, status);
                return new FileInfo(path);
            }
        }

        // Handles 4 kinds of links (they can be preceeded by https://):
        // - drive.google.com/open?id=FILEID
        // - drive.google.com/file/d/FILEID/view?usp=sharing
        // - drive.google.com/uc?id=FILEID&export=download
        public static string GetDriveFileId(string url)
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

            return url.Substring(index, closingIndex - index);
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
