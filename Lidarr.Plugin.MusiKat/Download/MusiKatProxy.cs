using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers.MusiKat;

namespace NzbDrone.Core.Download.Clients.MusiKat
{
    public interface IMusiKatProxy
    {
        List<MusiKatJob> GetJobs(MusiKatDownloadClientSettings settings);

        MusiKatDownloadResponse DownloadTrack(string trackId, MusiKatDownloadClientSettings settings);

        MusiKatAlbumDownloadResponse DownloadAlbum(string albumId, MusiKatDownloadClientSettings settings);

        void Cancel(string jobId, MusiKatDownloadClientSettings settings);

        List<string> GetLibraries(MusiKatDownloadClientSettings settings);

        MusiKatHealthResponse GetHealth(MusiKatDownloadClientSettings settings);
    }

    public class MusiKatProxy : IMusiKatProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public MusiKatProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<MusiKatJob> GetJobs(MusiKatDownloadClientSettings settings)
        {
            var request = BuildRequest(settings).Resource("/api/jobs").Build();
            var response = Execute(request);
            return Json.Deserialize<MusiKatJobListResponse>(response.Content)?.Jobs ?? new List<MusiKatJob>();
        }

        public MusiKatDownloadResponse DownloadTrack(string trackId, MusiKatDownloadClientSettings settings)
        {
            var body = DownloadBody(settings);
            body["track_id"] = trackId;

            var request = BuildRequest(settings).Resource("/api/download").Post().Build();
            request.SetContent(Json.ToJson(body));
            request.Headers.ContentType = "application/json";

            var response = Execute(request);
            return Json.Deserialize<MusiKatDownloadResponse>(response.Content) ?? new MusiKatDownloadResponse();
        }

        public MusiKatAlbumDownloadResponse DownloadAlbum(string albumId, MusiKatDownloadClientSettings settings)
        {
            var body = DownloadBody(settings);
            body["album_id"] = albumId;

            var request = BuildRequest(settings).Resource("/api/download/album").Post().Build();
            request.SetContent(Json.ToJson(body));
            request.Headers.ContentType = "application/json";

            var response = Execute(request);
            return Json.Deserialize<MusiKatAlbumDownloadResponse>(response.Content) ?? new MusiKatAlbumDownloadResponse();
        }

        public void Cancel(string jobId, MusiKatDownloadClientSettings settings)
        {
            var request = BuildRequest(settings)
                .Resource($"/api/download/cancel/{Uri.EscapeDataString(jobId)}")
                .Post()
                .Build();

            Execute(request);
        }

        public List<string> GetLibraries(MusiKatDownloadClientSettings settings)
        {
            var request = BuildRequest(settings).Resource("/api/navidrome/libraries").Build();
            var response = Execute(request);
            return Json.Deserialize<MusiKatLibrariesResponse>(response.Content)?.Libraries?.ConvertAll(l => l.Path)
                ?? new List<string>();
        }

        public MusiKatHealthResponse GetHealth(MusiKatDownloadClientSettings settings)
        {
            var request = BuildRequest(settings).Resource("/api/health").Build();
            var response = Execute(request);
            return Json.Deserialize<MusiKatHealthResponse>(response.Content) ?? new MusiKatHealthResponse();
        }

        private static Dictionary<string, object> DownloadBody(MusiKatDownloadClientSettings settings)
        {
            var body = new Dictionary<string, object>
            {
                { "location", "navidrome" },
                { "navidrome_library", settings.LibraryPath },
                { "provider", MusiKatFormats.ProviderValue(settings.Provider) },
                { "format", MusiKatFormats.FromFormat(settings.Format).ApiValue },
                { "max_retries", settings.MaxRetries },
                { "force", settings.ForceRedownload }
            };

            var quality = MusiKatFormats.QualityValue(settings.Quality);
            if (quality != null)
            {
                body["quality"] = quality;
            }

            return body;
        }

        private HttpRequestBuilder BuildRequest(MusiKatDownloadClientSettings settings)
        {
            var builder = new HttpRequestBuilder(settings.BaseUrl.TrimEnd('/'))
            {
                LogResponseContent = true
            };

            if (settings.ApiKey.IsNotNullOrWhiteSpace())
            {
                builder.SetHeader("X-Api-Key", settings.ApiKey);
            }

            return builder;
        }

        private HttpResponse Execute(HttpRequest request)
        {
            HttpResponse response;

            try
            {
                response = _httpClient.Execute(request);
            }
            catch (HttpException ex)
            {
                throw new DownloadClientException($"MusiKat request failed: {ex.Message}", ex);
            }

            if (response.HasHttpError)
            {
                throw new DownloadClientException(GetErrorMessage(response));
            }

            return response;
        }

        private static string GetErrorMessage(HttpResponse response)
        {
            try
            {
                var parsed = Json.Deserialize<Dictionary<string, object>>(response.Content);
                if (parsed != null && parsed.TryGetValue("detail", out var detail) && detail != null)
                {
                    return detail.ToString();
                }
            }
            catch
            {
                // The body is not JSON. Use the generic message below.
            }

            return $"MusiKat API returned HTTP {response.StatusCode}: {response.Content.Truncate(200)}";
        }
    }
}
