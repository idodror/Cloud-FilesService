using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FilesService.Models;
using FilesService.Helpers;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;
using StackExchange.Redis;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Operations.MessageSequence;
using RawRabbit;

namespace FilesService.Controllers
{

    [Route("api/[controller]")]
    public class FilesController : Controller
    {
        IBusClient client;
        IDatabase cachingDB;

        public FilesController (IBusClient _client, IRedisConnectionFactory caching) {
            client = _client;
            cachingDB = caching.Connection().GetDatabase();

            client.SubscribeAsync<String>(async (json) =>
            {
                // download image and upload it with the new user as owner
                await CopyFile(json);
            });
        }


        private async Task CopyFile(String json)
        {
            JObject obj = JObject.Parse(json);
            ImageFile downloadedFile = await DownloadFile(obj["imgId"].ToString());
            downloadedFile._id = obj["toUser"].ToString();
            ImageFileNoRev fileNoRev = new ImageFileNoRev(downloadedFile);
             var response = await CouchDBConnect.PostToDB(fileNoRev, "files");
            return;
        }

        [HttpGet]
        [Route("/init")]
        public void init() { }

        [HttpGet]
        [Route("/ReadFromCache/{id}")]
        public Data ReadFromCache(string id) {
            Data d = Newtonsoft.Json.JsonConvert.DeserializeObject<Data>(cachingDB.StringGet(id.ToString()));
            return d;
        }

        [HttpPost]
        [Route("UploadFile")]
        public async Task<String> UploadFile([FromBody]ImageFile file)
        {
            if (VerifyTheToken(file._id))
            {
                ImageFileNoRev fileNoRev = new ImageFileNoRev(file);
                var response = await CouchDBConnect.PostToDB(fileNoRev, "files");
                
                Console.WriteLine(response);

                return "Upload Succeeded";
            }
            else
            {
                return "Please login first!";
            }


        }

        private bool VerifyTheToken(string id)
        {
            int index = id.LastIndexOf(':');
            string newUserId = id.Substring(index + 1, id.Length - index - 1);
            Data getData = ReadFromCache("id:" + newUserId);

             if ((DateTime.Now - getData.create.AddSeconds(getData.ttl)) < TimeSpan.FromHours(1))
             {
                 return true;
             }
             else
             {
                 return false;
             }
        }

        public int NthIndexOf(string s, char c, int n)
        {
            var takeCount = s.TakeWhile(x => (n -= (x == c ? 1 : 0)) > 0).Count();
            return takeCount == s.Length ? -1 : takeCount;
        }

        [HttpGet]
        [Route("DownloadFile/{id}")]
        public async Task<ImageFile> DownloadFile(string id) {
            int index1 = NthIndexOf(id, ':', 1);
            int index2 = NthIndexOf(id, ':', 2);
            string userId = id.Substring(index1 + 1, index2 - index1 - 1);
            if (VerifyTheToken(userId)){

                var hc = CouchDBConnect.GetClient("files");
                var response = await hc.GetAsync("/files/" + id);

                if (!response.IsSuccessStatusCode) {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();

                ImageFile file = (ImageFile) JsonConvert.DeserializeObject(content, typeof(ImageFile));
                
                return file;
            }

            else
            {
                return null;
            }
        }

        [HttpDelete]
        [Route("Delete/{id}")]
        public async Task<int> Delete(string id) {
            int index1 = NthIndexOf(id, ':', 1);
            int index2 = NthIndexOf(id, ':', 2);
            string userId = id.Substring(index1 + 1, index2 - index1 - 1);
            if (VerifyTheToken(userId)) {
                var hc = CouchDBConnect.GetClient("files");
                ImageFile imageRemove = null;
                imageRemove = await DownloadFile(id);

                if(imageRemove != null)
                {
                    string uri = "/files/" + id + "?rev=" + imageRemove._rev;
                    var response = await hc.DeleteAsync(uri);

                    if (!response.IsSuccessStatusCode) {
                        return -1;
                    }
                        return 1;
                }
                
                return -1;
            }
            else
            {
                return -1;
            }

        }

        [HttpGet]
        [Route("GetList/{id}")] //all image by id (user)
        public async Task<IEnumerable<ImageFile>> GetList(string id)
        {
            if(VerifyTheToken(id)){
                var hc = CouchDBConnect.GetClient("files");
                List<ImageFile> imagesList = new List<ImageFile>();
                var query = "/files/_all_docs?startkey=\"userid:" + id + ":\"&endkey=\"userid:" + id + ":\uffff\"&include_docs=true";
                var response = await hc.GetAsync(query);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                await GetListFromDB(imagesList, response);
                
                return imagesList;
            }
            else
            {
                return null;
            }
        }

        private static async Task GetListFromDB(List<ImageFile> imagesList, HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            string listData = JObject.Parse(content).ToString();

            var data = JsonImagesListHelper.FromJson(listData);     // make an object built from the json

            foreach (var row in data.Rows)
                imagesList.Add(new ImageFile(row.Doc));
        }
    }     
        
}            
