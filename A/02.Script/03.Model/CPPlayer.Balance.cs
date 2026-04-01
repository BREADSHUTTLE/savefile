using System;
using UnityEngine;


namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        public static class Balance
        {
            public static Action<long, long> MyBalTextAnimEvent;
            public static Action<long, long> VaultBalTextAnimEvent;

            public static void Dispose()
            {
                MyBalTextAnimEvent = null;
                VaultBalTextAnimEvent = null;
            }
        }
    }
}