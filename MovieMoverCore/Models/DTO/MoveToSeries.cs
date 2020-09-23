using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieMoverCore.Models.DTO
{
    public class MoveToSeries
    {
        [JsonPropertyName("downloads")]
        public List<string> Downloads { get; set; }
        [JsonPropertyName("seriesId")]
        public int SeriesId { get; set; }
        [JsonPropertyName("season")]
        public int Season { get; set; }
    }
}
