    using System;
    using System.Net;
    using System.Collections.Generic;
    using Newtonsoft.Json;

/*
 * This class represent a helper to get json list from server and convert it to one object
 */

namespace FilesService.Helpers
{
    public partial class JsonImagesListHelper
    {
        [JsonProperty("total_rows")]
        public long TotalRows { get; set; }

        [JsonProperty("offset")]
        public long Offset { get; set; }

        [JsonProperty("rows")]
        public Row[] Rows { get; set; }
    }

    public partial class Row
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public Value Value { get; set; }

        [JsonProperty("doc")]
        public ImageDoc Doc { get; set; }
    }

    public partial class Value
    {
        [JsonProperty("rev")]
        public string Rev { get; set; }
    }

    public partial class ImageDoc
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("_rev")]
        public string Rev { get; set; }

        [JsonProperty("filetype")]
        public string Filetype { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }
    }

    public partial class JsonImagesListHelper
    {
        public static JsonImagesListHelper FromJson(string json) => JsonConvert.DeserializeObject<JsonImagesListHelper>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this JsonImagesListHelper self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };
    }
}
