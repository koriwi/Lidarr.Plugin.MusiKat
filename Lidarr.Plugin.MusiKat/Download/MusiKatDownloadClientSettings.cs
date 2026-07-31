using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.MusiKat;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.MusiKat
{
    public class MusiKatDownloadClientSettingsValidator : AbstractValidator<MusiKatDownloadClientSettings>
    {
        public MusiKatDownloadClientSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.LibraryPath).NotEmpty();
        }
    }

    public class MusiKatDownloadClientSettings : IProviderConfig
    {
        private static readonly MusiKatDownloadClientSettingsValidator Validator = new MusiKatDownloadClientSettingsValidator();

        public MusiKatDownloadClientSettings()
        {
            BaseUrl = "http://localhost:8000";
            Provider = MusiKatMetadataProvider.Deezer;
            Format = MusiKatAudioFormat.Flac;
            LibraryPath = "/downloads/musikat";
        }

        [FieldDefinition(0, Label = "Base URL", Type = FieldType.Url, HelpText = "MusiKat server URL, e.g. https://musicdl.gosewis.ch")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Metadata Provider", Type = FieldType.Select, SelectOptions = typeof(MusiKatMetadataProvider), HelpText = "Catalog MusiKat uses to find tracks on YouTube. Deezer needs no API key.")]
        public MusiKatMetadataProvider Provider { get; set; }

        [FieldDefinition(2, Label = "Download Root (server path)", Type = FieldType.Textbox, HelpText = "Absolute path on the MusiKat server. Add this path to NAVIDROME_MUSIC_PATHS in the MusiKat environment. Lidarr imports completed downloads from this folder.")]
        public string LibraryPath { get; set; }

        [FieldDefinition(3, Label = "Audio Format", Type = FieldType.Select, SelectOptions = typeof(MusiKatAudioFormat), HelpText = "Format MusiKat encodes from YouTube audio")]
        public MusiKatAudioFormat Format { get; set; }

        [FieldDefinition(4, Label = "Audio Quality", Type = FieldType.Select, SelectOptions = typeof(MusiKatAudioQuality), Advanced = true, HelpText = "Optional bitrate. Leave at Default to use the MusiKat server setting.")]
        public MusiKatAudioQuality Quality { get; set; }

        [FieldDefinition(5, Label = "Extra Download Retries", Type = FieldType.Number, Advanced = true, HelpText = "Extra attempts per track if the YouTube download fails (0-5)")]
        public int MaxRetries { get; set; }

        [FieldDefinition(6, Label = "Force Re-download", Type = FieldType.Checkbox, Advanced = true, HelpText = "Skip MusiKat duplicate checks. Enable this when Lidarr re-grabs an album for an upgrade and MusiKat returns a 409.")]
        public bool ForceRedownload { get; set; }

        [FieldDefinition(7, Label = "API Key (optional)", Type = FieldType.Password, Advanced = true, Privacy = PrivacyLevel.ApiKey, HelpText = "Optional X-Api-Key header. MusiKat does not require an API key yet.")]
        public string ApiKey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
