using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public class FileMoveOperation : ICloneable
    {
        public uint ID { get; set; }
        public FileMoveState CurrentState { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public PlexSection PlexSection { get; set; }
        public DateTime? Finished { get; set; }
        public string ErrorMessage { get; set; }
        public string Name { get; set; }

        public FileMoveOperation Clone()
        {
            return new FileMoveOperation
            {
                CurrentState = CurrentState,
                Destination = Destination,
                ErrorMessage = ErrorMessage,
                Finished = Finished,
                ID = ID,
                Name = Name,
                PlexSection = PlexSection,
                Source = Source
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
