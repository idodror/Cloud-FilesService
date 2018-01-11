using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace FilesService.Models
{
    public class ImageFile
    {
        public string _id { get; set; }    // file name
        public string _rev { get; set; }
        public string data { get; set; }    // file as Base64

        public ImageFile(){} //this fix the upload bug because the Helpers.ImageDoc doc is null

        public ImageFile(Helpers.ImageDoc doc) {
            _id = doc.Id;
            _rev = doc.Rev;
            data = doc.Data;
        }
    }

    public class ImageFileNoRev
    {
        public string _id { get; set; }    // file name
        public string data { get; set; }    // file as Base64

        public ImageFileNoRev(ImageFile file) {
            this._id = "userid:" + file._id + ":imgid:" + Guid.NewGuid() ;
            this.data = file.data;
        }
    }
}