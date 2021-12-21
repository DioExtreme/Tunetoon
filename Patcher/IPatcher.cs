using System;
using System.Threading.Tasks;

namespace Tunetoon.Patcher
{
    public interface IPatcher
    {
        void Initialize(string directory);
        bool HasFailed();
        string GetGameDirectory();
        void GetPatchManifest();
        void CheckGameFiles(Progress<PatchProgress> progress);
        Task DownloadGameFiles(Progress<PatchProgress> progress);
        void PatchGameFiles(Progress<PatchProgress> progress);
    }
}
