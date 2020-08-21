using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using MovieMoverCore.Services;

namespace MovieMoverCore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IPlex _plex;

        public IndexModel(ILogger<IndexModel> logger, IPlex plex)
        {
            _logger = logger;
            _plex = plex;
        }

        public void OnGet()
        {
            var s = new Series
            {
                Name = "13 Reasons Why",
                PlexId = "125185"
            };
            _plex.GetFilePathOfEpisode(s, 4, 3);
        }

        public async Task OnPostSeriesnamesAsync()
        {
            var dat = await _plex.GetSeriesNamesAsync();
            var s = new Series
            {
                PlexId = dat[5].id,
                Name = dat[5].name
            };
            var r = await _plex.GetNewestEpisodeAsync(s);
            TempData["msg"] = $"Got {dat.Count} series, the first is: {dat.First()}";
        }
    }
}
