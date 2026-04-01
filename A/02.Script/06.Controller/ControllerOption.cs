using System;
using CAPYBARA.Bundles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using CAPYBARA.Model;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace CAPYBARA.Core
{
    public class ControllerOption
    {
        ViewOption _viewcanvasOption;
        private bool _isScrollingByTab;
        private Tween _scrollTween;
        
        private CancellationTokenSource _cts;

        public ControllerOption(ViewOption view, CancellationTokenSource cts)
        {
            _cts = cts;

            _viewcanvasOption = view;

            if (_viewcanvasOption.tabGroup != null)
                _viewcanvasOption.tabGroup.onIndexChanged += OnTabIndexChanged;

            if (_viewcanvasOption.scrollRect != null)
                _viewcanvasOption.scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            _viewcanvasOption.reserveBet.toggle.onValueChanged.AddListener(ReserveBetToggleEvent);
            _viewcanvasOption.myTurnViberate.toggle.onValueChanged.AddListener(MyTurnVibeEvent);
            _viewcanvasOption.fourColor.toggle.onValueChanged.AddListener(FourColorEvent);
            _viewcanvasOption.useEmoji.toggle.onValueChanged.AddListener(UseEmojiEvent);
            
            _viewcanvasOption.chatVerticalMode.toggle.onValueChanged.AddListener(ChatVerticalModeEvent);
            
            _viewcanvasOption.allSound.toggle.onValueChanged.AddListener(AllsoundEvent);

            _viewcanvasOption.bgmVolume.onValueChanged += BgmVolumSliderEvent;
            _viewcanvasOption.effectVolume.onValueChanged += EffectVolumSliderEvent;
            _viewcanvasOption.voiceVolume.onValueChanged += VoiceVolumeSliderEvent;

            _viewcanvasOption.termsService.onClick.AddListener(TermsOfServiceOpen);
            _viewcanvasOption.privacyPolicy.onClick.AddListener(PrivatePolicyOpen);
            _viewcanvasOption.operatingPolicy.onClick.AddListener(OperatingUrlOpen);
            _viewcanvasOption.logoutBtn.onClick.AddListener(LogOut);
            _viewcanvasOption.closeBtn.onClick.AddListener(() => _viewcanvasOption.gameObject.SetActive(false));

            CPPlayer.OutGame.openOptionUI += OpenWindow;

            NewDayInit();
            TimeSaveLoop(_cts.Token).Forget();
        }

        void OpenWindow()
        {
            ResetScrollPosition();
            
            _viewcanvasOption.gameObject.SetActive(true);
            
            _viewcanvasOption.reserveBet.ApplyState(CPPlayer.Cloud.optionValue.reserveBet, true);
            _viewcanvasOption.myTurnViberate.ApplyState(CPPlayer.Cloud.optionValue.myTurnViberate, true);
            _viewcanvasOption.fourColor.ApplyState(CPPlayer.Cloud.optionValue.fourColor, true);
            _viewcanvasOption.useEmoji.ApplyState(CPPlayer.Cloud.optionValue.useEmoji, true);
            
            _viewcanvasOption.chatVerticalMode.ApplyState(CPPlayer.Cloud.optionValue.chatVerticalMode, true);
            
            _viewcanvasOption.allSound.ApplyState(CPPlayer.Cloud.optionValue.allSoundOnOff, true);

            _viewcanvasOption.bgmVolume.SetValueWithoutNotify(CPPlayer.Cloud.optionValue.bgmVolum);
            _viewcanvasOption.effectVolume.SetValueWithoutNotify(CPPlayer.Cloud.optionValue.effectVolum);
            _viewcanvasOption.voiceVolume.SetValueWithoutNotify(CPPlayer.Cloud.optionValue.voiceVolum);
            
            UpdateVolumeText(_viewcanvasOption.bgmVolumeText, _viewcanvasOption.bgmVolume.NormalizedValue);
            UpdateVolumeText(_viewcanvasOption.effectVolumeText, _viewcanvasOption.effectVolume.NormalizedValue);
            UpdateVolumeText(_viewcanvasOption.voiceVolumeText, _viewcanvasOption.voiceVolume.NormalizedValue);
        }
        
        private void UpdateVolumeText(TMPro.TMP_Text text, float value)
        {
            if (text != null)
                text.text = Mathf.RoundToInt(value * 100).ToString();
        }

        private void ResetScrollPosition()
        {
            _viewcanvasOption.scrollRect.normalizedPosition = new Vector2(0, 1);
        }

        private void OnTabIndexChanged(int index)
        {
            RectTransform targetSection = index switch
            {
                0 => _viewcanvasOption.sectionGame,
                1 => _viewcanvasOption.sectionChat,
                2 => _viewcanvasOption.sectionSound,
                _ => null
            };
            
            ScrollToSection(targetSection);
        }

        private void ScrollToSection(RectTransform section)
        {
            if (section == null || _viewcanvasOption.scrollRect == null) 
                return;

            var scrollRect = _viewcanvasOption.scrollRect;
            var content = scrollRect.content;
            var viewport = scrollRect.viewport;

            float contentHeight = content.rect.height;
            float viewportHeight = viewport.rect.height;

            float scrollableHeight = contentHeight - viewportHeight;
            
            if (scrollableHeight <= 0) 
                return;

            float sectionY = Mathf.Abs(section.anchoredPosition.y);
            float sectionPivotOffset = section.rect.height * section.pivot.y;
            float targetScrollY = sectionY - sectionPivotOffset;
            float normalizedY = 1f - Mathf.Clamp01(targetScrollY / scrollableHeight);

            _isScrollingByTab = true;
            _scrollTween?.Kill();
            _scrollTween = DOTween.To(() => scrollRect.verticalNormalizedPosition, x => scrollRect.verticalNormalizedPosition = x, normalizedY, 0.3f)
                                    .SetEase(Ease.OutQuad)
                                    .OnComplete(() => _isScrollingByTab = false)
                                    .OnKill(() => _isScrollingByTab = false);
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            if (_isScrollingByTab)
                return;

            int sectionIndex = GetCurrentSectionIndex();
            
            if (_viewcanvasOption.tabGroup != null && _viewcanvasOption.tabGroup.CurrentIndex != sectionIndex)
                _viewcanvasOption.tabGroup.SetActiveToggle(sectionIndex, false);
        }

        private int GetCurrentSectionIndex()
        {
            var scrollRect = _viewcanvasOption.scrollRect;
            if (scrollRect == null)
                return 0;

            var content = scrollRect.content;
            var viewport = scrollRect.viewport;

            float contentHeight = content.rect.height;
            float viewportHeight = viewport.rect.height;
            float scrollableHeight = contentHeight - viewportHeight;

            if (scrollableHeight <= 0)
                return 0;

            float currentNormY = scrollRect.verticalNormalizedPosition;

            RectTransform[] sections = {_viewcanvasOption.sectionGame, _viewcanvasOption.sectionChat, _viewcanvasOption.sectionSound};
            int result = 0;
            float[] sectionNormYs = new float[sections.Length];

            for (int i = 0; i < sections.Length; i++)
            {
                if (sections[i] == null)
                    continue;

                float sectionY = Mathf.Abs(sections[i].anchoredPosition.y);
                float sectionPivotOffset = sections[i].rect.height * sections[i].pivot.y;
                float targetScrollY = sectionY - sectionPivotOffset;
                sectionNormYs[i] = 1f - Mathf.Clamp01(targetScrollY / scrollableHeight);

                if (currentNormY <= sectionNormYs[i])
                    result = i;
            }

            int currentTab = _viewcanvasOption.tabGroup != null ? _viewcanvasOption.tabGroup.CurrentIndex : 0;
            if (result < currentTab)
            {
                float buffer = 30f / scrollableHeight;
                if (currentNormY < sectionNormYs[result] - buffer)
                    result = currentTab;
            }

            return result;
        }

        private void ReserveBetToggleEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.reserveBet = isOn;
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        private void MyTurnVibeEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.myTurnViberate = isOn;
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        private void FourColorEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.fourColor = isOn;
            CPPlayer.Option.FourCardModeChange?.Invoke(isOn);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        private void UseEmojiEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.useEmoji = isOn;
            CPPlayer.Option.EmojiUseChange?.Invoke(isOn);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        
        private void ChatVerticalModeEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.chatVerticalMode = isOn;
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        
        private void AllsoundEvent(bool _value)
        {
            CPPlayer.Cloud.optionValue.allSoundOnOff = _value;
            float _volume = CPPlayer.Cloud.optionValue.allSoundOnOff ? 1 : 0;
            AudioManager.Instance.SetAllVolume(_volume);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        private void BgmVolumSliderEvent(float _value)
        {
            CPPlayer.Cloud.optionValue.bgmVolum = _value;
            AudioManager.Instance.SetVolume(SoundType.BGM,CPPlayer.Cloud.optionValue.bgmVolum);
            UpdateVolumeText(_viewcanvasOption.bgmVolumeText, _viewcanvasOption.bgmVolume.NormalizedValue);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }

        private void EffectVolumSliderEvent(float _value)
        {
            CPPlayer.Cloud.optionValue.effectVolum = _value;
            AudioManager.Instance.SetVolume(SoundType.Effect,CPPlayer.Cloud.optionValue.effectVolum);
            UpdateVolumeText(_viewcanvasOption.effectVolumeText, _viewcanvasOption.effectVolume.NormalizedValue);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }

        private void VoiceVolumeSliderEvent(float _value)
        {
            CPPlayer.Cloud.optionValue.voiceVolum = _value;
            AudioManager.Instance.SetVolume(SoundType.Voice,CPPlayer.Cloud.optionValue.voiceVolum);
            UpdateVolumeText(_viewcanvasOption.voiceVolumeText, _viewcanvasOption.voiceVolume.NormalizedValue);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }

        private void TermsOfServiceOpen()
        {
            Application.OpenURL(Constraints.TermsOfServiceUrl);
        }
        private void PrivatePolicyOpen()
        {
            Application.OpenURL(Constraints.privacypolicyUrl);
        }
        private void OperatingUrlOpen()
        {
            Application.OpenURL(Constraints.operatingpolicyUrl);
        }

        private const string LogOutSceneName = "Loading";
        private void LogOut()
        {
            
            LogoutAsync().Forget();
           
        }

        async UniTask LogoutAsync()
        {
            var json = JsonUtility.ToJson(CPPlayer.Cloud);
            await Services.Lobby.UserSettingsSetReq(json);
            
            LocalSaveLoader.DeleteCloudData();
  
       
            ConnectionManager.Instance.Dispose();
            PoolManager.Clear();
            PopupManager.Instance.CloseAll();
            
            SceneManager.LoadScene(LogOutSceneName);
            _viewcanvasOption.gameObject.SetActive(false);
        }

        private float autoSaveTimeforLocalSave = 3f;
        private float elapsedTimeforLocalSave = 0;
        
        private float autoSaveTime = 1800f;
        private float elapsedTime = 0;
        private async UniTask TimeSaveLoop(CancellationToken token)
        {
            while (true)
            {
                elapsedTimeforLocalSave += Time.deltaTime;
                if (elapsedTimeforLocalSave > autoSaveTimeforLocalSave)
                {
                    elapsedTimeforLocalSave = 0;
                    NewDayInit();
                    LocalSaveLoader.SaveUserCloudData();    
                }

                elapsedTime += Time.deltaTime;
                if (elapsedTime > autoSaveTime)
                {
                    elapsedTime = 0;
                    NewDayInit();
                    var json = JsonUtility.ToJson(CPPlayer.Cloud);
                    Services.Lobby.UserSettingsSetReq(json).Forget();
                }
                await UniTask.Yield(token);
            }
        }

        void NewDayInit()
        {
            int savedDay = CPPlayer.Cloud.optionValue.lastSaveDay;
            if (LocalSaveLoader.IsNewDay(savedDay))
            {
                LocalSaveLoader.SaveUserCloudData();
            }
        }
    }
}
