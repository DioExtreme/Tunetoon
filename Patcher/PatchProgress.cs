using System;
using System.Threading;

namespace Tunetoon.Patcher
{
    // Thread-safe progress reporting
    public class PatchProgress
    {
        public int CurrentFilesProcessed;
        public int TotalFilesToProcess;
        public string currentAction;

        public void NewWork(IProgress<PatchProgress> progress, int totalFiles)
        {
            CurrentFilesProcessed = 0;
            TotalFilesToProcess = totalFiles;
            progress.Report(this);
        }

        public void FileProcessed(IProgress<PatchProgress> progress)
        {
            Interlocked.Increment(ref CurrentFilesProcessed);
            progress.Report(this);
        }

        public void FileProcessed(IProgress<PatchProgress> progress, CancellationToken ct)
        {
            if (!ct.IsCancellationRequested)
            {
                Interlocked.Increment(ref CurrentFilesProcessed);
            }
            progress.Report(this);
        }
    }
}
