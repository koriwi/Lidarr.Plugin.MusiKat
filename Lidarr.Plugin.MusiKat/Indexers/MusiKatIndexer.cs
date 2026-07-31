using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.MusiKat
{
    public class MusiKatIndexer : HttpIndexerBase<MusiKatIndexerSettings>
    {
        public override string Name => "MusiKat";

        public override string Protocol => nameof(MusiKatDownloadProtocol);

        public override bool SupportsRss => false;

        public override bool SupportsSearch => true;

        public override int PageSize => 20;

        public override TimeSpan RateLimit => TimeSpan.FromSeconds(1);

        public MusiKatIndexer(IHttpClient httpClient,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new MusiKatRequestGenerator
            {
                Settings = Settings,
                Logger = _logger
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new MusiKatParser
            {
                Settings = Settings
            };
        }
    }
}
