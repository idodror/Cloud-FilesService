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

        public FilesController (IBusClient _client) {
            client = _client;
            client.SubscribeAsync<ImageFile, MessageContext>((img, ctx) => {
                // store image in database;
                Console.WriteLine("Image id: {0}, fileType: {1}", img._id, img.filetype);
                return Task.FromResult(0);
            });
        }

        [HttpGet]
        [Route("/init")]
        public async void init() {
            await client.PublishAsync(new ImageFile {
                filetype = ".jpg",
                data = "blabla"
            });
        }  

        [HttpPost]
        [Route("UploadFile")]
        public async Task<int> UploadFile([FromBody]ImageFile file) {
            ImageFileNoRev fileNoRev = new ImageFileNoRev(file);
            var response = await CouchDBConnect.PostToDB(fileNoRev, "files");
            
            Console.WriteLine(response);
            return 1;
        }

        [HttpGet]
        [Route("DownloadFile/{id}")]
        public async Task<ImageFile> DownloadFile(string id) {
            var hc = CouchDBConnect.GetClient("files");
            var response = await hc.GetAsync("/files/imgname:" + id);

            if (!response.IsSuccessStatusCode) {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            ImageFile file = (ImageFile) JsonConvert.DeserializeObject(content, typeof(ImageFile));
            
            return file;
        }

        [HttpDelete]
        [Route("Delete/{id}")]
        public async Task<int> Delete(string id){
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

        [HttpGet]
        [Route("GetList/{id}")] //all image by id (user)
        public async Task<IEnumerable<ImageFile>> GetList(string id)
        {
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



        [HttpGet]
        [Route("Search/{id}/{fileName}")] //search by name of image file (per user)
        public async Task<IEnumerable<ImageFile>> Search(string id,string fileName) {
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