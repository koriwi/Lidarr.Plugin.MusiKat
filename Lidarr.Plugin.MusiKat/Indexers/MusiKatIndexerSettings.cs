using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.MusiKat
{
    public class MusiKatIndexerSettingsValidator : AbstractValidator<MusiKatIndexerSettings>
    {
        public MusiKatIndexerSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
        }
    }

    public class MusiKatIndexerSettings : IIndexerSettings
    {
        private static readonly MusiKatIndexerSettingsValidator Validator = new MusiKatIndexerSettingsValidator();

        public MusiKatIndexerSettings()
        {
            BaseUrl = "http://localhost:8000";
            Provider = MusiKatMetadataProvider.Deezer;
            Format = MusiKatAudioFormat.Flac;
        }

        [FieldDefinition(0, Label = "Base URL", Type = FieldType.Url, HelpText = "MusiKat server URL, e.g. https://musicdl.gosewis.ch")]
        public string BaseUrl { get; set; }

        public int? EarlyReleaseLimit { get; set; }

        [FieldDefinition(1, Label = "Metadata Provider", Type = FieldType.Select, SelectOptions = typeof(MusiKatMetadataProvider), HelpText = "Catalog MusiKat uses to find tracks on YouTube. Deezer needs no API key.")]
        public MusiKatMetadataProvider Provider { get; set; }

        [FieldDefinition(2, Label = "Audio Format", Type = FieldType.Select, SelectOptions = typeof(MusiKatAudioFormat), HelpText = "Format MusiKat encodes from YouTube audio")]
        public MusiKatAudioFormat Format { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
