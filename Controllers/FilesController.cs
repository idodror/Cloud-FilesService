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
using ManageService.Models;

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

            client.SubscribeAsync<ShareFileNoRev, MessageContext>(async (sfnr, ctx) =>
            {
                // download image and upload it with the new user as owner
                await CopyFile(sfnr);
            });
        }


        private async Task CopyFile(ShareFileNoRev sfnr)
        {
            ImageFile downloadedFile = await DownloadFile(sfnr.imgId);
            int index = downloadedFile._id.LastIndexOf(':');
            string newImgId = downloadedFile._id.Substring(0, index + 1);
            newImgId += sfnr.toUser;
            downloadedFile._id = newImgId;
            index = downloadedFile._id.IndexOf(':');
            downloadedFile._id = downloadedFile._id.Substring(index + 1, downloadedFile._id.Length - 1 - index);
            await UploadFile(downloadedFile);
            Console.WriteLine(downloadedFile._id);
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
        public async Task<int> UploadFile([FromBody]ImageFile file)
        {
            if (VerifyTheToken(file._id))
            {
                ImageFileNoRev fileNoRev = new ImageFileNoRev(file); //"bla:user:Moris"
                var response = await CouchDBConnect.PostToDB(fileNoRev, "files");
                
                Console.WriteLine(response);

                return 1;
            }
            else
            {
                return -1;
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

        [HttpGet]
        [Route("DownloadFile/{id}")]
        public async Task<ImageFile> DownloadFile(string id) {
            if (VerifyTheToken(id)){

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
        public async Task<int> Delete(string id){
            if(VerifyTheToken(id)){
                var hc = CouchDBConnect.GetClient("files");
                ImageFile imageRemove = null;
                imageRemove = await DownloadFile(id);

                if(imageRemove != null)
                {
                    string uri = "/files/imgname:" + id + "?rev=" + imageRemove._rev;
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
                var response = await hc.GetAsync("/files/_all_docs?startkey=\"imgname:" + id + "\"&include_docs=true");

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



        [HttpGet]
        [Route("Search/{id}/{fileName}")] //search by name of image file (per user)
        public async Task<IEnumerable<ImageFile>> Search(string id,string fileName) {
            if(VerifyTheToken(id)){
                var hc = Helpers.CouchDBConnect.GetClient("files");
                var response = await hc.GetAsync("/files/_all_docs?startkey=\"imgname:" + id +":file:"+ fileName+ "\"&include_docs=true");
                List<ImageFile> imagesList = new List<ImageFile>();

                if (!response.IsSuccessStatusCode) {
                        return null;
                }

                await GetListFromDB(imagesList, response);

                if(imagesList.Count>0)
                    return imagesList;

                return null;
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
