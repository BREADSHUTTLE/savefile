// MissionItem.cs - MissionItem implementation file
//
// Description      : MissionItem
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/07/12
// Last Update      : 2019/07/12
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dorothy;
using Dorothy.Network;
using Dorothy.DataPool;
using Dorothy.UI.Popup;

namespace Dorothy.UI
{
    public class MissionItem : MonoBehaviour
    {
        enum Type
        {
            Completed,
            Quest,
            Lock,
        }

        //[Serializable] public class ItemQuestFX
        //{
        //    public Animation target = null;
        //    public AnimationClip idle = null;
        //    public AnimationClip collect = null;
        //}

        //[SerializeField] private ItemQuestFX itemQuestFX = new ItemQuestFX();

        [SerializeField] private PopupMission popupMission;
        [SerializeField] private RectTransform completedItem;
        [SerializeField] private RectTransform questItem;
        [SerializeField] private RectTransform lockItem;
        [SerializeField] private Image questGauge;
        [SerializeField] private Text questInfo;
        [SerializeField] private Text questPercent;

        [SerializeField] private RectTransform progressRect;
        [SerializeField] private RectTransform collectRect;
        [SerializeField] private Text[] collectText;
        [SerializeField] private Button collectButton;
        
        private MissionStatus itemData = null;
        private Type itemType = Type.Lock;

		private Action updateCallback;
        public void Build(MissionStatus data, Action updateCallback)
        {
			this.updateCallback = updateCallback;

			if (data == null)
            {
                itemType = Type.Lock;
            }
            else
            {
                if (data.receivedReward == false)
                //if (MissionInfo.Instance.CheckCurrentMission(data.missionId))
                {
                    itemType = Type.Quest;
                }
                else
                {
                    itemType = Type.Completed;
                }
            }

            itemData = data;

            SetRect();
            SetItem();
            SetCollect();
            SetQuestGauge();
            SetQuestText();
        }

        private void SetRect()
        {
            RectTransform myRect = gameObject.GetComponent<RectTransform>();

            if (itemType == Type.Completed)
                myRect.sizeDelta = completedItem.sizeDelta;
            else if (itemType == Type.Quest)
                myRect.sizeDelta = questItem.sizeDelta;
            else if (itemType == Type.Lock)
                myRect.sizeDelta = lockItem.sizeDelta;
        }

        private void SetItem()
        {
            CommonTools.SetActive(completedItem, itemType == Type.Completed);
            CommonTools.SetActive(questItem, itemType == Type.Quest);
            CommonTools.SetActive(lockItem, itemType == Type.Lock);
        }

        private void SetCollect()
        {
            if (itemData == null)
                return;

            var curData = MissionInfo.Instance.MissionData.currentMission;
            if (curData == null)
                return;

            //bool collect = (itemData.val / curData.type1ValN) == 1 ? true : false;
            bool collect = (itemData.val >= curData.type1ValN) ? true : false;

            if (progressRect != null)
                CommonTools.SetActive(progressRect, !collect);

            if (collectRect != null)
            {
                CommonTools.SetActive(collectRect, collect);
                //if (collect)
                {
                    var random = UnityEngine.Random.Range(1, 8);
                    foreach (var text in collectText)
                        text.text = LocalizationSystem.Instance.Localize("DAILYQUEST.MissionComplete.Description." + random);
                }

                if (collectButton != null)
                {
                    CommonTools.SetActive(collectButton, !itemData.receivedReward);
                    collectButton.interactable = true;
                }
            }
        }

        //TODO :: total = cur 에 있는 type1ValN / cur = status에 있는 val
        private void SetQuestGauge()
        {
            if (itemData == null)
                return;

            if (questGauge == null)
                return;
            
            var curData = MissionInfo.Instance.MissionData.currentMission;

            if (curData != null)
                questGauge.fillAmount = (float)itemData.val / (float)curData.type1ValN;
        }

        private void SetQuestText()
        {
            if (itemData == null)
                return;

            if (questInfo != null)
            {
                if (MissionInfo.Instance.MissionData.currentMission != null)
                {
                    if (MissionInfo.Instance.MissionData.currentMission.type1ValM > 0)
                    {
                        questInfo.text = string.Format(LocalizationSystem.Instance.Localize("DAILYQUEST.MissionType.Description." + MissionInfo.Instance.MissionData.currentMission.type1),
                                                    MissionInfo.Instance.MissionData.currentMission.type1ValN, MissionInfo.Instance.MissionData.currentMission.type1ValM);
                    }
                    else
                    {
                        questInfo.text = string.Format(LocalizationSystem.Instance.Localize("DAILYQUEST.MissionType.Description." + MissionInfo.Instance.MissionData.currentMission.type1),
                                                    MissionInfo.Instance.MissionData.currentMission.type1ValN);
                    }
                }
            }

            if (questPercent != null)
            {
                var curData = MissionInfo.Instance.MissionData.currentMission;
                if (curData != null)
                {
                    var value = ((float)itemData.val / (float)curData.type1ValN) * 100;
                    questPercent.text = string.Format("{0}%", value >= 100f ? 100f : Math.Truncate(value));
                }
            }
        }

        public void OnClickCollect()
        {
            if (itemData.receivedReward)
                return;

            collectButton.interactable = false;

            SessionLobby.Instance.RequestMissionDailyReward(() =>
            {
                if (updateCallback != null)
				{
					updateCallback();
				}

				long totalCoin = MissionInfo.Instance.GetTotalDailyRewardCoin();
                var effectDic = MissionInfo.Instance.GetDailyRewardEffect();

                popupMission.SetWeeklyMission();
                popupMission.OpenRewardPopup(totalCoin, 
                                             MissionInfo.Instance.RewardPoint,
                                             MissionType.Daily,
                                             PopupMission.Group.One,
                                             effectDic.Count > 0 ? MissionRewardType.CoinAndBooster : MissionRewardType.Coin);

                collectButton.interactable = true;
            });
        }
    }
}