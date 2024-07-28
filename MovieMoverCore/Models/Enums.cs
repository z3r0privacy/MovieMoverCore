using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public enum MultimediaType
    {
        Movies, Series
    }

    public enum FileOperationState
    {
        Queued, InOperation, Success, Failed
    }
}
