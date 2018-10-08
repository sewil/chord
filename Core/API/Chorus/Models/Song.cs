using System;

namespace Chord.Core.API.Chorus.Models
{
    public class Song
    {
        public string name;
        public string artist;
        public string album;
        public string charter;
        public string year;
        public DateTime uploadedAt;
        public DateTime? lastModified;
        public string link;
        public DirectLink directLinks;
    }
}
