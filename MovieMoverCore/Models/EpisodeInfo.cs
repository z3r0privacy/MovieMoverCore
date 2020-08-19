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
    }
}
