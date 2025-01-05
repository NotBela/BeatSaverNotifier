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
using HarmonyLib;
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
        [Inject] private readonly BeatSaverChecker _beatSaverChecker = null!;
        [Inject] private readonly SiraLog _logger = null!;
        [Inject] private readonly MapQueueManager _mapQueueManager = null!;
        [Inject] private readonly BeatSaverNotifierFlowCoordinator flowCoordinator = null!;
        [Inject] private readonly SongPreviewController _songPreviewController = null!;

        private List<BeatmapModel> _beatmapsInList = new();
        
        private BeatmapModel _selectedBeatmap;
        private DifficultyModel.CharacteristicTypes _selectedCharacteristic;

        private bool areMapsInQueue => _beatmapsInList.Any();
        
        [UIParams] private readonly BSMLParserParams parserParams = null;
        
        [UIComponent("mapList")]
        private readonly CustomListTableData customListTableData = null;
        
        [UIComponent("rightPanelContainer")] private readonly HorizontalLayoutGroup _rightPanelContainer = null;
        
        [UIComponent("mapListVertical")] private readonly VerticalLayoutGroup _mapListVertical = null;
        [UIComponent("noMapsVertical")] private readonly VerticalLayoutGroup _noMapsVertical = null;
        
        [UIComponent("downloadAllModalText")] private readonly TextMeshProUGUI _downloadAllModalText = null;
        
        [UIComponent("songNameText")] private readonly TextMeshProUGUI mapNameText = null;
        [UIComponent("songAuthorText")] private readonly TextMeshProUGUI songAuthorText = null;
        [UIComponent("songSubNameText")] private readonly TextMeshProUGUI songSubNameText = null;
        [UIComponent("coverArtImage")] private readonly Image coverArtImage = null;
        
        [UIComponent("downloadButton")] private readonly Button downloadButton = null;
        [UIComponent("ignoreButton")] private readonly Button ignoreButton = null;
        
        [UIComponent("npsText")] private readonly TextMeshProUGUI _npsText = null;
        [UIComponent("noteCountText")] private readonly TextMeshProUGUI _noteCountText = null;
        [UIComponent("wallCountText")] private readonly TextMeshProUGUI _wallCountText = null;
        [UIComponent("bombCountText")] private readonly TextMeshProUGUI _bombCountText = null;
        
        [UIComponent("characteristicTabSelector")] private readonly TabSelector _characteristicTabSelector = null;
        [UIComponent("difficultyTabSelector")] private readonly TabSelector _difficultyTabSelector = null;
        
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
        
        [UIComponent("downloadAllButton")] private readonly Button _downloadAllButton = null;
        
        
        [UIAction("ignoreButtonOnClick")]
        private void IgnoreButtonOnClick()
        {
            if (!PluginConfig.Instance.keysToIgnore.Contains(_selectedBeatmap.Id)) 
                PluginConfig.Instance.keysToIgnore.Add(_selectedBeatmap.Id);
            
            _rightPanelContainer.gameObject.SetActive(false);
            
            updateBeatmapList();
            
            _selectedBeatmap = null;
            showOrHideNoMapsVertical();
        }

        private void updateBeatmapList()
        {
            customListTableData.Data.RemoveAt(_beatmapsInList.IndexOf(_selectedBeatmap));
            _beatSaverChecker.removeFromCachedMaps(_selectedBeatmap);
            _beatmapsInList.Remove(_selectedBeatmap);
            customListTableData.TableView.ReloadData();
            customListTableData.TableView.ClearSelection();
        }
        
        [UIAction("downloadButtonOnClick")]
        private async void DownloadButtonOnClick()
        {
            try
            {
                downloadButton.SetButtonText("Downloading...");
                downloadButton.interactable = false;
                ignoreButton.interactable = false;

                updateBeatmapList();
                
                await _mapQueueManager.addMapToQueue(_selectedBeatmap);
                _rightPanelContainer.gameObject.SetActive(areMapsInQueue);
                showOrHideNoMapsVertical();
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

        [UIAction("downloadAllButtonOnClick")]
        private void downloadAllButtonOnClick()
        {
            _downloadAllModalText.text = $"Are you sure you want to download {_beatmapsInList.Count} maps?";
            parserParams.EmitEvent("downloadAllModalShow");
        }
        
        [UIAction("downloadAllModalDenyOnClick")]
        private void downloadAllModalDenyOnClick() => parserParams.EmitEvent("downloadAllModalHide");
        
        [UIAction("#post-parse")]
        void postParse()
        {
            coverArtImage.material = Resources.FindObjectsOfTypeAll<Material>()
                .FirstOrDefault(m => m.name == "UINoGlowRoundEdge");
            
            _beatmapsInList = _beatSaverChecker.CachedMaps.ToList();
            ReloadTableData();
            showOrHideNoMapsVertical();
        }

        [UIAction("downloadAllModalConfirmOnClick")]
        private void downloadAllModalConfirmOnClick()
        {
            parserParams.EmitEvent("downloadAllModalHide");
            _rightPanelContainer.gameObject.SetActive(false);
            _selectedBeatmap = null; 
            customListTableData.Data.Clear(); 
            customListTableData.TableView.ReloadData();
            _downloadAllButton.interactable = false;
            _beatmapsInList.Do(async void (i) =>
            {
                try
                {
                    await _mapQueueManager.addMapToQueue(i);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            });
            _beatmapsInList.Clear();
        }

        private DifficultyModel.CharacteristicTypes getSelectedCharacteristicFromText(string text) => text switch
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
        
        private void resetSelectedDifficulty() => _difficultyTabSelector.TextSegmentedControl.cells.Last().SetSelected(true, SelectableCell.TransitionType.Instant, this, false);
        
        private void resetSelectedCharacteristic() => _characteristicTabSelector.TextSegmentedControl.cells.First().SetSelected(true, SelectableCell.TransitionType.Instant, this, false);
        
        private DifficultyModel.DifficultyTypes getSelectedDifficultyFromText(string text) => text switch
            {
                "Easy" => DifficultyModel.DifficultyTypes.Easy,
                "Normal" => DifficultyModel.DifficultyTypes.Normal,
                "Hard" => DifficultyModel.DifficultyTypes.Hard,
                "Expert" => DifficultyModel.DifficultyTypes.Expert,
                "Expert+" => DifficultyModel.DifficultyTypes.ExpertPlus,
                _ => DifficultyModel.DifficultyTypes.Unknown
            };

        [UIAction("difficultyTabOnSelect")]
        private void difficultyTabOnSelect(TextSegmentedControl textSegmentedControl, int idx)
        {
            var selectedDiffType = getSelectedDifficultyFromText(textSegmentedControl.cells[idx].GetComponentInChildren<TextMeshProUGUI>().text);

            var selectedDiffData = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].First(i => i.Difficulty == selectedDiffType);

            _npsText.text = selectedDiffData.NotesPerSecond.ToString("F2", CultureInfo.InvariantCulture);
            _noteCountText.text = selectedDiffData.NoteCount.ToString(CultureInfo.InvariantCulture);
            _bombCountText.text = selectedDiffData.BombCount.ToString(CultureInfo.InvariantCulture);
            _wallCountText.text = selectedDiffData.WallCount.ToString(CultureInfo.InvariantCulture);
        }

        [UIAction("characteristicTabOnSelect")]
        private void characteristicTabOnSelect(TextSegmentedControl textSegmentedControl, int idx)
        {
            _selectedCharacteristic = getSelectedCharacteristicFromText(textSegmentedControl.cells[idx].GetComponentInChildren<TextMeshProUGUI>().text);
            
            showCorrectDifficultyTabs();

            var selectedDiffData = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Last();
            
            _npsText.text = selectedDiffData.NotesPerSecond.ToString("F2", CultureInfo.InvariantCulture);
            _noteCountText.text = selectedDiffData.NoteCount.ToString(CultureInfo.InvariantCulture);
            _bombCountText.text = selectedDiffData.BombCount.ToString(CultureInfo.InvariantCulture);
            _wallCountText.text = selectedDiffData.WallCount.ToString(CultureInfo.InvariantCulture);
            
            resetSelectedDifficulty();
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
                mapNameText.text = _selectedBeatmap.SongName;
                songAuthorText.text = _selectedBeatmap.Author;
                
                showCorrectCharacteristicTabs();
                showCorrectDifficultyTabs();
                resetSelectedCharacteristic();
                resetSelectedDifficulty();

                var selectedDiffData = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Last();
                
                _songPreviewController.playPreview(_selectedBeatmap.PreviewAudioClip);

                _npsText.text = selectedDiffData.NotesPerSecond.ToString("F2", CultureInfo.InvariantCulture);
                _noteCountText.text = selectedDiffData.NoteCount.ToString(CultureInfo.InvariantCulture);
                _bombCountText.text = selectedDiffData.BombCount.ToString(CultureInfo.InvariantCulture);
                _wallCountText.text = selectedDiffData.WallCount.ToString(CultureInfo.InvariantCulture);
                
                bool mapIsQueuedOrDownloaded = _mapQueueManager.readOnlyQueue.Contains(_selectedBeatmap) || BeatSaverChecker.mapAlreadyDownloaded(_selectedBeatmap);
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

        private void showCorrectCharacteristicTabs()
        {
            _standardCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.Standard);
            _oneSaberCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.OneSaber);
            _noArrowsCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.NoArrows);
            _threeSixtyDegreeCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.ThreeSixtyDegree);
            _ninetyDegreeCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.NintetyDegree);
            _legacyCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.Legacy);
            _lawlessCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.Lawless);
            _lightshowCharacteristicTab.IsVisible = _selectedBeatmap.DifficultyDictionary.ContainsKey(DifficultyModel.CharacteristicTypes.Lightshow);
        }

        private void showCorrectDifficultyTabs()
        {
            _easyDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Easy);
            _normalDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Normal);
            _hardDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Hard);
            _exDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.Expert);
            _exPlusDifficultyTab.IsVisible = _selectedBeatmap.DifficultyDictionary[_selectedCharacteristic].Any(i => i.Difficulty == DifficultyModel.DifficultyTypes.ExpertPlus);
        }

        private void showOrHideNoMapsVertical()
        {
            if (_noMapsVertical != null) _noMapsVertical.gameObject.SetActive(!areMapsInQueue);
            if (_mapListVertical != null) _mapListVertical.gameObject.SetActive(areMapsInQueue);
            if (_downloadAllButton != null) _downloadAllButton.interactable = areMapsInQueue;
            if (ignoreButton != null) ignoreButton.interactable = areMapsInQueue;
        }
        
        private void OnBeatSaverCheckFinished(List<BeatmapModel> mapList)
        { 
            flowCoordinator.switchToView(BeatSaverNotifierFlowCoordinator.FlowState.MapList); 
            _beatmapsInList = mapList;
            ReloadTableData();
            showOrHideNoMapsVertical();
        }
        
        public void ReloadTableData()
        {
            if (customListTableData == null) return;
            
            customListTableData.Data = _beatmapsInList.Select(i => i.getCustomListCellInfo()).ToList();
            customListTableData.TableView.ReloadData();
        }

        public void Initialize()
        {
            _beatSaverChecker.OnBeatSaverCheckFinished += OnBeatSaverCheckFinished;
        }

        public void Dispose()
        {
            _beatSaverChecker.OnBeatSaverCheckFinished -= OnBeatSaverCheckFinished;
        }
    }
}