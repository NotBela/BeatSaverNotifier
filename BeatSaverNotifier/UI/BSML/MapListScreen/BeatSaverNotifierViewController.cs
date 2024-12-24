using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Tags;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverNotifier.BeatSaver;
using BeatSaverNotifier.BeatSaver.Auth;
using BeatSaverNotifier.BeatSaver.Models;
using BeatSaverNotifier.Configuration;
using BeatSaverNotifier.UI.FlowCoordinators;
using HMUI;
using SiraUtil.Logging;
using SongCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaverNotifier.UI.BSML.MapListScreen
{
    [ViewDefinition("BeatSaverNotifier.UI.BSML.MapListScreen.BeatSaverNotifierView.bsml")]
    internal class BeatSaverNotifierViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private BeatSaverChecker _beatSaverChecker;
        private SiraLog _logger;
        private OAuthApi _oAuthApi;
        private MapQueueManager _mapQueueManager;
        private BeatSaverNotifierFlowCoordinator flowCoordinator;

        private List<BeatmapModel> _beatmapsInList = new();
        private BeatmapModel _selectedBeatmap;
        private DifficultyModel.CharacteristicTypes _selectedCharacteristic;
        
        public bool areMapsInQueue => _beatmapsInList.Any();
        
        [UIParams] private readonly BSMLParserParams parserParams = null;
        
        [UIComponent("mapList")]
        private readonly CustomListTableData customListTableData = null;
        
        [Inject]
        public void Inject(SiraLog siraLog, BeatSaverChecker beatSaverChecker, OAuthApi oAuthApi, MapQueueManager mapQueueManager, BeatSaverNotifierFlowCoordinator flowCoordinator)
        {
            this._logger = siraLog;
            this._beatSaverChecker = beatSaverChecker;
            this._oAuthApi = oAuthApi;
            this._mapQueueManager = mapQueueManager;
            this.flowCoordinator = flowCoordinator;
        }
        
        [UIComponent("rightPanelContainer")] private readonly HorizontalLayoutGroup _rightPanelContainer = null;
        
        [UIComponent("songNameText")] private readonly TextMeshProUGUI mapNameText = null;
        [UIComponent("songAuthorText")] private readonly TextMeshProUGUI songAuthorText = null;
        [UIComponent("songSubNameText")] private readonly TextMeshProUGUI songSubNameText = null;
        [UIComponent("coverArtImage")] private readonly Image coverArtImage = null;
        
        [UIComponent("downloadButton")] private readonly Button downloadButton = null;
        [UIComponent("ignoreButton")] private readonly Button ignoreButton = null;
        
        // dont think theres a better way to do this
        [UIComponent("StandardCharacteristicTab")] private readonly Tab _standardCharacteristicTab = null;
        [UIComponent("OneSaberCharacteristicTab")] private readonly Tab _oneSaberCharacteristicTab = null;
        [UIComponent("NoArrowsCharacteristicTab")] private readonly Tab _noArrowsCharacteristicTab = null;
        [UIComponent("ThreeSixtyDegreeCharacteristicTab")] private readonly Tab _threeSixtyDegreeCharacteristicTab = null;
        [UIComponent("NinetyDegreeCharacteristicTab")] private readonly Tab _ninetyDegreeCharacteristicTab = null;
        [UIComponent("LawlessCharacteristicTab")] private readonly Tab _lawlessCharacteristicTab = null;
        [UIComponent("LegacyCharacteristicTab")] private readonly Tab _legacyCharacteristicTab = null;
        [UIComponent("LightshowCharacteristicTab")] private readonly Tab _lightshowCharacteristicTab = null;
        
        [UIComponent("ExPlusDifficultyTab")] private readonly Tab _exPlusDifficultyTab = null;
        [UIComponent("ExDifficultyTab")] private readonly Tab _exDifficultyTab = null;
        [UIComponent("HardDifficultyTab")] private readonly Tab _hardDifficultyTab = null;
        [UIComponent("NormalDifficultyTab")] private readonly Tab _normalDifficultyTab = null;
        [UIComponent("EasyDifficultyTab")] private readonly Tab _easyDifficultyTab = null;
        
        
        [UIAction("ignoreButtonOnClick")]
        private void IgnoreButtonOnClick()
        {
            if (!PluginConfig.Instance.keysToIgnore.Contains(_selectedBeatmap.Id)) 
                PluginConfig.Instance.keysToIgnore.Add(_selectedBeatmap.Id);
            
            _rightPanelContainer.gameObject.SetActive(false);
            
            _beatmapsInList.Remove(_selectedBeatmap);
            _beatSaverChecker.removeFromCachedMaps(_selectedBeatmap);
            customListTableData.Data.RemoveAt(_beatmapsInList.IndexOf(_selectedBeatmap));
            _selectedBeatmap = null;
            customListTableData.TableView.ReloadData();
        }
        
        [UIAction("downloadButtonOnClick")]
        private async void DownloadButtonOnClick()
        {
            try
            {
                downloadButton.SetButtonText("Downloading...");
                downloadButton.interactable = false;
                ignoreButton.interactable = false;

                customListTableData.Data.RemoveAt(_beatmapsInList.IndexOf(_selectedBeatmap));
                _beatSaverChecker.removeFromCachedMaps(_selectedBeatmap);
                _beatmapsInList.Remove(_selectedBeatmap);
                customListTableData.TableView.ReloadData();
                customListTableData.TableView.ClearSelection();
                
                await _mapQueueManager.addMapToQueue(_selectedBeatmap);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                showErrorModal();
            }
        }

        [UIAction("errorModalOkButtonOnClick")]
        private void errorModalOkButtonOnClick() => parserParams.EmitEvent("errorModalHide");
        
        private void showErrorModal() => parserParams.EmitEvent("errorModalShow");
        
        [UIAction("refreshButtonOnClick")]
        private async void refreshButtonOnClick()
        {
            try
            {
                _rightPanelContainer.gameObject.SetActive(false);
                customListTableData.TableView.ClearSelection();
                await _beatSaverChecker.CheckBeatSaverAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                showErrorModal();
            }
        }
        
        [UIAction("#post-parse")]
        void postParse()
        {
            coverArtImage.material = Resources.FindObjectsOfTypeAll<Material>()
                .FirstOrDefault(m => m.name == "UINoGlowRoundEdge");
            
            _beatmapsInList = _beatSaverChecker.CachedMaps.ToList();
            ReloadTableData();
        }

        [UIAction("characteristicTabOnSelect")]
        private void updateCharacteristicTabOnSelect(TextSegmentedControl textSegmentedControl, int idx)
        {
            _selectedCharacteristic = textSegmentedControl.cells[idx].GetComponentInChildren<TextMeshProUGUI>().text switch
            {
                "Standard" => DifficultyModel.CharacteristicTypes.Standard,
                "OneSaber" => DifficultyModel.CharacteristicTypes.OneSaber,
                "NoArrows" => DifficultyModel.CharacteristicTypes.NoArrows,
                "Legacy" => DifficultyModel.CharacteristicTypes.Legacy,
                "360Degree" => DifficultyModel.CharacteristicTypes.ThreeSixtyDegree,
                "90Degree" => DifficultyModel.CharacteristicTypes.NintetyDegree,
                "Lawless" => DifficultyModel.CharacteristicTypes.Lawless,
                "Lightshow" => DifficultyModel.CharacteristicTypes.Lightshow,
                _ => DifficultyModel.CharacteristicTypes.Unknown
            };
            
            _easyDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Easy);
            _normalDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Normal);
            _hardDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Hard);
            _exDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Expert);
            _exPlusDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.ExpertPlus);
        }

        [UIAction("coverArtOnClick")]
        private void coverArtOnClick() => Process.Start($"https://beatsaver.com/maps/{_selectedBeatmap.Id}");
        
        [UIAction("onCellSelect")]
        private void onCellSelected(TableView tableView, int index)
        {
            try
            {
                _selectedBeatmap = _beatmapsInList[index];
                _selectedCharacteristic = _selectedBeatmap.DifficultyDictionary.Keys.First();
                
                _rightPanelContainer.gameObject.SetActive(true);
                
                songSubNameText.text = _selectedBeatmap.SongSubName;
                mapNameText.text = $"{_selectedBeatmap.SongName}";
                songAuthorText.text = _selectedBeatmap.Author;
                
                // THIS SUUUUUUUCKS
                // AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                _standardCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.Standard);
                _oneSaberCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.OneSaber);
                _noArrowsCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.NoArrows);
                _threeSixtyDegreeCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.ThreeSixtyDegree);
                _ninetyDegreeCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.NintetyDegree);
                _legacyCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.Legacy);
                _lawlessCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.Lawless);
                _lightshowCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.Lightshow);
                
                _easyDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Easy);
                _normalDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Normal);
                _hardDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Hard);
                _exDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Expert);
                _exPlusDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.ExpertPlus);
                
                bool mapIsQueuedOrDownloaded = _mapQueueManager.readOnlyQueue.Contains(_selectedBeatmap) || Loader.GetLevelByHash(_selectedBeatmap.VersionHashes[0]) != null;
                downloadButton.interactable = !mapIsQueuedOrDownloaded;
                ignoreButton.interactable = !mapIsQueuedOrDownloaded;
                downloadButton.SetButtonText(mapIsQueuedOrDownloaded ? "Downloading..." : "Download");
                
                coverArtImage.sprite = _selectedBeatmap.CoverSprite;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                showErrorModal();
            }
        }
        
        private void OnBeatSaverCheckFinished(List<BeatmapModel> mapList)
        { 
            flowCoordinator.switchToView(BeatSaverNotifierFlowCoordinator.FlowState.MapList); 
            _beatmapsInList = mapList;
            ReloadTableData();
        }
        
        public void ReloadTableData()
        {
            if (customListTableData == null) return;
            
            customListTableData.Data = _beatmapsInList.Select(i => i.getCustomListCellInfo()).ToList();
            customListTableData.TableView.ReloadData();
        }

        public async void Initialize()
        {
            try
            {
                _beatSaverChecker.OnBeatSaverCheckFinished += OnBeatSaverCheckFinished;
            
                await _beatSaverChecker.CheckBeatSaverAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public void Dispose()
        {
            _beatSaverChecker.OnBeatSaverCheckFinished -= OnBeatSaverCheckFinished;
        }
    }
}