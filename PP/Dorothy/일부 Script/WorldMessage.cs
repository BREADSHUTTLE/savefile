// WorldMessage.cs - WorldMessage implementation file
//
// Description      : WorldMessage
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2020/03/17
// Last Update      : 2020/03/17
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dorothy.Network;
using NEXT.GAMES;
using DG.Tweening;
using PlayEarth.Unity.Auth;

namespace Dorothy
{
    [Serializable] public class WorldMessageJackpot
    {
        public int slotId;
        public List<Jackpot> jackpotList;
    }

    [Serializable] public class WorldMessageAnimation
    {
        public Animation animation;
        public AnimationClip show;
    }

    public class WorldMessage : MonoBehaviour
    {
        [SerializeField] private WorldMessageAnimation worldMessageAnimation;
        [SerializeField] private List<WorldMessageJackpot> messageJackpotList = new List<WorldMessageJackpot>();
        [SerializeField] private RectTransform contents;
        [SerializeField] private Image profile;
        [SerializeField] private Image profileGuest;
        [SerializeField] private Text name;
        [SerializeField] private Text jackpot;
        [SerializeField] private Text slot;
        [SerializeField] private Text jackpotValue;
        [SerializeField] private RectTransform textRect;
        [SerializeField] private float duration = 4f;
        [SerializeField] private int jackpotDigit = 18;

        [SerializeField] private float startPos = -650f;
        [SerializeField] private float endPos = -70f;

        private Queue<NtfWorldMessage> messageQueue = new Queue<NtfWorldMessage>();
        private bool isShow = false;
        private float showTime = 0f;
        private bool notiShow = false;
        private Action<bool> showCallback = null;

        public void Show(NtfWorldMessage message, Action<bool> callback)
        {
            showCallback = callback;

            //TODO:: 기획 스펙상 이미 월드메시지 노출중일때 오면 무시한다.
            if (notiShow)
            {
                Debug.Log("world message active self true");
                if (showCallback != null)
                    showCallback(false);

                return;
            }

            messageQueue.Enqueue(message);

            SetMessage();
        }

        private void Update()
        {
            if (isShow)
            {
                showTime += Time.deltaTime;
                if (showTime >= duration)
                {
                    showTime = 0f;
                    isShow = false;
                    CloseMessage();
                }
            }
        }

        private void SetMessage()
        {
            if (messageQueue.Count <= 0)
            {
                CommonTools.SetActive(contents, false);
                showTime = 0f;
                isShow = false;
                notiShow = false;
                return;
            }

            if (messageQueue.Count > 0 && isShow == false)
            {
                NtfWorldMessage message = messageQueue.Dequeue();

                DownloadProfile(message.profileImg, (success) => 
                {
                    if (success)
                    {
                        contents.anchoredPosition = new Vector2(startPos, contents.anchoredPosition.y);
                        CommonTools.SetActive(contents, true);

                        SetUI(message);

                        isShow = true;
                        notiShow = true;
                    }
                    else
                    {
                        CommonTools.SetActive(contents, false);
                        notiShow = false;
                    }
                });
            }
        }

        private void SetUI(NtfWorldMessage message)
        {
            if (showCallback != null)
                showCallback(true);

            if (profile != null)
                CommonTools.SetActive(profile, true);

            if (name != null)
            {
                string nameValue = "";
                if (message.nickName.Length > 13)
                {
                    nameValue = message.nickName.Substring(0, 10);
                    nameValue += "...";
                }
                else
                {
                    nameValue = message.nickName;
                }

                name.text = nameValue;
            }

            if (jackpot != null)
            {
                if (message.winTypeIndex <= 0 && message.jackpotTypeIndex <= 0)
                {
                    CommonTools.SetActive(jackpot, false);
                }
                else
                {
                    CommonTools.SetActive(jackpot, true);

                    if (message.jackpotTypeIndex <= 0)
                    {
                        string winName = ((GameConstants.WIN_TYPE)message.winTypeIndex).ToString();
                        jackpot.text = winName.Replace("_", " ");
                    }
                    else
                    {
                        jackpot.text = GetJackpotName(message.slotId, message.jackpotTypeIndex);
                    }
                }
            }

            if (slot != null)
                slot.text = message.slotName;

            if (jackpotValue != null)
            {
                string value = message.winCoin.ToString("N0");
                jackpotValue.text = value.Length > jackpotDigit ? CommonTools.MoneyFormat((long)message.winCoin) : value;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

            if (worldMessageAnimation.animation != null)
            {
                if (worldMessageAnimation.show != null)
                {
                    worldMessageAnimation.animation.Stop(worldMessageAnimation.show.name);
                    worldMessageAnimation.animation.Play(worldMessageAnimation.show.name);
                }
            }
        }

        private string GetJackpotName(int id, int jackpotIndex)
        {
            List<Jackpot> jackpotList = null;
            GameConstants.JACKPOT_TYPE type = (GameConstants.JACKPOT_TYPE)jackpotIndex;

            for (int i = 0; i < messageJackpotList.Count; i++)
            {
                if (messageJackpotList[i].slotId == id)
                    jackpotList = messageJackpotList[i].jackpotList;
            }

            if (jackpotList != null)
            {
                for (int i = 0; i < jackpotList.Count; i++)
                {
                    if (jackpotList[i].jackpotType == type)
                        return jackpotList[i].text;
                }
            }
            
            return ((GameConstants.JACKPOT_TYPE)jackpotIndex).ToString().Replace("_", " ");
        }

        private void DownloadProfile(string url, Action<bool> action)
        {
            if (string.IsNullOrEmpty(url) || url == "none")
            {
                CommonTools.SetActive(profileGuest, true);
                CommonTools.SetActive(profile, false);

                if (action != null)
                    action(true);

                return;
            }

            ImageDownloader.Instance.DownloadSprite(url, (id, state, progress, sprite) =>
            {
                if ((state == ImageDownloader.DownloadImageInfo.State.Succeeded) && (sprite != null))
                {
                    CommonTools.SetActive(profileGuest, false);
                    CommonTools.SetActive(profile, true);

                    if (profile != null)
                        profile.sprite = sprite;

                    if (action != null)
                        action(true);
                }
                else if (state == ImageDownloader.DownloadImageInfo.State.Failed)
                {
                    CommonTools.SetActive(profileGuest, true);
                    CommonTools.SetActive(profile, false);

                    if (action != null)
                        action(false);
                }
            });
        }

        public void CloseMessage()
        {
            //contents.DOAnchorPosX(startPos, 1f).OnComplete(() => 
            //{
                SetMessage();
            //});
        }
    }
}