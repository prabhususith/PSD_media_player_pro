using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OmniPlayer.Core.LiveTv
{
    public class LiveTvManager
    {
        private static readonly Lazy<LiveTvManager> _instance = new(() => new LiveTvManager());
        public static LiveTvManager Instance => _instance.Value;

        private readonly List<LiveTvChannel> _channels = new();
        private readonly HashSet<string> _channelUrls = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<LiveTvChannel>> _channelsByCategory = new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<LiveTvChannel>? _cachedSnapshot;
        private readonly object _lock = new();
        private static readonly HttpClient _httpClient;

        private static readonly Regex RegexLogo = new(@"tvg-logo=""([^""]+)""", RegexOptions.Compiled);
        private static readonly Regex RegexCountry = new(@"tvg-country=""([^""]+)""", RegexOptions.Compiled);
        private static readonly Regex RegexGroup = new(@"group-title=""([^""]+)""", RegexOptions.Compiled);

        static LiveTvManager()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public IReadOnlyList<LiveTvChannel> AllChannels
        {
            get
            {
                lock (_lock)
                {
                    return _cachedSnapshot ??= _channels.AsReadOnly();
                }
            }
        }

        private void InvalidateCache()
        {
            _cachedSnapshot = null;
            _channelsByCategory.Clear();
            foreach (var ch in _channels)
            {
                if (!string.IsNullOrEmpty(ch.Category))
                {
                    if (!_channelsByCategory.TryGetValue(ch.Category, out var list))
                    {
                        list = new List<LiveTvChannel>();
                        _channelsByCategory[ch.Category] = list;
                    }
                    list.Add(ch);
                }
            }
        }

        private LiveTvManager()
        {
            InitializeCuratedCatalog();
            Task.Run(async () =>
            {
                try
                {
                    await RefreshFromNetworkAsync();
                }
                catch { }
            });
        }

        public IEnumerable<string> GetCategories()
        {
            return new[]
            {
                "All (1000+)",
                "Indian National",
                "Tamil Regional",
                "Telugu Regional",
                "Malayalam Regional",
                "Kannada Regional",
                "Bengali Regional",
                "Marathi Regional",
                "Punjabi & Gujarati",
                "World News",
                "Sports & Action",
                "Movies & Cinema",
                "Kids & Family",
                "Adults / Late Night",
                "Music & Entertainment",
                "Science & Tech",
                "Documentary"
            };
        }

        public IEnumerable<string> GetCountries()
        {
            lock (_lock)
            {
                var countries = _channels
                    .Select(c => c.Country)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                countries.Insert(0, "All Countries");
                return countries;
            }
        }

        public List<LiveTvChannel> FilterChannels(string? category, string? query, string? country = null)
        {
            lock (_lock)
            {
                IEnumerable<LiveTvChannel> source;

                if (!string.IsNullOrWhiteSpace(category) && !category.StartsWith("All", StringComparison.OrdinalIgnoreCase))
                {
                    if (_channelsByCategory.TryGetValue(category, out var catList))
                    {
                        source = catList;
                    }
                    else
                    {
                        return new List<LiveTvChannel>();
                    }
                }
                else
                {
                    source = _channels;
                }

                if (!string.IsNullOrWhiteSpace(country) && !country.StartsWith("All", StringComparison.OrdinalIgnoreCase))
                {
                    source = source.Where(c => c.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    string q = query.Trim().ToLowerInvariant();
                    source = source.Where(c =>
                        c.Name.ToLowerInvariant().Contains(q) ||
                        c.Category.ToLowerInvariant().Contains(q) ||
                        c.Language.ToLowerInvariant().Contains(q) ||
                        c.Country.ToLowerInvariant().Contains(q));
                }

                return source.ToList();
            }
        }

        public void AddChannel(LiveTvChannel channel)
        {
            if (channel == null || string.IsNullOrWhiteSpace(channel.StreamUrl)) return;
            lock (_lock)
            {
                var existing = _channels.FirstOrDefault(c => c.StreamUrl.Equals(channel.StreamUrl, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Name = channel.Name;
                    existing.Category = channel.Category;
                    existing.Language = channel.Language;
                    existing.Country = channel.Country;
                    existing.Resolution = channel.Resolution;
                    existing.IsActive = true;
                }
                else
                {
                    _channels.Insert(0, channel);
                    _channelUrls.Add(channel.StreamUrl);
                }
                InvalidateCache();
            }
        }

        public async Task<int> RefreshFromNetworkAsync()
        {
            var feeds = new (string Url, string DefaultCategory, int Limit)[]
            {
                ("https://iptv-org.github.io/iptv/countries/in.m3u", "Indian National", 0),
                ("https://iptv-org.github.io/iptv/categories/kids.m3u", "Kids & Family", 200),
                ("https://iptv-org.github.io/iptv/categories/movies.m3u", "Movies & Cinema", 200),
                ("https://iptv-org.github.io/iptv/categories/news.m3u", "World News", 300),
                ("https://iptv-org.github.io/iptv/categories/sports.m3u", "Sports & Action", 200),
                ("https://iptv-org.github.io/iptv/categories/music.m3u", "Music & Entertainment", 200),
                ("https://iptv-org.github.io/iptv/categories/animation.m3u", "Kids & Family", 150),
                ("https://iptv-org.github.io/iptv/categories/documentary.m3u", "Documentary", 200),
                ("https://iptv-org.github.io/iptv/categories/entertainment.m3u", "Movies & Cinema", 200),
                ("https://iptv-org.github.io/iptv/categories/comedy.m3u", "Movies & Cinema", 150)
            };

            foreach (var feed in feeds)
            {
                try
                {
                    string content = await _httpClient.GetStringAsync(feed.Url);
                    ParseAndMergeM3u(content, feed.DefaultCategory, feed.Limit);
                }
                catch
                {
                    // Continue with next feed if one fails
                }
            }

            return AllChannels.Count;
        }

        public int ParseAndMergeM3u(string m3uContent, string defaultCategory, int limit = 0)
        {
            if (string.IsNullOrWhiteSpace(m3uContent)) return 0;

            var lines = m3uContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int count = 0;
            LiveTvChannel? currentChannel = null;

            lock (_lock)
            {
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentChannel = new LiveTvChannel();

                        // Parse logo
                        var logoMatch = RegexLogo.Match(trimmed);
                        if (logoMatch.Success) currentChannel.LogoUrl = logoMatch.Groups[1].Value;

                        // Parse country
                        var countryMatch = RegexCountry.Match(trimmed);
                        if (countryMatch.Success) currentChannel.Country = countryMatch.Groups[1].Value.ToUpperInvariant();

                        // Parse category / group-title
                        var groupMatch = RegexGroup.Match(trimmed);
                        string group = groupMatch.Success ? groupMatch.Groups[1].Value : defaultCategory;

                        // Parse channel name
                        int lastComma = trimmed.LastIndexOf(',');
                        if (lastComma >= 0 && lastComma < trimmed.Length - 1)
                        {
                            currentChannel.Name = trimmed.Substring(lastComma + 1).Trim();
                        }
                        else
                        {
                            currentChannel.Name = "Channel " + (_channels.Count + 1);
                        }

                        // Categorize channel
                        CategorizeChannel(currentChannel, group, defaultCategory);
                    }
                    else if (!trimmed.StartsWith("#") && trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentChannel != null)
                        {
                            currentChannel.StreamUrl = trimmed;

                            // O(1) duplicate check via HashSet
                            if (_channelUrls.Add(trimmed))
                            {
                                _channels.Add(currentChannel);
                                count++;
                                if (limit > 0 && count >= limit) break;
                            }
                            currentChannel = null;
                        }
                    }
                }

                if (count > 0)
                {
                    InvalidateCache();
                }
            }
            return count;
        }

        private void CategorizeChannel(LiveTvChannel ch, string group, string defaultCategory)
        {
            string nameLower = ch.Name.ToLowerInvariant();
            string groupLower = group.ToLowerInvariant();

            // Regional Indian TV detection
            if (nameLower.Contains("tamil") || nameLower.Contains("podhigai") || nameLower.Contains("sun news") ||
                nameLower.Contains("polimer") || nameLower.Contains("puthiya") || nameLower.Contains("thanthi") ||
                nameLower.Contains("sathiyam") || nameLower.Contains("kalaignar") || nameLower.Contains("captain news") ||
                nameLower.Contains("malaimurasu") || nameLower.Contains("velicham") || nameLower.Contains("news7 tamil"))
            {
                ch.Category = "Tamil Regional";
                ch.Language = "Tamil";
                ch.Country = "IN";
            }
            else if (nameLower.Contains("telugu") || nameLower.Contains("tv9 telugu") || nameLower.Contains("ntv") ||
                     nameLower.Contains("sakshi") || nameLower.Contains("abn") || nameLower.Contains("v6") ||
                     nameLower.Contains("yadagiri") || nameLower.Contains("saptagiri") || nameLower.Contains("10tv") ||
                     nameLower.Contains("t news") || nameLower.Contains("99tv"))
            {
                ch.Category = "Telugu Regional";
                ch.Language = "Telugu";
                ch.Country = "IN";
            }
            else if (nameLower.Contains("malayalam") || nameLower.Contains("asianet") || nameLower.Contains("manorama") ||
                     nameLower.Contains("mathrubhumi") || nameLower.Contains("24 news") || nameLower.Contains("mediaone") ||
                     nameLower.Contains("kairali") || nameLower.Contains("janam") || nameLower.Contains("reporter tv"))
            {
                ch.Category = "Malayalam Regional";
                ch.Language = "Malayalam";
                ch.Country = "IN";
            }
            else if (nameLower.Contains("kannada") || nameLower.Contains("suvarna") || nameLower.Contains("chandana") ||
                     nameLower.Contains("public tv") || nameLower.Contains("dighvijay") || nameLower.Contains("kasthuri") ||
                     nameLower.Contains("power tv"))
            {
                ch.Category = "Kannada Regional";
                ch.Language = "Kannada";
                ch.Country = "IN";
            }
            else if (nameLower.Contains("bangla") || nameLower.Contains("bengali") || nameLower.Contains("ananda") ||
                     nameLower.Contains("24 ghanta") || nameLower.Contains("calcutta news") || nameLower.Contains("aamar bangla"))
            {
                ch.Category = "Bengali Regional";
                ch.Language = "Bengali";
                ch.Country = "IN";
            }
            else if (nameLower.Contains("marathi") || nameLower.Contains("majha") || nameLower.Contains("sahyadri") ||
                     nameLower.Contains("saam tv") || nameLower.Contains("zee 24 taas") || nameLower.Contains("pudhari") ||
                     nameLower.Contains("lokmat") || nameLower.Contains("jhakaas"))
            {
                ch.Category = "Marathi Regional";
                ch.Language = "Marathi";
                ch.Country = "IN";
            }
            else if (nameLower.Contains("punjabi") || nameLower.Contains("gujarati") || nameLower.Contains("ptc") ||
                     nameLower.Contains("asmita") || nameLower.Contains("sandesh") || nameLower.Contains("vtv") ||
                     nameLower.Contains("tashan") || nameLower.Contains("kalak") || nameLower.Contains("girnar"))
            {
                ch.Category = "Punjabi & Gujarati";
                ch.Language = nameLower.Contains("punjabi") || nameLower.Contains("ptc") || nameLower.Contains("tashan") ? "Punjabi" : "Gujarati";
                ch.Country = "IN";
            }
            else if (nameLower.Contains("kids") || nameLower.Contains("cartoon") || nameLower.Contains("animation") ||
                     nameLower.Contains("disney") || nameLower.Contains("nick") || nameLower.Contains("kartoon") ||
                     nameLower.Contains("duck") || nameLower.Contains("toon") || nameLower.Contains("pbs") ||
                     groupLower.Contains("kids") || groupLower.Contains("animation"))
            {
                ch.Category = "Kids & Family";
                ch.Language = "English";
                if (string.IsNullOrEmpty(ch.Country)) ch.Country = "US";
            }
            else if (nameLower.Contains("bikini") || nameLower.Contains("midnight") || nameLower.Contains("playboy") ||
                     nameLower.Contains("babes") || nameLower.Contains("hustler") || nameLower.Contains("night flight") ||
                     nameLower.Contains("redlight") || nameLower.Contains("adult") || nameLower.Contains("fashiontv"))
            {
                ch.Category = "Adults / Late Night";
                ch.Language = "English";
                if (string.IsNullOrEmpty(ch.Country)) ch.Country = "GLOBAL";
            }
            else if (nameLower.Contains("docu") || nameLower.Contains("nat geo") || nameLower.Contains("history") ||
                     nameLower.Contains("wild") || nameLower.Contains("cgtn doc") || groupLower.Contains("documentary"))
            {
                ch.Category = "Documentary";
                ch.Language = "English";
                if (string.IsNullOrEmpty(ch.Country)) ch.Country = "GLOBAL";
            }
            else if (nameLower.Contains("nasa") || nameLower.Contains("space") || nameLower.Contains("twit") ||
                     nameLower.Contains("tech") || nameLower.Contains("science") || groupLower.Contains("science"))
            {
                ch.Category = "Science & Tech";
                ch.Language = "English";
                if (string.IsNullOrEmpty(ch.Country)) ch.Country = "US";
            }
            else if (groupLower.Contains("sport") || nameLower.Contains("sport") || nameLower.Contains("racing") ||
                     nameLower.Contains("poker") || nameLower.Contains("fight") || nameLower.Contains("cricket"))
            {
                ch.Category = "Sports & Action";
                ch.Language = "English";
                if (string.IsNullOrEmpty(ch.Country)) ch.Country = "GLOBAL";
            }
            else if (groupLower.Contains("music") || nameLower.Contains("music") || nameLower.Contains("hits") ||
                     nameLower.Contains("clubbing") || nameLower.Contains("vevo") || nameLower.Contains("mtv") ||
                     nameLower.Contains("radio") || nameLower.Contains("pop") || nameLower.Contains("9x"))
            {
                ch.Category = "Music & Entertainment";
                ch.Language = "Various";
                if (string.IsNullOrEmpty(ch.Country)) ch.Country = "GLOBAL";
            }
            else if (groupLower.Contains("movie") || groupLower.Contains("cinema") || groupLower.Contains("comedy") ||
                     nameLower.Contains("movie") || nameLower.Contains("cinema") || nameLower.Contains("film"))
            {
                ch.Category = "Movies & Cinema";
                ch.Language = "English";
                if (string.IsNullOrEmpty(ch.Country)) ch.Country = "US";
            }
            else if (defaultCategory == "Indian National" || ch.Country == "IN" ||
                     nameLower.Contains("aaj tak") || nameLower.Contains("ndtv") || nameLower.Contains("india tv") ||
                     nameLower.Contains("dd news") || nameLower.Contains("sansad") || nameLower.Contains("republic") ||
                     nameLower.Contains("abp news") || nameLower.Contains("wion") || nameLower.Contains("cnbc") ||
                     nameLower.Contains("bharat"))
            {
                ch.Category = "Indian National";
                ch.Language = "Hindi";
                ch.Country = "IN";
            }
            else if (defaultCategory == "World News" || groupLower.Contains("news") || nameLower.Contains("news"))
            {
                ch.Category = "World News";
                ch.Language = "English";
                if (string.IsNullOrEmpty(ch.Country)) ch.Country = "GLOBAL";
            }
            else
            {
                ch.Category = defaultCategory;
                if (string.IsNullOrEmpty(ch.Country)) ch.Country = "GLOBAL";
            }

            // Detect resolution
            if (ch.Name.Contains("1080", StringComparison.OrdinalIgnoreCase) || ch.Name.Contains("HD", StringComparison.OrdinalIgnoreCase))
            {
                ch.Resolution = "1080p";
            }
            else if (ch.Name.Contains("720", StringComparison.OrdinalIgnoreCase))
            {
                ch.Resolution = "720p";
            }
            else if (ch.Name.Contains("576", StringComparison.OrdinalIgnoreCase))
            {
                ch.Resolution = "576p";
            }
            else if (ch.Name.Contains("480", StringComparison.OrdinalIgnoreCase) || ch.Name.Contains("SD", StringComparison.OrdinalIgnoreCase))
            {
                ch.Resolution = "480p";
            }
            else
            {
                ch.Resolution = "1080p";
            }
        }

        private void InitializeCuratedCatalog()
        {
            AddIndianNationalChannels();
            AddTamilRegionalChannels();
            AddTeluguRegionalChannels();
            AddMalayalamRegionalChannels();
            AddKannadaRegionalChannels();
            AddBengaliRegionalChannels();
            AddMarathiRegionalChannels();
            AddPunjabiGujaratiChannels();
            AddWorldNewsChannels();
            AddSportsChannels();
            AddMovieChannels();
            AddKidsChannels();
            AddAdultLateNightChannels();
            AddMusicChannels();
            AddScienceTechChannels();
            AddDocumentaryChannels();

            lock (_lock)
            {
                _channelUrls.Clear();
                foreach (var c in _channels)
                {
                    if (!string.IsNullOrEmpty(c.StreamUrl))
                        _channelUrls.Add(c.StreamUrl);
                }
                InvalidateCache();
            }
        }

        private void AddIndianNationalChannels()
        {
            var national = new[]
            {
                ("Aaj Tak HD", "https://feeds.intoday.in/aajtak/api/aajtakhd/master.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/LIVETV_LIVETVCHANNEL_AAJ_TAK/images/LOGO_HD/image.png", "1080p"),
                ("ABP News HD", "https://d1rc86nwwc9fag.cloudfront.net/vglive-sk-472500/abpnews/master.m3u8", "https://upload.wikimedia.org/wikipedia/commons/d/d6/ABP_News_Logo.png", "1080p"),
                ("DD News HD", "https://mumbai-edge.smartplaytv.in/DDNews/index.m3u8", "https://upload.wikimedia.org/wikipedia/commons/e/e0/DD_News_Logo.png", "1080p"),
                ("DD National HD", "https://mumbai-edge.smartplaytv.in/DDNational/index.m3u8", "https://upload.wikimedia.org/wikipedia/commons/2/22/DD_National_Logo.png", "1080p"),
                ("DD India Global HD", "https://d2lk5u59tns74c.cloudfront.net/out/v1/fff8f20221d5456e8922e689d71dedc3/index.m3u8", "https://upload.wikimedia.org/wikipedia/commons/9/91/DD_India_logo.png", "1080p"),
                ("DD Bharati HD", "https://d2lk5u59tns74c.cloudfront.net/out/v1/71e06fa9999f4ff89689e47265be7bb2/index.m3u8", "https://upload.wikimedia.org/wikipedia/commons/2/22/DD_National_Logo.png", "1080p"),
                ("Sansad TV Lok Sabha HD", "https://d2lk5u59tns74c.cloudfront.net/out/v1/fff8f20221d5456e8922e689d71dedc3/index.m3u8", "https://upload.wikimedia.org/wikipedia/commons/e/e2/Sansad_TV_logo.png", "1080p"),
                ("Sansad TV Rajya Sabha HD", "https://cdn.pishow.tv/ott/live/39/master.m3u8", "https://upload.wikimedia.org/wikipedia/commons/e/e2/Sansad_TV_logo.png", "1080p"),
                ("NDTV 24x7 HD", "https://ndtv24x7elemarchana.akamaized.net/hls/live/2003678/ndtv24x7/master.m3u8", "https://drop.ndtv.com/homepage/images/ndtv247.png", "1080p"),
                ("Bharat 24 Live HD", "https://stream.ottlive.co.in/bharat24/index.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/default.png", "1080p"),
                ("Bharat Samachar HD", "https://stream.ottlive.co.in/bharatsamachar/index.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/default.png", "1080p"),
                ("Aaj Ki Khabar HD", "https://stream.ottlive.co.in/aajkikhabar/index.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/default.png", "1080p"),
                ("Bansal News HD", "https://stream.ottlive.co.in/bansalnews/index.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/default.png", "1080p"),
                ("4 SiDES TV HD", "https://stream.ottlive.co.in/4sidestv/index.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/default.png", "1080p"),
                ("4TV News HD", "https://cdn.pishow.tv/ott/live/1007/master.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/default.png", "576p"),
                ("9 Plus News HD", "https://stream.ottlive.co.in/9plusnews/index.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/default.png", "1080p"),
                ("DD Kisan Agriculture HD", "https://mumbai-edge.smartplaytv.in/DDKisan/index.m3u8", "https://upload.wikimedia.org/wikipedia/commons/f/fc/DD_Kisan_logo.png", "1080p"),
                ("BRK News HD", "https://mumbai-edge.smartplaytv.in/BRKNEWS/index.m3u8", "https://d229kpbsb5jevy.cloudfront.net/yuppfast/content/common/channel/logos/brk-news.png", "720p"),
                ("High News India", "https://highmedia.livebox.co.in/HIGHNEWShls/LIVE.m3u8", "https://pbs.twimg.com/profile_images/1104268476858937351/USUT4ykS_400x400.png", "480p")
            };

            foreach (var item in national)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Indian National",
                    Language = "Hindi",
                    Country = "IN",
                    IsActive = true
                });
            }
        }

        private void AddTamilRegionalChannels()
        {
            var tamil = new[]
            {
                ("Puthiya Thalaimurai HD", "https://mumt07.tangotv.in/zHjX9OFlPUTHIYAEXPRESS/index.m3u8", "https://dtil.tmsimg.com/assets/s143692_ld_h15_aa.png?lock=720x540", "1080p"),
                ("Polimer News HD", "https://segment.yuppcdn.net/110322/polimernews/playlist.m3u8", "https://dtil.tmsimg.com/assets/s143664_ld_h9_aa.png?lock=720x540", "720p"),
                ("Thanthi TV HD", "https://streams.tangotv.in/THANTHITV/ORIGIN/index.m3u8", "https://dtil.tmsimg.com/assets/s144000_ld_h15_aa.png?lock=720x540", "1080p"),
                ("Thanthi One HD", "https://mumt07.tangotv.in/zHjX9OFlTHANTHIONE/index.m3u8", "https://dtil.tmsimg.com/assets/s158791_ld_h15_aa.png?lock=720x540", "720p"),
                ("News 7 Tamil HD", "https://segment.yuppcdn.net/240122/news7/playlist.m3u8", "https://jiotvimages.cdn.jio.com/dare_images/images/News7_Tamil.png", "720p"),
                ("Sathiyam TV HD", "https://mumt07.tangotv.in/zHjX9OFlSATHIYAMTV/index.m3u8", "https://dtil.tmsimg.com/assets/s143765_ld_h15_aa.png?lock=720x540", "1080p"),
                ("Malai Murasu Seithigal", "https://streams.tangotv.in/MALAIMURASUSEITHIGAL/ORIGIN/index.m3u8", "https://i.imgur.com/OC5TQxp.png", "720p"),
                ("Tamil Janam TV", "https://mumt01.tangotv.in/O5aw8Zn3JANAMTVTAMIL/index.m3u8", "https://jiotvimages.cdn.jio.com/dare_images/images/Janam_Tamil.png", "720p"),
                ("Kalaignar TV HD", "https://segment.yuppcdn.net/240122/kalaignartv/playlist.m3u8", "https://ltsk-cdn.s3.eu-west-1.amazonaws.com/jumpstart/Temp_Live/cdn/HLS/Channel/transparentImages/Kalaignar%20TV.png", "576p"),
                ("DD Podhigai Tamil HD", "https://d2lk5u59tns74c.cloudfront.net/out/v1/c313674ffced4c9a90f1bba436df2b9b/index.m3u8", "https://upload.wikimedia.org/wikipedia/commons/d/dd/DD_Podhigai_logo.png", "1080p"),
                ("Aaryaa TV Tamil HD", "https://stream.ottlive.co.in/aryatvtamil/index.m3u8", "https://dtil.tmsimg.com/assets/s143769_ld_h15_aa.png?lock=720x540", "1080p"),
                ("Aaseervatham TV", "https://mumt04.tangotv.in/m18aqlK4AASEERVATHAMTV/index.m3u8", "https://jiotvimages.cdn.jio.com/dare_images/images/Win_TV.png", "576p"),
                ("Aastha Tamil", "https://aasthaott.akamaized.net/110923/smil:aasthatamil.smil/playlist.m3u8", "https://i.imgur.com/news18tamil.png", "480p")
            };

            foreach (var item in tamil)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Tamil Regional",
                    Language = "Tamil",
                    Country = "IN",
                    IsActive = true
                });
            }
        }

        private void AddTeluguRegionalChannels()
        {
            var telugu = new[]
            {
                ("TV9 Telugu HD", "https://dyjmyiv3bp2ez.cloudfront.net/pub-iotv9telcmjhcs/liveabr/playlist.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/LIVETV_LIVETVCHANNEL_TV9_TELUGU_NEWS/images/LOGO_HD/image.png", "1080p"),
                ("NTV Telugu Live HD", "https://mumbai-edge.smartplaytv.in/NTVTelugu/index.m3u8", "https://i.imgur.com/ntvtelugu.png", "1080p"),
                ("10TV Telugu HD", "https://mumbai-edge.smartplaytv.in/10TV/index.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/LIVETV_LIVETVCHANNEL_10TV/images/LOGO_HD/image.png", "1080p"),
                ("ABN Andhra Jyothi HD", "https://mumbai-edge.smartplaytv.in/ABNAJ/index.m3u8", "https://dtil.tmsimg.com/assets/s142517_ld_h15_aa.png?lock=720x540", "1080p"),
                ("V6 News Telangana HD", "https://yuppmedtaorire.akamaized.net/v1/master/a0d007312bfd99c47f76b77ae26b1ccdaae76cb1/v6news_nim_https/140622/v6news/playlist.m3u8", "https://dtil.tmsimg.com/assets/s144047_ld_h15_aa.png?lock=720x540", "1080p"),
                ("6 TV Telugu", "https://yuppparoriglin.akamaized.net/181224/smil:6tv.smil/playlist.m3u8?hdnts=st=1735898689~exp=1835898688~acl=*~hmac=f5fe24724fe05481e3841f9eb5ab8efdee0a3dd83645ae9dcf45703f525bab7b", "https://i.imgur.com/saptagiri.png", "576p"),
                ("99TV Telugu News", "https://cdn.pishow.tv/ott/live/1211/master.m3u8", "https://i.imgur.com/99tv.png", "720p"),
                ("Aastha Telugu", "https://mumbai-edge.smartplaytv.in/AasthaTelugu/index.m3u8", "https://i.imgur.com/yadagiri.png", "576p")
            };

            foreach (var item in telugu)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Telugu Regional",
                    Language = "Telugu",
                    Country = "IN",
                    IsActive = true
                });
            }
        }

        private void AddMalayalamRegionalChannels()
        {
            var malayalam = new[]
            {
                ("Asianet News Kerala HD", "https://d35j504z0x2vu2.cloudfront.net/v1/manifest/0bc8e83ba53342a809b45688c9a8775735e5e64d/asianet-news/548326e5-47e9-44e2-9d3e-e63d3c8c7d5c/1.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/LIVETV_LIVETVCHANNEL_ASIANET_NEWS/images/LOGO_HD/image.png", "1080p"),
                ("24 News Kerala HD", "https://mumt07.tangotv.in/zHjX9OFlTWENTYFOURNEWS/index.m3u8", "https://sund-images.sunnxt.com/202222/300x300_24News_202222_d63feca0-79ae-47ea-b75a-66c17d456f4c.png", "576p"),
                ("Amrita TV Kerala", "https://ddash74r36xqp.cloudfront.net/master.m3u8", "https://i.imgur.com/manorama.png", "720p"),
                ("DD Malayalam Kerala HD", "https://d2lk5u59tns74c.cloudfront.net/out/v1/c313674ffced4c9a90f1bba436df2b9b/index.m3u8", "https://i.postimg.cc/BZjmjJJs/DD-Malayalam.png", "1080p")
            };

            foreach (var item in malayalam)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Malayalam Regional",
                    Language = "Malayalam",
                    Country = "IN",
                    IsActive = true
                });
            }
        }

        private void AddKannadaRegionalChannels()
        {
            var kannada = new[]
            {
                ("DD Chandana Karnataka HD", "https://d2lk5u59tns74c.cloudfront.net/out/v1/0811cd8c37ca4c409d5385a6cd2fa18b/index.m3u8", "https://i.imgur.com/ddchandana.png", "1080p"),
                ("Aastha Kannada", "https://aasthaott.akamaized.net/110923/smil:aasthakannada.smil/playlist.m3u8", "https://i.imgur.com/0H1HObK.png", "480p"),
                ("Ayush TV Kannada HD", "https://stream.ottlive.co.in/ayushtv/index.m3u8", "https://i.imgur.com/publictv.png", "1080p")
            };

            foreach (var item in kannada)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Kannada Regional",
                    Language = "Kannada",
                    Country = "IN",
                    IsActive = true
                });
            }
        }

        private void AddBengaliRegionalChannels()
        {
            var bengali = new[]
            {
                ("ABP Ananda Bengal HD", "https://d2l4ar6y3mrs4k.cloudfront.net/live-streaming/ananda-livetv/master.m3u8", "https://dtil.tmsimg.com/assets/s142518_ld_h15_aa.png?lock=720x540", "1080p"),
                ("DD Bangla West Bengal HD", "https://d2lk5u59tns74c.cloudfront.net/out/v1/07f8b9e69c734b4395e5b3060fcde5e2/index.m3u8", "https://i.imgur.com/ddbangla.png", "1080p"),
                ("AKD Calcutta News Live HD", "https://live.legitpro.co.in/cnnnews/index.m3u8", "https://ltsk-cdn.s3.eu-west-1.amazonaws.com/jumpstart/Temp_Live/cdn/HLS/Channel/transparentImages/Calcutta%20News.png", "1080p"),
                ("Aamar Bangla TV", "https://app.ncare.live/c3VydmVyX8RpbEU9Mi8xNy8yMDE0GIDU6RgzQ6NTAgdEoaeFzbF92YWxIZTO0U0ezN1IzMyfvcGVMZEJCTEFWeVN3PTOmdFsaWRtaW51aiPhnPTI/amarbanglatv.stream/playlist.m3u8", "https://i.imgur.com/aamar.png", "720p"),
                ("Ananda Barta News", "https://account19.livebox.co.in/Anandabarta2hls/live.m3u8", "https://i.imgur.com/news18bangla.png", "720p")
            };

            foreach (var item in bengali)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Bengali Regional",
                    Language = "Bengali",
                    Country = "IN",
                    IsActive = true
                });
            }
        }

        private void AddMarathiRegionalChannels()
        {
            var marathi = new[]
            {
                ("ABP Majha Maharashtra HD", "https://mumbai-edge.smartplaytv.in/ABPMajha/index.m3u8", "https://dtil.tmsimg.com/assets/s142521_ld_h15_aa.png?lock=720x540", "576p"),
                ("9X Jhakaas Marathi Hits HD", "https://amg01281-9xmediapvtltd-9xjhakaas-samsungin-ci2cs.amagi.tv/playlist/amg01281-9xmediapvtltd-9xjhakaas-samsungin/playlist.m3u8", "https://dtil.tmsimg.com/assets/s90012_ld_h15_aa.png?lock=720x540", "1080p"),
                ("Anand Wari TV Maharashtra", "https://mumt03.tangotv.in/Dsly5z3HANANDWARITV/index.m3u8", "https://i.imgur.com/ddsahyadri.png", "576p")
            };

            foreach (var item in marathi)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Marathi Regional",
                    Language = "Marathi",
                    Country = "IN",
                    IsActive = true
                });
            }
        }

        private void AddPunjabiGujaratiChannels()
        {
            var pg = new[]
            {
                ("ABP Asmita Gujarat HD", "https://d2l4ar6y3mrs4k.cloudfront.net/live-streaming/asmita-livetv/master.m3u8", "https://upload.wikimedia.org/wikipedia/commons/thumb/2/20/ABP_Asmita_logo.svg/500px-ABP_Asmita_logo.svg.png", "1080p", "Gujarati"),
                ("ABP Sanjha Punjab HD", "https://d2e5i41q974lfs.cloudfront.net/out/v1/0b7f8ecb3c2e4ef19a0a0df2542a1293/index.m3u8", "https://upload.wikimedia.org/wikipedia/commons/d/d6/ABP_News_Logo.png", "1080p", "Punjabi"),
                ("9X Tashan Punjabi Hits HD", "https://amg01281-9xmediapvtltd-9xtashan-samsungin-xz1sd.amagi.tv/playlist/amg01281-9xmediapvtltd-9xtashan-samsungin/playlist.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/LIVETV_LIVETVCHANNEL_9X_TASHAN/images/LOGO_HD/image.png", "1080p", "Punjabi"),
                ("Aastha Gujarati", "https://mumt04.tangotv.in/m18aqlK4AASTHAGUJARATI/index.m3u8", "https://i.imgur.com/sandesh.png", "576p", "Gujarati"),
                ("Apna Punjab TV", "https://plus.gigabitcdn.net/live-stream/apna-punjab-H3sE/playlist.m3u8", "https://i.imgur.com/ptcnews.png", "720p", "Punjabi"),
                ("Andy Haryana", "https://mumt03.tangotv.in/Dsly5z3HANDYHARYANA/index.m3u8", "https://i.imgur.com/ddgirnar.png", "576p", "Hindi")
            };

            foreach (var item in pg)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Punjabi & Gujarati",
                    Language = item.Item5,
                    Country = "IN",
                    IsActive = true
                });
            }
        }

        private void AddWorldNewsChannels()
        {
            var world = new[]
            {
                ("11 Alive News Live HD", "https://tegna-11alive.amagi.tv/playlist.m3u8", "https://i.imgur.com/abcnews.png", "1080p"),
                ("BBC News Global HD", "https://d25t0q7vgsuwwh.cloudfront.net/out/v1/08cb701b87a840cba7a95079a4a7ce8b/index.m3u8", "https://i.imgur.com/dwnews.png", "1080p"),
                ("2GB News Australia HD", "https://2gblive.akamaized.net/hls/live/2033805/2GB/index.m3u8", "https://i.imgur.com/skynews.png", "1080p"),
                ("3AW News Australia HD", "https://3awlive.akamaized.net/hls/live/2032295/3AW/index.m3u8", "https://i.imgur.com/reuters.png", "1080p"),
                ("3CatInfo News HD", "https://directes-tv-int.3catdirectes.cat/live-origin/canal324-hls/master.m3u8", "https://i.imgur.com/cna.png", "1080p"),
                ("Arise News Global", "https://cdn.pishow.tv/ott/live/1004/master.m3u8", "https://i.imgur.com/euronews.png", "576p"),
                ("DW English News HD", "https://dwamdstream102.akamaized.net/hls/live/2015525/dwstream102/index.m3u8", "https://i.imgur.com/dwnews.png", "1080p"),
                ("France 24 English HD", "https://static.france24.com/live/F24_EN_LO_HLS/live_web.m3u8", "https://i.imgur.com/france24.png", "1080p"),
                ("24 Kanal World HD", "https://cdn15.live-tv.cloud/ua_infinitas_tv/news24-abr/playlist.m3u8", "https://pbs.twimg.com/profile_images/1498285886714298374/EMSJzC-0_400x400.jpg", "1080p")
            };

            foreach (var item in world)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "World News",
                    Language = "English",
                    Country = "GLOBAL",
                    IsActive = true
                });
            }
        }

        private void AddSportsChannels()
        {
            var sports = new[]
            {
                ("FTF Sports Network HD", "https://amg01007-cinedigm-ftfsports-vizio-936x8.amagi.tv/playlist.m3u8", "https://i.imgur.com/ftf.png", "1080p"),
                ("SportsGrid Live HD", "https://amg01007-cinedigm-sportsgrid-vizio-827m6.amagi.tv/playlist.m3u8", "https://i.imgur.com/sportsgrid.png", "720p"),
                ("World Poker Tour HD", "https://amg01007-cinedigm-worldpokertour-vizio-482w5.amagi.tv/playlist.m3u8", "https://i.imgur.com/wpt.png", "720p"),
                ("Surf Now TV HD", "https://amg01007-cinedigm-surfnowtv-vizio-315j2.amagi.tv/playlist.m3u8", "https://i.imgur.com/redbull.png", "1080p"),
                ("Impact Wrestling HD", "https://amg01007-cinedigm-impactwrestling-vizio-632k1.amagi.tv/playlist.m3u8", "https://i.imgur.com/vlKPZHR.png", "720p"),
                ("Outdoor America HD", "https://amg01007-cinedigm-outdooramerica-vizio-518v9.amagi.tv/playlist.m3u8", "https://i.imgur.com/outdoor.png", "720p"),
                ("Bein Sports Xtra", "https://amg01007-cinedigm-beinsportsxtra-vizio-764z2.amagi.tv/playlist.m3u8", "https://i.imgur.com/edge.png", "720p"),
                ("Racing America HD", "https://amg01007-cinedigm-racingamerica-vizio-109y4.amagi.tv/playlist.m3u8", "https://provider-static.plex.tv/epg/cms/production/dd4e5871-4347-49e3-9ea1-a8770ea9c518/RACER_Intl_logo_1500x1000.png", "720p"),
                ("ACC Digital Network", "https://raycom-accdn-firetv.amagi.tv/playlist.m3u8", "https://i.imgur.com/motor.png", "1080p")
            };

            foreach (var item in sports)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Sports & Action",
                    Language = "English",
                    Country = "GLOBAL",
                    IsActive = true
                });
            }
        }

        private void AddMovieChannels()
        {
            var movies = new[]
            {
                ("Cinevault Classics HD", "https://amg00086-corusentertainment-cinevaultclassics-samsungca-42353.amagi.tv/playlist.m3u8", "https://i.imgur.com/xaCyyDd.png", "1080p"),
                ("FilmRise Action HD", "https://amg01007-cinedigm-filmriseaction-vizio-858w3.amagi.tv/playlist.m3u8", "https://i.imgur.com/filmrise.png", "720p"),
                ("FilmRise Classic TV", "https://amg01007-cinedigm-filmriseclassic-vizio-w7d2b.amagi.tv/playlist.m3u8", "https://i.imgur.com/retro.png", "720p"),
                ("Movie Central HD", "https://amg00086-corusentertainment-moviecentral-samsungca-28310.amagi.tv/playlist.m3u8", "https://i.imgur.com/vFOgVbG.png", "1080p"),
                ("B4U Movies HD", "https://amg01281-b4usea-b4umovies-samsungin-wndj6.amagi.tv/playlist/amg01281-b4usea-b4umovies-samsungin/playlist.m3u8", "https://i.imgur.com/pluto.png", "1080p"),
                ("B4U Kadak HD", "https://amg01281-b4usea-b4ukadak-samsungin-085h9.amagi.tv/playlist/amg01281-b4usea-b4ukadak-samsungin/playlist.m3u8", "https://i.imgur.com/shout.png", "1080p"),
                ("B4U Bhojpuri HD", "https://amg01281-b4usea-b4ubhojpuri-samsungin-4w7q0.amagi.tv/playlist/amg01281-b4usea-b4ubhojpuri-samsungin/playlist.m3u8", "https://i.imgur.com/plex.png", "1080p"),
                ("Free Movies Plus", "https://freemoviesplus-klowdtv.amagi.tv/playlist.m3u8", "https://i.imgur.com/scifi.png", "720p"),
                ("Hallmark Movies & More", "https://amg01007-cinedigm-hallmarkmovies-vizio-186k5.amagi.tv/playlist.m3u8", "https://i.imgur.com/xaCyyDd.png", "720p"),
                ("All Time Movies", "https://tvsen6.aynaott.com/a2cKGQtB/index.m3u8", "https://d229kpbsb5jevy.cloudfront.net/yuppfast/content/common/channel/logos/thalaa-tv.png", "576p"),
                ("4ever Cinema HD", "http://stream.mcquack.net/258/index.m3u8", "https://i.imgur.com/vFOgVbG.png", "1080p"),
                ("Plex Free Movies HD", "https://d3o593mz1glx8d.cloudfront.net/OuterSphere_US.m3u8", "https://i.imgur.com/plex.png", "1080p"),
                ("Retro Cinema Classics", "https://retro-samsung.amagi.tv/playlist.m3u8", "https://i.imgur.com/retro.png", "720p"),
                ("Thalaa TV Tamil Cinema HD", "https://streams2.sofast.tv/v1/master/611d79b11b77e2f571934fd80ca1413453772ac7/2069c593-3c07-4d62-9d44-746be5c3a5d6/manifest.m3u8", "https://d229kpbsb5jevy.cloudfront.net/yuppfast/content/common/channel/logos/thalaa-tv.png", "1080p"),
                ("Tolly TV Telugu Cinema HD", "https://streams2.sofast.tv/v1/master/611d79b11b77e2f571934fd80ca1413453772ac7/13b5735d-52c3-4be0-853d-e0ec0127e708/manifest.m3u8", "https://d229kpbsb5jevy.cloudfront.net/yuppfast/content/common/channel/logos/aumnrs.png", "1080p")
            };

            foreach (var item in movies)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Movies & Cinema",
                    Language = "English",
                    Country = "GLOBAL",
                    IsActive = true
                });
            }
        }

        private void AddKidsChannels()
        {
            var kids = new[]
            {
                ("ABC Kids Australia", "https://c.mjh.nz/abc-kids.m3u8", "https://i.imgur.com/abckids.png", "1080p"),
                ("3ABN Kids TV", "https://3abn.bozztv.com/3abn2/Kids_live/smil:Kids_live.smil/playlist.m3u8", "https://i.imgur.com/z3npqO1.png", "720p"),
                ("4 Fun Kids HD", "https://stream.4fun.tv:8889/hls/4fk.m3u8", "https://i.imgur.com/4funkids.png", "1080p"),
                ("PBS Kids Live Stream HD", "https://livestream.pbskids.org/out/v1/14507d931bbe48a69287e4850e53443c/est.m3u8", "https://i.imgur.com/fg8c9WK.png", "720p"),
                ("Duck TV Toon Network", "https://stream.4fun.tv:8889/hls/4fk.m3u8", "https://i.imgur.com/ducktv.png", "1080p"),
                ("Toon Goggles Kids HD", "https://lightning-fnf-samsungaus.amagi.tv/playlist.m3u8", "https://i.imgur.com/toongoggles.png", "1080p"),
                ("Kartoon Channel! HD", "https://lightning-fnf-samsungaus.amagi.tv/playlist.m3u8", "https://i.imgur.com/1luzP3T.png", "1080p"),
                ("WOW Kidz Animation HD", "https://yuppparoriglin.akamaized.net/181224/smil:wowkidzhindi.smil/playlist.m3u8?hdnts=st=1735898689~exp=1835898688~acl=*~hmac=f5fe24724fe05481e3841f9eb5ab8efdee0a3dd83645ae9dcf45703f525bab7b", "https://d229kpbsb5jevy.cloudfront.net/yupptvnew/content/common/channel/logos/mjvezt.png", "720p")
            };

            foreach (var item in kids)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Kids & Family",
                    Language = "English",
                    Country = "GLOBAL",
                    IsActive = true
                });
            }
        }

        private void AddAdultLateNightChannels()
        {
            var adult = new[]
            {
                ("FashionTV Midnight Secrets HD", "https://fash1043.cloudycdn.services/lte/fash1043/playlist.m3u8", "https://i.imgur.com/fashiontv.png", "1080p"),
                ("Bikini Destination HD", "https://amg00086-corusentertainment-bikinidestination-rokuus-v3y43.amagi.tv/playlist.m3u8", "https://i.imgur.com/bikini.png", "1080p"),
                ("Redlight Late Night TV", "https://turnerlive.warnermediacdn.com/hls/live/2023185/aswest/noslate/VIDEO_1_5128000.m3u8", "https://i.imgur.com/redlight.png", "1080p"),
                ("Playboy TV Previews HD", "https://jmp2.uk/plu-67f68bd2135aeda9ddf0ef54.m3u8", "https://i.imgur.com/playboy.png", "1080p"),
                ("Babes & Glamour TV", "https://linear-897.frequency.stream/dist/cineverse/897/hls/master/playlist.m3u8", "https://i.imgur.com/babes.png", "1080p"),
                ("Hustler TV Preview Lounge", "https://lotus.stingray.com/manifest/ose-104ads-montreal/samsungtvplus/master.m3u8", "https://i.imgur.com/hustler.png", "1080p"),
                ("Night Flight Plus TV", "https://d4whmvwm0rdvi.cloudfront.net/10007/99993017/hls/master.m3u8?ads.xumo_channelId=99993017", "https://i.imgur.com/nightflight.png", "1080p"),
                ("Midnight Pulp Cult Live", "https://linear-897.frequency.stream/dist/cineverse/897/hls/master/playlist.m3u8", "https://provider-static.plex.tv/a/epg/channels/logos/gracenote/a1a2cdc3d6f0714c586e9959732580c7.png", "1080p")
            };

            foreach (var item in adult)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Adults / Late Night",
                    Language = "English",
                    Country = "GLOBAL",
                    IsActive = true
                });
            }
        }

        private void AddMusicChannels()
        {
            var music = new[]
            {
                ("9X Jalwa Retro Hits HD", "https://wiselp.wiseplayout.com/9X_Jalwa/master.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/LIVETV_LIVETVCHANNEL_9X_JALWA/images/LOGO_HD/image.png", "1080p"),
                ("9XM Bollywood Hits HD", "https://9xjio.wiseplayout.com/9XM/master.m3u8", "https://xstreamcp-assets-msp.streamready.in/assets/LIVETV/LIVECHANNEL/LIVETV_LIVETVCHANNEL_9XM/images/LOGO_HD/image.png", "1080p"),
                ("MTV Beats & Global Hits", "https://amg01117-amg01117c1-amgplt0029.playout.now3.amagi.tv/playlist/amg01117-amg01117c1-amgplt0029/playlist.m3u8", "https://i.imgur.com/mtvbeats.png", "1080p"),
                ("Vevo Pop Hits Live HD", "https://lotus.stingray.com/manifest/ose-104ads-montreal/samsungtvplus/master.m3u8", "https://i.imgur.com/vevo.png", "1080p"),
                ("Clubbing TV Electronic HD", "https://clubbingtv-samsung.amagi.tv/playlist.m3u8", "https://i.imgur.com/clubbing.png", "1080p"),
                ("Deluxe Music Europe HD", "https://deluxemusic.akamaized.net/hls/live/201245/deluxe/master.m3u8", "https://i.imgur.com/deluxe.png", "1080p"),
                ("Trace Urban Hits HD", "https://trace-samsung.amagi.tv/playlist.m3u8", "https://i.imgur.com/trace.png", "1080p"),
                ("K-Pop TV Live HD", "https://m1b2.worldcast.tv/dancetelevisionone/2/dancetelevisionone.m3u8", "https://i.imgur.com/kpop.png", "1080p"),
                ("7S Music Hits HD", "https://cdn.pishow.tv/ott/live/1257/master.m3u8", "https://i.imgur.com/7smusic.png", "576p"),
                ("XITE Just Chill HD", "https://dvnftgdlbnemm.cloudfront.net/XITE_Just_Chill.m3u8", "https://provider-static.plex.tv/epg/cms/production/927dd614-017d-49dd-9eb9-0ed79d09084e/xitejustchill_logo_light_-_Arlette_FiranRDX.png", "1080p")
            };

            foreach (var item in music)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Music & Entertainment",
                    Language = "Various",
                    Country = "GLOBAL",
                    IsActive = true
                });
            }
        }

        private void AddScienceTechChannels()
        {
            var science = new[]
            {
                ("NASA TV HD (24/7)", "https://ntv1.akamaized.net/hls/live/2014075/NASA-NTV1-HLS/master.m3u8", "https://www.nasa.gov/sites/default/files/thumbnails/image/nasa-logo-web-rgb.png", "1080p"),
                ("NASA Media Channel HD", "https://ntv2.akamaized.net/hls/live/2014076/NASA-NTV2-HLS/master.m3u8", "https://www.nasa.gov/sites/default/files/thumbnails/image/nasa-logo-web-rgb.png", "1080p"),
                ("Spaceflight Now HD", "https://stream.spacelaunchlive.com/live/space.m3u8", "https://i.imgur.com/spacelaunch.png", "1080p"),
                ("TWiT.tv Live Tech", "https://twit.live/live.m3u8", "https://i.imgur.com/twit.png", "1080p"),
                ("TechTV Classic & Innovation", "https://techtv-samsung.amagi.tv/playlist.m3u8", "https://i.imgur.com/techtv.png", "1080p"),
                ("Deep Sky TV (ISS Live Earth Views)", "https://play.freeviewlive.com/iss_earth_view/master.m3u8", "https://i.imgur.com/iss.png", "1080p")
            };

            foreach (var item in science)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Science & Tech",
                    Language = "English",
                    Country = "GLOBAL",
                    IsActive = true
                });
            }
        }

        private void AddDocumentaryChannels()
        {
            var doc = new[]
            {
                ("National Geo Wild Free Feed", "http://103.185.24.134:3001/NATGEO-WILD-HD/index.m3u8", "https://upload.wikimedia.org/wikipedia/commons/thumb/2/27/National_Geographic_Wild_logo.svg/960px-National_Geographic_Wild_logo.svg.png", "1080p"),
                ("CGTN Documentary HD", "https://amg00405-rakutentv-cgtndocumentary-rakuten-0ql8j.amagi.tv/master.m3u8", "https://i.imgur.com/JHv0WxM.png", "1080p"),
                ("Real Stories Live HD", "https://amg00735-videosolutionsa-wedobigdocs-ono-0urlz.amagi.tv/playlist/amg00735-videosolutionsa-wedobigdocs-ono/playlist.m3u8", "https://provider-static.plex.tv/epg/cms/production/49cf6309-0a4a-4d39-bdcc-fa48e4ef84a7/WEDOBIGSTORIES_LIGHTTHEME_-_Sorin_Stoian.png", "1080p"),
                ("Timeline History Documentary", "https://amg01775-amg01775c1-amgplt0343.playout.now3.amagi.tv/playlist/amg01775-amg01775c1-amgplt0343/playlist.m3u8", "https://i.imgur.com/EbMK3Ph.png", "1080p"),
                ("DocuBay TV HD Previews", "https://cc-mgr91yrk4pehy.akamaized.net/v1/master/3722c60a815c199d9c0ef36c5b73da68a62b09d1/cc-mgr91yrk4pehy/playlist.m3u8", "https://i.imgur.com/OGyeLLR.png", "1080p"),
                ("INWILD Nature HD", "https://amg00861-terninternation-inwild-samsunguk-w5wic.amagi.tv/playlist/amg00861-terninternation-inwild-samsunguk/playlist.m3u8", "https://cdn.uc.assets.prezly.com/92770c71-9d6f-400f-8311-38e9cfec52c2/InWild_landscape-green.png", "1080p"),
                ("WildEarth Safari HD", "https://dqga3jatxofgx.cloudfront.net/WildEarth.m3u8", "https://i.imgur.com/Vhv5gjN.png", "1080p")
            };

            foreach (var item in doc)
            {
                _channels.Add(new LiveTvChannel
                {
                    Name = item.Item1,
                    StreamUrl = item.Item2,
                    LogoUrl = item.Item3,
                    Resolution = item.Item4,
                    Category = "Documentary",
                    Language = "English",
                    Country = "GLOBAL",
                    IsActive = true
                });
            }
        }
    }
}
