using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public class CrawledDownloadPackage
    {
        public EpisodeInfo EpisodeInfo { get; set; }
        public string SubtitleLink { get; set; }
        public bool IsSubtitleDDL { get; set; }
        public List<string> VideoLink { get; set; }
        public bool IsVideoDDL { get; set; }
    }
}
