using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using FilesService.Models;
using System.Threading.Tasks;

namespace FilesService.Helpers
{
    public static class CouchDBConnect
    {
        private static string host = "https://0677dc4a-00da-46cd-97db-f627e643765e-bluemix:137c1cea45ee9a1ee20523e99f21c3086ebd7f37392def5e1bb3a4a1ffc9dc9c@0677dc4a-00da-46cd-97db-f627e643765e-bluemix.cloudant.com/{0}";
    
        public static HttpClient GetClient(string db) {
            var hc = new HttpClient();
            hc.BaseAddress = new Uri(string.Format(host,db));
            hc.DefaultRequestHeaders.Clear();
            hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Console.WriteLine(hc.DefaultRequestHeaders);
            return hc;
        }

        public static async Task<HttpResponseMessage> PostToDB(object obj, string dbName)
        {
            var hc = Helpers.CouchDBConnect.GetClient(dbName);
            string json = JsonConvert.SerializeObject(obj);
            HttpContent htc = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await hc.PostAsync("", htc);
            return response;
        }
    }
}