using Chord.Core.API.Chorus.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using static Google.Apis.Requests.BatchRequest;

namespace Chord.Core.API.Chorus
{
    public static class ChorusAPI
    {
        private const string API_URL = "https://api.enchor.us";

        public static SearchResult Search(int page, string query)
        {
            string endpoint = "/search";
            var searchRequestData = new SearchRequestData
            {
                //{"search":"opeth","page":1,"instrument":"guitar","difficulty":"expert","drumType":null,"source":"website"}
                Search = query,
                Page = page,
                Instrument = "guitar",
                Difficulty = "expert",
                DrumType = null,
                Source = "website"
            };
            string dataString = JsonConvert.SerializeObject(searchRequestData);
            using (var client = new WebClient())
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                client.Headers.Set("Content-Type", "application/json");
                client.Headers.Set("Accept", "application/json");
                try
                {
                    string responseString = client.UploadString(API_URL + endpoint, dataString);
                    var result = JsonConvert.DeserializeObject<SearchResult>(responseString);
                    return result;
                }
                catch (WebException e)
                {
                    if (e.Response == null) throw;
                    Stream dataStream = e.Response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string responseString = reader.ReadToEnd();
                    var responseData = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                    foreach (var error in responseData.message)
                    {
                        Console.WriteLine($"Error code: {error.code}, Expected: {error.expected}, Received: {error.received}, Message: {error.message}, Path: {error.path}");
                    }
                    throw;
                }
            }
        }
    }
}
