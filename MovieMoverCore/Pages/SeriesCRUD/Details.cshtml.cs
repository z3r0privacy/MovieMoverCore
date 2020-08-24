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
    public class DetailsModel : PageModel
    {
        private IDatabase _db;

        public DetailsModel(IDatabase db)
        {
            _db = db;
        }

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
    }
}
