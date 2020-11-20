using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MovieMoverCore.Models.DTO;
using MovieMoverCore.Services;

namespace MovieMoverCore.Pages
{
    public class SubtitlesModel : PageModel
    {
        public List<DisplaySeries> Series { get; set; }

        private IDatabase _database;

        public SubtitlesModel(IDatabase database)
        {
            _database = database;
        }
        
        public void OnGet()
        {
            Series = _database.GetSeries(s => !s.IsFinished).Select(s => new DisplaySeries { Id = s.Id, Name = s.Name }).OrderBy(s => s.Name).ToList();
        }
    }
}
