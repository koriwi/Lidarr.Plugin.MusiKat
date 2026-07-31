using System;
using System.Collections.Generic;
using System.Globalization;
using NzbDrone.Common.Http;
using NzbDrone.Core.Download.Clients.MusiKat;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.MusiKat
{
    public class MusiKatParser : IParseIndexerResponse
    {
        public MusiKatIndexerSettings Settings { get; set; }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse response)
        {
            var releases = new List<ReleaseInfo>();
            var json = new HttpResponse<List<MusiKatAlbum>>(response.HttpResponse);

            foreach (var album in json.Resource)
            {
                releases.Add(ToReleaseInfo(album));
            }

            return releases;
        }

        private ReleaseInfo ToReleaseInfo(MusiKatAlbum album)
        {
            var year = 0;
            var publishDate = DateTime.UtcNow;

            if (DateTime.TryParse(album.ReleaseDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                publishDate = parsed;
                year = publishDate.Year;
            }

            var format = MusiKatFormats.FromFormat(Settings.Format);
            var title = $"{album.Artist} - {album.Name}";

            if (year > 0)
            {
                title += $" ({year})";
            }

            title += $" [{format.Label}] [WEB]";

            // Rough estimate: 8 MiB per track.
            var size = album.TotalTracks * 8L * 1024L * 1024L;

            return new ReleaseInfo
            {
                Guid = $"MusiKat-{album.Id}",
                Title = title,
                Artist = album.Artist,
                Album = album.Name,
                DownloadUrl = BuildDownloadUrl(album.Id),
                InfoUrl = album.ExternalUrl,
                PublishDate = publishDate,
                DownloadProtocol = nameof(MusiKatDownloadProtocol),
                Codec = format.Codec,
                Container = format.Container,
                Size = size
            };
        }

        private string BuildDownloadUrl(string albumId)
        {
            var url = $"{Settings.BaseUrl.TrimEnd('/')}/api/album/{albumId}";
            var query = new List<string>
            {
                $"provider={MusiKatFormats.ProviderValue(Settings.Provider)}",
                $"format={MusiKatFormats.FromFormat(Settings.Format).ApiValue}"
            };

            return $"{url}?{string.Join("&", query)}";
        }
    }
}
