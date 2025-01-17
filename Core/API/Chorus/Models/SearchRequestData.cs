using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chord.Core.API.Chorus.Models
{
    public class SearchRequestData
    {
        [JsonProperty("search")]
        public string Search { get; set; }
        [JsonProperty("page")]
        public int Page { get; set; }
        [JsonProperty("instrument")]
        public string Instrument { get; set; }
        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }
        [JsonProperty("drumType")]
        public string DrumType { get; set; }
        [JsonProperty("source")]
        public string Source { get; set; }
    }
}
