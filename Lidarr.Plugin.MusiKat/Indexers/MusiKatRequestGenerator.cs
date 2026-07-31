using System.Collections.Generic;
using System.Net.Http;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.MusiKat
{
    public class MusiKatRequestGenerator : IIndexerRequestGenerator
    {
        private const int SearchLimit = 20;

        public MusiKatIndexerSettings Settings { get; set; }

        public Logger Logger { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();
            var query = $"{searchCriteria.ArtistQuery} {searchCriteria.AlbumQuery}".Trim();

            chain.AddTier(GetRequests(query));

            return chain;
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();

            chain.AddTier(GetRequests(searchCriteria.ArtistQuery));

            return chain;
        }

        private IEnumerable<IndexerRequest> GetRequests(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                yield break;
            }

            var body = new Dictionary<string, object>
            {
                { "query", query },
                { "limit", SearchLimit },
                { "provider", MusiKatFormats.ProviderValue(Settings.Provider) }
            }.ToJson();

            var request = new IndexerRequest($"{Settings.BaseUrl.TrimEnd('/')}/api/search/albums", HttpAccept.Json);
            request.HttpRequest.Method = HttpMethod.Post;
            request.HttpRequest.SetContent(body);
            request.HttpRequest.Headers.ContentType = "application/json";

            yield return request;
        }
    }
}
