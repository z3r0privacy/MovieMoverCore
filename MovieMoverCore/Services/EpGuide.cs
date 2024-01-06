using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MovieMoverCore.Services
{
    public interface IEpGuide
    {
        Task<(List<EpisodeInfo> upcoming, EpisodeInfo nextAdding)> GetEpisodesAsync(EpisodeInfo newestAvailable);
    }

    public class EpGuidesCom : IEpGuide
    {
        private class EpCsv
        {
            public EpCsv() { }

            public class CsvNumberConverter : ITypeConverter
            {
                public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
                {
                    if (int.TryParse(text, out var res))
                    {
                        return res;
                    }
                    return 0;
                }

                public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
                {
                    return value.ToString();
                }
            }

            private static string _dateFormat = "dd MMM yy";
            private DateTime _airDate;
            // number,season,episode,airdate,title,tvmaze link
            [Name("number")]
            [TypeConverter(typeof(CsvNumberConverter))]
            public int Number { get; set; }
            [Name("season")]
            public int Season { get; set; }
            [Name("episode")]
            public int Episode { get; set; }
            [Name("airdate")]
            public string AirdateStr { get; set; }
            [Name("title")]
            public string Title { get; set; }
            [Name("tvmaze link")]
            public string TvmazeLink { get; set; }

            [Ignore]
            public DateTime AirDate => _airDate.AddDays(1);
            [Ignore]
            public bool CanParseDate => DateTime.TryParseExact(AirdateStr, _dateFormat, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out _airDate);
        }

        private ISettings _settings;
        private ILogger<EpGuidesCom> _logger;
        private ICache<EpGuidesCom, EpisodeInfo, (List<EpisodeInfo> upcoming, EpisodeInfo nextAdding)> _cache;
        private static readonly SemaphoreSlim ALLSHOWS_LOCK = new SemaphoreSlim(1, 1);
        private Dictionary<string, int> _mazeCache;


        public EpGuidesCom(ISettings settings, ILogger<EpGuidesCom> logger, ICache<EpGuidesCom, EpisodeInfo, (List<EpisodeInfo> upcoming, EpisodeInfo nextAdding)> cache)
        {
            _settings = settings;
            _logger = logger;
            _cache = cache;
            _mazeCache = new Dictionary<string, int>();
        }

        private async Task<string> AcquireCsv(string url)
        {
            var wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:96.0) Gecko/20100101 Firefox/96.0");
            var htmlresponse = await wc.DownloadStringTaskAsync(url);
            var markerStart = "<pre>";
            var markerStop = "</pre>";
            var start = htmlresponse.IndexOf(markerStart) + markerStart.Length;
            var stop = htmlresponse.IndexOf(markerStop, start);
            if (start == -1 || stop == -1)
            {
                return htmlresponse;
            }
            return htmlresponse.Substring(start, stop - start).Trim();
        }

        private async Task<int> GetShowMaze(string name)
        {
            name = name.ToLower();
            await ALLSHOWS_LOCK.WaitAsync();
            try
            {
                if (!_mazeCache.ContainsKey(name))
                {
                    _logger.LogInformation("Download AllShows.txt file");
                    var csv_raw = await AcquireCsv(_settings.EpGuide_AllShows);
                    var csv_config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        Delimiter = ",",
                        HasHeaderRecord = false
                    };
                    using var csv = new CsvReader(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(csv_raw))), csv_config);
                    // csv.Read(); // skip header row
                    while (csv.Read())
                    {
                        var title = csv.GetField<string>(1);
                        var _mazeid = csv.GetField<string>(3);
                        int mazeid;
                        if (!int.TryParse(_mazeid, out mazeid))
                        {
                            mazeid = 0;
                        }
                        _mazeCache[title.ToLower()] = mazeid;
                    }
                }
                return _mazeCache[name];
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read maze ids");
                return -1;
            }
            finally
            {
                ALLSHOWS_LOCK.Release();
            }
        }

        public async Task<(List<EpisodeInfo> upcoming, EpisodeInfo nextAdding)> GetEpisodesAsync(EpisodeInfo newestAvailable)
        {
            if (_cache.Retrieve(newestAvailable, out var result))
            {
                return result;
            }

            var upcoming = new List<EpisodeInfo>();
            EpisodeInfo nextAdding = null;

            var mazeid = await GetShowMaze(newestAvailable.Series.EpGuidesName);
            if (mazeid < 0)
            {
                _logger.LogWarning($"Could not get mazeid for show {newestAvailable.Series.EpGuidesName}");
                return (new List<EpisodeInfo>(), null);
            }
            var strData = await AcquireCsv(string.Format(_settings.EpGuide_SearchLink, mazeid));

            //var strData = data.Skip(start).Take(end - start).Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
            using var memStream = new MemoryStream(Encoding.UTF8.GetBytes(strData));
            using var sr = new StreamReader(memStream);
            try
            {
                using var csv = new CsvReader(sr, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ","
                });
                var records = csv.GetRecords<EpCsv>();

                foreach (var r in records)
                {
                    if (r.CanParseDate)
                    {
                        if (nextAdding == null)
                        {
                            if (r.Season > newestAvailable.Season ||
                                (r.Season == newestAvailable.Season && r.Episode > newestAvailable.Episode))
                            {
                                nextAdding = new EpisodeInfo
                                {
                                    AirDate = r.AirDate,
                                    Episode = r.Episode,
                                    Season = r.Season,
                                    Series = newestAvailable.Series,
                                    Title = r.Title
                                };
                            }
                        }

                        if (r.AirDate >= DateTime.Now.Date)
                        {
                            upcoming.Add(new EpisodeInfo
                            {
                                AirDate = r.AirDate,
                                Episode = r.Episode,
                                Season = r.Season,
                                Series = newestAvailable.Series,
                                Title = r.Title
                            });
                        }
                    }
                }

                _cache.UpdateOrAdd(newestAvailable, (upcoming, nextAdding), _cache.EoD);
                return (upcoming, nextAdding);

            } catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed for series {newestAvailable.Series.Name}");
                throw;
            }
        }
    }
}


/*

 
<!DOCTYPE html>
<html lang="en">
	<head>
		<meta name="viewport" content="width=device-width, initial-scale=1, user-scalable=yes" />
		<meta charset="utf-8" />
		<title>List Output</title>
		<link href="includes/style.css" rel="stylesheet" type="text/css" />
		
	</head>

	

	<body>
		<pre>
number,season,episode,airdate,title,tvmaze link
1,1,1,24 Sep 13,"Pilot","https://www.tvmaze.com/episodes/1643/marvels-agents-of-shield-1x01-pilot"
2,1,2,01 Oct 13,"0-8-4","https://www.tvmaze.com/episodes/1644/marvels-agents-of-shield-1x02-0-8-4"
3,1,3,08 Oct 13,"The Asset","https://www.tvmaze.com/episodes/1645/marvels-agents-of-shield-1x03-the-asset"
4,1,4,15 Oct 13,"Eye Spy","https://www.tvmaze.com/episodes/1646/marvels-agents-of-shield-1x04-eye-spy"
5,1,5,22 Oct 13,"Girl in the Flower Dress","https://www.tvmaze.com/episodes/1647/marvels-agents-of-shield-1x05-girl-in-the-flower-dress"
6,1,6,05 Nov 13,"FZZT","https://www.tvmaze.com/episodes/1648/marvels-agents-of-shield-1x06-fzzt"
7,1,7,12 Nov 13,"The Hub","https://www.tvmaze.com/episodes/1649/marvels-agents-of-shield-1x07-the-hub"
8,1,8,19 Nov 13,"The Well","https://www.tvmaze.com/episodes/1650/marvels-agents-of-shield-1x08-the-well"
9,1,9,26 Nov 13,"Repairs","https://www.tvmaze.com/episodes/1651/marvels-agents-of-shield-1x09-repairs"
10,1,10,10 Dec 13,"The Bridge","https://www.tvmaze.com/episodes/1652/marvels-agents-of-shield-1x10-the-bridge"
11,1,11,07 Jan 14,"The Magical Place","https://www.tvmaze.com/episodes/1653/marvels-agents-of-shield-1x11-the-magical-place"
12,1,12,14 Jan 14,"Seeds","https://www.tvmaze.com/episodes/1654/marvels-agents-of-shield-1x12-seeds"
13,1,13,04 Feb 14,"T.R.A.C.K.S.","https://www.tvmaze.com/episodes/1655/marvels-agents-of-shield-1x13-tracks"
14,1,14,04 Mar 14,"T.A.H.I.T.I.","https://www.tvmaze.com/episodes/1656/marvels-agents-of-shield-1x14-tahiti"
15,1,15,11 Mar 14,"Yes Men","https://www.tvmaze.com/episodes/1657/marvels-agents-of-shield-1x15-yes-men"
16,1,16,01 Apr 14,"End of the Beginning","https://www.tvmaze.com/episodes/1658/marvels-agents-of-shield-1x16-end-of-the-beginning"
17,1,17,08 Apr 14,"Turn, Turn, Turn","https://www.tvmaze.com/episodes/1659/marvels-agents-of-shield-1x17-turn-turn-turn"
18,1,18,15 Apr 14,"Providence","https://www.tvmaze.com/episodes/1660/marvels-agents-of-shield-1x18-providence"
19,1,19,22 Apr 14,"The Only Light in the Darkness","https://www.tvmaze.com/episodes/1661/marvels-agents-of-shield-1x19-the-only-light-in-the-darkness"
20,1,20,29 Apr 14,"Nothing Personal","https://www.tvmaze.com/episodes/1662/marvels-agents-of-shield-1x20-nothing-personal"
21,1,21,06 May 14,"Ragtag","https://www.tvmaze.com/episodes/1663/marvels-agents-of-shield-1x21-ragtag"
22,1,22,13 May 14,"Beginning of the End","https://www.tvmaze.com/episodes/1664/marvels-agents-of-shield-1x22-beginning-of-the-end"
23,2,1,23 Sep 14,"Shadows","https://www.tvmaze.com/episodes/1665/marvels-agents-of-shield-2x01-shadows"
24,2,2,30 Sep 14,"Heavy Is the Head","https://www.tvmaze.com/episodes/1666/marvels-agents-of-shield-2x02-heavy-is-the-head"
25,2,3,07 Oct 14,"Making Friends and Influencing People","https://www.tvmaze.com/episodes/1667/marvels-agents-of-shield-2x03-making-friends-and-influencing-people"
26,2,4,14 Oct 14,"Face My Enemy","https://www.tvmaze.com/episodes/1668/marvels-agents-of-shield-2x04-face-my-enemy"
27,2,5,21 Oct 14,"A Hen in the Wolf House","https://www.tvmaze.com/episodes/1669/marvels-agents-of-shield-2x05-a-hen-in-the-wolf-house"
28,2,6,28 Oct 14,"A Fractured House","https://www.tvmaze.com/episodes/14416/marvels-agents-of-shield-2x06-a-fractured-house"
29,2,7,11 Nov 14,"The Writing on the Wall","https://www.tvmaze.com/episodes/38906/marvels-agents-of-shield-2x07-the-writing-on-the-wall"
30,2,8,18 Nov 14,"The Things We Bury","https://www.tvmaze.com/episodes/39407/marvels-agents-of-shield-2x08-the-things-we-bury"
31,2,9,02 Dec 14,"...Ye Who Enter Here","https://www.tvmaze.com/episodes/45238/marvels-agents-of-shield-2x09-ye-who-enter-here"
32,2,10,09 Dec 14,"What They Become","https://www.tvmaze.com/episodes/45239/marvels-agents-of-shield-2x10-what-they-become"
33,2,11,03 Mar 15,"Aftershocks","https://www.tvmaze.com/episodes/46274/marvels-agents-of-shield-2x11-aftershocks"
34,2,12,10 Mar 15,"Who You Really Are","https://www.tvmaze.com/episodes/141795/marvels-agents-of-shield-2x12-who-you-really-are"
35,2,13,17 Mar 15,"One of Us","https://www.tvmaze.com/episodes/141796/marvels-agents-of-shield-2x13-one-of-us"
36,2,14,24 Mar 15,"Love in the Time of Hydra","https://www.tvmaze.com/episodes/142346/marvels-agents-of-shield-2x14-love-in-the-time-of-hydra"
37,2,15,31 Mar 15,"One Door Closes","https://www.tvmaze.com/episodes/142347/marvels-agents-of-shield-2x15-one-door-closes"
38,2,16,07 Apr 15,"Afterlife","https://www.tvmaze.com/episodes/151461/marvels-agents-of-shield-2x16-afterlife"
39,2,17,14 Apr 15,"Melinda","https://www.tvmaze.com/episodes/151462/marvels-agents-of-shield-2x17-melinda"
40,2,18,21 Apr 15,"The Frenemy of My Enemy","https://www.tvmaze.com/episodes/153323/marvels-agents-of-shield-2x18-the-frenemy-of-my-enemy"
41,2,19,28 Apr 15,"The Dirty Half Dozen","https://www.tvmaze.com/episodes/153395/marvels-agents-of-shield-2x19-the-dirty-half-dozen"
42,2,20,05 May 15,"Scars","https://www.tvmaze.com/episodes/153396/marvels-agents-of-shield-2x20-scars"
43,2,21,12 May 15,"S.O.S.: Part 1","https://www.tvmaze.com/episodes/153397/marvels-agents-of-shield-2x21-sos-part-1"
44,2,22,12 May 15,"S.O.S.: Part 2","https://www.tvmaze.com/episodes/153398/marvels-agents-of-shield-2x22-sos-part-2"
45,3,1,29 Sep 15,"Laws of Nature","https://www.tvmaze.com/episodes/167570/marvels-agents-of-shield-3x01-laws-of-nature"
46,3,2,06 Oct 15,"Purpose in the Machine","https://www.tvmaze.com/episodes/250013/marvels-agents-of-shield-3x02-purpose-in-the-machine"
47,3,3,13 Oct 15,"A Wanted (Inhu)man","https://www.tvmaze.com/episodes/280706/marvels-agents-of-shield-3x03-a-wanted-inhuman"
48,3,4,20 Oct 15,"Devils You Know","https://www.tvmaze.com/episodes/334512/marvels-agents-of-shield-3x04-devils-you-know"
49,3,5,27 Oct 15,"4,722 Hours","https://www.tvmaze.com/episodes/363110/marvels-agents-of-shield-3x05-4722-hours"
50,3,6,03 Nov 15,"Among Us Hide…","https://www.tvmaze.com/episodes/379111/marvels-agents-of-shield-3x06-among-us-hide"
51,3,7,10 Nov 15,"Chaos Theory","https://www.tvmaze.com/episodes/400123/marvels-agents-of-shield-3x07-chaos-theory"
52,3,8,17 Nov 15,"Many Heads, One Tale","https://www.tvmaze.com/episodes/406992/marvels-agents-of-shield-3x08-many-heads-one-tale"
53,3,9,01 Dec 15,"Closure","https://www.tvmaze.com/episodes/461933/marvels-agents-of-shield-3x09-closure"
54,3,10,08 Dec 15,"Maveth","https://www.tvmaze.com/episodes/461934/marvels-agents-of-shield-3x10-maveth"
55,3,11,08 Mar 16,"Bouncing Back","https://www.tvmaze.com/episodes/470641/marvels-agents-of-shield-3x11-bouncing-back"
56,3,12,15 Mar 16,"The Inside Man","https://www.tvmaze.com/episodes/628886/marvels-agents-of-shield-3x12-the-inside-man"
57,3,13,22 Mar 16,"Parting Shot","https://www.tvmaze.com/episodes/637838/marvels-agents-of-shield-3x13-parting-shot"
58,3,14,29 Mar 16,"Watchdogs","https://www.tvmaze.com/episodes/637839/marvels-agents-of-shield-3x14-watchdogs"
59,3,15,05 Apr 16,"Spacetime","https://www.tvmaze.com/episodes/663303/marvels-agents-of-shield-3x15-spacetime"
60,3,16,12 Apr 16,"Paradise Lost","https://www.tvmaze.com/episodes/663305/marvels-agents-of-shield-3x16-paradise-lost"
61,3,17,19 Apr 16,"The Team","https://www.tvmaze.com/episodes/686681/marvels-agents-of-shield-3x17-the-team"
62,3,18,26 Apr 16,"The Singularity","https://www.tvmaze.com/episodes/692367/marvels-agents-of-shield-3x18-the-singularity"
63,3,19,03 May 16,"Failed Experiments","https://www.tvmaze.com/episodes/692368/marvels-agents-of-shield-3x19-failed-experiments"
64,3,20,10 May 16,"Emancipation","https://www.tvmaze.com/episodes/692369/marvels-agents-of-shield-3x20-emancipation"
65,3,21,17 May 16,"Absolution","https://www.tvmaze.com/episodes/692370/marvels-agents-of-shield-3x21-absolution"
66,3,22,17 May 16,"Ascension","https://www.tvmaze.com/episodes/692371/marvels-agents-of-shield-3x22-ascension"
67,4,1,20 Sep 16,"The Ghost","https://www.tvmaze.com/episodes/848217/marvels-agents-of-shield-4x01-the-ghost"
68,4,2,27 Sep 16,"Meet the New Boss","https://www.tvmaze.com/episodes/887967/marvels-agents-of-shield-4x02-meet-the-new-boss"
69,4,3,11 Oct 16,"Uprising","https://www.tvmaze.com/episodes/887968/marvels-agents-of-shield-4x03-uprising"
70,4,4,18 Oct 16,"Let Me Stand Next to Your Fire","https://www.tvmaze.com/episodes/887969/marvels-agents-of-shield-4x04-let-me-stand-next-to-your-fire"
71,4,5,25 Oct 16,"Lockup","https://www.tvmaze.com/episodes/945961/marvels-agents-of-shield-4x05-lockup"
72,4,6,01 Nov 16,"The Good Samaritan","https://www.tvmaze.com/episodes/961165/marvels-agents-of-shield-4x06-the-good-samaritan"
73,4,7,29 Nov 16,"Deals with Our Devils","https://www.tvmaze.com/episodes/978673/marvels-agents-of-shield-4x07-deals-with-our-devils"
74,4,8,06 Dec 16,"The Laws of Inferno Dynamics","https://www.tvmaze.com/episodes/995494/marvels-agents-of-shield-4x08-the-laws-of-inferno-dynamics"
75,4,9,10 Jan 17,"Broken Promises","https://www.tvmaze.com/episodes/1008012/marvels-agents-of-shield-4x09-broken-promises"
76,4,10,17 Jan 17,"The Patriot","https://www.tvmaze.com/episodes/1026483/marvels-agents-of-shield-4x10-the-patriot"
77,4,11,24 Jan 17,"Wake Up","https://www.tvmaze.com/episodes/1029665/marvels-agents-of-shield-4x11-wake-up"
78,4,12,31 Jan 17,"Hot Potato Soup","https://www.tvmaze.com/episodes/1039837/marvels-agents-of-shield-4x12-hot-potato-soup"
79,4,13,07 Feb 17,"BOOM","https://www.tvmaze.com/episodes/1029667/marvels-agents-of-shield-4x13-boom"
80,4,14,14 Feb 17,"The Man Behind the Shield","https://www.tvmaze.com/episodes/1029668/marvels-agents-of-shield-4x14-the-man-behind-the-shield"
81,4,15,21 Feb 17,"Self Control","https://www.tvmaze.com/episodes/1056672/marvels-agents-of-shield-4x15-self-control"
82,4,16,04 Apr 17,"What If...","https://www.tvmaze.com/episodes/1076018/marvels-agents-of-shield-4x16-what-if"
83,4,17,11 Apr 17,"Identity and Change","https://www.tvmaze.com/episodes/1109704/marvels-agents-of-shield-4x17-identity-and-change"
84,4,18,18 Apr 17,"No Regrets","https://www.tvmaze.com/episodes/1117442/marvels-agents-of-shield-4x18-no-regrets"
85,4,19,25 Apr 17,"All the Madame's Men","https://www.tvmaze.com/episodes/1117443/marvels-agents-of-shield-4x19-all-the-madames-men"
86,4,20,02 May 17,"Farewell, Cruel World!","https://www.tvmaze.com/episodes/1117444/marvels-agents-of-shield-4x20-farewell-cruel-world"
87,4,21,09 May 17,"The Return","https://www.tvmaze.com/episodes/1117445/marvels-agents-of-shield-4x21-the-return"
88,4,22,16 May 17,"World's End","https://www.tvmaze.com/episodes/1117446/marvels-agents-of-shield-4x22-worlds-end"
89,5,1,01 Dec 17,"Orientation (Part One)","https://www.tvmaze.com/episodes/1327188/marvels-agents-of-shield-5x01-orientation-part-one"
90,5,2,01 Dec 17,"Orientation (Part Two)","https://www.tvmaze.com/episodes/1354781/marvels-agents-of-shield-5x02-orientation-part-two"
91,5,3,08 Dec 17,"A Life Spent","https://www.tvmaze.com/episodes/1357412/marvels-agents-of-shield-5x03-a-life-spent"
92,5,4,15 Dec 17,"A Life Earned","https://www.tvmaze.com/episodes/1359476/marvels-agents-of-shield-5x04-a-life-earned"
93,5,5,22 Dec 17,"Rewind","https://www.tvmaze.com/episodes/1359477/marvels-agents-of-shield-5x05-rewind"
94,5,6,05 Jan 18,"Fun &amp; Games","https://www.tvmaze.com/episodes/1369076/marvels-agents-of-shield-5x06-fun-games"
95,5,7,12 Jan 18,"Together or Not at All","https://www.tvmaze.com/episodes/1369077/marvels-agents-of-shield-5x07-together-or-not-at-all"
96,5,8,19 Jan 18,"The Last Day","https://www.tvmaze.com/episodes/1369078/marvels-agents-of-shield-5x08-the-last-day"
97,5,9,26 Jan 18,"Best Laid Plans","https://www.tvmaze.com/episodes/1369079/marvels-agents-of-shield-5x09-best-laid-plans"
98,5,10,02 Feb 18,"Past Life","https://www.tvmaze.com/episodes/1388410/marvels-agents-of-shield-5x10-past-life"
99,5,11,02 Mar 18,"All the Comforts of Home","https://www.tvmaze.com/episodes/1369081/marvels-agents-of-shield-5x11-all-the-comforts-of-home"
100,5,12,09 Mar 18,"The Real Deal","https://www.tvmaze.com/episodes/1369082/marvels-agents-of-shield-5x12-the-real-deal"
101,5,13,16 Mar 18,"Principia","https://www.tvmaze.com/episodes/1415224/marvels-agents-of-shield-5x13-principia"
102,5,14,23 Mar 18,"The Devil Complex","https://www.tvmaze.com/episodes/1418891/marvels-agents-of-shield-5x14-the-devil-complex"
103,5,15,30 Mar 18,"Rise and Shine","https://www.tvmaze.com/episodes/1422615/marvels-agents-of-shield-5x15-rise-and-shine"
104,5,16,06 Apr 18,"Inside Voices","https://www.tvmaze.com/episodes/1427045/marvels-agents-of-shield-5x16-inside-voices"
105,5,17,13 Apr 18,"The Honeymoon","https://www.tvmaze.com/episodes/1431607/marvels-agents-of-shield-5x17-the-honeymoon"
106,5,18,20 Apr 18,"All Roads Lead…","https://www.tvmaze.com/episodes/1436806/marvels-agents-of-shield-5x18-all-roads-lead"
107,5,19,27 Apr 18,"Option Two","https://www.tvmaze.com/episodes/1440707/marvels-agents-of-shield-5x19-option-two"
108,5,20,04 May 18,"The One Who Will Save Us All","https://www.tvmaze.com/episodes/1445218/marvels-agents-of-shield-5x20-the-one-who-will-save-us-all"
109,5,21,11 May 18,"The Force of Gravity","https://www.tvmaze.com/episodes/1445219/marvels-agents-of-shield-5x21-the-force-of-gravity"
110,5,22,18 May 18,"The End","https://www.tvmaze.com/episodes/1445220/marvels-agents-of-shield-5x22-the-end"
111,6,1,10 May 19,"Missing Pieces","https://www.tvmaze.com/episodes/1499431/marvels-agents-of-shield-6x01-missing-pieces"
112,6,2,17 May 19,"Window of Opportunity","https://www.tvmaze.com/episodes/1645659/marvels-agents-of-shield-6x02-window-of-opportunity"
113,6,3,24 May 19,"Fear and Loathing on the Planet of Kitson","https://www.tvmaze.com/episodes/1645660/marvels-agents-of-shield-6x03-fear-and-loathing-on-the-planet-of-kitson"
114,6,4,31 May 19,"Code Yellow","https://www.tvmaze.com/episodes/1648578/marvels-agents-of-shield-6x04-code-yellow"
115,6,5,14 Jun 19,"The Other Thing","https://www.tvmaze.com/episodes/1648579/marvels-agents-of-shield-6x05-the-other-thing"
116,6,6,21 Jun 19,"Inescapable","https://www.tvmaze.com/episodes/1648344/marvels-agents-of-shield-6x06-inescapable"
117,6,7,28 Jun 19,"Toldja","https://www.tvmaze.com/episodes/1664981/marvels-agents-of-shield-6x07-toldja"
118,6,8,05 Jul 19,"Collision Course (Part I)","https://www.tvmaze.com/episodes/1668279/marvels-agents-of-shield-6x08-collision-course-part-i"
119,6,9,12 Jul 19,"Collision Course (Part II)","https://www.tvmaze.com/episodes/1668280/marvels-agents-of-shield-6x09-collision-course-part-ii"
120,6,10,19 Jul 19,"Leap","https://www.tvmaze.com/episodes/1668558/marvels-agents-of-shield-6x10-leap"
121,6,11,26 Jul 19,"From the Ashes","https://www.tvmaze.com/episodes/1672839/marvels-agents-of-shield-6x11-from-the-ashes"
122,6,12,02 Aug 19,"The Sign","https://www.tvmaze.com/episodes/1673318/marvels-agents-of-shield-6x12-the-sign"
123,6,13,02 Aug 19,"New Life","https://www.tvmaze.com/episodes/1678212/marvels-agents-of-shield-6x13-new-life"
124,7,1,27 May 20,"The New Deal","https://www.tvmaze.com/episodes/1839278/marvels-agents-of-shield-7x01-the-new-deal"
125,7,2,03 Jun 20,"Know Your Onions","https://www.tvmaze.com/episodes/1839329/marvels-agents-of-shield-7x02-know-your-onions"
126,7,3,10 Jun 20,"Alien Commies from the Future!","https://www.tvmaze.com/episodes/1839330/marvels-agents-of-shield-7x03-alien-commies-from-the-future"
127,7,4,17 Jun 20,"Out of the Past","https://www.tvmaze.com/episodes/1839331/marvels-agents-of-shield-7x04-out-of-the-past"
128,7,5,24 Jun 20,"A Trout in the Milk","https://www.tvmaze.com/episodes/1839332/marvels-agents-of-shield-7x05-a-trout-in-the-milk"
129,7,6,01 Jul 20,"Adapt or Die","https://www.tvmaze.com/episodes/1839333/marvels-agents-of-shield-7x06-adapt-or-die"
130,7,7,08 Jul 20,"The Totally Excellent Adventures of Mack and the D","https://www.tvmaze.com/episodes/1839334/marvels-agents-of-shield-7x07-the-totally-excellent-adventures-of-mack-and-the-d"
131,7,8,15 Jul 20,"After, Before","https://www.tvmaze.com/episodes/1839335/marvels-agents-of-shield-7x08-after-before"
132,7,9,22 Jul 20,"As I Have Always Been","https://www.tvmaze.com/episodes/1839336/marvels-agents-of-shield-7x09-as-i-have-always-been"
133,7,10,29 Jul 20,"Stolen","https://www.tvmaze.com/episodes/1839337/marvels-agents-of-shield-7x10-stolen"
134,7,11,05 Aug 20,"Brand New Day","https://www.tvmaze.com/episodes/1839338/marvels-agents-of-shield-7x11-brand-new-day"
135,7,12,12 Aug 20,"The End is at Hand","https://www.tvmaze.com/episodes/1839339/marvels-agents-of-shield-7x12-the-end-is-at-hand"
136,7,13,12 Aug 20,"What We're Fighting For","https://www.tvmaze.com/episodes/1839340/marvels-agents-of-shield-7x13-what-were-fighting-for"
S01,1,0,18 Mar 14,"Marvel Studios: Assembling a Universe","https://www.tvmaze.com/episodes/625651/marvels-agents-of-shield-s01-special-marvel-studios-assembling-a-universe"

		</pre>
	</body>
</html>

 
  
 */
