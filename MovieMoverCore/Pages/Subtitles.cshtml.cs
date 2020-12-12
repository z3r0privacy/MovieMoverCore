using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models.DTO;
using MovieMoverCore.Services;

namespace MovieMoverCore.Pages
{
    public class SubtitlesModel : PageModel
    {
        public List<DisplaySeries> Series { get; set; }

        private IDatabase _database;
        private ILogger<SubtitlesModel> _logger;

        public SubtitlesModel(IDatabase database, ILogger<SubtitlesModel> logger)
        {
            _database = database;
            _logger = logger;
        }
        
        public void OnGet()
        {
            Series = _database.GetSeries(s => !s.IsFinished).Select(s => new DisplaySeries { Id = s.Id, Name = s.Name }).OrderBy(s => s.Name).ToList();
        }

        public void OnPost(List<IFormFile> files)
        {
            _logger.LogDebug($"Received {files.Count} files");
            foreach (var f in files)
            {
                _logger.LogDebug($"File '{f.Name}' ({f.FileName})");
            }
        }
    }
}
