using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public class Series : ICloneable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PlexId { get; set; }
        public string EpGuidesName { get; set; }
        public string SubtitlesName { get; set; }
        public string DirectoryName { get; set; }
        public bool SearchNewEpisodes { get; set; }
        public bool IsFinished { get; set; }

        public Series Clone()
        {
            return new Series
            {
                DirectoryName = DirectoryName,
                EpGuidesName = EpGuidesName,
                Id = Id,
                IsFinished = IsFinished,
                Name = Name,
                PlexId = PlexId,
                SearchNewEpisodes = SearchNewEpisodes,
                SubtitlesName = SubtitlesName
            };
        }

        public void Apply(Series series)
        {
            DirectoryName = series.DirectoryName;
            EpGuidesName = series.EpGuidesName;
            IsFinished = series.IsFinished;
            Name = series.Name;
            PlexId = series.PlexId;
            SearchNewEpisodes = series.SearchNewEpisodes;
            SubtitlesName = series.SubtitlesName;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
