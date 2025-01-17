using System.Collections.Generic;

namespace Chord.Core.API.Chorus.Models
{
    public class SearchResult
    {
        public Song[] data;
        public int found;
        public int out_of;
        public int page;
        public int search_time_ms;
    }
}
