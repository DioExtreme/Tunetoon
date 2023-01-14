using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tunetoon.Patcher
{
    public class PatcherBase : IPatcher
    {
        protected HttpClient httpClient = Program.HttpClient;
        protected FileDownloader fileDownloader = new FileDownloader();

        protected CancellationTokenSource cts = new CancellationTokenSource();
        protected CancellationToken ct;

        protected Config config;

        protected PatchProgress patchProgress = new PatchProgress();
        protected PatcherStatus status;

        public virtual void Initialize(string directory)
        {
            if (HasFailed())
            {
                status = PatcherStatus.Success;
            }

            if (Directory.Exists(directory))
            {
                return;
            }
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch
            {
                status = PatcherStatus.DirectoryCreationFailure;
            }
        }

        public virtual string GetGameDirectory()
        {
            throw new NotImplementedException();
        }

        public virtual bool HasFailed()
        {
            return status != PatcherStatus.Success;
        }

        public virtual void GetPatchManifest()
        {
            throw new NotImplementedException();
        }

        public virtual void CheckGameFiles(Progress<PatchProgress> progress)
        {
            patchProgress.currentAction = "Checking";
        }       

        public virtual Task DownloadGameFiles(Progress<PatchProgress> progress)
        {
            patchProgress.currentAction = "Downloading";
            return Task.CompletedTask;
        }
    
        public virtual void PatchGameFiles(Progress<PatchProgress> progress)
        {
            patchProgress.currentAction = "Patching";
        }
    }
}
