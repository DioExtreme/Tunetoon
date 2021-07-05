using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tunetoon.Patcher
{
    public class RewrittenPatcher : IPatcher
    {
        private HttpClient httpClient = Program.HttpClient;
        private FileDownloader fileDownloader = new FileDownloader();

        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken ct;

        private Config config;

        private Dictionary<string, RewrittenFile> patchManifest;
        private Dictionary<string, RewrittenPatch> filesToUpdate = new Dictionary<string, RewrittenPatch>();
        private Dictionary<string, RewrittenFile> filesNeeded = new Dictionary<string, RewrittenFile>();

        private List<string> mirrors;

        private const string UpdateUrl = "https://cdn.toontownrewritten.com";

        private PatchProgress patchProgress = new PatchProgress();
        private PatcherStatus status;

        public RewrittenPatcher(Config config)
        {
            this.config = config;
        }

        public bool HasFailed()
        {
            return status != PatcherStatus.Success;
        }

        public void GetPatchManifest()
        {
            try
            {
                string json = httpClient.GetStringAsync(UpdateUrl + "/content/patchmanifest.txt").Result;
                patchManifest = JsonConvert.DeserializeObject<Dictionary<string, RewrittenFile>>(json);
            }
            catch
            {
                status = PatcherStatus.PatchManifestFailure;
            }
        }

        private void GetMirrors()
        {
            try
            {
                string json = httpClient.GetStringAsync(UpdateUrl + "/mirrors.txt").Result;
                mirrors = JsonConvert.DeserializeObject<List<string>>(json);
            }
            catch
            {
                status = PatcherStatus.MirrorsFailure;
            }
        }

        public void CheckGameFiles(Progress<PatchProgress> progress)
        {
            patchProgress.NewWork(progress, patchManifest.Count);
            string gamePath = config.RewrittenPath;

            foreach (var file in patchManifest)
            {
                RewrittenFile fileObject = file.Value;

                //meh
                patchProgress.FileProcessed(progress);

                if (!fileObject.Only.Contains("win64"))
                {
                    continue;
                }

                bool hasPatchAvailable = false;
                string localFile = gamePath + file.Key;

                if (File.Exists(localFile))
                {
                    string fileHash = GamePatchUtils.GetSha1FileHash(localFile);

                    if (fileHash == fileObject.Hash)
                    {
                        continue;
                    }

                    foreach (var patch in fileObject.Patches)
                    {
                        if (fileHash == patch.Key)
                        {
                            hasPatchAvailable = true;
                            patch.Value.FinalFileHash = fileObject.Hash;
                            filesToUpdate.Add(file.Key, patch.Value);
                            break;
                        }
                    }
                }

                if (!hasPatchAvailable)
                {
                    filesNeeded.Add(file.Key, file.Value);
                }
            }
        }

        private async Task AcquireFileAsync(string fileToDownload, string compHash)
        {
            string rewrittenPath = config.RewrittenPath;
            string downloadedFilePath = rewrittenPath + fileToDownload;
            string extractedFilePath = downloadedFilePath + ".extracted";

            foreach (string mirror in mirrors)
            {
                string url = mirror + fileToDownload;
                int downloadStatus = await fileDownloader.DownloadAsync(url, downloadedFilePath);

                if (downloadStatus != 0)
                {
                    File.Delete(downloadedFilePath);
                    continue;
                }

                // Patch hashes are compared to the downloaded file
                bool patch = GamePatchUtils.FileIsCorrect(downloadedFilePath, compHash);

                if (GamePatchUtils.Extract(downloadedFilePath, extractedFilePath, "bzip2") != 0)
                {
                    continue;
                }

                // Full file hashes are compared to the extracted file
                if (patch || GamePatchUtils.FileIsCorrect(extractedFilePath, compHash))
                {
                    return;
                }

                File.Delete(extractedFilePath);
            }

            status = PatcherStatus.FileDownloadFailure;
            cts.Cancel();
        }

        public async Task DownloadGameFiles(Progress<PatchProgress> progress)
        {
            GetMirrors();

            if (status == PatcherStatus.MirrorsFailure)
            {
                return;
            }

            var tasks = new List<Task>();

            ct = cts.Token;
            patchProgress.NewWork(progress, filesToUpdate.Count + filesNeeded.Count);

            foreach (var file in filesToUpdate)
            {
                RewrittenPatch patchInfo = file.Value;
                tasks.Add(Task.Run(() => AcquireFileAsync(patchInfo.Filename, patchInfo.CompPatchHash), ct)
                    .ContinueWith(_ => patchProgress.FileProcessed(progress, ct)));
            }

            foreach (var file in filesNeeded)
            {
                RewrittenFile fileInfo = file.Value;
                tasks.Add(Task.Run(() => AcquireFileAsync(fileInfo.Dl, fileInfo.Hash), ct)
                    .ContinueWith(_ => patchProgress.FileProcessed(progress, ct)));
            }

            await Task.WhenAll(tasks);
        }

        public void PatchGameFiles(Progress<PatchProgress> progress)
        {
            string rewrittenPath = config.RewrittenPath;

            patchProgress.NewWork(progress, filesToUpdate.Count + filesNeeded.Count);

            foreach (var file in filesToUpdate)
            {
                string fileName = file.Key;
                RewrittenPatch patchInfo = file.Value;

                string localFilePath = rewrittenPath + fileName;
                string extractedFilePath = rewrittenPath + patchInfo.Filename + ".extracted";

                GamePatchUtils.Patch(extractedFilePath, localFilePath);
                string patchedFilePath = localFilePath + ".tmp";

                if (GamePatchUtils.FileIsCorrect(patchedFilePath, patchInfo.FinalFileHash))
                {
                    File.Delete(localFilePath);
                    File.Move(patchedFilePath, localFilePath);
                    patchProgress.FileProcessed(progress);
                }
                else
                {
                    File.Delete(patchedFilePath);
                    status = PatcherStatus.PatchFailure;
                }
                File.Delete(extractedFilePath);
            }

            foreach (var file in filesNeeded)
            { 
                string filename = file.Key;
                RewrittenFile fileInfo = file.Value;

                string localFilePath = rewrittenPath + filename;
                string extractedFilePath = rewrittenPath + fileInfo.Dl + ".extracted";

                File.Delete(localFilePath);
                File.Move(extractedFilePath, localFilePath);

                patchProgress.FileProcessed(progress);
            }
        }
    }
}
