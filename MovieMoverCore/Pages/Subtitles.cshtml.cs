using System;
using System.Collections.Generic;
using System.IO;
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
    [IgnoreAntiforgeryToken]
    public class SubtitlesModel : PageModel
    {
        public List<DisplaySeries> Series { get; set; }

        private IDatabase _database;
        private IFileMover _fileMover;
        private ILogger<SubtitlesModel> _logger;

        public SubtitlesModel(IDatabase database, IFileMover fileMover, ILogger<SubtitlesModel> logger)
        {
            _database = database;
            _fileMover = fileMover;
            _logger = logger;
        }
        
        public void OnGet()
        {
            Series = _database.GetSeries(s => !s.IsFinished).Select(s => new DisplaySeries { Id = s.Id, Name = s.Name }).OrderBy(s => s.Name).ToList();
        }

        public async Task<IActionResult> OnPostAsync(List<IFormFile> files)
        {
            _logger.LogDebug($"Received {files.Count} files");
            foreach (var f in files)
            {
                _logger.LogDebug($"File '{f.FileName}' uploaded. Trying to match and save.");

                if (Request.Form.TryGetValue("sid", out var sid) && int.TryParse(sid, out var id))
                {
                    var series = _database.GetSeries(id);
                    if (series == null)
                    {
                        _logger.LogDebug("Could not find matching series");
                        return new NotFoundResult();
                    }
                    using var sContent = f.OpenReadStream();
                    var data = new byte[f.Length];
                    await sContent.ReadAsync(data, 0, data.Length);
                    await _fileMover.AddSubtitleAsync(f.FileName, data, series);
                    _logger.LogDebug($"Successfully uploaded {f.FileName}");
                } else
                {
                    _logger.LogDebug("Could not parse series id");
                    return new BadRequestResult();
                }
            }
            return new OkResult();
        }
    }
}
