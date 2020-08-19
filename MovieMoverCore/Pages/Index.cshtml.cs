using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Services;

namespace MovieMoverCore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IPlex _plex;

        public string PlexSettings => _plex.GetSettingsInfo();

        public IndexModel(ILogger<IndexModel> logger, IPlex plex)
        {
            _logger = logger;
            _plex = plex;
        }

        public void OnGet()
        {

        }
    }
}
