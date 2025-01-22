using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Components;
using IPA.Config.Data;
using IPA.Utilities;
using ModestTree;
using Newtonsoft.Json.Linq;
using SongCore;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaverNotifier.BeatSaver.Models
{
    public class BeatmapModel
    {
        private static readonly HttpClient _client = new HttpClient();

        private readonly byte[] _coverBytes;
        public Sprite CoverSprite { get; private set; }
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
        public AudioClip PreviewAudioClip { get; private set; }
        
        public Dictionary<DifficultyModel.CharacteristicTypes, List<DifficultyModel>> DifficultyDictionary { get; private set; }

        private BeatmapModel(
            byte[] coverBytes, 
            string uploadName, 
            string[] versionHashes, 
            string songName, 
            string songSubName, 
            string author, 
            string id, 
            string[] mappers, 
            string description, 
            string downloadUrl, 
            DateTime uploadDate, 
            Dictionary<DifficultyModel.CharacteristicTypes, 
                List<DifficultyModel>> difficultyDictionary,
            AudioClip coverAudioClip)
        {
            this._coverBytes = coverBytes;
            this.CoverSprite = getSpriteFromImageBuffer(coverBytes);
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
            this.DifficultyDictionary = difficultyDictionary;
            this.PreviewAudioClip = coverAudioClip;
        }

        public CustomListTableData.CustomCellInfo getCustomListCellInfo(bool queuedText = false) => 
            new(this.UploadName, queuedText ? "Queued" : this.Mappers.Join(", "), getSpriteFromImageBuffer(_coverBytes));
        
        private Sprite getSpriteFromImageBuffer(byte[] buffer)
        {
            var tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, buffer);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }
        // thank you beatsaver api for being ass <3
        public bool isAlreadyDownloaded() => VersionHashes.Any(i => Loader.GetLevelByHash(i) != null) || 
                                             Directory.GetDirectories(Path.Combine(UnityGame.InstallPath, "Beat Saber_Data", "CustomLevels"))
                                                 .Select(i => i.Substring(0, Math.Min(i.Length, Id.Length)))
                                                 .Any(i => i == Id);
            
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
            
            if (jsonObject.TryGetValue("collaborators", out var value))
            {
                var collaborators = JArray.Parse(value?.ToString());
                collab.AddRange(collaborators.Select(collaborator => collaborator["name"]?.Value<string>()));
            }
            
            var downloadUrl = versions[0]["downloadURL"]?.Value<string>();

            var previewUrl = versions[0]["previewURL"]?.Value<string>()!;

            AudioClip preview;

            using (var www = UnityWebRequestMultimedia.GetAudioClip(previewUrl, AudioType.MPEG))
            {
                www.SendWebRequest();
                while (!www.isDone) await Task.Delay(10);
                preview = DownloadHandlerAudioClip.GetContent(www);
            }

            var uploadDate = DateTime.ParseExact(jsonObject["uploaded"]?.Value<string>(), 
                "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None);

            var diffsArray = JArray.Parse(versions[0]["diffs"]?.ToString() ?? throw new Exception("This map has no difficulties!"));
            
            var dictionary = new Dictionary<DifficultyModel.CharacteristicTypes, List<DifficultyModel>>();
            
            foreach (var diff in diffsArray)
            {
                var diffModel = DifficultyModel.Parse(diff.ToString());
                
                if (dictionary.ContainsKey(diffModel.Characteristic)) dictionary[diffModel.Characteristic].Add(diffModel);
                else dictionary.Add(diffModel.Characteristic, [diffModel]);
            }

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
                uploadDate, 
                dictionary,
                preview);
        }
    }
}