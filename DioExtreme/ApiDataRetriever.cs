using System.Net.Http;
using System.Text.Json;

namespace Tunetoon.DioExtreme
{
    public class ApiDataRetriever
    {
        private const string ClashUrlsApi = "https://clashmf.dioextreme.com/";
        private static HttpClient httpClient = Program.HttpClient;

        public static ClashUrls LoadClashUrls()
        {
            ClashUrls clashUrls;
            try
            {
                string json = httpClient.GetStringAsync(ClashUrlsApi).Result;
                clashUrls = JsonSerializer.Deserialize<ClashUrls>(json);
                clashUrls.Initialized = true;
                return clashUrls;
            }
            catch
            {
                // Fallback in case Cloudflare decides to block an IP
                clashUrls = new ClashUrls();
                clashUrls.Initialized = true;
                clashUrls.GameServer = "gs-prd.corporateclash.net";
                clashUrls.PatchServer = "https://aws1.corporateclash.net/productionv2/";
                clashUrls.MainManifest = "https://corporateclash.net/api/v1/launcher/manifest/v3/production/windows";
                clashUrls.ResourceManifest = "https://corporateclash.net/api/v1/launcher/manifest/v3/production/resources";
                return clashUrls;
            }
        }
    }
}
