using Newtonsoft.Json;
using System;

namespace Chord.Core.API.RhythmVerse.Models
{
    public class SearchResult
    {
        [JsonProperty("data")]
        public SearchResultData Data { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
    }
    public class SearchResultData
    {
        [JsonProperty("songs")]
        public Song[] Songs { get; set; }
    }
    public class Song
    {
        [JsonProperty("data")]
        public SongData Data { get; set; }
        [JsonProperty("file")]
        public SongFile File { get; set; }
    }
    public class SongData
    {
        [JsonProperty("album")]
        public string Album { get; set; }
        [JsonProperty("artist")]
        public string Artist { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
    }
    public class SongFile
    {
        [JsonProperty("user")]
        public string User { get; set; }
        [JsonProperty("external_url")]
        public string ExternalUrl { get; set; }
        [JsonProperty("download_url")]
        public string DownloadUrl { get; set; }
        [JsonProperty("gameformat")]
        public string GameFormat { get; set; }
    }
}
