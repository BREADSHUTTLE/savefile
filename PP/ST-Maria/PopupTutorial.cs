// PopupTutorial.cs - PopupTutorial implementation file
//
// Description      : PopupTutorial
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2018/10/18
// Last Update      : 2018/10/18
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ST.MARIA.Popup
{
    public sealed class PopupTutorial : PopupBaseLayout<PopupTutorial>
    {
        public enum Type
        {
            None,
            StarMission,    // 스타미션 버튼 옆
            Casino,         // 로비 카지노
            Slot,           // 로비 슬롯 DP
            MyStripButton,  // 로비 마이스트립 버튼
            MyStripEdit,    // 마이스트립 에디터 버튼
            VideoAd,        // 마이스트립 비디오 건물
            CasinoStructure,// 마이스트립 카지노 건물
            Inventory,      // 마이스트립 인벤토리
            Map,            // 에디터 최초 진입 후, 맵 위에 뜸
        }

        [SerializeField] private Text message;
        [SerializeField] private Button page;
        [SerializeField] private RectTransform messageRect;

        private Type tutorialType = Type.None;
        private List<string> messageList = new List<string>();
        private Coroutine messageCoroutine = null;
        private Coroutine pageCoroutine = null;
        private bool wait = false;
        private int pageIndex = 0;

        public static PopupTutorial Create(Type type)
        {
            PopupTutorial popup = OnCreate("POPUP-Tutorial");
            popup.Initialize(type);
            return popup;
        }

        public void OnClickSkip()
        {
            if (page != null)
            {
                if (pageCoroutine != null)
                {
                    StopCoroutine(pageCoroutine);
                    pageCoroutine = null;
                }

                CommonTools.SetActive(page, false);
            }

            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }

            if (wait)
            {
                message.text = messageList[pageIndex];

                pageIndex++;
                wait = false;

                SetNext();
            }
            else
            {
                if (messageList.Count == pageIndex)
                    Close();
                else
                    SetMessage(pageIndex);
            }
        }

        private void Initialize(Type type)
        {
            tutorialType = type;
            pageIndex = 0;

            if (tutorialType == Type.None)
                return;

            if (page != null)
            {
                if (pageCoroutine != null)
                {
                    StopCoroutine(pageCoroutine);
                    pageCoroutine = null;
                }

                CommonTools.SetActive(page, false);
            }

            SetList();
            SetMessage(pageIndex);
        }

        private void SetMessage(int index)
        {
            if (messageList.Count < 0)
                return;

            messageCoroutine = StartCoroutine(WaitMessage(index));
        }

        private IEnumerator WaitMessage(int index)
        {
            message.text = string.Empty;
            int count = 0;
            wait = true;
            while (true)
            {
                if (messageList[index].Length == count)
                    break;

                message.text += messageList[index][count];
                count++;
                LayoutRebuilder.ForceRebuildLayoutImmediate(messageRect);
                yield return new WaitForSeconds(0.02f);
            }

            pageIndex++;
            wait = false;

            SetNext();
        }

        private void SetNext()
        {
            if (messageList.Count > pageIndex)
            {
                if (page != null)
                {
                    CommonTools.SetActive(page, true);
                    pageCoroutine = StartCoroutine(WaitPage());
                }
            }
        }

        private IEnumerator WaitPage()
        {
            bool show = page.gameObject.activeSelf;
            while (true)
            {
                var color = page.image.color;
                color.a = show ? 1f : 0f;
                page.image.color = color;
                show = !show;
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void SetList()
        {
            switch (tutorialType)
            {
                case Type.StarMission:
                    AddList("POPUP.TUTORIAL.StarMission.");
                    break;
                case Type.Casino:
                    AddList("POPUP.TUTORIAL.Casino.");
                    break;
                case Type.Slot:
                    AddList("POPUP.TUTORIAL.Slot.");
                    break;
                case Type.MyStripButton:
                    AddList("POPUP.TUTORIAL.MyStripButton.");
                    break;
                case Type.MyStripEdit:
                    AddList("POPUP.TUTORIAL.MyStripEditButton.");
                    break;
                case Type.VideoAd:
                    AddList("POPUP.TUTORIAL.VideoAd.");
                    break;
                case Type.CasinoStructure:
                    AddList("POPUP.TUTORIAL.CasinoStructure.");
                    break;
                case Type.Inventory:
                    AddList("POPUP.TUTORIAL.EditInventory.");
                    break;
                case Type.Map:
                    AddList("POPUP.TUTORIAL.MyStripMap.");
                    break;
                default:
                    Error("an invalid format : " + tutorialType);
                    break;
            }
        }

        private void AddList(string key)
        {
            messageList.Clear();
            int count = 0;
            while (true)
            {
                string localKey = key + count;
                string value = LocalizationSystem.Instance.Localize(localKey);
                if (localKey == value)
                    break;

                count++;
                messageList.Add(value);
            }
        }
    }
}