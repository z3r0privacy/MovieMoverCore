using MovieMoverCore.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{

    public class Series : ICloneable
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string PlexId { get; set; }
        [DisplayName("EpGuide Search")]
        public string EpGuidesName { get; set; }
        [DisplayName("Subtitle Search Name")]
        public string SubtitlesName { get; set; }
        [DisplayName("Video Search Name")]
        public string VideoSearch { get; set; }
        [Required]
        [DisplayName("Directory")]
        public string DirectoryName { get; set; }
        [DisplayName("Search for new episodes")]
        public bool SearchNewEpisodes { get; set; }
        [DisplayName("Series Finished")]
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

        public List<(string key, string error)> IsValid(IPlex plex, IFileMover files, bool isNewEntry = false)
        {
            var errs = new List<(string key, string error)>();

            if (!plex.ResolvePlexId(this) && !isNewEntry)
            {
                errs.Add((nameof(Series) + "." + nameof(Name), "The given name could not be found on the Plex server."));
            }

            if (SearchNewEpisodes)
            {
                if (string.IsNullOrWhiteSpace(EpGuidesName))
                {
                    errs.Add((nameof(Series) + "." + nameof(EpGuidesName), "If searching for new episodes is enabled, a search name needs to be specified."));
                }
                if (string.IsNullOrWhiteSpace(SubtitlesName))
                {
                    if (isNewEntry)
                    {
                        SubtitlesName = Name;
                    }
                    else
                    {
                        errs.Add((nameof(Series) + "." + nameof(SubtitlesName), "If searching for new episodes is enabled, a search name needs to be specified."));
                    }
                }
                if (string.IsNullOrWhiteSpace(VideoSearch))
                {
                    if (isNewEntry)
                    {
                        VideoSearch = Name;
                    } else
                    {
                        errs.Add((nameof(Series) + "." + nameof(VideoSearch), "If searching for new episodes is enabled, a search name needs to be specified."));
                    }
                }
            }

            if (errs.Count == 0)
            {
                if (!files.ValidateSeriesPath(this, isNewEntry))
                {
                    errs.Add((nameof(Series) + "." + nameof(DirectoryName), "The directory does not exist and could not be created."));
                }
            }

            return errs;
        }

        public override bool Equals(object obj)
        {
            return obj is Series series &&
                   Id == series.Id &&
                   Name == series.Name &&
                   EpGuidesName == series.EpGuidesName &&
                   SubtitlesName == series.SubtitlesName &&
                   VideoSearch == series.VideoSearch &&
                   DirectoryName == series.DirectoryName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, EpGuidesName, SubtitlesName, VideoSearch, DirectoryName);
        }
    }
}
