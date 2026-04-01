using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CAPYBARA
{
    public class NotiDesc
    {
        public NotiType notiType;
        public string title;
        public string desc;
    }
    public static partial class CPPlayer
    {
        public static class Noti
        {

            public static Action<NotiDesc> notiEvent;
            public static void Dispose()
            {
                notiEvent = null;
            }
        }
    }
}
