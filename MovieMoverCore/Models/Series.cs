using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public class Series
    {
        [Required]
        public int Id { get; set; }

        public string PlexName { get; set; }
        public string EpGuidesName { get; set; }
        public string SubtitlesName { get; set; }
        public string DirectoryName { get; set; }
        [Required]
        public bool SearchNewEpisodes { get; set; }
        public bool IsFinished { get; set; }
    }
}
