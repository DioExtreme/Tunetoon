using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tunetoon.Patcher
{
    public class ClashPatcher : IPatcher
    {
        private HttpClient httpClient = Program.HttpClient;
        private FileDownloader fileDownloader = new FileDownloader();

        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken ct;

        private List<ClashFile> patchManifest;
        private List<ClashFile> filesNeeded = new List<ClashFile>();

        private Config config;

        private const string WinManifest = "https://corporateclash.net/api/v1/launcher/manifest/v2/windows_production.js";
        private const string ResourceManifest = "https://corporateclash.net/api/v1/launcher/manifest/v2/resources_production.js";
        private const string UpdateUrl = "https://aws1.corporateclash.net/productionv2/";

        private PatchProgress patchProgress = new PatchProgress();
        private PatcherStatus status;

        public ClashPatcher(Config config)
        {
            this.config = config;
        }

        public bool HasFailed()
        {
            return status != PatcherStatus.Success;
        }

        public void GetPatchManifest()
        {
            patchManifest = new List<ClashFile>();

            try
            {
                // Windows specific
                string json = httpClient.GetStringAsync(WinManifest).Result;
                var manifest = JsonConvert.DeserializeObject<ClashManifest>(json);
                patchManifest.AddRange(manifest.Files);

                // Resources
                json = httpClient.GetStringAsync(ResourceManifest).Result;
                manifest = JsonConvert.DeserializeObject<ClashManifest>(json);
                patchManifest.AddRange(manifest.Files);
            }
            catch
            {
                status = PatcherStatus.PatchManifestFailure;
            }
        }

        public void CheckGameFiles(Progress<PatchProgress> progress)
        {
            string gamePath = config.ClashPath;
            patchProgress.NewWork(progress, patchManifest.Count);

            foreach (var file in patchManifest)
            { 
                string localFilePath = gamePath + file.FilePath;

                // meh
                patchProgress.FileProcessed(progress);

                if (File.Exists(localFilePath))
                {
                    string fileHash = GamePatchUtils.GetSha1FileHash(localFilePath);

                    if (fileHash == file.Sha1)
                    {
                        continue;
                    }
                }
                filesNeeded.Add(file);
            }
        }

        private async Task AcquireFileAsync(ClashFile file)
        {
            string fileToDownload = GamePatchUtils.GetSha1HashString(file.FilePath);

            string url = UpdateUrl + fileToDownload;
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


        public async Task DownloadGameFiles(Progress<PatchProgress> progress)
        {
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

        public void PatchGameFiles(Progress<PatchProgress> progress)
        {
            string gamePath = config.ClashPath;
            patchProgress.NewWork(progress, filesNeeded.Count);

            foreach (var file in filesNeeded)
            {
                string localFilePath = gamePath + file.FilePath; 
                File.Delete(localFilePath);
                File.Move(gamePath + file.FileName, gamePath + file.FilePath);
                patchProgress.FileProcessed(progress);
            } 
        }
    }
}
