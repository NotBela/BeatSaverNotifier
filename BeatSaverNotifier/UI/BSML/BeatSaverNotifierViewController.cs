using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverSharp.Models;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace BeatSaverNotifier.UI
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.BeatSaverNotifierView.bsml")]
    internal class BeatSaverNotifierViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private readonly BeatSaverChecker _beatSaverChecker;
        private readonly SiraLog _logger;
        
        [UIComponent("mapList")]
        private readonly CustomListTableData customListTableData = null;

        public BeatSaverNotifierViewController(SiraLog siraLog, BeatSaverChecker beatSaverChecker)
        {
            this._logger = siraLog;
            this._beatSaverChecker = beatSaverChecker;
        }

        private async void OnBeatSaverCheck(List<Beatmap> mapList)
        {
            try
            {
                var mapTableData = new List<CustomListTableData.CustomCellInfo>();
                foreach (var map in mapList)
                {
                    mapTableData.Add(await getCustomListCellData(map));
                }

                customListTableData.Data = mapTableData;
                customListTableData.TableView.ReloadData();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private async Task<CustomListTableData.CustomCellInfo> getCustomListCellData(Beatmap beatmap)
        {
            var imgBytes = await beatmap.LatestVersion.DownloadCoverImage();
            var tex = new Texture2D(2, 2);
            tex.LoadRawTextureData(imgBytes);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            
            return new CustomListTableData.CustomCellInfo(beatmap.Name, beatmap.Uploader.Name, sprite);
        }
        
        public void Initialize()
        {
            if (_beatSaverChecker == null) Plugin.Log.Info("yeah");
            // _beatSaverChecker.OnBeatSaverCheck += OnBeatSaverCheck;
        }
        
        public void Dispose()
        {
            // _beatSaverChecker.OnBeatSaverCheck -= OnBeatSaverCheck;
        }
    }
}