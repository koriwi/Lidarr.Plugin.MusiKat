using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.MusiKat;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.MusiKat
{
    public class MusiKatDownloadClient : DownloadClientBase<MusiKatDownloadClientSettings>
    {
        private static readonly TimeSpan ForeignJobWindow = TimeSpan.FromHours(24);

        private readonly IMusiKatProxy _proxy;
        private readonly ConcurrentDictionary<string, byte> _rememberedIds;

        public MusiKatDownloadClient(IMusiKatProxy proxy,
            IConfigService configService,
            IDiskProvider diskProvider,
            IRemotePathMappingService remotePathMappingService,
            ILocalizationService localizationService,
            Logger logger)
            : base(configService, diskProvider, remotePathMappingService, localizationService, logger)
        {
            _proxy = proxy;
            _rememberedIds = new ConcurrentDictionary<string, byte>();
        }

        public override string Name => "MusiKat";

        public override string Protocol => nameof(MusiKatDownloadProtocol);

        public override Task<string> Download(RemoteAlbum remoteAlbum, IIndexer indexer)
        {
            var release = remoteAlbum.Release;
            var resource = MusiKatDownloadUrl.Parse(release.DownloadUrl);

            if (resource == null)
            {
                throw new DownloadClientException($"MusiKat could not read the download URL: {release.DownloadUrl}");
            }

            try
            {
                if (resource.IsAlbum)
                {
                    var result = _proxy.DownloadAlbum(resource.Id, Settings);
                    Remember($"album:{result.AlbumId}");
                    if (result.QueuedTrackIds != null)
                    {
                        foreach (var trackId in result.QueuedTrackIds)
                        {
                            Remember(trackId);
                        }
                    }

                    return Task.FromResult($"album:{result.AlbumId}");
                }

                _proxy.DownloadTrack(resource.Id, Settings);
                Remember(resource.Id);

                return Task.FromResult(resource.Id);
            }
            catch (DownloadClientException ex)
            {
                if (ex.Message.Contains("already in progress"))
                {
                    // The download already exists in MusiKat. Return its id so
                    // Lidarr tracks it.
                    var existingId = resource.IsAlbum ? $"album:{resource.Id}" : resource.Id;
                    Remember(existingId);
                    return Task.FromResult(existingId);
                }

                throw;
            }
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            List<MusiKatJob> jobs;

            try
            {
                jobs = _proxy.GetJobs(Settings);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "MusiKat: could not fetch jobs");
                return new List<DownloadClientItem>();
            }

            var cutoffMs = DateTimeOffset.UtcNow.AddMilliseconds(-ForeignJobWindow.TotalMilliseconds).ToUnixTimeMilliseconds();
            var stagingRoot = Settings.LibraryPath.TrimEnd('/');

            var metaJobs = new Dictionary<string, MusiKatJob>();
            var albumTrackJobs = new Dictionary<string, List<MusiKatJob>>();
            var standaloneJobs = new List<MusiKatJob>();

            foreach (var job in jobs)
            {
                if (job.JobId.IsNotNullOrWhiteSpace() && job.JobId.StartsWith("album:"))
                {
                    metaJobs[job.JobId] = job;
                    continue;
                }

                if (!IsRelevant(job, cutoffMs, stagingRoot))
                {
                    continue;
                }

                if (job.AlbumId.IsNotNullOrWhiteSpace())
                {
                    if (!albumTrackJobs.TryGetValue(job.AlbumId, out var list))
                    {
                        list = new List<MusiKatJob>();
                        albumTrackJobs[job.AlbumId] = list;
                    }

                    list.Add(job);
                }
                else
                {
                    standaloneJobs.Add(job);
                }
            }

            var items = new List<DownloadClientItem>();

            foreach (var (albumId, trackJobs) in albumTrackJobs)
            {
                metaJobs.TryGetValue($"album:{albumId}", out var meta);
                var item = BuildAlbumItem(albumId, trackJobs, meta);

                if (item != null)
                {
                    items.Add(item);
                }
            }

            foreach (var job in standaloneJobs)
            {
                var item = ToDownloadClientItem(job);

                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            if (deleteData)
            {
                DeleteItemData(item);
            }

            try
            {
                _proxy.Cancel(item.DownloadId, Settings);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "MusiKat: could not cancel download {0}", item.DownloadId);
            }
        }

        public override DownloadClientInfo GetStatus()
        {
            try
            {
                return new DownloadClientInfo
                {
                    IsLocalhost = IsLocalhost,
                    OutputRootFolders = new List<OsPath> { GetOutputRoot() }
                };
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "MusiKat: GetStatus failed");
                return new DownloadClientInfo
                {
                    IsLocalhost = false,
                    OutputRootFolders = new List<OsPath>()
                };
            }
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var health = _proxy.GetHealth(Settings);

                if (health.Status != "healthy")
                {
                    return new NzbDroneValidationFailure(string.Empty, $"MusiKat is not healthy: {health.Status}");
                }

                if (Settings.Provider == MusiKatMetadataProvider.Spotify && health.SpotifyConfigured != true)
                {
                    return new NzbDroneValidationFailure(
                        nameof(MusiKatDownloadClientSettings.Provider),
                        "Spotify is not configured in MusiKat. Use Deezer or add Spotify credentials to MusiKat.");
                }

                var libraries = _proxy.GetLibraries(Settings);

                if (libraries.All(l => !PathsEqual(l, Settings.LibraryPath)))
                {
                    return new NzbDroneValidationFailure(
                        nameof(MusiKatDownloadClientSettings.LibraryPath),
                        $"MusiKat does not allow the folder '{Settings.LibraryPath}'. Add it to NAVIDROME_MUSIC_PATHS in the MusiKat environment.");
                }

                return null;
            }
            catch (DownloadClientException ex)
            {
                return new NzbDroneValidationFailure(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MusiKat test failed");
                return new NzbDroneValidationFailure(string.Empty, $"Could not connect to MusiKat: {ex.Message}");
            }
        }

        private bool IsLocalhost
        {
            get
            {
                var host = new Uri(Settings.BaseUrl).Host;
                return host == "localhost" || host == "127.0.0.1" || host == "::1";
            }
        }

        private OsPath GetOutputRoot()
        {
            var host = new Uri(Settings.BaseUrl).Host;
            return _remotePathMappingService.RemapRemoteToLocal(host, new OsPath(Settings.LibraryPath));
        }

        private void Remember(string id)
        {
            if (id.IsNotNullOrWhiteSpace())
            {
                _rememberedIds.TryAdd(id, 0);
            }
        }

        private bool IsRelevant(MusiKatJob job, long cutoffMs, string stagingRoot)
        {
            if (job.Status == "cancelled")
            {
                return false;
            }

            if (_rememberedIds.ContainsKey(job.JobId) || _rememberedIds.ContainsKey("album:" + job.AlbumId))
            {
                return true;
            }

            if (job.UpdatedAtMs >= cutoffMs)
            {
                return true;
            }

            return job.FilePath.IsNotNullOrWhiteSpace() &&
                   (job.FilePath.StartsWith(stagingRoot + "/") || job.FilePath.StartsWith(stagingRoot + System.IO.Path.DirectorySeparatorChar));
        }

        private DownloadClientItem BuildAlbumItem(string albumId, List<MusiKatJob> trackJobs, MusiKatJob meta)
        {
            var total = trackJobs.Count;
            var active = trackJobs.Count(j => j.Status == "queued" || j.Status == "processing");
            var completed = trackJobs.Count(j => j.Status == "completed");
            var failed = trackJobs.Count(j => j.Status == "error");

            if (active == 0 && completed == 0 && failed == 0)
            {
                return null;
            }

            DownloadItemStatus status;

            if (active > 0)
            {
                status = DownloadItemStatus.Downloading;
            }
            else if (completed == 0 && failed > 0)
            {
                status = DownloadItemStatus.Failed;
            }
            else if (failed > 0)
            {
                status = DownloadItemStatus.Warning;
            }
            else
            {
                status = DownloadItemStatus.Completed;
            }

            var item = new DownloadClientItem
            {
                DownloadId = $"album:{albumId}",
                Title = BuildAlbumTitle(meta, albumId),
                Status = status,
                Message = meta?.Message,
                CanMoveFiles = true,
                CanBeRemoved = true
            };

            var outputPath = FindAlbumOutputPath(trackJobs);

            if (!outputPath.IsEmpty)
            {
                item.OutputPath = outputPath;
            }

            item.DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false);

            return item;
        }

        private string BuildAlbumTitle(MusiKatJob meta, string albumId)
        {
            if (meta?.Payload != null &&
                meta.Payload.TryGetValue("album_name", out var albumName) &&
                meta.Payload.TryGetValue("artist", out var artist))
            {
                return $"{artist} - {albumName}";
            }

            return $"MusiKat album {albumId}";
        }

        private OsPath FindAlbumOutputPath(List<MusiKatJob> trackJobs)
        {
            var completedFile = trackJobs
                .Where(j => j.Status == "completed" && j.FilePath.IsNotNullOrWhiteSpace())
                .Select(j => j.FilePath)
                .FirstOrDefault();

            if (completedFile == null)
            {
                return new OsPath();
            }

            var path = new OsPath(completedFile);

            return path.Directory.IsEmpty ? new OsPath() : path.Directory;
        }

        private DownloadClientItem ToDownloadClientItem(MusiKatJob job)
        {
            DownloadItemStatus status;

            switch (job.Status)
            {
                case "queued":
                case "processing":
                    status = DownloadItemStatus.Downloading;
                    break;
                case "completed":
                    status = DownloadItemStatus.Completed;
                    break;
                case "error":
                    status = DownloadItemStatus.Failed;
                    break;
                default:
                    return null;
            }

            var item = new DownloadClientItem
            {
                DownloadId = job.JobId,
                Title = job.Message.IsNotNullOrWhiteSpace() ? job.Message : $"MusiKat {job.JobId}",
                Status = status,
                Message = job.Message,
                CanMoveFiles = true,
                CanBeRemoved = true
            };

            if (status == DownloadItemStatus.Completed && job.FilePath.IsNotNullOrWhiteSpace())
            {
                item.OutputPath = new OsPath(job.FilePath);
            }

            item.DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false);

            return item;
        }

        private static bool PathsEqual(string left, string right)
        {
            return left.TrimEnd('/').TrimEnd('\\')
                .Equals(right.TrimEnd('/').TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }
    }
}
