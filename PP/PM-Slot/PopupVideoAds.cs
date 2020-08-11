using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DUNK.DataPool;
using DUNK.Tools;

namespace DUNK.Popup
{
    [Serializable]
    public class ViewAnimation
    {
        public Animation target = null;
        public AnimationClip[] viewAniClip = null;
    }

    public sealed class PopupVideoAds : PopupBaseLayout<PopupVideoAds>
    {
        [SerializeField] private ViewAnimation viewAnimation = new ViewAnimation();
        [SerializeField] private int viewCount = 0;
        [SerializeField] List<UILabel> viewActiveLabel = new List<UILabel>();
        [SerializeField] List<UILabel> viewInActiveLabel = new List<UILabel>();
        
        public static PopupVideoAds Create ()
        {
#if !DUNK_PCKLP
            if (SessionConnection.Instance.CheckOnlineMode() == false)
                return null;
#endif
            PopupVideoAds popup = OnCreate("POPUP.VideoAds");
            return popup;
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ActionOpen += SetupUI;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ActionOpen -= SetupUI;
        }

        public void SetupUI()
        {
            int count = 0;
            if (VideoAdsInfo.Instance.TodayViewCount >= viewAnimation.viewAniClip.Length)
                count = viewAnimation.viewAniClip.Length - 1;
            else
                count = (int)VideoAdsInfo.Instance.TodayViewCount;

            for(int i = 0; i < VideoAdsInfo.Instance.RewardInfo.Count; i++)
            {
                if (VideoAdsInfo.Instance.RewardInfo.ContainsKey(i + 1) == true)
                {
                    string reward = string.Format("{0} view : {1}", i + 1, VideoAdsInfo.Instance.RewardInfo[i + 1].ToString("N0"));
                    viewActiveLabel[i].text = reward;
                    viewInActiveLabel[i].text = reward;
                }
                else
                {
                    viewActiveLabel[i].text = "Not Reward Key";
                    viewInActiveLabel[i].text = "Not Reward Key";
                }
            }

            PlayViewAnimation(count);
        }

        private void PlayViewAnimation(int count)
        {
            CommonTools.PlayAnimation(viewAnimation.target, viewAnimation.viewAniClip[count].name);
        }

        private void OnClickShowVideo()
        {
            if (Session.Instance != null)
            {
                if (Session.Instance.ShowVideo == false)
                {
                    PopupMessage.Create("POPUP.SHOW.VideoAds.Title", "POPUP.SHOW.VideoAds.Description", 0f, PopupMessage.Type.Ok);
                    return;
                }
            }
            
            VideoAdsSystem.Instance.ShowVideo();
            base.Close();
        }
    }
}
