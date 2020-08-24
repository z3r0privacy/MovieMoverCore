using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MovieMoverCore.Models;
using MovieMoverCore.Services;

namespace MovieMoverCore.Pages.SeriesCRUD
{
    public class CreateModel : PageModel
    {
        private readonly IDatabase _db;
        private readonly IPlex _plex;
        private readonly IFileMover _file;

        public IList<string> AvailablePlexSeries;
        public IList<string> AvailableDirectories;

        public CreateModel(IDatabase db, IPlex plex, IFileMover file)
        {
            //_context = context;
            _db = db;
            _plex = plex;
            _file = file;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            AvailablePlexSeries = (await _plex.GetSeriesNamesAsync()).Select(t => t.name).ToList();
            AvailableDirectories = _file.GetSeriesEntries();
            return Page();
        }

        [BindProperty]
        public Series Series { get; set; }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            foreach (var (key, error) in Series.IsValid(_plex, _file, true))
            {
                ModelState.AddModelError(key, error);
            }
            
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _db.AddSeries(Series);
            await _db.SaveSeriesChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
