using CodeStage.AntiCheat.ObscuredTypes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        public static class Option
        {
            public static Action<bool> OpenAnnouncementWebView;
            public static Action<bool> OpenQAWebView;
            public static Action<bool> SafeAreaActive;

            public static Action<bool> FourCardModeChange;
            public static Action<bool> ReserveBetChange;
            public static Action<bool> EmojiUseChange;
            public static Action<bool> JokboUseChange;
            public static Action<bool> HandRankUseChange;

            public static void Dispose()
            {
                OpenAnnouncementWebView = null;
                OpenQAWebView = null;
                SafeAreaActive = null;
                FourCardModeChange = null;
                ReserveBetChange = null;
                EmojiUseChange = null;
                JokboUseChange = null;
                HandRankUseChange = null;

            }
        }
    }
}