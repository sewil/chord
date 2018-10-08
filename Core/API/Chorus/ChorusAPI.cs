using Chord.Core.API.Chorus.Models;
using Newtonsoft.Json;
using System;
using System.Net;

namespace Chord.Core.API.Chorus
{
    public static class ChorusAPI
    {
        private const string API_URL = "https://chorus.fightthe.pw/api";

        public static SearchResult Search(int from, string query)
        {
            query = Uri.EscapeDataString(query);
            return Request<SearchResult>("/search/?query=" + query + "&from=" + from);
        }

        private static TResult Request<TResult>(string endpoint)
        {
            using (var client = new WebClient())
            {
                string responseString = client.DownloadString(API_URL + endpoint);
                TResult result = JsonConvert.DeserializeObject<TResult>(responseString);
                return result;
            }
        }
    }
}
