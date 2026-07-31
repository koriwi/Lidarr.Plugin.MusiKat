using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Download.Clients.MusiKat
{
    public class MusiKatAlbum
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; }

        [JsonProperty("total_tracks")]
        public int TotalTracks { get; set; }

        [JsonProperty("external_url")]
        public string ExternalUrl { get; set; }
    }

    public class MusiKatJob
    {
        [JsonProperty("job_id")]
        public string JobId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("stage")]
        public string Stage { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("file_path")]
        public string FilePath { get; set; }

        [JsonProperty("album_id")]
        public string AlbumId { get; set; }

        [JsonProperty("updated_at_ms")]
        public long UpdatedAtMs { get; set; }

        [JsonProperty("payload")]
        public Dictionary<string, object> Payload { get; set; }
    }

    public class MusiKatJobListResponse
    {
        [JsonProperty("jobs")]
        public List<MusiKatJob> Jobs { get; set; }
    }

    public class MusiKatLibrary
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }
    }

    public class MusiKatLibrariesResponse
    {
        [JsonProperty("libraries")]
        public List<MusiKatLibrary> Libraries { get; set; }
    }

    public class MusiKatHealthResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("spotify_configured")]
        public bool? SpotifyConfigured { get; set; }
    }

    public class MusiKatDownloadResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("track_id")]
        public string TrackId { get; set; }
    }

    public class MusiKatAlbumDownloadResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("album_id")]
        public string AlbumId { get; set; }

        [JsonProperty("queued_track_ids")]
        public List<string> QueuedTrackIds { get; set; }
    }
}
