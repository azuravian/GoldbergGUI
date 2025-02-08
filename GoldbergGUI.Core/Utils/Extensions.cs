using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GoldbergGUI.Core.Utils
{
    public static class Extensions
    {
        public static async Task GetFileAsync(this HttpClient client, string requestUri, Stream destination,
            CancellationToken cancelToken = default)
        {
            var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancelToken)
                .ConfigureAwait(false);
            await using var download = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await download.CopyToAsync(destination, cancelToken).ConfigureAwait(false);
            if (destination.CanSeek) destination.Position = 0;
        }
    }
}