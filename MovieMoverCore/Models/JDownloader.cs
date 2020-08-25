using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public class JD_LoginResponse
    {
        [JsonPropertyName("sessiontoken")]
        public string SessionToken { get; set; }
        [JsonPropertyName("regaintoken")]
        public string RegainToken { get; set; }
        [JsonPropertyName("rid")]
        public int Rid { get; set; }
    }

    public class JD_ListDevice_Response
    {
        [JsonPropertyName("rid")]
        public int Rid { get; set; }
        [JsonPropertyName("list")]
        public List<JD_Device> Devices { get; set; }
    }

    public class JD_Device
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
