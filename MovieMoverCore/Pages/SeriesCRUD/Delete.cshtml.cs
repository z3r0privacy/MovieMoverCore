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
    public class DeleteModel : PageModel
    {
        private readonly IDatabase _db;

        public DeleteModel(IDatabase db)
        {
            _db = db;
        }

        [BindProperty]
        public Series Series { get; set; }

        public IActionResult OnGet(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Series = _db.GetSeries(id.Value);

            if (Series == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Series = _db.GetSeries(id.Value);

            if (Series != null)
            {
                _db.DeleteSeries(Series);
                await _db.SaveSeriesChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
