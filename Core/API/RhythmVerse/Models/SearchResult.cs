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
    public class SongsConverter : JsonConverter<Song[]>
    {
        public override Song[] ReadJson(JsonReader reader, Type objectType, Song[] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Boolean && reader.Value is bool boolean && !boolean)
            {
                return Array.Empty<Song>();
            }
            return serializer.Deserialize<Song[]>(reader);
        }

        public override void WriteJson(JsonWriter writer, Song[] value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
    public class SearchResultData
    {
        [JsonProperty("songs")]
        [JsonConverter(typeof(SongsConverter))]
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
