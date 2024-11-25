using MovieMoverCore.Services;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MovieMoverCore.Models
{
    public enum ObtainState
    {
        New, Obtained, Postponed, Deleted
    }
    public class Obtain : IFileBasedDatabaseItem
    {
        [Key]
        public int Id { get; set; }
        [DisplayName("Name")]
        [Required(ErrorMessage = "Name required")]
        public string Name { get; set; }
        [DisplayName("Release Datum")]
        [Required(ErrorMessage = "Date when to download required")]
        public DateTime? ReleaseDate { get; set; }
        [DisplayName("Status")]
        [Required(ErrorMessage = "State required")]
        public ObtainState State { get; set; }

        public IFileBasedDatabaseItem Clone()
        {
            return new Obtain()
            {
                Id = Id,
                Name = Name,
                ReleaseDate = ReleaseDate,
                State = State
            };
        }
    }
}
