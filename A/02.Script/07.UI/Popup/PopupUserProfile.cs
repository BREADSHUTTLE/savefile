using System;
using CAPYBARA;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using BlackTree.Bundles;
using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupUserProfile : BasePopup
{
    [Serializable]
    public class RecordUI
    {
        public TMP_Text totalText;
        public TMP_Text winText;
        public TMP_Text loseText;
        public TMP_Text winRateText;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text nickText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image avatarBackImage;
    [SerializeField] private Image avatarIconImage;
    [SerializeField] private RectTransform nameGroup;
    [SerializeField] private UISegmentedControlGroup toggleTodayGroup;
    [SerializeField] private UISegmentedControlGroup toggleTotalGroup;
    [SerializeField] private CPButton messageButton;

    [Header("Records")]
    [SerializeField] private RecordUI todayRecord;
    [SerializeField] private RecordUI totalRecord;

    private long currentUid;
    private bool isOpen;
    private CancellationTokenSource cts;
    private int currentTodayIndex;
    private int currentTotalIndex;

    private User cachedUser;

    private struct RecordCache
    {
        public bool hasData;
        public int win;
        public int lose;
    }

    private RecordCache[] todayRecordCache = new RecordCache[4];
    private RecordCache[] totalRecordCache = new RecordCache[4];

    protected override void OnInit()
    {
        base.OnInit();

        if (messageButton != null)
        {
            messageButton.onClick.RemoveAllListeners();
            messageButton.onClick.AddListener(OnClickMessage);
        }

        toggleTodayGroup.onIndexChanged += OnTodayToggle;
        toggleTotalGroup.onIndexChanged += OnTotalToggle;
    }

    public void OpenWithUid(long uid)
    {
        currentUid = uid;
        cachedUser = null;
        OpenWithDataAsync(uid).Forget();
    }

    private async UniTaskVoid OpenWithDataAsync(long uid)
    {
        if (uid <= 0)
            return;

        CancelRequests();
        cts = new CancellationTokenSource();

        PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
        try
        {
            var userResult = await Services.Lobby.UserReqByUserIdsAsync(new[] { uid }, cts.Token);
            if (cts == null || cts.IsCancellationRequested)
                return;

            if (userResult.IsSuccess && userResult.Data?.User?.Count > 0)
                cachedUser = userResult.Data.User[0];

            ResetCaches();
            var recordResult = await Services.Lobby.GetMatchRecordAsync(uid, cts.Token);
            if (cts == null || cts.IsCancellationRequested)
                return;

            if (recordResult.IsSuccess && recordResult.Data?.MatchRecord != null)
            {
                var records = recordResult.Data.MatchRecord;
                for (int i = 0; i < 4; i++)
                {
                    var gameTypeStr = GetGameTypeString(i);
                    var (todayWin, todayLose) = GetRecordData(records, "TODAY", gameTypeStr);
                    StoreRecordCache(true, i, todayWin, todayLose, true);
                    var (totalWin, totalLose) = GetRecordData(records, "TOTAL", gameTypeStr);
                    StoreRecordCache(false, i, totalWin, totalLose, true);
                }
            }
        }
        finally
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
        }

        toggleTodayGroup.SetActiveToggle(0, false);
        toggleTotalGroup.SetActiveToggle(0, false);

        Open();
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        isOpen = true;
        currentTodayIndex = 0;
        currentTotalIndex = 0;

        SetUserInfoUI(cachedUser);
        ApplyAllRecords();
    }

    protected override void OnClose()
    {
        base.OnClose();
        isOpen = false;
        CancelRequests();
    }

    private void CancelRequests()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }

    private void OnTodayToggle(int index)
    {
        if (!isOpen)
            return;

        currentTodayIndex = index;
        TryApplyCachedRecord(todayRecordCache, index, todayRecord);
    }

    private void OnTotalToggle(int index)
    {
        if (!isOpen)
            return;

        currentTotalIndex = index;
        TryApplyCachedRecord(totalRecordCache, index, totalRecord);
    }

    private string GetGameTypeString(int index)
    {
        return index switch
        {
            1 => "7POKER",
            2 => "BADUGI",
            3 => "HOLDEM",
            _ => "" // 전체
        };
    }

    private (int win, int lose) GetRecordData(Google.Protobuf.Collections.RepeatedField<MatchRecord> records, string matchStats, string gameTypeStr)
    {
        int win = 0;
        int lose = 0;
        
        if (string.IsNullOrEmpty(gameTypeStr))
        {
            foreach (var record in records)
            {
                if (record.MatchStats == matchStats)
                {
                    win += record.Win;
                    lose += record.Lose;
                }
            }
        }
        else
        {
            foreach (var record in records)
            {
                if (record.MatchStats == matchStats && record.GameType == gameTypeStr)
                {
                    win = record.Win;
                    lose = record.Lose;
                    break;
                }
            }
        }
        
        return (win, lose);
    }

    private void ApplyAllRecords()
    {
        // 현재 선택된 탭에 맞게 UI 업데이트
        TryApplyCachedRecord(todayRecordCache, currentTodayIndex, todayRecord);
        TryApplyCachedRecord(totalRecordCache, currentTotalIndex, totalRecord);
    }

    private void ResetCaches()
    {
        Array.Clear(todayRecordCache, 0, todayRecordCache.Length);
        Array.Clear(totalRecordCache, 0, totalRecordCache.Length);
    }

    private bool TryApplyCachedRecord(RecordCache[] cache, int index, RecordUI target)
    {
        if (cache == null || index < 0 || index >= cache.Length)
            return false;

        if (!cache[index].hasData)
            return false;

        ApplyRecord(target, cache[index].win, cache[index].lose);
        return true;
    }

    private void StoreRecordCache(bool isToday, int index, int win, int lose, bool hasData)
    {
        var cache = isToday ? todayRecordCache : totalRecordCache;
        if (index < 0 || index >= cache.Length)
            return;

        cache[index] = new RecordCache
        {
            hasData = hasData,
            win = win,
            lose = lose
        };
    }

    private void ApplyRecord(RecordUI target, int win, int lose)
    {
        int total = win + lose;
        float winRate = total == 0 ? 0f : (float)win / total * 100f;
        float roundedRate = (float)Math.Round(winRate, 1, MidpointRounding.AwayFromZero);
        SetRecordUI(target, total, win, lose, $"{roundedRate:F1}%");
    }

    private void SetUserInfoUI(User user)
    {
        if (user == null)
            return;

        if (nickText != null)
            nickText.text = user.Nick ?? "-";

        var avatarData = ItemBundle.Loaded?.GetAvatarById(user.AvatarId);
        bool hasAvatar = avatarData != null;

        if (avatarImage != null)
        {
            avatarImage.gameObject.SetActive(hasAvatar);
            if (hasAvatar)
            {
                avatarImage.sprite = avatarData.AvatarSprite;
                avatarImage.SetNativeSize();
            }
        }

        if (avatarIconImage != null)
        {
            avatarIconImage.gameObject.SetActive(hasAvatar);
            if (hasAvatar)
                avatarIconImage.sprite = avatarData.AvatarIcon;
        }

        if (avatarBackImage != null)
        {
            avatarBackImage.gameObject.SetActive(hasAvatar);
            if (hasAvatar)
            {
                avatarBackImage.sprite = avatarData.AvatarShadowSprite;
                avatarBackImage.SetNativeSize();
            }
        }

        if (nameGroup != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(nameGroup);
    }

    private void SetRecordUI(RecordUI recordUI, int total, int win, int lose, string winRateTextOverride = null)
    {
        if (recordUI == null)
            return;

        if (recordUI.totalText != null)
            recordUI.totalText.text = total.ToString();
        if (recordUI.winText != null)
            recordUI.winText.text = win.ToString();
        if (recordUI.loseText != null)
            recordUI.loseText.text = lose.ToString();
        if (recordUI.winRateText != null)
            recordUI.winRateText.text = winRateTextOverride ?? "0.0%";
    }

    private void OnClickMessage()
    {
        if (currentUid <= 0)
            return;

        Close();
        CPPlayer.OutGame.CreateConversationFriend?.Invoke(currentUid);
    }
}

