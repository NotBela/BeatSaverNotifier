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
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.Configuration;
using BeatSaverSharp.Models;
using ModestTree;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace BeatSaverNotifier.UI
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.BeatSaverNotifierView.bsml")]
    [HotReload(RelativePathToLayout = @"../UI/BSML/BeatSaverNotifierView.bsml")]
    internal class BeatSaverNotifierViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private BeatSaverChecker _beatSaverChecker;
        private SiraLog _logger;
        private OAuthApi _oAuthApi;
        
        [UIComponent("mapList")]
        private readonly CustomListTableData customListTableData = null;
        
        [Inject]
        public void Inject(SiraLog siraLog, BeatSaverChecker beatSaverChecker, OAuthApi oAuthApi)
        {
            this._logger = siraLog;
            this._beatSaverChecker = beatSaverChecker;
            this._oAuthApi = oAuthApi;
        }

        [UIAction("testButtonOnClick")]
        private async void testButtonOnClick()
        {
            try
            {
                if (string.IsNullOrEmpty(PluginConfig.Instance.refreshToken)) _oAuthApi.startOAuthFlow();
                else await _oAuthApi.getNewToken();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
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
            
            return new CustomListTableData.CustomCellInfo(beatmap.Name, beatmap.Metadata.SongAuthorName, sprite);
        }
        
        public void Initialize()
        {
            _beatSaverChecker.OnBeatSaverCheck += OnBeatSaverCheck;
        }
        
        public void Dispose()
        {
            _beatSaverChecker.OnBeatSaverCheck -= OnBeatSaverCheck;
        }
    }
}