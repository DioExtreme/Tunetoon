using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tunetoon.Patcher
{
    class FileDownloader
    {
        private SemaphoreSlim semaphore = new SemaphoreSlim(4);
        private HttpClient httpClient = Program.httpClient;

        public async Task<int> GetFileAsync(string url, string filePath)
        {
            try
            {
                using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (FileStream fs = File.OpenWrite(filePath))
                {
                    await stream.CopyToAsync(fs);
                }
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        public async Task<int> DownloadAsync(string url, string filePath)
        {
            await semaphore.WaitAsync();
            int downloaded = await GetFileAsync(url, filePath);

            semaphore.Release();
            return downloaded;
        }
    }
}
