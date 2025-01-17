using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class ShizaProject : IndexerBase
    {
        public override string Id => "shizaroject";
        public override string Name => "ShizaProject";
        public override string Description => "ShizaProject Tracker is a Public RUSSIAN tracker and release group for ANIME";
        public override string SiteLink { get; protected set; } = "https://shiza-project.com/";
        public override string[] LegacySiteLinks => new[]
        {
            "http://shiza-project.com/" // site is forcing https
        };
        public override string Language => "ru-RU";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public ShizaProject(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime");

            return caps;
        }

        /// <summary>
        /// http://shiza-project.com/graphql
        /// </summary>
        private string GraphqlEndpointUrl => SiteLink + "graphql";

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releasesQuery = new
            {
                operationName = "fetchReleases",
                variables = new
                {
                    first = 50, //Number of fetched releases (required parameter) TODO: consider adding pagination 
                    query = query.SearchTerm
                },
                query = @"
                query fetchReleases($first: Int, $query: String) {
                    releases(first: $first, query: $query) {
                        edges {
                            node {
                                name
                                originalName
                                alternativeNames
                                publishedAt
                                slug
                                posters {
                                    preview: resize(width: 360, height: 500) {
                                        url
                                    }
                                }
                                torrents {
                                    downloaded
                                    seeders
                                    leechers
                                    size
                                    magnetUri
                                    updatedAt
                                    file {
                                        url
                                    }
                                    videoQualities
                                }
                            }
                        }
                    }
                }"
            };
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json; charset=utf-8" },
            };
            var response = await RequestWithCookiesAndRetryAsync(GraphqlEndpointUrl, method: RequestType.POST, rawbody: Newtonsoft.Json.JsonConvert.SerializeObject(releasesQuery), headers: headers);
            var j = JsonConvert.DeserializeObject<ReleasesResponse>(response.ContentString);
            var releases = new List<ReleaseInfo>();
            foreach (var e in j.Data.Releases.Edges)
            {
                var n = e.Node;
                var baseRelease = new ReleaseInfo
                {
                    Title = composeTitle(n),
                    Poster = getFirstPoster(n),
                    Details = new Uri(SiteLink + "releases/" + n.Slug),
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    Category = new[] { TorznabCatType.TVAnime.ID }
                };

                foreach (var t in n.Torrents)
                {
                    var release = (ReleaseInfo)baseRelease.Clone();

                    release.Title += getTitleQualities(t);
                    release.Size = t.Size;
                    release.Seeders = t.Seeders;
                    release.Peers = t.Leechers + t.Seeders;
                    release.Grabs = t.Downloaded;
                    release.Link = t.File?.Url;
                    release.Guid = t.File?.Url;
                    release.MagnetUri = t.MagnetUri;
                    release.PublishDate = getActualPublishDate(n, t);
                    releases.Add(release);
                }
            }

            return releases;
        }

        private string composeTitle(Node n)
        {
            var title = n.Name;
            title += " / " + n.OriginalName;
            foreach (string name in n.AlternativeNames)
                title += " / " + name;

            return title;
        }

        private DateTime getActualPublishDate(Node n, Torrent t)
        {
            if (n.PublishedAt == null)
            {
                return t.UpdatedAt;
            }
            else
            {
                return (t.UpdatedAt > n.PublishedAt) ? t.UpdatedAt : n.PublishedAt.Value;
            }
        }

        private string getTitleQualities(Torrent t)
        {
            var s = " [";

            foreach (string q in t.VideoQualities)
            {
                s += " " + q;
            }

            return s + " ]";
        }

        private Uri getFirstPoster(Node n)
        {
            if (n.Posters.Length == 0)
            {
                return null;
            }
            else
            {
                return n.Posters[0].Preview.Url;
            }
        }

        public partial class ReleasesResponse
        {
            [JsonProperty("data")]
            public Data Data { get; set; }
        }

        public partial class Data
        {
            [JsonProperty("releases")]
            public Releases Releases { get; set; }
        }

        public partial class Releases
        {
            [JsonProperty("edges")]
            public Edge[] Edges { get; set; }
        }

        public partial class Edge
        {
            [JsonProperty("node")]
            public Node Node { get; set; }
        }

        public partial class Node
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("originalName")]
            public string OriginalName { get; set; }

            [JsonProperty("alternativeNames")]
            public string[] AlternativeNames { get; set; }

            [JsonProperty("publishedAt")]
            public DateTime? PublishedAt { get; set; }

            [JsonProperty("slug")]
            public string Slug { get; set; }

            [JsonProperty("posters")]
            public Poster[] Posters { get; set; }

            [JsonProperty("torrents")]
            public Torrent[] Torrents { get; set; }
        }

        public partial class Poster
        {
            [JsonProperty("preview")]
            public Preview Preview { get; set; }
        }

        public partial class Preview
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }
        }

        public partial class Torrent
        {
            [JsonProperty("downloaded")]
            public long Downloaded { get; set; }

            [JsonProperty("seeders")]
            public long Seeders { get; set; }

            [JsonProperty("leechers")]
            public long Leechers { get; set; }

            [JsonProperty("size")]
            public long Size { get; set; }

            [JsonProperty("magnetUri")]
            public Uri MagnetUri { get; set; }

            [JsonProperty("updatedAt")]
            public DateTime UpdatedAt { get; set; }

            [JsonProperty("file")]
            public Preview File { get; set; }

            [JsonProperty("videoQualities")]
            public string[] VideoQualities { get; set; }
        }
    }
}
