using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PopupNotice : BasePopup
    {
        [Header("API")]
        [SerializeField] private string noticeApiUrl = "https://gw.dev.atozgames.net/api/notice";

        [Header("Notice List UI")]
        [SerializeField] private UISegmentedControlGroup noticeSegmentGroup;
        [SerializeField] private GameObject[] noticeBadges;
        [SerializeField] private Color segmentTitleOnColor = Color.white;
        [SerializeField] private Color segmentTitleOffColor = new Color(0.4666667f, 0.4901961f, 0.5960785f, 1f); // 777D98

        [Header("Notice Detail UI")]
        [SerializeField] private Transform detailTextRoot;
        [SerializeField] private TMP_Text detailTitleText;
        [SerializeField] private TMP_Text detailDateText;
        [Header("Body Blocks")]
        [SerializeField] private Transform detailContentRoot;
        [SerializeField] private GameObject detailTextBlockPrefab;
        [SerializeField] private GameObject detailImageBlockPrefab;
        [SerializeField] private ScrollRect detailScrollRect;
        [Header("Debug")]
        [SerializeField] private bool enableNoticeDebugLog = true;

        private readonly List<NoticeItem> _notices = new List<NoticeItem>();
        private readonly List<GameObject> _spawnedBodyBlocks = new List<GameObject>();
        private readonly List<Texture2D> _loadedBodyTextures = new List<Texture2D>();
        private readonly List<Sprite> _loadedBodySprites = new List<Sprite>();
        private readonly HashSet<int> _readRequestSentNoticeIds = new HashSet<int>();
        private bool _isLoading;
        private int _detailRenderVersion;

        private enum NoticeBodyBlockType
        {
            Text,
            Image,
        }

        private struct NoticeBodyBlock
        {
            public NoticeBodyBlockType Type;
            public string Value;
        }

        [Serializable]
        private class NoticeApiResponse
        {
            public List<NoticeItem> notices;
        }

        [Serializable]
        private class NoticeItem
        {
            public int id;
            public string title;
            public string body;
            public long start_ts;
            public long end_ts;
            public int noti;
        }

        [Serializable]
        private class NoticeReadRequest
        {
            public int noticeId;
        }

        [Serializable]
        private class NoticeReadResponse
        {
            public bool ok;
        }

        protected override void OnInit()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (detailTextBlockPrefab != null)
                detailTextBlockPrefab.SetActive(false);
            if (detailImageBlockPrefab != null)
                detailImageBlockPrefab.SetActive(false);

            BindNoticeSlots();
        }

        public override void Open()
        {
            RefreshNoticesAsync().Forget();
        }

        protected override void OnClose()
        {
            base.OnClose();
            CleanupAllLoadedResources();
        }

        private void OnDestroy()
        {
            if (noticeSegmentGroup != null)
                noticeSegmentGroup.onIndexChanged -= OnNoticeSegmentChanged;
        }

        private void BindNoticeSlots()
        {
            if (noticeSegmentGroup != null)
            {
                noticeSegmentGroup.EnsureInitialized();
                noticeSegmentGroup.onIndexChanged -= OnNoticeSegmentChanged;
                noticeSegmentGroup.onIndexChanged += OnNoticeSegmentChanged;
            }

            ConfigureBadgeRaycastBlocking();
        }

        private void OnNoticeSegmentChanged(int index)
        {
            ApplySelectedNotice(index);
        }

        private async UniTaskVoid RefreshNoticesAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            try
            {
                string requestUrl = noticeApiUrl;
                using (UnityWebRequest req = UnityWebRequest.Get(requestUrl))
                {
                    string jwtToken = GetUserAutoTokenForNoticeRead();
                    req.SetRequestHeader("Cache-Control", "no-cache");
                    req.SetRequestHeader("Accept", "*/*");
                    req.SetRequestHeader("User-Agent", "PostmanRuntime/7.51.1");
                    req.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
                    req.SetRequestHeader("Connection", "keep-alive");
                    if (!string.IsNullOrWhiteSpace(jwtToken))
                    {
                        req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
                    }

                    if (enableNoticeDebugLog)
                    {
                        string authState = string.IsNullOrWhiteSpace(jwtToken) ? "none" : "jwtToken";
                        Extension.eLog($"PopupNotice GET url:{requestUrl}, auth:{authState}");
                    }

                    await req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.NoticeLoadFailed].StringToLocal, true));
                        ClearAllNoticeUI();
                        return;
                    }

                    string json = req.downloadHandler.text;
                    NoticeApiResponse response = JsonUtility.FromJson<NoticeApiResponse>(json);
                    LogNoticeApiRaw(response, json);

                    _notices.Clear();
                    _readRequestSentNoticeIds.Clear();
                    if (response?.notices != null)
                    {
                        for (int i = 0; i < response.notices.Count; i++)
                        {
                            if (IsVisibleNotice(response.notices[i]))
                                _notices.Add(response.notices[i]);
                        }
                    }
                    LogFilteredNotices();

                    BindNoticeListUI();

                    if (_notices.Count > 0)
                    {
                        if (noticeSegmentGroup != null)
                            noticeSegmentGroup.SetActiveToggle(0, true);
                        else
                            ApplySelectedNotice(0);
                    }
                    else
                    {
                        ClearDetailUI();
                    }
                }
            }
            catch (Exception e)
            {
                Extension.eLog($"PopupNotice RefreshNoticesAsync Exception: {e}");
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.NoticeLoadError].StringToLocal, true));
                ClearAllNoticeUI();
            }
            finally
            {
                _isLoading = false;
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
                base.Open();
            }
        }

        private void BindNoticeListUI()
        {
            int count = GetNoticeSlotCount();

            for (int i = 0; i < count; i++)
            {
                bool hasData = i < _notices.Count;

                if (noticeSegmentGroup != null && i < noticeSegmentGroup.Toggles.Count && noticeSegmentGroup.Toggles[i] != null)
                {
                    var segment = noticeSegmentGroup.Toggles[i];
                    segment.gameObject.SetActive(hasData);
                    segment.SetIsOn(false, false);
                    segment.SetText(hasData ? _notices[i].title : string.Empty, segmentTitleOffColor);
                }

                if (i < noticeBadges?.Length && noticeBadges[i] != null)
                    noticeBadges[i].SetActive(hasData && _notices[i].noti == 1);

                LogBadgeBindingState(i, hasData);
            }
        }

        private void UpdateNoticeBadge(int index, bool isOn)
        {
            if (index < 0 || index >= _notices.Count)
                return;

            if (index < noticeBadges?.Length && noticeBadges[index] != null)
                noticeBadges[index].SetActive(isOn);
        }

        private void ApplySelectedNotice(int index)
        {
            if (index < 0 || index >= _notices.Count)
                return;

            int count = GetNoticeSlotCount();
            for (int i = 0; i < count; i++)
            {
                bool isSelected = i == index;

                if (noticeSegmentGroup != null && i < noticeSegmentGroup.Toggles.Count && noticeSegmentGroup.Toggles[i] != null)
                {
                    var segment = noticeSegmentGroup.Toggles[i];
                    segment.SetIsOn(isSelected, false);

                    if (i < _notices.Count)
                        segment.SetText(_notices[i].title, isSelected ? segmentTitleOnColor : segmentTitleOffColor);
                }
            }

            UpdateDetailUI(_notices[index]).Forget();
            MarkNoticeAsReadAsync(index).Forget();
        }

        private async UniTaskVoid MarkNoticeAsReadAsync(int index)
        {
            if (index < 0 || index >= _notices.Count)
                return;

            NoticeItem notice = _notices[index];
            if (notice == null || notice.noti != 1 || notice.id <= 0)
                return;

            if (_readRequestSentNoticeIds.Contains(notice.id))
                return;

            _readRequestSentNoticeIds.Add(notice.id);

            try
            {
                NoticeReadRequest payload = new NoticeReadRequest { noticeId = notice.id };
                string json = JsonUtility.ToJson(payload);
                string jwtToken = GetUserAutoTokenForNoticeRead();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    _readRequestSentNoticeIds.Remove(notice.id);
                    Extension.eLog($"PopupNotice MarkNoticeAsReadAsync Failed - jwtToken is empty, id:{notice.id}");
                    return;
                }

                using (UnityWebRequest req = new UnityWebRequest(noticeApiUrl, UnityWebRequest.kHttpVerbPOST))
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.SetRequestHeader("Accept", "*/*");
                    req.SetRequestHeader("Cache-Control", "no-cache");
                    req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
                    req.SetRequestHeader("User-Agent", "PostmanRuntime/7.51.1");
                    req.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
                    req.SetRequestHeader("Connection", "keep-alive");

                    string requestException = null;
                    try
                    {
                        await req.SendWebRequest();
                    }
                    catch (Exception e)
                    {
                        requestException = e.Message;
                    }

                    long statusCode = req.responseCode;
                    string responseBody = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                    string requestError = req.error;
                    if (string.IsNullOrEmpty(requestError) && !string.IsNullOrEmpty(requestException))
                        requestError = requestException;

                    bool isHttpSuccess = req.result == UnityWebRequest.Result.Success && statusCode >= 200 && statusCode < 300;
                    bool isBodySuccess = IsNoticeReadResponseSuccess(responseBody);

                    if (enableNoticeDebugLog)
                    {
                        Extension.eLog(
                            $"PopupNotice MarkRead Try - id:{notice.id}, tokenSource:jwtToken, status:{statusCode}, httpResult:{req.result}, body:{responseBody}");
                    }

                    if (isHttpSuccess && isBodySuccess)
                    {
                        notice.noti = 0;
                        UpdateNoticeBadge(index, false);
                        Extension.eLog($"PopupNotice MarkNoticeAsReadAsync Success - id:{notice.id}, tokenSource:jwtToken");
                        return;
                    }

                    if (isHttpSuccess && !isBodySuccess)
                        requestError = "Response ok is false";

                    _readRequestSentNoticeIds.Remove(notice.id);
                    Extension.eLog($"PopupNotice MarkNoticeAsReadAsync Failed - id:{notice.id}, tokenSource:jwtToken, status:{statusCode}, error:{requestError}, body:{responseBody}");
                    return;
                }
            }
            catch (Exception e)
            {
                _readRequestSentNoticeIds.Remove(notice.id);
                Extension.eLog($"PopupNotice MarkNoticeAsReadAsync Exception: {e}");
            }
        }

        private string GetUserAutoTokenForNoticeRead()
        {
            var login = LoginData.Cloud?.loginValue;
            if (login == null)
            {
                if (enableNoticeDebugLog)
                    Extension.eLog("PopupNotice MarkRead TokenSource - loginValue is null");
                return string.Empty;
            }

            if (enableNoticeDebugLog)
                LogLoginTokenState("userAutoToken", login.userAutoToken);
            return login.userAutoToken ?? string.Empty;
        }

        private void LogLoginTokenState(string source, string token)
        {
            bool hasValue = !string.IsNullOrWhiteSpace(token);
            int len = string.IsNullOrEmpty(token) ? 0 : token.Length;
            string tail = string.IsNullOrEmpty(token) ? string.Empty : (token.Length <= 6 ? token : token.Substring(token.Length - 6));
            Extension.eLog($"PopupNotice MarkRead TokenSourceState source:{source}, hasValue:{hasValue}, len:{len}, tail:*{tail}");
        }

        private static bool IsNoticeReadResponseSuccess(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return true;

            try
            {
                NoticeReadResponse response = JsonUtility.FromJson<NoticeReadResponse>(responseBody);
                if (response != null)
                    return response.ok;
            }
            catch
            {
                // fall through
            }

            return responseBody.IndexOf("\"ok\":true", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void LogNoticeApiRaw(NoticeApiResponse response, string rawJson)
        {
            if (!enableNoticeDebugLog)
                return;

            int count = response?.notices != null ? response.notices.Count : 0;
            Extension.eLog($"PopupNotice Raw API - noticeCount:{count}, rawLength:{(rawJson ?? string.Empty).Length}");

            if (response?.notices == null)
                return;

            for (int i = 0; i < response.notices.Count; i++)
            {
                NoticeItem notice = response.notices[i];
                if (notice == null)
                {
                    Extension.eLog($"PopupNotice Raw[{i}] null");
                    continue;
                }

                Extension.eLog($"PopupNotice Raw[{i}] id:{notice.id}, noti:{notice.noti}, start:{notice.start_ts}, end:{notice.end_ts}, title:{GetLogSafeTitle(notice.title)}");
            }
        }

        private void LogFilteredNotices()
        {
            if (!enableNoticeDebugLog)
                return;

            Extension.eLog($"PopupNotice Visible Notice Count:{_notices.Count}");
            for (int i = 0; i < _notices.Count; i++)
            {
                NoticeItem notice = _notices[i];
                if (notice == null)
                {
                    Extension.eLog($"PopupNotice Visible[{i}] null");
                    continue;
                }

                Extension.eLog($"PopupNotice Visible[{i}] id:{notice.id}, noti:{notice.noti}, title:{GetLogSafeTitle(notice.title)}");
            }
        }

        private void LogBadgeBindingState(int slotIndex, bool hasData)
        {
            if (!enableNoticeDebugLog)
                return;

            bool badgeOn = slotIndex < noticeBadges?.Length && noticeBadges[slotIndex] != null && noticeBadges[slotIndex].activeSelf;
            int noticeId = hasData ? _notices[slotIndex].id : -1;
            int noticeNoti = hasData ? _notices[slotIndex].noti : -1;

            Extension.eLog($"PopupNotice BadgeBind slot:{slotIndex}, hasData:{hasData}, noticeId:{noticeId}, noti:{noticeNoti}, badgeOn:{badgeOn}");
        }

        private static string GetLogSafeTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return string.Empty;
            return title.Length > 40 ? title.Substring(0, 40) + "..." : title;
        }

        private async UniTaskVoid UpdateDetailUI(NoticeItem notice)
        {
            _detailRenderVersion++;
            int renderVersion = _detailRenderVersion;

            if (detailTitleText != null)
                detailTitleText.text = notice.title ?? string.Empty;

            if (detailDateText != null)
                detailDateText.text = FormatDateRange(notice.start_ts, notice.end_ts);

            await RenderNoticeBodyBlocksAsync(notice.body, renderVersion);
        }

        private bool CanUseDynamicBodyLayout()
        {
            return detailContentRoot != null && (detailTextBlockPrefab != null || detailImageBlockPrefab != null);
        }

        private async UniTask RenderNoticeBodyBlocksAsync(string html, int renderVersion)
        {
            CleanupDynamicBodyResources();
            RefreshDetailLayout(true);

            if (!CanUseDynamicBodyLayout())
                return;

            List<NoticeBodyBlock> blocks = ParseNoticeBodyBlocks(html);
            if (blocks.Count == 0)
                return;

            for (int i = 0; i < blocks.Count; i++)
            {
                if (renderVersion != _detailRenderVersion)
                    return;

                NoticeBodyBlock block = blocks[i];
                if (block.Type == NoticeBodyBlockType.Text)
                {
                    if (detailTextBlockPrefab == null)
                        continue;

                    GameObject textGo = Instantiate(detailTextBlockPrefab, detailContentRoot);
                    textGo.SetActive(true);
                    _spawnedBodyBlocks.Add(textGo);
                    TMP_Text textComponent = textGo.GetComponentInChildren<TMP_Text>(true);
                    if (textComponent != null)
                        textComponent.text = block.Value;

                    RefreshDetailLayout(false);
                }
                else
                {
                    if (detailImageBlockPrefab == null || string.IsNullOrWhiteSpace(block.Value))
                        continue;

                    GameObject imageGo = Instantiate(detailImageBlockPrefab, detailContentRoot);
                    imageGo.SetActive(true);
                    _spawnedBodyBlocks.Add(imageGo);

                    Image imageComponent = imageGo.GetComponentInChildren<Image>(true);
                    if (imageComponent == null)
                    {
                        imageGo.SetActive(false);
                        continue;
                    }

                    using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(block.Value))
                    {
                        await req.SendWebRequest();

                        if (renderVersion != _detailRenderVersion)
                            return;
                        if (req.result != UnityWebRequest.Result.Success)
                        {
                            imageGo.SetActive(false);
                            continue;
                        }

                        Texture2D texture = DownloadHandlerTexture.GetContent(req);
                        if (texture == null)
                        {
                            imageGo.SetActive(false);
                            continue;
                        }

                        _loadedBodyTextures.Add(texture);
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        _loadedBodySprites.Add(sprite);
                        imageComponent.sprite = sprite;
                        imageComponent.SetNativeSize();

                        RefreshDetailLayout(false);
                    }
                }
            }

            RefreshDetailLayout(true);
        }

        private static List<NoticeBodyBlock> ParseNoticeBodyBlocks(string html)
        {
            List<NoticeBodyBlock> blocks = new List<NoticeBodyBlock>();
            if (string.IsNullOrEmpty(html))
                return blocks;

            // 텍스트/이미지 순서를 유지하기 위해 단순 토큰 스트림으로 파싱
            Regex tokenRegex = new Regex(
                "(<img[^>]*src=[\"'](?<src>[^\"']+)[\"'][^>]*>)|(?<br><\\s*br\\s*/?>)|(?<openp><\\s*p[^>]*>)|(?<closep><\\s*/p\\s*>)|(?<text>[^<]+)|(?<othertag><[^>]+>)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            StringBuilder textBuilder = new StringBuilder();

            void FlushText()
            {
                string decoded = WebUtility.HtmlDecode(textBuilder.ToString()).Trim();
                if (!string.IsNullOrEmpty(decoded))
                {
                    blocks.Add(new NoticeBodyBlock
                    {
                        Type = NoticeBodyBlockType.Text,
                        Value = decoded
                    });
                }
                textBuilder.Clear();
            }

            MatchCollection matches = tokenRegex.Matches(html);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];

                if (match.Groups["src"].Success)
                {
                    FlushText();
                    blocks.Add(new NoticeBodyBlock
                    {
                        Type = NoticeBodyBlockType.Image,
                        Value = match.Groups["src"].Value
                    });
                    continue;
                }

                if (match.Groups["br"].Success || match.Groups["closep"].Success)
                {
                    FlushText();
                    continue;
                }

                if (match.Groups["text"].Success)
                    textBuilder.Append(match.Groups["text"].Value);
            }

            FlushText();
            return blocks;
        }

        private static string FormatDateRange(long startTs, long endTs)
        {
            if (startTs <= 0)
                return string.Empty;

            DateTimeOffset start = DateTimeOffset.FromUnixTimeSeconds(startTs).ToLocalTime();
            string startText = start.ToString("yy.MM.dd");

            if (endTs <= 0)
                return startText;

            DateTimeOffset end = DateTimeOffset.FromUnixTimeSeconds(endTs).ToLocalTime();
            string endText = end.ToString("yy.MM.dd");
            return $"{startText}~{endText}";
        }

        private static bool IsVisibleNotice(NoticeItem notice)
        {
            if (notice == null)
                return false;

            // end_ts == 0 은 상시 노출로 처리
            if (notice.end_ts <= 0)
                return true;

            long nowTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return nowTs <= notice.end_ts;
        }

        private void ClearAllNoticeUI()
        {
            int count = GetNoticeSlotCount();
            for (int i = 0; i < count; i++)
            {
                if (noticeSegmentGroup != null && i < noticeSegmentGroup.Toggles.Count && noticeSegmentGroup.Toggles[i] != null)
                    noticeSegmentGroup.Toggles[i].gameObject.SetActive(false);

                if (i < noticeBadges?.Length && noticeBadges[i] != null)
                    noticeBadges[i].SetActive(false);
            }

            ClearDetailUI();
        }

        private int GetNoticeSlotCount()
        {
            return (noticeSegmentGroup != null && noticeSegmentGroup.Toggles != null) ? noticeSegmentGroup.Toggles.Count : 0;
        }

        private void ClearDetailUI()
        {
            _detailRenderVersion++;

            if (detailTitleText != null)
                detailTitleText.text = string.Empty;
            if (detailDateText != null)
                detailDateText.text = string.Empty;

            CleanupAllLoadedResources();
            RefreshDetailLayout(true);
        }

        private void CleanupAllLoadedResources()
        {
            CleanupDynamicBodyResources();
        }

        private void CleanupDynamicBodyResources()
        {
            for (int i = 0; i < _spawnedBodyBlocks.Count; i++)
            {
                if (_spawnedBodyBlocks[i] != null)
                    DestroyImmediate(_spawnedBodyBlocks[i]);
            }
            _spawnedBodyBlocks.Clear();

            for (int i = 0; i < _loadedBodySprites.Count; i++)
            {
                if (_loadedBodySprites[i] != null)
                    Destroy(_loadedBodySprites[i]);
            }
            _loadedBodySprites.Clear();

            for (int i = 0; i < _loadedBodyTextures.Count; i++)
            {
                if (_loadedBodyTextures[i] != null)
                    Destroy(_loadedBodyTextures[i]);
            }
            _loadedBodyTextures.Clear();
        }

        private void RefreshDetailLayout(bool scrollToTop)
        {
            if (detailContentRoot is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            if (detailTextRoot is RectTransform textRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

            Canvas.ForceUpdateCanvases();

            if (detailScrollRect != null)
            {
                if (scrollToTop)
                {
                    detailScrollRect.velocity = Vector2.zero;
                    detailScrollRect.verticalNormalizedPosition = 1f;
                    Canvas.ForceUpdateCanvases();
                }

                float contentHeight = detailScrollRect.content != null ? detailScrollRect.content.rect.height : 0f;
                float viewportHeight = detailScrollRect.viewport != null ? detailScrollRect.viewport.rect.height : ((RectTransform)detailScrollRect.transform).rect.height;

                bool needScroll = contentHeight > viewportHeight;
                detailScrollRect.vertical = needScroll;
                detailScrollRect.movementType = needScroll ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
            }
        }

        private void ConfigureBadgeRaycastBlocking()
        {
            if (noticeBadges == null)
                return;

            for (int i = 0; i < noticeBadges.Length; i++)
            {
                if (noticeBadges[i] == null)
                    continue;

                var graphics = noticeBadges[i].GetComponentsInChildren<Graphic>(true);
                for (int j = 0; j < graphics.Length; j++)
                    graphics[j].raycastTarget = false;
            }
        }

    }
}