using System.Collections.Generic;

namespace Chord.Core.API.Chorus.Models
{
    public class SearchResult
    {
        public IDictionary<string, string> roles;
        public IList<Song> songs;
    }
}
