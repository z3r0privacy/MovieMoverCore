using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public class EpisodeInfo
    {
        public Series Series { get; set; }
        public int Season { get; set; }
        public int Episode { get; set; }
        public string Title { get; set; }
        public DateTime AirDate { get; set; }

        public override bool Equals(object obj)
        {
            return obj is EpisodeInfo info &&
                   EqualityComparer<Series>.Default.Equals(Series, info.Series) &&
                   Season == info.Season &&
                   Episode == info.Episode &&
                   Title == info.Title &&
                   AirDate == info.AirDate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Series, Season, Episode, Title, AirDate);
        }
    }
}
