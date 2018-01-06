using System;

namespace ManageService.Models {
    public class ShareFile {
        public string _id { get; set; }
        public string _rev { get; set; }
        public string imgId { get; set; }
        public string toUser { get; set; }
    }

    public class ShareFileNoRev {
        public string _id { get; set; }
        public string imgId { get; set; }
        public string toUser { get; set; }

        public ShareFileNoRev(){}
        
        public ShareFileNoRev(ShareFile sf) {
            this._id = "id:" + sf._id;
            this.imgId = sf.imgId;
            this.toUser = sf.toUser;
        }
    }
}