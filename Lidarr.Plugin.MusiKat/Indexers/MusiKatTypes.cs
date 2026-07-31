using System;
using System.Text.RegularExpressions;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Indexers.MusiKat
{
    public enum MusiKatMetadataProvider
    {
        [FieldOption(label: "Deezer")]
        Deezer = 0,

        [FieldOption(label: "Spotify")]
        Spotify = 1
    }

    public enum MusiKatAudioFormat
    {
        [FieldOption(label: "MP3")]
        Mp3 = 0,

        [FieldOption(label: "M4A/AAC")]
        M4a = 1,

        [FieldOption(label: "Opus")]
        Opus = 2,

        [FieldOption(label: "OGG Vorbis")]
        Ogg = 3,

        [FieldOption(label: "FLAC")]
        Flac = 4
    }

    public enum MusiKatAudioQuality
    {
        [FieldOption(label: "Default (server)")]
        Default = 0,

        [FieldOption(label: "96 kbps")]
        Q96 = 96,

        [FieldOption(label: "128 kbps")]
        Q128 = 128,

        [FieldOption(label: "192 kbps")]
        Q192 = 192,

        [FieldOption(label: "256 kbps")]
        Q256 = 256,

        [FieldOption(label: "320 kbps")]
        Q320 = 320
    }

    public class MusiKatFormatInfo
    {
        public string Label { get; set; }

        public string Codec { get; set; }

        public string Container { get; set; }

        public string ApiValue { get; set; }
    }

    public static class MusiKatFormats
    {
        public static string ProviderValue(MusiKatMetadataProvider provider)
        {
            return provider == MusiKatMetadataProvider.Spotify ? "spotify" : "deezer";
        }

        public static MusiKatFormatInfo FromFormat(MusiKatAudioFormat format)
        {
            switch (format)
            {
                case MusiKatAudioFormat.Mp3:
                    return new MusiKatFormatInfo { Label = "MP3", Codec = "MP3", Container = "320", ApiValue = "mp3" };
                case MusiKatAudioFormat.M4a:
                    return new MusiKatFormatInfo { Label = "M4A", Codec = "AAC", Container = "M4A", ApiValue = "m4a" };
                case MusiKatAudioFormat.Opus:
                    return new MusiKatFormatInfo { Label = "Opus", Codec = "Opus", Container = "OGG", ApiValue = "opus" };
                case MusiKatAudioFormat.Ogg:
                    return new MusiKatFormatInfo { Label = "OGG", Codec = "Vorbis", Container = "OGG", ApiValue = "ogg" };
                default:
                    return new MusiKatFormatInfo { Label = "FLAC", Codec = "FLAC", Container = "Lossless", ApiValue = "flac" };
            }
        }

        public static string QualityValue(MusiKatAudioQuality quality)
        {
            return quality == MusiKatAudioQuality.Default ? null : ((int)quality).ToString();
        }
    }

    /// <summary>
    /// Parses the DownloadUrl that the MusiKat indexer stores on a release.
    /// The URL looks like {base}/api/album/{id}?provider=...&format=... for an
    /// album release and {base}/api/track/{id}?... for a track release.
    /// </summary>
    public class MusiKatDownloadUrl
    {
        public bool IsAlbum { get; set; }

        public string Id { get; set; }

        public static MusiKatDownloadUrl Parse(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var match = Regex.Match(url, @"/(?:album|track)/([^/?]+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            return new MusiKatDownloadUrl
            {
                IsAlbum = url.Contains("/api/album/", StringComparison.OrdinalIgnoreCase),
                Id = match.Groups[1].Value
            };
        }
    }
}
