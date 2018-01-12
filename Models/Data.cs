using System;

namespace FilesService.Models {
    public class Data {
        public string userID { get; set; }
        public string tokenID { get; set; }
        public int ttl { get; set; }
        public DateTime create { get; set; }

        Data() { }

    }
}