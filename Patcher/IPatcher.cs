using System;
using System.Threading.Tasks;

namespace Tunetoon.Patcher
{
    public interface IPatcher
    {
        bool HasFailed();
        void GetPatchManifest();
        void CheckGameFiles(Progress<PatchProgress> progress);
        Task DownloadGameFiles(Progress<PatchProgress> progress);
        void PatchGameFiles(Progress<PatchProgress> progress);
    }
}
