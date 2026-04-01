using CAPYBARA.Bundles;
using CAPYBARA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class InGameOption : MonoBehaviour
    {
        public CPButton closeBtn;
        public AnimatedToggle bgmVolume;
        public AnimatedToggle effectVolume;
        public AnimatedToggle voiceVolume;
        
        public AnimatedToggle reserveBet;
        public AnimatedToggle myTurnViberate;
        public AnimatedToggle fourColor;
        public AnimatedToggle useEmoji;

        public AnimatedToggle jokboInform;

        public AnimatedToggle handrankInform;

        [HideInInspector]public AutoCloseWindow autoCloseWindow;
        public void Init()
        {
            autoCloseWindow = this.GetComponent<AutoCloseWindow>();
            closeBtn.onClick.AddListener(()=>
            {
                autoCloseWindow.CloseThisWindow();
                this.gameObject.SetActive(false);
            });
            
            bgmVolume.toggle.onValueChanged.AddListener(BgmToggleEvent);
            effectVolume.toggle.onValueChanged.AddListener(EffectSoundEvent);
            voiceVolume.toggle.onValueChanged.AddListener(VoiceSoundEvent);
            
            reserveBet.toggle.onValueChanged.AddListener(ReserveBetToggleEvent);
            myTurnViberate.toggle.onValueChanged.AddListener(MyTurnVibeEvent);
            fourColor.toggle.onValueChanged.AddListener(FourColorEvent);
            useEmoji.toggle.onValueChanged.AddListener(UseEmojiEvent);
            jokboInform.toggle.onValueChanged.AddListener(JokboInformEvent);
            handrankInform.toggle.onValueChanged.AddListener(HandRankInformEvent);
        }

        public void OpenWindow()
        {
            this.gameObject.SetActive(true);
            
            bgmVolume.ApplyState( CPPlayer.Cloud.optionValue.bgmSoundOnOff,true);
            effectVolume.ApplyState(CPPlayer.Cloud.optionValue.effectSoundOnOff,true);
            voiceVolume.ApplyState(CPPlayer.Cloud.optionValue.voiceSoundOnOff,true);
            
            reserveBet.ApplyState(CPPlayer.Cloud.optionValue.reserveBet,true);
            myTurnViberate.ApplyState( CPPlayer.Cloud.optionValue.myTurnViberate,true);
            fourColor.ApplyState(CPPlayer.Cloud.optionValue.fourColor,true);
            useEmoji.ApplyState(CPPlayer.Cloud.optionValue.useEmoji, true);
            jokboInform.ApplyState(CPPlayer.Cloud.optionValue.jokboInform,true);
            handrankInform.ApplyState(CPPlayer.Cloud.optionValue.handRankInform,true);
        }

        public void CloseWindow()
        {
            this.gameObject.SetActive(false);
            
        }
        private void BgmToggleEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.bgmSoundOnOff = isOn;
            AudioManager.Instance.SetMute(SoundType.BGM,CPPlayer.Cloud.optionValue.bgmSoundOnOff);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        private void EffectSoundEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.effectSoundOnOff = isOn;
            AudioManager.Instance.SetMute(SoundType.Effect,CPPlayer.Cloud.optionValue.effectSoundOnOff);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        private void VoiceSoundEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.voiceSoundOnOff = isOn;
            AudioManager.Instance.SetMute(SoundType.Voice,CPPlayer.Cloud.optionValue.voiceSoundOnOff);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        private void ReserveBetToggleEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.reserveBet = isOn;
            CPPlayer.Option.ReserveBetChange?.Invoke(isOn);
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
        
        private void JokboInformEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.jokboInform = isOn;
            CPPlayer.Option.JokboUseChange?.Invoke(isOn);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
        
        private void HandRankInformEvent(bool isOn)
        {
            CPPlayer.Cloud.optionValue.handRankInform = isOn;
            CPPlayer.Option.HandRankUseChange?.Invoke(isOn);
            CPPlayer.Cloud.optionValue.UpdateHash().SetDirty(true);
        }
    }

}
