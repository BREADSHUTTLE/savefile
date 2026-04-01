using CAPYBARA.Bundles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA.Bundles
{
    public class ViewOption : BackButtonView
    {
        [Header("Close")]
        public CPButton closeBtn;

        [Header("Tab")]
        public UISegmentedControlGroup tabGroup;

        [Header("Section Anchors (스크롤 타겟)")]
        public RectTransform sectionGame;
        public RectTransform sectionChat;
        public RectTransform sectionSound;

        [Header("Game Toggles")]
        public AnimatedToggle reserveBet;
        public AnimatedToggle myTurnViberate;
        public AnimatedToggle fourColor;
        public AnimatedToggle useEmoji;

        [Header("Chat Toggles")]
        public AnimatedToggle chatVerticalMode;

        [Header("Sound Toggles")]
        public AnimatedToggle allSound;

        [Header("Volume Sliders")]
        public AnimatedSlider bgmVolume;
        public TMP_Text bgmVolumeText;
        public AnimatedSlider effectVolume;
        public TMP_Text effectVolumeText;
        public AnimatedSlider voiceVolume;
        public TMP_Text voiceVolumeText;

        [Header("Links")]
        public CPButton termsService;
        public CPButton privacyPolicy;
        public CPButton operatingPolicy;
        
        [Header("Account")]
        public CPButton logoutBtn;

        [Header("Scroll")]
        public ScrollRect scrollRect;
    }
}

