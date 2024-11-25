using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Build.Construction;
using MovieMoverCore.Models;
using MovieMoverCore.Services;
using System.Collections.Generic;
using System.Linq;

namespace MovieMoverCore.Pages
{
    public class ObtainPage : PageModel
    {
        private readonly IFileBasedDatabase<Obtain> _backend;

        public List<Obtain> Obtains { get; private set; }

        public ObtainPage(IFileBasedDatabase<Obtain> backend)
        {
            _backend = backend; 
        }
        public void OnGet()
        {
            Obtains = _backend.Where(o => o.State == ObtainState.New).OrderBy(o => o.ReleaseDate).ToList();
        }
    }
}
