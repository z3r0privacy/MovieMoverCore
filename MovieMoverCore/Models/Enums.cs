using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public enum PlexSection
    {
        Movies, Series
    }

    public enum FileMoveState
    {
        Queued, Moving, Success, Failed
    }
}
