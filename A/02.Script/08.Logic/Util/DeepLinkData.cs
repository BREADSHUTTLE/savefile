using System;

namespace CAPYBARA
{
    public enum DeepLinkSource
    {
        None,
        Direct,
        Deferred,
        Retargeting
    }

    public static class DeepLinkData
    {
        static string _pendingInviteCode;

        public static event Action<string> OnInviteCodeReceived;

        public static string PendingInviteCode
        {
            get => _pendingInviteCode;
            set
            {
                _pendingInviteCode = value;
                if (!string.IsNullOrEmpty(value))
                    OnInviteCodeReceived?.Invoke(value);
            }
        }

        public static DeepLinkSource Source { get; set; } = DeepLinkSource.None;
        public static bool HasPendingInvite => !string.IsNullOrEmpty(_pendingInviteCode);

        public static void Clear()
        {
            _pendingInviteCode = null;
            Source = DeepLinkSource.None;
        }
    }
}
