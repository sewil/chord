using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;

namespace Chord.Core.Util
{
    public static class GoogleDriveUtil
    {
        private static DriveService Service { get; set; }
        public static DriveService GetService()
        {
            if (Service != null) return Service;
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new string[] { DriveService.Scope.DriveReadonly },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)
                ).Result;
            }

            Service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "chord",
            });
            return Service;
        }
        public static IList<Google.Apis.Drive.v3.Data.File> ListFiles(DriveService service, string folderId)
        {
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.Q = "'" + folderId + "' in parents";

            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
            return files;
        }
        public static Google.Apis.Drive.v3.Data.File GetFile(DriveService service, string fileId, out FilesResource.GetRequest getRequest)
        {
            getRequest = service.Files.Get(fileId);
            var file = getRequest.Execute();
            return file;
        }
        public static void DownloadFile(Google.Apis.Drive.v3.DriveService service, string fileId, string saveTo)
        {
            var getRequest = service.Files.Get(fileId);
            DownloadFile(getRequest, saveTo);
        }
        public static void DownloadFile(FilesResource.GetRequest getRequest, string saveTo)
        {
            var stream = new System.IO.MemoryStream();

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            getRequest.MediaDownloader.ProgressChanged += (Google.Apis.Download.IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case Google.Apis.Download.DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case Google.Apis.Download.DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            SaveStream(stream, saveTo);
                            break;
                        }
                    case Google.Apis.Download.DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                }
            };
            getRequest.Download(stream);
        }
        public static void SaveStream(System.IO.MemoryStream stream, string saveTo)
        {
            using (System.IO.FileStream file = new System.IO.FileStream(saveTo, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                stream.WriteTo(file);
            }
        }
    }
}
