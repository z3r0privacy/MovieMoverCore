﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MovieMoverCore.Models;
using MovieMoverCore.Services;

namespace MovieMoverCore.Pages.SeriesCRUD
{
    public class EditModel : PageModel
    {
        private IDatabase _db;
        private readonly IMultimediaMetadataProvider _metadataProvider;
        private readonly IFileMover _files;

        public IList<string> AvailableMetadataSeries;
        public IList<string> AvailableDirectories;

        public EditModel(IDatabase db, IMultimediaMetadataProvider metadataProvider, IFileMover file)
        {
            _db = db;
            _metadataProvider = metadataProvider;
            _files = file;
        }

        [BindProperty]
        public Series Series { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
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

            AvailableMetadataSeries = (await _metadataProvider.GetSeriesNamesAsync()).Select(t => t.name).ToList();
            AvailableDirectories = _files.GetSeriesEntries();

            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            AvailableMetadataSeries = (await _metadataProvider.GetSeriesNamesAsync()).Select(t => t.name).ToList();
            AvailableDirectories = _files.GetSeriesEntries();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            foreach (var (key, error) in Series.IsValid(_metadataProvider, _files))
            {
                ModelState.AddModelError(key, error);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _db.UpdateSeries(Series);
            await _db.SaveSeriesChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
