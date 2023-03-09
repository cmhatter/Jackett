using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class AlphaRatio : GazelleTracker
    {
        public override string Id => "alpharatio";
        public override string Name => "AlphaRatio";
        public override string Description => "AlphaRatio (AR) is a Private Torrent Tracker for 0DAY / GENERAL";
        public override string SiteLink { get; protected set; } = "https://alpharatio.cc/";
        public override string Language => "en-US";
        public override string Type => "private";

        public AlphaRatio(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   supportsFreeleechOnly: true,
                   imdbInTags: true)
        {
            AddCategoryMapping(1, TorznabCatType.TVSD, "TvSD");
            AddCategoryMapping(2, TorznabCatType.TVHD, "TvHD");
            AddCategoryMapping(3, TorznabCatType.TVUHD, "TvUHD");
            AddCategoryMapping(4, TorznabCatType.TVSD, "TvDVDRip");
            AddCategoryMapping(5, TorznabCatType.TVSD, "TvPackSD");
            AddCategoryMapping(6, TorznabCatType.TVHD, "TvPackHD");
            AddCategoryMapping(7, TorznabCatType.TVUHD, "TvPackUHD");
            AddCategoryMapping(8, TorznabCatType.MoviesSD, "MovieSD");
            AddCategoryMapping(9, TorznabCatType.MoviesHD, "MovieHD");
            AddCategoryMapping(10, TorznabCatType.MoviesUHD, "MovieUHD");
            AddCategoryMapping(11, TorznabCatType.MoviesSD, "MoviePackSD");
            AddCategoryMapping(12, TorznabCatType.MoviesHD, "MoviePackHD");
            AddCategoryMapping(13, TorznabCatType.MoviesUHD, "MoviePackUHD");
            AddCategoryMapping(14, TorznabCatType.XXX, "MovieXXX");
            AddCategoryMapping(15, TorznabCatType.MoviesBluRay, "Bluray");
            AddCategoryMapping(16, TorznabCatType.TVAnime, "AnimeSD");
            AddCategoryMapping(17, TorznabCatType.TVAnime, "AnimeHD");
            AddCategoryMapping(18, TorznabCatType.PCGames, "GamesPC");
            AddCategoryMapping(19, TorznabCatType.ConsoleXBox, "GamesxBox");
            AddCategoryMapping(20, TorznabCatType.ConsolePS4, "GamesPS");
            AddCategoryMapping(21, TorznabCatType.ConsoleWii, "GamesNin");
            AddCategoryMapping(22, TorznabCatType.PC0day, "AppsWindows");
            AddCategoryMapping(23, TorznabCatType.PCMac, "AppsMAC");
            AddCategoryMapping(24, TorznabCatType.PC0day, "AppsLinux");
            AddCategoryMapping(25, TorznabCatType.PCMobileOther, "AppsMobile");
            AddCategoryMapping(26, TorznabCatType.XXX, "0dayXXX");
            AddCategoryMapping(27, TorznabCatType.Books, "eBook");
            AddCategoryMapping(28, TorznabCatType.AudioAudiobook, "AudioBook");
            AddCategoryMapping(29, TorznabCatType.AudioOther, "Music");
            AddCategoryMapping(30, TorznabCatType.Other, "Misc");
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = await base.PerformQuery(query);
            foreach (var release in releases)
            {
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 259200;
            }
            return releases;
        }
    }
}
