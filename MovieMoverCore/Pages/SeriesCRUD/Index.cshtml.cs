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

        public IndexModel(IDatabase db)
        {
            //_context = context;
            _db = db;
        }

        public IList<Series> Series { get;set; }

        public void OnGet()
        {
            Series = _db.GetSeries(); //await _context.Series.ToListAsync();
        }
    }
}
