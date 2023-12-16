using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tunetoon.Patcher
{
    public class ClashPatcher : PatcherBase
    {
        private List<ClashFile> patchManifest;
        private List<ClashFile> filesNeeded = new List<ClashFile>();

        public ClashPatcher(Config config)
        {
            this.config = config;
        }

        public override void Initialize(string directory)
        {
            filesNeeded.Clear();

            base.Initialize(directory);
        }

        public override string GetGameDirectory()
        {
            return config.ClashPath;
        }

        public override void GetPatchManifest()
        {
            patchManifest = new List<ClashFile>();

            try
            {
                // Windows specific
                string json = httpClient.GetStringAsync(config.ClashUrls.MainManifest).Result;
                var manifest = JsonSerializer.Deserialize<ClashManifest>(json);
                foreach (ClashFile file in manifest.Files)
                {
                    file.PlatformSpecific = true;
                    patchManifest.Add(file);
                }

                // Resources
                json = httpClient.GetStringAsync(config.ClashUrls.ResourceManifest).Result;
                manifest = JsonSerializer.Deserialize<ClashManifest>(json);
                patchManifest.AddRange(manifest.Files);
            }
            catch
            {
                status = PatcherStatus.PatchManifestFailure;
            }
        }

        public override void CheckGameFiles(Progress<PatchProgress> progress)
        {
            base.CheckGameFiles(progress);

            string gamePath = config.ClashPath;
            patchProgress.NewWork(progress, patchManifest.Count);

            Parallel.ForEach(patchManifest, file =>
            {
                string localFilePath = gamePath + file.FilePath;

                // meh
                patchProgress.FileProcessed(progress);

                if (File.Exists(localFilePath))
                {
                    string fileHash = GamePatchUtils.GetSha1FileHash(localFilePath);

                    if (fileHash == file.Sha1)
                    {
                        return;
                    }
                }
                lock (filesNeeded)
                {
                    filesNeeded.Add(file);
                }
            });
        }

        private async Task AcquireFileAsync(ClashFile file)
        {
            string platform = file.PlatformSpecific ? "windows" : "resources";
            string fileToDownload = GamePatchUtils.GetSha1HashString(file.FilePath + platform);

            string url = config.ClashUrls.PatchServer + fileToDownload;
            string downloadedFilePath = config.ClashPath + fileToDownload;
            string extractedFilePath = downloadedFilePath + "~";

            int downloadStatus = await fileDownloader.DownloadAsync(url, downloadedFilePath);

            if (downloadStatus != 0)
            {
                File.Delete(downloadedFilePath);
                status = PatcherStatus.FileDownloadFailure;
                cts.Cancel();
                return;
            }

            if (!GamePatchUtils.FileIsCorrect(downloadedFilePath, file.CompressedSha1))
            {
                status = PatcherStatus.FileDownloadFailure;
                cts.Cancel();
                return;
            }

            if (GamePatchUtils.Extract(downloadedFilePath, extractedFilePath, "gzip") != 0)
            {
                status = PatcherStatus.FileDownloadFailure;
                cts.Cancel();
                return;
            }

            if (GamePatchUtils.FileIsCorrect(extractedFilePath, file.Sha1))
            {
                // Cheat a bit to avoid SHA1 hashes again
                file.FileName = fileToDownload + "~";
                return;
            }

            status = PatcherStatus.FileDownloadFailure;
            cts.Cancel();
        }


        public override async Task DownloadGameFiles(Progress<PatchProgress> progress)
        {
            await base.DownloadGameFiles(progress);

            var tasks = new List<Task>();

            ct = cts.Token;
            patchProgress.NewWork(progress, filesNeeded.Count);

            foreach (var file in filesNeeded)
            {
                tasks.Add(Task.Run(() => AcquireFileAsync(file), ct)
                    .ContinueWith(_ => patchProgress.FileProcessed(progress, ct)));
            }

            await Task.WhenAll(tasks);
        }

        public override void PatchGameFiles(Progress<PatchProgress> progress)
        {
            base.PatchGameFiles(progress);

            string gamePath = config.ClashPath;
            patchProgress.NewWork(progress, filesNeeded.Count);

            foreach (var file in filesNeeded)
            {
                string localFilePath = gamePath + file.FilePath;
                string localDirPath = Path.GetDirectoryName(localFilePath);

                if (Directory.Exists(localDirPath))
                {
                    if (File.Exists(localFilePath))
                    {
                        File.Delete(localFilePath);
                    }
                }
                else
                {
                    Directory.CreateDirectory(localDirPath);
                }
                File.Move(gamePath + file.FileName, gamePath + file.FilePath);
                patchProgress.FileProcessed(progress);
            } 
        }
    }
}
