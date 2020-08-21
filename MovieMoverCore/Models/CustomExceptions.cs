﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public class VideoDependencyNotFoundException: Exception
    {
        public Series Series { get; set; }
        public int Season { get; set; }
        public int Episode { get; set; }

        public VideoDependencyNotFoundException(Series series, int season, int episode)
            :base($"Video file for episode S{season:00}E{episode:00} in {series.Name} not found")
        {
            Series = series;
            Season = season;
            Episode = episode;
        }
    }
}
