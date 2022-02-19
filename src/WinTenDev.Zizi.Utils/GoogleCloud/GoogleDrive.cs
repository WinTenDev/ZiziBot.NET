﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using HeyRed.Mime;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils.IO;
using File=Google.Apis.Drive.v3.Data.File;

namespace WinTenDev.Zizi.Utils.GoogleCloud;

public static class GoogleDrive
{
    private static readonly string[] Scopes = { DriveService.Scope.Drive };
    private const string AppName = "Zizi Uploader";
    private static DriveService Service { get; set; }

    [Obsolete("Please Initialize Drive service via Dependency Injection")]
    public static void Auth()
    {
        Log.Information("Initializing GoogleDrive client.");
        var googleCred = BotSettings.GoogleDriveAuth.SanitizeSlash();

        Log.Debug("GoogleDrive cred {0}", googleCred);
        using var stream = new FileStream(googleCred, FileMode.Open, FileAccess.Read);

        Log.Debug("Authorizing client..");

        var credPath = Path.Combine("Storage", "Common", "gdrive-auth-token-store").SanitizeSlash()
            .EnsureDirectory();

        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync
        (
            GoogleClientSecrets.Load(stream).Secrets,
            Scopes,
            "user",
            CancellationToken.None,
            new FileDataStore(credPath, true)
        ).Result;

        Log.Debug("Credential saved to {0}", credPath);

        Log.Debug("Initializing Drive service");

        var service = new DriveService
        (
            new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName
            }
        );

        Log.Information("Creating GoogleDrive client finish.");
        Service = service;
    }

    public static File CreateFolder(
        string name,
        string id = "root"
    )
    {
        var fileMetadata = new File()
        {
            Name = name,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new[] { id }
        };

        var request = Service.Files.Create(fileMetadata);
        request.Fields = "id, name, parents, createdTime, modifiedTime, mimeType";

        return request.Execute();
    }


    public static void UploadFile(
        string filePath,
        Func<CallbackAnswer, Task> answer
    )
    {
        Log.Information("Starting upload {0} to Drive", filePath);

        if (!filePath.IsFileExist())
        {
            Log.Information("File {0} is not exist, file skipped.", filePath);
            var notExist = "Sesuatu telah terjadi. File tidak tersedia.";

            answer
            (
                new CallbackAnswer()
                {
                    CallbackAnswerText = notExist,
                    CallbackAnswerModes = new List<CallbackAnswerMode>()
                    {
                        CallbackAnswerMode.EditMessage
                    }
                }
            ).Wait();

            return;
        }

        var fileName = Path.GetFileName(filePath);
        var fileSize = filePath.FileSize().ToDouble().SizeFormat();
        var mimeType = MimeTypesMap.GetMimeType(fileName);
        var baseUpload = "https://drive.jovanzers.workers.dev/2:/";
        var uploadedUrl = baseUpload + fileName;
        var swUpdater = new Stopwatch();
        swUpdater.Start();

        Log.Debug("Building body");

        var bodyFile = new File()
        {
            Name = Path.GetFileName(filePath),
            Description = "Uploaded by ZiZi Bot",
            MimeType = MimeTypesMap.GetMimeType(fileName),
            Parents = new List<string>()
            {
                "1ZGcchFh9M6VZN9a9-8u5d_VzsQOMFXUq"
            }
        };

        Log.Debug("Load file into memory");
        // var byteArray = System.IO.File.ReadAllBytes(filePath);
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        Log.Debug("Creating request..");
        var request = Service.Files.Create(bodyFile, stream, mimeType);
        // request.ChunkSize = 100 * 1024 * 1024;
        request.SupportsTeamDrives = true;

        var updateCommon = "Uploading file to Drive" +
                           $"\nFile: {fileName}";

        request.ProgressChanged += async delegate(IUploadProgress progress) {
            if (swUpdater.Elapsed.Seconds < 5) return;

            swUpdater.Reset();
            var sendProgress = progress.BytesSent.ToDouble().SizeFormat();

            var uploadProgress = $"{updateCommon}" +
                                 $"\nSent: {sendProgress}" +
                                 $"\nSize: {fileSize}";

            await answer
            (
                new CallbackAnswer()
                {
                    CallbackAnswerText = uploadProgress,
                    CallbackAnswerModes = new List<CallbackAnswerMode>()
                    {
                        CallbackAnswerMode.EditMessage
                    }
                }
            );

            Log.Debug("Progress {0}", sendProgress);
            swUpdater.Start();
        };

        request.ResponseReceived += async delegate(File file) {
            var uploadResult = "File uploaded to Drive" +
                               $"\nUrl: {uploadedUrl}" +
                               $"\nSize: {fileSize}" +
                               $"\n\nCC: Fulan";

            await answer
            (
                new CallbackAnswer()
                {
                    CallbackAnswerText = uploadResult,
                    CallbackAnswerModes = new List<CallbackAnswerMode>()
                    {
                        CallbackAnswerMode.EditMessage
                    }
                }
            );

            Log.Debug("Response: {0}", file.Id);

            filePath.DeleteFile();
        };

        Log.Debug("Do upload!");
        request.Upload();
        stream.Dispose();
    }
}