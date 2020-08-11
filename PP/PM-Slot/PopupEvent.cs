// PopupEvent.cs : PopupEvent implementation file
//
// Description     : PopupEvent
// Author          : icoder, uhrain7761
// Maintainer      : 
// How to use      : 
// Created         : 2016/08/25
// Last Update     : 2019/04/15
// Known bugs      : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using UnityEngine;
using System.Linq;
using DUNK.DataPool;
using DUNK.Games;


namespace DUNK.Popup
{
    public sealed class PopupEvent : PopupBaseLayout<PopupEvent>
    {
        public static PopupEvent Create()
        {
#if !DUNK_PCKLP
            if (SessionConnection.Instance.CheckOnlineMode() == false)
                return null;
#endif
            PopupEvent popup = OnCreate("POPUP.Event");
            popup.Initialize();
            return popup;
        }

        public void Initialize()
        {
            EventInfo.Instance.MarkAsRead();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SessionService.Instance.ActionEventInfo += OnGetEventInfo;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SessionService.Instance.ActionEventInfo -= OnGetEventInfo;
        }

        private void OnGetEventInfo(object msg)
        {
            ShowLoading(false);
        }

        public static void ShowLink(string title)
        {
#if !DUNK_PCKLP
            if (SessionConnection.Instance.CheckOnlineMode() == false)
                return;
#endif
            EventData data =
                EventInfo.Instance.EventDatas.Values.ToList().Find(
                    item => item.Title == title);

            if (data != null)
                ShowLink(data);
        }

        public static void ShowLink(EventData data)
        {
#if !DUNK_PCKLP
            if (SessionConnection.Instance.CheckOnlineMode() == false)
                return;
#endif
            switch (data.Type)
            {
                case "I": ShowInternalLink(data); break;
                case "E": ShowExternalLink(data); break;
                case "S": ShowSlotLink(data);     break;
                case "B": ShowBrowserLink(data);  break;
            }
        }

        private static void ShowExternalLink(EventData data)
        {
            SessionService.Instance.GenerateEventHash(data.LaunchURL, (hash) =>
            {
                //PopupWebView.Create(data.LaunchURL + "?gsn=" + MyInfo.Instance.GSN + "&usn=" + MyInfo.Instance.USN + "&hash=" + hash, Vector4.zero);
                PopupWebView.Create(data.LaunchURL + "?gsn=" + MyInfo.Instance.GSN + "&usn=" + MyInfo.Instance.USN + "&hash=" + hash, UIRoot.Scaling.FixedRatio);
            });
        }

        private static void ShowBrowserLink(EventData data)
        {
            SessionService.Instance.GenerateEventHash(data.LaunchURL, (hash) =>
            {
                Application.OpenURL(data.LaunchURL + "?gsn=" + MyInfo.Instance.GSN + "&usn=" + MyInfo.Instance.USN + "&hash=" + hash);
            });
        }

        private static void ShowSlotLink(EventData data)
        {
            if (string.IsNullOrEmpty(data.LaunchURL))
                return;

            SessionGame.Instance.StartGame(
                GameTools.MachineToGSSN(data.LaunchURL),
                GameTools.MachineToCategory(data.LaunchURL));
        }

        private static void ShowInternalLink(EventData data)
        {
            // DO Nothing.
        }
    }
}
