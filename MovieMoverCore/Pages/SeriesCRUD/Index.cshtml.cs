using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MovieMoverCore.Models;
using MovieMoverCore.Services;

namespace MovieMoverCore.Pages.SeriesCRUD
{
    public class IndexModel : PageModel
    {
        private readonly IDatabase _db;
        private readonly ISettings _settings;

        public IndexModel(IDatabase db, ISettings settings)
        {
            //_context = context;
            _db = db;
            _settings = settings;
        }

        public IList<Series> Series { get;set; }
        public IList<(string Regex, string Template)> Renamings => _settings.Files_RenameSchemes;

        public void OnGet()
        {
            Series = _db.GetSeries().OrderBy(s => s.Name).ToList(); //await _context.Series.ToListAsync();
        }
    }
}
