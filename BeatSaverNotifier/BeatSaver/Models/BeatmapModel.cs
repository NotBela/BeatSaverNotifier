using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BeatSaverNotifier.BeatSaver.Models
{
    public class BeatmapModel
    {
        private static HttpClient _client = new HttpClient();
        
        public byte[] Cover { get; private set; }
        public string UploadName { get; private set; }
        public string[] VersionHashes { get; private set; }
        
        public string SongName { get; private set; }
        public string SongSubName { get; private set; }
        public string Author { get; private set; }
        
        public string[] Mappers {get; private set; }
        public string Description { get; private set; }
        public string Id { get; private set; }
        public string DownloadUrl { get; private set; }
        public DateTime UploadDate { get; private set; }

        private BeatmapModel(byte[] cover, string uploadName, string[] versionHashes, string songName, string songSubName, string author, string id, string[] mappers, string description, string downloadUrl, DateTime uploadDate)
        {
            this.Cover = cover;
            this.UploadName = uploadName;
            this.VersionHashes = versionHashes;
            this.SongName = songName;
            this.SongSubName = songSubName;
            this.Author = author;
            this.Mappers = mappers;
            this.Id = id;
            this.Description = description;
            this.DownloadUrl = downloadUrl;
            this.UploadDate = uploadDate;
        }

        public static async Task<BeatmapModel> Parse(string json)
        {
            var jsonObject = JObject.Parse(json);
            
            var uploadName = jsonObject["name"]?.Value<string>();
            var id = jsonObject["id"]?.Value<string>();
            var description = jsonObject["description"]?.Value<string>();
            
            var metadata = jsonObject["metadata"];
            
            var songName = metadata?["songName"]?.Value<string>();
            var songSubName = metadata?["songSubName"]?.Value<string>();
            var author = metadata?["songAuthorName"]?.Value<string>();
            
            var versions = JArray.Parse(jsonObject["versions"]?.ToString() ?? throw new InvalidOperationException());
            
            var coverResponse = await _client.GetAsync(new Uri(versions[0]["coverURL"]?.Value<string>() ?? throw new Exception("No cover art!"), UriKind.Absolute));
            var cover = await coverResponse.Content.ReadAsByteArrayAsync();
            
            string[] versionHashes = versions.Select(i => i["hash"]?.Value<string>()).ToArray();
            
            var primaryUploader = jsonObject["uploader"]?["name"]?.Value<string>();
            var collab = new List<string>(){primaryUploader};
            
            if (jsonObject.ContainsKey("collaborators"))
            {
                var collaborators = JArray.Parse(jsonObject["collaborators"]?.ToString());
                collab.AddRange(collaborators.Select(collaborator => collaborator["name"]?.Value<string>()));
            }
            
            var downloadUrl = versions[0]["downloadUrl"]?.Value<string>();

            var uploadDate = DateTime.ParseExact(jsonObject["uploaded"]?.Value<string>(), 
                "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None);

            return new BeatmapModel(
                cover, 
                uploadName, 
                versionHashes, 
                songName, 
                songSubName, 
                author, 
                id, 
                collab.ToArray(),
                description,
                downloadUrl,
                uploadDate);
        }
    }
}