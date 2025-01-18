using Chord.Core.API.RhythmVerse.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chord.Core.API.RhythmVerse
{
    public static class RhythmVerseAPI
    {
        public const string HOST_URL = "https://rhythmverse.co";
        private const string API_URL = HOST_URL + "/api";
        public static SearchResult Search(int page, string query)
        {
            var data = new NameValueCollection();
            data.Set("text", query);
            data.Set("data_type", "full");
            data.Set("records", "25");
            data.Set("page", page.ToString());
            string endpoint = "/all/songfiles/search/live";
            using (var client = new WebClient())
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                string address = API_URL + endpoint;
                client.Headers.Set(HttpRequestHeader.Accept, "application/json, text/javascript, */*; q=0.01");
                //client.Headers.Set(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded; charset=UTF-8");
                
                try
                {
                    var response = client.UploadValues(address, data);
                    var responseString = Encoding.UTF8.GetString(response);
                    var result = JsonConvert.DeserializeObject<SearchResult>(responseString);
                    return result;
                }
                catch (WebException e)
                {
                    if (e.Response == null) throw;
                    Stream dataStream = e.Response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string responseString = reader.ReadToEnd();
                    Console.WriteLine("Error response: " + responseString);
                    throw;
                }
            }
        }
    }
}
