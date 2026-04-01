using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CAPYBARA
{
    public class PopupCustomerService : BasePopup
    {
        private enum CustomerServicePage
        {
            Main = 0,
            Faq = 1,
            Inquiry = 2,
            InquiryWrite = 3,
        }

        [Header("Common Header")]
        [SerializeField] private TMP_Text headerTitleText;
        [SerializeField] private CPButton backButton;

        [Header("Main Buttons")]
        [SerializeField] private CPButton faqOpenButton;
        [SerializeField] private CPButton inquiryOpenButton;

        [Header("Page Roots")]
        [SerializeField] private GameObject mainPageRoot;
        [SerializeField] private GameObject faqPageRoot;
        [SerializeField] private GameObject inquiryPageRoot;

        [Header("FAQ")]
        [SerializeField] private string faqApiUrl = "https://gw.dev.atozgames.net/api/faq";
        [SerializeField] private ScrollRect faqScrollRect;
        [SerializeField] private RectTransform faqLayoutRoot;
        [SerializeField] private CustomerServiceFaqItem faqItemPrefab;
        [SerializeField] private bool allowOnlyOneExpanded = false;
        [SerializeField] private bool openFirstItemOnLoad = false;

        [Header("Inquiry")]
        [SerializeField] private string inquiryApiUrl = "https://gw.dev.atozgames.net/api/ask";
        [SerializeField] private GameObject inquiryBadge;
        [SerializeField] private ScrollRect inquiryListScrollRect;
        [SerializeField] private RectTransform inquiryListLayoutRoot;
        [SerializeField] private CustomerServiceInquiryItem inquiryItemPrefab;
        [SerializeField] private GameObject inquiryEmptyRoot;
        [SerializeField] private GameObject inquiryDetailRoot;
        [SerializeField] private ScrollRect inquiryDetailScrollRect;
        [SerializeField] private TMP_Text inquiryDetailTitle;
        [SerializeField] private TMP_Text inquiryDetailDate;
        [SerializeField] private TMP_Text inquiryDetailBody;
        [SerializeField] private TMP_Text inquiryDetailAnswer;
        [SerializeField] private GameObject inquiryDetailAnswerRoot;
        [SerializeField] private CPButton inquiryDeleteButton;
        [SerializeField] private CPButton inquiryWriteButton;

        [Header("Inquiry Detail - Attachments")]
        [SerializeField] private GameObject inquiryDetailAttachmentRoot;
        [SerializeField] private RectTransform inquiryDetailAttachmentGrid;
        [SerializeField] private CPButton inquiryDetailAttachmentSlotPrefab;

        [Header("Media Viewer (Image + Video)")]
        [SerializeField] private GameObject imageViewerRoot;
        [SerializeField] private RawImage imageViewerImage;
        [SerializeField] private CPButton imageViewerCloseButton;
        [SerializeField] private CPButton imageViewerPrevButton;
        [SerializeField] private CPButton imageViewerNextButton;
        [SerializeField] private GameObject videoControlsRoot;
        [SerializeField] private CPButton videoPlayPauseButton;
        [SerializeField] private Slider videoSeekBar;
        [SerializeField] private TMP_Text videoTimeText;

        [Header("Inquiry Write")]
        [SerializeField] private string inquiryAddApiUrl = "https://gw.dev.atozgames.net/api/ask";
        [SerializeField] private GameObject inquiryWritePageRoot;
        [SerializeField] private ScrollRect inquiryWriteScrollRect;
        [SerializeField] private TMP_InputField inquiryWriteTitleInput;
        [SerializeField] private TMP_InputField inquiryWriteEmailInput;
        [SerializeField] private TMP_InputField inquiryWriteBodyInput;
        [SerializeField] private CPButton inquirySubmitButton;

        [Header("Inquiry Write - Attachments")]
        [SerializeField] private RectTransform attachmentGridRoot;
        [SerializeField] private CustomerServiceAttachmentSlot attachmentSlotPrefab;
        [SerializeField] private CPButton attachmentAddButton;
        [SerializeField] private int maxAttachmentCount = 8;
        [SerializeField] private long maxFileSizeBytes = 10L * 1024 * 1024;

        [Serializable]
        private class FaqApiResponse
        {
            public List<FaqData> faqs;
        }

        [Serializable]
        private class FaqData
        {
            public int id;
            public string category;
            public string title;
            public string body;
        }

        [Serializable]
        private class InquiryApiResponse
        {
            public int totalPage;
            public List<InquiryData> asks;
        }

        [Serializable]
        private class InquiryData
        {
            public int id;
            public long ask_ts;
            public string title;
            public string body;
            public string email;
            public List<string> files;
            public long answer_ts;
            public string answer;
            public int noti;
        }

        [Serializable]
        private class InquiryAddRequest
        {
            public string title;
            public string body;
            public string email;
            public List<string> files;
        }

        [Serializable]
        private class PresignedUrlRequest
        {
            public string fileName;
            public string fileType;
        }

        [Serializable]
        private class PresignedUrlResponse
        {
            public string uploadUrl;
            public string fileUrl;
        }

        private class AttachmentFileInfo
        {
            public string filePath;
            public Texture2D thumbnail;
            public bool isVideo;
        }

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };

        private readonly List<AttachmentFileInfo> attachedFiles = new List<AttachmentFileInfo>();
        private readonly List<CustomerServiceAttachmentSlot> attachmentSlotInstances = new List<CustomerServiceAttachmentSlot>();

        private readonly List<FaqData> faqDataList = new List<FaqData>();
        private readonly List<CustomerServiceFaqItem> faqItemInstances = new List<CustomerServiceFaqItem>();
        private bool faqLoaded;
        private bool faqLoading;
        private bool[] expandedStates = Array.Empty<bool>();

        private readonly List<InquiryData> inquiryDataList = new List<InquiryData>();
        private readonly List<CustomerServiceInquiryItem> inquiryItemInstances = new List<CustomerServiceInquiryItem>();
        private bool inquiryLoaded;
        private bool inquiryLoading;
        private int inquiryCurrentPage = 1;
        private int inquiryTotalPage = 1;
        private int inquirySelectedIndex = -1;

        private CustomerServicePage currentPage = CustomerServicePage.Main;

        private readonly List<CPButton> detailAttachmentSlots = new List<CPButton>();
        private readonly List<Texture2D> detailAttachmentTextures = new List<Texture2D>();

        protected override void OnInit()
        {
            base.OnInit();

            BindButton(backButton, OnBackButtonClicked);
            BindButton(faqOpenButton, ShowFaqPage);
            BindButton(inquiryOpenButton, ShowInquiryPage);

            if (faqItemPrefab != null)
                faqItemPrefab.gameObject.SetActive(false);

            if (inquiryItemPrefab != null)
                inquiryItemPrefab.gameObject.SetActive(false);

            if (inquiryBadge != null)
                inquiryBadge.SetActive(false);

            BindButton(inquiryWriteButton, ShowInquiryWritePage);
            BindButton(inquirySubmitButton, OnInquirySubmitClicked);
            BindButton(attachmentAddButton, OnAttachmentAddClicked);

            if (inquiryWriteTitleInput != null)
            {
                inquiryWriteTitleInput.characterLimit = 30;
                inquiryWriteTitleInput.onValueChanged.AddListener(OnTitleInputChanged);
            }

            if (inquiryWriteBodyInput != null)
            {
                inquiryWriteBodyInput.characterLimit = 3000;
                inquiryWriteBodyInput.onValueChanged.AddListener(OnBodyInputChanged);

                var scrollable = inquiryWriteBodyInput.GetComponent<ScrollableInputField>();
                if (scrollable == null)
                {
                    scrollable = inquiryWriteBodyInput.gameObject.AddComponent<ScrollableInputField>();
                    scrollable.scrollRect = inquiryWriteScrollRect;
                }
            }

            if (attachmentSlotPrefab != null)
                attachmentSlotPrefab.gameObject.SetActive(false);

            if (inquiryDetailAttachmentSlotPrefab != null)
                inquiryDetailAttachmentSlotPrefab.gameObject.SetActive(false);

            BindButton(imageViewerCloseButton, CloseMediaViewer);
            BindButton(imageViewerPrevButton, () => ShowMediaViewerPage(imageViewerCurrentIndex - 1));
            BindButton(imageViewerNextButton, () => ShowMediaViewerPage(imageViewerCurrentIndex + 1));
            BindButton(videoPlayPauseButton, ToggleVideoPlayPause);

            if (videoSeekBar != null)
                videoSeekBar.onValueChanged.AddListener(OnVideoSeekBarChanged);

            if (imageViewerRoot != null)
                imageViewerRoot.SetActive(false);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            ShowMainPage();
        }

        public void ShowMainPage()
        {
            SetPage(CustomerServicePage.Main);
            CheckInquiryBadge().Forget();
        }

        public void ShowFaqPage()
        {
            SetPage(CustomerServicePage.Faq);
            ResetFaqScrollPosition();
            CollapseAllFaqItems();
            EnsureFaqLoaded().Forget();
        }

        public void ShowInquiryPage()
        {
            SetPage(CustomerServicePage.Inquiry);

            inquirySelectedIndex = -1;
            if (inquiryDetailRoot != null)
                inquiryDetailRoot.SetActive(false);
            foreach (var item in inquiryItemInstances)
            {
                if (item != null)
                    item.gameObject.SetActive(false);
            }

            ResetInquiryScrollPosition();
            LoadInquiryList().Forget();
        }

        private void SetPage(CustomerServicePage page)
        {
            currentPage = page;

            bool isMain = page == CustomerServicePage.Main;
            bool isFaq = page == CustomerServicePage.Faq;
            bool isInquiry = page == CustomerServicePage.Inquiry;
            bool isInquiryWrite = page == CustomerServicePage.InquiryWrite;

            if (mainPageRoot != null)
                mainPageRoot.SetActive(isMain);

            if (faqPageRoot != null)
                faqPageRoot.SetActive(isFaq);

            if (inquiryPageRoot != null)
                inquiryPageRoot.SetActive(isInquiry);

            if (inquiryWritePageRoot != null)
                inquiryWritePageRoot.SetActive(isInquiryWrite);

            if (backButton != null)
                backButton.gameObject.SetActive(!isMain);

            if (headerTitleText != null)
            {
                switch (page)
                {
                    case CustomerServicePage.Main:
                        headerTitleText.text = "고객센터";
                        break;
                    case CustomerServicePage.Faq:
                        headerTitleText.text = "자주 묻는 질문";
                        break;
                    case CustomerServicePage.Inquiry:
                        headerTitleText.text = "문의하기 / 내역";
                        break;
                    case CustomerServicePage.InquiryWrite:
                        headerTitleText.text = "문의하기";
                        break;
                }
            }
        }

        private void OnBackButtonClicked()
        {
            switch (currentPage)
            {
                case CustomerServicePage.Faq:
                case CustomerServicePage.Inquiry:
                    ShowMainPage();
                    break;
                case CustomerServicePage.InquiryWrite:
                    ShowInquiryPage();
                    break;
                case CustomerServicePage.Main:
                default:
                    Close();
                    break;
            }
        }

        public override void OnBackButtonPressed()
        {
            if (imageViewerRoot != null && imageViewerRoot.activeSelf)
            {
                CloseMediaViewer();
                return;
            }

            if (currentPage == CustomerServicePage.Main)
            {
                Close();
                return;
            }

            OnBackButtonClicked();
        }

        private void ResetFaqScrollPosition()
        {
            if (faqScrollRect == null)
                return;

            faqScrollRect.velocity = Vector2.zero;
            faqScrollRect.verticalNormalizedPosition = 1f;
        }

        private async UniTaskVoid EnsureFaqLoaded()
        {
            if (faqLoaded || faqLoading)
                return;

            faqLoading = true;
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            try
            {
                using (UnityWebRequest req = UnityWebRequest.Get(faqApiUrl))
                {
                    string jwtToken = GetUserAutoToken();
                    req.SetRequestHeader("Cache-Control", "no-cache");
                    req.SetRequestHeader("Accept", "*/*");
                    req.SetRequestHeader("User-Agent", "PostmanRuntime/7.51.1");
                    req.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
                    req.SetRequestHeader("Connection", "keep-alive");

                    if (!string.IsNullOrWhiteSpace(jwtToken))
                        req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

                    await req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("FAQ를 불러오지 못했습니다.", true));
                        return;
                    }

                    string json = req.downloadHandler.text;
                    Debug.Log($"[CustomerService] FAQ API response: {json.Substring(0, Mathf.Min(json.Length, 500))}");
                    FaqApiResponse response = JsonUtility.FromJson<FaqApiResponse>(json);
                    if (response?.faqs == null || response.faqs.Count == 0)
                    {
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("등록된 FAQ가 없습니다.", true));
                        return;
                    }

                    faqDataList.Clear();
                    for (int i = 0; i < response.faqs.Count; i++)
                    {
                        var faq = response.faqs[i];
                        Debug.Log($"[CustomerService] FAQ[{i}] category={faq.category}, title={faq.title}, body={faq.body?.Substring(0, Mathf.Min(faq.body?.Length ?? 0, 100))}");
                        faqDataList.Add(faq);
                    }
                }

                faqLoaded = true;
                BindFaqDataToUI();
            }
            finally
            {
                faqLoading = false;
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
            }
        }

        private void BindFaqDataToUI()
        {
            if (faqLayoutRoot == null || faqItemPrefab == null)
                return;

            EnsureFaqItemInstances(faqDataList.Count);

            expandedStates = new bool[faqItemInstances.Count];
            for (int i = 0; i < faqItemInstances.Count; i++)
            {
                var item = faqItemInstances[i];
                if (item == null)
                    continue;

                bool hasData = i < faqDataList.Count;
                item.gameObject.SetActive(hasData);

                if (!hasData)
                    continue;

                var data = faqDataList[i];
                item.SetTexts(BuildQuestionText(data), ConvertHtmlToPlainText(data?.body));

                int index = i;
                item.BindToggle(() => ToggleFaqItem(index));

                bool startOpen = openFirstItemOnLoad && i == 0;
                SetFaqItemExpanded(i, startOpen);
            }

            RefreshFaqLayout();
        }

        private void ToggleFaqItem(int index)
        {
            if (expandedStates == null || index < 0 || index >= expandedStates.Length)
                return;

            bool nextState = !expandedStates[index];

            if (allowOnlyOneExpanded && nextState)
            {
                for (int i = 0; i < expandedStates.Length; i++)
                {
                    if (i == index)
                        continue;

                    SetFaqItemExpanded(i, false);
                }
            }

            SetFaqItemExpanded(index, nextState);
            RefreshFaqLayout();
        }

        private void SetFaqItemExpanded(int index, bool expanded)
        {
            if (index < 0 || index >= faqItemInstances.Count)
                return;

            expandedStates[index] = expanded;

            var item = faqItemInstances[index];
            if (item == null)
                return;

            item.SetExpanded(expanded);
        }

        private void CollapseAllFaqItems()
        {
            if (expandedStates == null)
                return;

            for (int i = 0; i < expandedStates.Length; i++)
                SetFaqItemExpanded(i, false);

            RefreshFaqLayout();
        }

        private void RefreshFaqLayout()
        {
            if (faqLayoutRoot == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(faqLayoutRoot);
            Canvas.ForceUpdateCanvases();
        }

        private string GetUserAutoToken()
        {
            var login = LoginData.Cloud?.loginValue;
            if (login == null)
                return string.Empty;

            return login.userAutoToken ?? string.Empty;
        }

        private long GetUid()
        {
            var login = LoginData.Cloud?.loginValue;
            if (login == null)
                return 0;

            return login.UID;
        }

        private static string BuildQuestionText(FaqData data)
        {
            if (data == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(data.category))
                return data.title ?? string.Empty;

            return $"[{data.category}] {data.title}";
        }

        private static string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            string withBreaks = html
                .Replace("</p>", "\n")
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                .Replace("</li>", "\n");

            string noTags = Regex.Replace(withBreaks, "<.*?>", string.Empty);
            string decoded = WebUtility.HtmlDecode(noTags);
            decoded = Regex.Replace(decoded, @"\n{3,}", "\n\n");

            return decoded.Trim();
        }

        private void EnsureFaqItemInstances(int requiredCount)
        {
            if (faqLayoutRoot == null || faqItemPrefab == null)
                return;

            while (faqItemInstances.Count < requiredCount)
            {
                var instance = Instantiate(faqItemPrefab, faqLayoutRoot);
                instance.gameObject.SetActive(true);
                instance.SetExpanded(false);
                faqItemInstances.Add(instance);
            }
        }

        #region Inquiry

        private void ResetInquiryScrollPosition()
        {
            if (inquiryListScrollRect == null)
                return;

            inquiryListScrollRect.velocity = Vector2.zero;
            inquiryListScrollRect.verticalNormalizedPosition = 1f;
        }

        private void ResetInquiryDetailScrollPosition()
        {
            ScrollRect sr = inquiryDetailScrollRect;
            if (sr == null && inquiryDetailRoot != null)
                sr = inquiryDetailRoot.GetComponentInChildren<ScrollRect>(true);

            if (sr == null)
                return;

            sr.velocity = Vector2.zero;
            sr.verticalNormalizedPosition = 1f;
        }

        private async UniTaskVoid LoadInquiryList()
        {
            if (inquiryLoading)
                return;

            inquiryLoading = true;
            inquiryLoaded = false;
            inquiryCurrentPage = 1;
            inquiryDataList.Clear();

            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            try
            {
                await FetchInquiryPage(inquiryCurrentPage);
                inquiryLoaded = true;

                BindInquiryListToUI();
            }
            finally
            {
                inquiryLoading = false;
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
            }
        }

        private async UniTask FetchInquiryPage(int page)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(inquiryApiUrl))
            {
                SetCommonHeaders(req);

                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("문의 내역을 불러오지 못했습니다.", true));
                    return;
                }

                string json = req.downloadHandler.text;
                InquiryApiResponse response = JsonUtility.FromJson<InquiryApiResponse>(json);
                if (response == null)
                    return;

                inquiryTotalPage = response.totalPage;

                if (response.asks != null)
                {
                    for (int i = 0; i < response.asks.Count; i++)
                        inquiryDataList.Add(response.asks[i]);
                }
            }
        }

        private void SetCommonHeaders(UnityWebRequest req)
        {
            string jwtToken = GetUserAutoToken();
            req.SetRequestHeader("Cache-Control", "no-cache");
            req.SetRequestHeader("Accept", "*/*");
            req.SetRequestHeader("User-Agent", "PostmanRuntime/7.51.1");
            req.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
            req.SetRequestHeader("Connection", "keep-alive");

            if (!string.IsNullOrWhiteSpace(jwtToken))
                req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
        }

        private void BindInquiryListToUI()
        {
            bool hasData = inquiryDataList.Count > 0;

            if (inquiryEmptyRoot != null)
                inquiryEmptyRoot.SetActive(!hasData);

            if (inquiryListScrollRect != null)
                inquiryListScrollRect.gameObject.SetActive(hasData);

            if (inquiryDetailRoot != null)
                inquiryDetailRoot.SetActive(false);

            if (!hasData)
                return;

            EnsureInquiryItemInstances(inquiryDataList.Count);

            for (int i = 0; i < inquiryItemInstances.Count; i++)
            {
                var item = inquiryItemInstances[i];
                if (item == null)
                    continue;

                bool visible = i < inquiryDataList.Count;
                item.gameObject.SetActive(visible);

                if (!visible)
                    continue;

                var data = inquiryDataList[i];
                bool isAnswered = data.answer_ts > 0;
                string status = isAnswered ? "답변 완료" : "답변 대기중";
                string time = FormatRelativeTime(data.ask_ts);
                string title = data.title ?? string.Empty;
                bool isNew = data.noti > 0;

                int index = i;
                item.Bind(status, time, title, isNew, () => SelectInquiryItem(index));
                item.SetSelected(false);
            }

            RefreshInquiryListLayout();
            RefreshInquiryBadge();
            SelectInquiryItem(0);
        }

        private async UniTaskVoid CheckInquiryBadge()
        {
            if (inquiryBadge == null)
                return;

            try
            {
                using (UnityWebRequest req = UnityWebRequest.Get(inquiryApiUrl))
                {
                    SetCommonHeaders(req);
                    await req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                        return;

                    string json = req.downloadHandler.text;
                    var response = JsonUtility.FromJson<InquiryApiResponse>(json);
                    if (response?.asks == null)
                        return;

                    bool hasUnread = false;
                    for (int i = 0; i < response.asks.Count; i++)
                    {
                        if (response.asks[i].noti > 0)
                        {
                            hasUnread = true;
                            break;
                        }
                    }

                    inquiryBadge.SetActive(hasUnread);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CustomerService] CheckInquiryBadge failed: {e.Message}");
            }
        }

        private void RefreshInquiryBadge()
        {
            if (inquiryBadge == null)
                return;

            bool hasUnread = false;
            for (int i = 0; i < inquiryDataList.Count; i++)
            {
                if (inquiryDataList[i].noti > 0)
                {
                    hasUnread = true;
                    break;
                }
            }

            inquiryBadge.SetActive(hasUnread);
        }

        private void SelectInquiryItem(int index)
        {
            if (index < 0 || index >= inquiryDataList.Count)
                return;

            if (inquirySelectedIndex >= 0 && inquirySelectedIndex < inquiryItemInstances.Count)
                inquiryItemInstances[inquirySelectedIndex].SetSelected(false);

            inquirySelectedIndex = index;

            if (index < inquiryItemInstances.Count)
                inquiryItemInstances[index].SetSelected(true);

            var data = inquiryDataList[index];
            ShowInquiryDetail(data);

            if (data.noti > 0)
                MarkInquiryAsRead(data, index).Forget();

            Canvas.ForceUpdateCanvases();
            ResetInquiryDetailScrollPosition();
        }

        [Serializable]
        private class AskReadRequest
        {
            public int askId;
        }

        private async UniTaskVoid MarkInquiryAsRead(InquiryData data, int index)
        {
            string readApiUrl = inquiryApiUrl.TrimEnd('/') + "/read";
            var requestBody = new AskReadRequest { askId = data.id };
            string json = JsonUtility.ToJson(requestBody);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            try
            {
                using (UnityWebRequest req = new UnityWebRequest(readApiUrl, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    SetCommonHeaders(req);

                    await req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        data.noti = 0;

                        if (index < inquiryItemInstances.Count)
                            inquiryItemInstances[index].Bind(
                                data.answer_ts > 0 ? "답변 완료" : "답변 대기중",
                                FormatRelativeTime(data.ask_ts),
                                data.title ?? string.Empty,
                                false,
                                () => SelectInquiryItem(index));

                        RefreshInquiryBadge();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CustomerService] MarkInquiryAsRead error: {e.Message}");
            }
        }

        private void ShowInquiryDetail(InquiryData data)
        {
            if (inquiryDetailRoot != null)
                inquiryDetailRoot.SetActive(true);

            if (inquiryDetailTitle != null)
                inquiryDetailTitle.text = data.title ?? string.Empty;

            if (inquiryDetailDate != null)
                inquiryDetailDate.text = FormatTimestamp(data.ask_ts);

            if (inquiryDetailBody != null)
                inquiryDetailBody.text = data.body ?? string.Empty;

            ShowDetailAttachments(data.files);

            bool hasAnswer = data.answer_ts > 0 && !string.IsNullOrEmpty(data.answer);

            if (inquiryDetailAnswerRoot != null)
                inquiryDetailAnswerRoot.SetActive(hasAnswer);

            if (inquiryDetailAnswer != null)
                inquiryDetailAnswer.text = hasAnswer ? ConvertHtmlToPlainText(data.answer) : string.Empty;

            int deleteTargetId = data.id;
            BindButton(inquiryDeleteButton, () => OnInquiryDeleteClicked(deleteTargetId));
        }

        private void ShowDetailAttachments(List<string> files)
        {
            ClearDetailAttachments();

            bool hasFiles = files != null && files.Count > 0;

            if (inquiryDetailAttachmentRoot != null)
                inquiryDetailAttachmentRoot.SetActive(hasFiles);

            if (!hasFiles)
                return;

            for (int i = 0; i < files.Count; i++)
            {
                string url = files[i];
                if (string.IsNullOrEmpty(url))
                    continue;

                CPButton slot = GetOrCreateDetailAttachmentSlot(i);
                slot.gameObject.SetActive(true);

                var thumbnail = slot.GetComponentInChildren<RawImage>();
                if (thumbnail != null)
                    thumbnail.color = new Color(1f, 1f, 1f, 0.3f);

                var videoIcon = slot.transform.Find("videoIcon");
                bool isVideo = IsVideoUrl(url);
                if (videoIcon != null)
                    videoIcon.gameObject.SetActive(isVideo);

                slot.onClick.RemoveAllListeners();
                string capturedUrl = url;
                slot.onClick.AddListener(() => OpenAttachmentViewer(capturedUrl));

                if (!isVideo)
                    LoadDetailAttachmentImage(thumbnail, url).Forget();
                else
                    LoadVideoThumbnail(thumbnail, url).Forget();
            }

            if (inquiryDetailAttachmentGrid != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(inquiryDetailAttachmentGrid);
                Canvas.ForceUpdateCanvases();
            }
        }

        private CPButton GetOrCreateDetailAttachmentSlot(int index)
        {
            if (index < detailAttachmentSlots.Count)
                return detailAttachmentSlots[index];

            if (inquiryDetailAttachmentGrid == null || inquiryDetailAttachmentSlotPrefab == null)
                return null;

            var slot = Instantiate(inquiryDetailAttachmentSlotPrefab, inquiryDetailAttachmentGrid);
            slot.gameObject.SetActive(true);
            detailAttachmentSlots.Add(slot);

            return slot;
        }

        private async UniTaskVoid LoadDetailAttachmentImage(RawImage thumbnail, string url)
        {
            try
            {
                if (thumbnail == null)
                    return;

                using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
                {
                    await req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"[CustomerService] Failed to load attachment: {url}");
                        return;
                    }

                    if (thumbnail == null)
                        return;

                    Texture2D tex = DownloadHandlerTexture.GetContent(req);
                    detailAttachmentTextures.Add(tex);

                    thumbnail.texture = tex;
                    thumbnail.color = Color.white;

                    var rt = thumbnail.rectTransform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CustomerService] LoadDetailAttachmentImage error: {e.Message}");
            }
        }

        private async UniTaskVoid LoadVideoThumbnail(RawImage thumbnail, string url)
        {
            if (thumbnail == null)
                return;

            thumbnail.texture = null;
            thumbnail.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            try
            {
                var go = new GameObject("ThumbnailCapture");
                var vp = go.AddComponent<VideoPlayer>();
                vp.source = VideoSource.Url;
                vp.url = url;
                vp.playOnAwake = false;
                vp.renderMode = VideoRenderMode.APIOnly;
                vp.sendFrameReadyEvents = true;
                vp.audioOutputMode = VideoAudioOutputMode.None;

                var tcs = new UniTaskCompletionSource<Texture2D>();

                vp.frameReady += (source, frameIdx) =>
                {
                    var rt = source.texture as RenderTexture;
                    if (rt == null)
                    {
                        tcs.TrySetResult(null);
                        return;
                    }

                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;

                    tcs.TrySetResult(tex);
                };

                vp.errorReceived += (source, msg) =>
                {
                    Debug.LogWarning($"[CustomerService] Video thumbnail error: {msg}");
                    tcs.TrySetResult(null);
                };

                var prepareTask = new UniTaskCompletionSource();
                vp.prepareCompleted += _ => prepareTask.TrySetResult();

                vp.Prepare();

                Texture2D result = null;
                try
                {
                    var token = this.GetCancellationTokenOnDestroy();
                    var timeout = UniTask.Delay(10000, cancellationToken: token);

                    int idx = await UniTask.WhenAny(prepareTask.Task, timeout);
                    if (idx != 0)
                    {
                        Debug.LogWarning($"[CustomerService] Video thumbnail prepare timeout: {url}");
                        vp.Stop();
                        Destroy(go);
                        return;
                    }

                    vp.Play();

                    var (hasResult, tex2d) = await UniTask.WhenAny(tcs.Task, UniTask.Delay(5000, cancellationToken: token));
                    if (hasResult)
                        result = tex2d;
                    else
                        Debug.LogWarning($"[CustomerService] Video thumbnail frame timeout: {url}");
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning($"[CustomerService] Video thumbnail cancelled: {url}");
                }
                finally
                {
                    vp.Stop();
                    Destroy(go);
                }

                if (result != null && thumbnail != null)
                {
                    detailAttachmentTextures.Add(result);
                    thumbnail.texture = result;
                    thumbnail.color = Color.white;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CustomerService] LoadVideoThumbnail error: {e.Message}");
            }
        }

        private void ClearDetailAttachments()
        {
            foreach (var tex in detailAttachmentTextures)
            {
                if (tex != null)
                    Destroy(tex);
            }

            detailAttachmentTextures.Clear();

            foreach (var slot in detailAttachmentSlots)
            {
                if (slot != null)
                {
                    slot.onClick.RemoveAllListeners();

                    var thumbnail = slot.GetComponentInChildren<RawImage>();
                    if (thumbnail != null)
                        thumbnail.texture = null;

                    var videoIcon = slot.transform.Find("videoIcon");
                    if (videoIcon != null)
                        videoIcon.gameObject.SetActive(false);

                    slot.gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Media Viewer (Image + Video)

        private Texture2D imageViewerTexture;
        private List<string> imageViewerUrls = new List<string>();
        private int imageViewerCurrentIndex;
        private Vector2 imageViewerOriginalSize;

        private VideoPlayer videoPlayer;
        private RenderTexture videoRenderTexture;
        private bool videoPlayerUpdating;
        private bool isCurrentPageVideo;

        private static readonly string[] VideoUrlExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };

        private bool IsVideoUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            string lower = url.ToLowerInvariant();
            int queryIndex = lower.IndexOf('?');
            if (queryIndex >= 0)
                lower = lower.Substring(0, queryIndex);

            for (int i = 0; i < VideoUrlExtensions.Length; i++)
            {
                if (lower.EndsWith(VideoUrlExtensions[i]))
                    return true;
            }

            return false;
        }

        private void OpenAttachmentViewer(string clickedUrl)
        {
            if (imageViewerRoot == null || imageViewerImage == null)
                return;

            if (imageViewerOriginalSize == Vector2.zero)
                imageViewerOriginalSize = imageViewerImage.rectTransform.sizeDelta;

            imageViewerUrls.Clear();
            int startIndex = 0;

            var currentData = inquirySelectedIndex >= 0 && inquirySelectedIndex < inquiryDataList.Count ? inquiryDataList[inquirySelectedIndex] : null;
            if (currentData?.files != null)
            {
                for (int i = 0; i < currentData.files.Count; i++)
                {
                    if (!string.IsNullOrEmpty(currentData.files[i]))
                    {
                        if (currentData.files[i] == clickedUrl)
                            startIndex = imageViewerUrls.Count;

                        imageViewerUrls.Add(currentData.files[i]);
                    }
                }
            }

            if (imageViewerUrls.Count == 0)
            {
                imageViewerUrls.Add(clickedUrl);
                startIndex = 0;
            }

            imageViewerRoot.SetActive(true);
            ShowMediaViewerPage(startIndex);
        }

        private void ShowMediaViewerPage(int index)
        {
            if (imageViewerUrls.Count == 0)
                return;

            StopVideo();

            imageViewerCurrentIndex = Mathf.Clamp(index, 0, imageViewerUrls.Count - 1);

            if (imageViewerPrevButton != null)
                imageViewerPrevButton.gameObject.SetActive(imageViewerCurrentIndex > 0);

            if (imageViewerNextButton != null)
                imageViewerNextButton.gameObject.SetActive(imageViewerCurrentIndex < imageViewerUrls.Count - 1);

            string url = imageViewerUrls[imageViewerCurrentIndex];
            isCurrentPageVideo = IsVideoUrl(url);

            if (videoControlsRoot != null)
                videoControlsRoot.SetActive(isCurrentPageVideo);

            if (isCurrentPageVideo)
            {
                if (imageViewerImage != null)
                    imageViewerImage.color = Color.white;

                PlayVideo(url);
            }
            else
            {
                if (imageViewerImage != null)
                    imageViewerImage.color = new Color(1f, 1f, 1f, 0.3f);

                LoadImageViewerTexture(url).Forget();
            }
        }

        private async UniTaskVoid LoadImageViewerTexture(string url)
        {
            try
            {
                using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
                {
                    await req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"[CustomerService] Failed to load viewer image: {url}");
                        return;
                    }

                    if (imageViewerImage == null)
                        return;

                    if (imageViewerTexture != null)
                        Destroy(imageViewerTexture);

                    imageViewerTexture = DownloadHandlerTexture.GetContent(req);
                    imageViewerImage.texture = imageViewerTexture;
                    imageViewerImage.color = Color.white;
                    FitImageViewerAspect();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CustomerService] LoadImageViewerTexture error: {e.Message}");
            }
        }

        private void FitImageViewerAspect()
        {
            if (imageViewerTexture == null || imageViewerImage == null)
                return;

            var rt = imageViewerImage.rectTransform;
            float maxW = imageViewerOriginalSize.x;
            float maxH = imageViewerOriginalSize.y;
            float texW = imageViewerTexture.width;
            float texH = imageViewerTexture.height;

            if (texW <= 0 || texH <= 0 || maxW <= 0 || maxH <= 0)
                return;

            float scale = Mathf.Min(maxW / texW, maxH / texH, 1f);
            rt.sizeDelta = new Vector2(texW * scale, texH * scale);
        }

        private void PlayVideo(string url)
        {
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.playOnAwake = false;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
                videoPlayer.loopPointReached += OnVideoFinished;
            }

            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
            }

            videoRenderTexture = new RenderTexture(1920, 1080, 0);
            videoPlayer.targetTexture = videoRenderTexture;
            imageViewerImage.texture = videoRenderTexture;

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;

            videoPlayerUpdating = true;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();
            Debug.Log($"[CustomerService] VideoPlayer.Prepare() called, isPrepared: {videoPlayer.isPrepared}");

            UpdateVideoPlayerUI().Forget();
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"[CustomerService] VideoPlayer error: {message}");
            vp.errorReceived -= OnVideoError;
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            vp.prepareCompleted -= OnVideoPrepared;

            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
            }

            videoRenderTexture = new RenderTexture((int)vp.width, (int)vp.height, 0);
            vp.targetTexture = videoRenderTexture;

            if (imageViewerImage != null)
            {
                imageViewerImage.texture = videoRenderTexture;

                var rt = imageViewerImage.rectTransform;
                float maxW = imageViewerOriginalSize.x;
                float maxH = imageViewerOriginalSize.y;
                float vidW = vp.width;
                float vidH = vp.height;

                Debug.Log($"[CustomerService] OnVideoPrepared: {vidW}x{vidH}, originalSize: {maxW}x{maxH}");

                if (maxW <= 0 || maxH <= 0)
                {
                    maxW = rt.rect.width;
                    maxH = rt.rect.height;
                    if (maxW <= 0) maxW = Screen.width;
                    if (maxH <= 0) maxH = Screen.height;
                }

                if (vidW > 0 && vidH > 0)
                {
                    float scale = Mathf.Min(maxW / vidW, maxH / vidH);
                    rt.sizeDelta = new Vector2(vidW * scale, vidH * scale);
                }
            }

            vp.Pause();
            SetVideoPlayButtonAlpha(1f);
        }

        private void OnVideoFinished(VideoPlayer vp)
        {
            vp.time = 0;
            vp.Pause();
            SetVideoPlayButtonAlpha(1f);
        }

        private void ToggleVideoPlayPause()
        {
            if (videoPlayer == null)
                return;

            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                SetVideoPlayButtonAlpha(1f);
            }
            else
            {
                videoPlayer.Play();
                SetVideoPlayButtonAlpha(0f);
            }
        }

        private void SetVideoPlayButtonAlpha(float alpha)
        {
            if (videoPlayPauseButton == null)
                return;

            var image = videoPlayPauseButton.GetComponent<Image>();
            if (image != null)
            {
                var c = image.color;
                c.a = alpha;
                image.color = c;
            }

            var childImages = videoPlayPauseButton.GetComponentsInChildren<Image>(true);
            foreach (var img in childImages)
            {
                var c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }

        private async UniTaskVoid UpdateVideoPlayerUI()
        {
            while (videoPlayerUpdating && videoPlayer != null)
            {
                if (videoPlayer.length > 0)
                {
                    float current = (float)videoPlayer.time;
                    float total = (float)videoPlayer.length;

                    if (videoSeekBar != null && !isSeekBarDragging)
                        videoSeekBar.SetValueWithoutNotify(current / total);

                    if (videoTimeText != null)
                        videoTimeText.text = $"{FormatVideoTime(Mathf.Min(current, total))} / {FormatVideoTime(total)}";
                }

                await UniTask.Delay(200, cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }

        private bool isSeekBarDragging;

        private void OnVideoSeekBarChanged(float value)
        {
            if (videoPlayer == null || videoPlayer.length <= 0)
                return;

            isSeekBarDragging = true;
            videoPlayer.time = value * videoPlayer.length;
            isSeekBarDragging = false;
        }

        private static string FormatVideoTime(float seconds)
        {
            int totalSec = Mathf.RoundToInt(seconds);
            int min = totalSec / 60;
            int sec = totalSec % 60;
            return $"{min:D2}:{sec:D2}";
        }

        private void StopVideo()
        {
            videoPlayerUpdating = false;

            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.targetTexture = null;
            }

            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
                videoRenderTexture = null;
            }
        }

        private void CloseMediaViewer()
        {
            StopVideo();

            if (imageViewerRoot != null)
                imageViewerRoot.SetActive(false);

            if (imageViewerImage != null)
                imageViewerImage.texture = null;

            if (imageViewerTexture != null)
            {
                Destroy(imageViewerTexture);
                imageViewerTexture = null;
            }

            imageViewerUrls.Clear();
            imageViewerCurrentIndex = 0;
        }

        #endregion

        private void OnInquiryDeleteClicked(int inquiryId)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupTwoButtons("문의를 삭제하시겠습니까?", "삭제된 문의는 복구할 수 없습니다.", () => DeleteInquiry(inquiryId).Forget(), null));
        }

        [Serializable]
        private class AskDeleteRequest
        {
            public int askId;
        }

        private async UniTaskVoid DeleteInquiry(int inquiryId)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            try
            {
                var requestBody = new AskDeleteRequest { askId = inquiryId };
                string json = JsonUtility.ToJson(requestBody);
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

                using (UnityWebRequest req = new UnityWebRequest(inquiryApiUrl, "DELETE"))
                {
                    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    SetCommonHeaders(req);

                    await req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("문의 삭제에 실패했습니다.", true));
                        return;
                    }
                }

                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("문의가 삭제되었습니다.", true));

                inquiryLoaded = false;
                LoadInquiryList().Forget();
            }
            finally
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
            }
        }

        private void EnsureInquiryItemInstances(int requiredCount)
        {
            if (inquiryListLayoutRoot == null || inquiryItemPrefab == null)
                return;

            while (inquiryItemInstances.Count < requiredCount)
            {
                var instance = Instantiate(inquiryItemPrefab, inquiryListLayoutRoot);
                instance.gameObject.SetActive(true);
                inquiryItemInstances.Add(instance);
            }
        }

        private void RefreshInquiryListLayout()
        {
            if (inquiryListLayoutRoot == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(inquiryListLayoutRoot);
            Canvas.ForceUpdateCanvases();
        }

        private static string FormatRelativeTime(long unixTimestamp)
        {
            if (unixTimestamp <= 0)
                return string.Empty;

            var dto = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            TimeSpan diff = DateTimeOffset.UtcNow - dto;

            if (diff.TotalMinutes < 1)
                return "방금 전";
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes}분 전";
            if (diff.TotalDays < 1)
                return $"{(int)diff.TotalHours}시간 전";
            if (diff.TotalDays < 30)
                return $"{(int)diff.TotalDays}일 전";
            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)}개월 전";

            return $"{(int)(diff.TotalDays / 365)}년 전";
        }

        private static string FormatTimestamp(long unixTimestamp)
        {
            if (unixTimestamp <= 0)
                return string.Empty;

            var dto = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            var local = dto.ToLocalTime();
            return local.ToString("yyyy.MM.dd");
        }

        #region Inquiry Write

        public void ShowInquiryWritePage()
        {
            SetPage(CustomerServicePage.InquiryWrite);
            ClearInquiryWriteForm();
            ResetInquiryWriteScrollPosition();
        }

        private void ResetInquiryWriteScrollPosition()
        {
            if (inquiryWriteScrollRect == null)
                return;

            inquiryWriteScrollRect.velocity = Vector2.zero;
            inquiryWriteScrollRect.verticalNormalizedPosition = 1f;
        }

        private void ClearInquiryWriteForm()
        {
            if (inquiryWriteTitleInput != null)
                inquiryWriteTitleInput.text = string.Empty;

            if (inquiryWriteEmailInput != null)
                inquiryWriteEmailInput.text = string.Empty;

            if (inquiryWriteBodyInput != null)
                inquiryWriteBodyInput.text = string.Empty;

            ClearAttachments();
        }

        private void OnInquirySubmitClicked()
        {
            if (!ValidateInquiryForm())
                return;

            PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupTwoButtons("문의를 등록하시겠습니까?", "접수된 문의는 순차적으로 답변해 드리고 있습니다.", () => SubmitInquiry().Forget(), null));
        }

        private void OnTitleInputChanged(string text)
        {
            if (inquiryWriteTitleInput != null && text.Length >= inquiryWriteTitleInput.characterLimit)
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("제목은 30자를 초과할 수 없습니다", true));
        }

        private void OnBodyInputChanged(string text)
        {
            if (inquiryWriteBodyInput != null && text.Length >= inquiryWriteBodyInput.characterLimit)
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("문의내용은 3000자를 초과할 수 없습니다", true));
        }

        private bool ValidateInquiryForm()
        {
            string title = inquiryWriteTitleInput != null ? inquiryWriteTitleInput.text.Trim() : string.Empty;
            string body = inquiryWriteBodyInput != null ? inquiryWriteBodyInput.text.Trim() : string.Empty;

            if (title.Length < 2)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("제목은 두 글자 이상으로 기재해야 합니다", true));
                return false;
            }

            if (title.Length > 30)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("제목은 30자를 초과할 수 없습니다", true));
                return false;
            }

            if (body.Length < 5)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("문의내용은 다섯 글자 이상으로 기재해야 합니다", true));
                return false;
            }

            if (body.Length > 3000)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("문의내용은 3000자를 초과할 수 없습니다", true));
                return false;
            }

            return true;
        }

        private async UniTaskVoid SubmitInquiry()
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            try
            {
                var uploadedFileUrls = new List<string>();

                foreach (var info in attachedFiles)
                {
                    if (string.IsNullOrEmpty(info.filePath) || !File.Exists(info.filePath))
                        continue;

                    string fileName = Path.GetFileName(info.filePath);
                    string fileType = GetMimeType(fileName);
                    byte[] fileData = File.ReadAllBytes(info.filePath);

                    string fileUrl = await UploadFileViaPresignedUrl(fileName, fileType, fileData);
                    if (string.IsNullOrEmpty(fileUrl))
                    {
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup($"파일 업로드에 실패했습니다: {fileName}", true));
                        return;
                    }

                    uploadedFileUrls.Add(fileUrl);
                }

                var requestBody = new InquiryAddRequest
                {
                    title = inquiryWriteTitleInput != null ? inquiryWriteTitleInput.text.Trim() : string.Empty,
                    body = inquiryWriteBodyInput != null ? inquiryWriteBodyInput.text.Trim() : string.Empty,
                    email = inquiryWriteEmailInput != null ? inquiryWriteEmailInput.text.Trim() : string.Empty,
                    files = uploadedFileUrls
                };

                string jsonBody = JsonUtility.ToJson(requestBody);
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

                using (UnityWebRequest req = new UnityWebRequest(inquiryAddApiUrl, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    SetCommonHeaders(req);

                    await req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("문의 등록에 실패했습니다.", true));
                        return;
                    }
                }

                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("문의가 등록되었습니다.", true));

                inquiryLoaded = false;
                ShowInquiryPage();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CustomerService] SubmitInquiry exception: {e}");
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("문의 등록 중 오류가 발생했습니다.", true));
            }
            finally
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
            }
        }

        private async UniTask<string> UploadFileViaPresignedUrl(string fileName, string fileType, byte[] fileData)
        {
            string presignedApiUrl = inquiryAddApiUrl.Replace("/api/ask", "/api/ask/presigned-url");

            var presignedReq = new PresignedUrlRequest
            {
                fileName = fileName,
                fileType = fileType
            };

            string presignedJson = JsonUtility.ToJson(presignedReq);

            PresignedUrlResponse presignedResponse;

            using (UnityWebRequest req = new UnityWebRequest(presignedApiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(presignedJson);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                SetCommonHeaders(req);

                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                    return null;

                presignedResponse = JsonUtility.FromJson<PresignedUrlResponse>(req.downloadHandler.text);
            }

            if (string.IsNullOrEmpty(presignedResponse?.uploadUrl))
                return null;

            using (UnityWebRequest req = new UnityWebRequest(presignedResponse.uploadUrl, "PUT"))
            {
                req.uploadHandler = new UploadHandlerRaw(fileData);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", fileType);

                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[CustomerService] S3 upload error: {req.error}");
                    return null;
                }
            }

            return presignedResponse.fileUrl;
        }

        private static string GetMimeType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            switch (ext)
            {
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".bmp": return "image/bmp";
                case ".webp": return "image/webp";
                case ".mp4": return "video/mp4";
                case ".mov": return "video/quicktime";
                case ".avi": return "video/x-msvideo";
                case ".mkv": return "video/x-matroska";
                case ".webm": return "video/webm";
                default: return "application/octet-stream";
            }
        }

        #endregion

        #region Attachments

        private void OnAttachmentAddClicked()
        {
            if (attachedFiles.Count >= maxAttachmentCount)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup($"첨부파일은 최대 {maxAttachmentCount}개까지 등록 가능합니다.", true));
                return;
            }

            PickFileFromGallery();
        }

        private void PickFileFromGallery()
        {
            var mediaTypes = NativeGallery.MediaType.Image | NativeGallery.MediaType.Video;

            if (!NativeGallery.CheckPermission(NativeGallery.PermissionType.Read, mediaTypes))
            {
                NativeGallery.RequestPermissionAsync((permission) =>
                {
                    if (permission == NativeGallery.Permission.Granted)
                        OpenGalleryPicker(mediaTypes);
                    else
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("갤러리 접근 권한이 필요합니다.", true));
                }, NativeGallery.PermissionType.Read, mediaTypes);
                return;
            }

            OpenGalleryPicker(mediaTypes);
        }

        private void OpenGalleryPicker(NativeGallery.MediaType mediaTypes)
        {
            NativeGallery.GetMixedMediaFromGallery((path) =>
            {
                if (string.IsNullOrEmpty(path))
                    return;

                AddAttachment(path);
            }, mediaTypes);
        }

        private void AddAttachment(string path)
        {
            if (attachedFiles.Count >= maxAttachmentCount)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup($"첨부파일은 최대 {maxAttachmentCount}개까지 등록 가능합니다.", true));
                return;
            }

            try
            {
                var fileInfo = new FileInfo(path);
                if (!fileInfo.Exists)
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("파일을 찾을 수 없습니다.", true));
                    return;
                }

                if (fileInfo.Length > maxFileSizeBytes)
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("파일 크기는 10MB 이하만 가능합니다.", true));
                    return;
                }

                string ext = Path.GetExtension(path).ToLowerInvariant();
                bool isVideo = Array.Exists(VideoExtensions, e => e == ext);
                bool isImage = Array.Exists(ImageExtensions, e => e == ext);

                if (!isVideo && !isImage)
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("이미지 또는 영상 파일만 첨부할 수 있습니다.", true));
                    return;
                }

                Texture2D thumbnail = null;
                if (isImage)
                    thumbnail = NativeGallery.LoadImageAtPath(path, 256);
                else if (isVideo)
                    thumbnail = NativeGallery.GetVideoThumbnail(path, 256);

                var info = new AttachmentFileInfo
                {
                    filePath = path,
                    thumbnail = thumbnail,
                    isVideo = isVideo
                };

                attachedFiles.Add(info);
                int slotIndex = attachedFiles.Count - 1;
                CreateAttachmentSlot(info, slotIndex);
                RefreshAttachmentGrid();

                if (isVideo && thumbnail == null)
                    LoadLocalVideoThumbnail(path, info, slotIndex).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CustomerService] AddAttachment error: {e}");
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup("파일을 추가하는 중 오류가 발생했습니다.", true));
            }
        }

        private async UniTaskVoid LoadLocalVideoThumbnail(string filePath, AttachmentFileInfo info, int slotIndex)
        {
            try
            {
                string videoUrl = "file://" + filePath.Replace("\\", "/");

                var go = new GameObject("LocalThumbnailCapture");
                var vp = go.AddComponent<VideoPlayer>();
                vp.source = VideoSource.Url;
                vp.url = videoUrl;
                vp.playOnAwake = false;
                vp.renderMode = VideoRenderMode.APIOnly;
                vp.sendFrameReadyEvents = true;
                vp.audioOutputMode = VideoAudioOutputMode.None;

                var tcs = new UniTaskCompletionSource<Texture2D>();

                vp.frameReady += (source, frameIdx) =>
                {
                    var rt = source.texture as RenderTexture;
                    if (rt == null)
                    {
                        tcs.TrySetResult(null);
                        return;
                    }

                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;

                    tcs.TrySetResult(tex);
                };

                vp.errorReceived += (source, msg) =>
                {
                    Debug.LogWarning($"[CustomerService] Local video thumbnail error: {msg}");
                    tcs.TrySetResult(null);
                };

                var prepareTask = new UniTaskCompletionSource();
                vp.prepareCompleted += _ => prepareTask.TrySetResult();

                vp.Prepare();

                Texture2D result = null;
                try
                {
                    var token = this.GetCancellationTokenOnDestroy();
                    int idx = await UniTask.WhenAny(prepareTask.Task, UniTask.Delay(10000, cancellationToken: token));
                    if (idx != 0)
                    {
                        vp.Stop();
                        Destroy(go);
                        return;
                    }

                    vp.Play();

                    var (hasResult, tex2d) = await UniTask.WhenAny(tcs.Task, UniTask.Delay(5000, cancellationToken: token));
                    if (hasResult)
                        result = tex2d;
                }
                catch (OperationCanceledException) { }
                finally
                {
                    vp.Stop();
                    Destroy(go);
                }

                if (result != null && slotIndex < attachedFiles.Count && attachedFiles[slotIndex] == info)
                {
                    info.thumbnail = result;

                    if (slotIndex < attachmentSlotInstances.Count)
                    {
                        var slot = attachmentSlotInstances[slotIndex];
                        if (slot != null)
                            slot.Setup(info.filePath, result, info.isVideo, () => RemoveAttachment(slotIndex), () => OpenWriteAttachmentViewer(slotIndex));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CustomerService] LoadLocalVideoThumbnail error: {e.Message}");
            }
        }

        private void CreateAttachmentSlot(AttachmentFileInfo info, int index)
        {
            if (attachmentGridRoot == null || attachmentSlotPrefab == null)
                return;

            CustomerServiceAttachmentSlot slot;
            if (index < attachmentSlotInstances.Count)
            {
                slot = attachmentSlotInstances[index];
            }
            else
            {
                slot = Instantiate(attachmentSlotPrefab, attachmentGridRoot);
                attachmentSlotInstances.Add(slot);
            }

            slot.gameObject.SetActive(true);
            int capturedIndex = index;
            slot.Setup(info.filePath, info.thumbnail, info.isVideo, () => RemoveAttachment(capturedIndex), () => OpenWriteAttachmentViewer(capturedIndex));
        }

        private void OpenWriteAttachmentViewer(int index)
        {
            if (imageViewerRoot == null || imageViewerImage == null)
                return;

            if (imageViewerOriginalSize == Vector2.zero)
                imageViewerOriginalSize = imageViewerImage.rectTransform.sizeDelta;

            imageViewerUrls.Clear();
            int startIndex = 0;

            for (int i = 0; i < attachedFiles.Count; i++)
            {
                string path = attachedFiles[i].filePath;
                if (string.IsNullOrEmpty(path))
                    continue;

                string url = attachedFiles[i].isVideo
                    ? "file://" + path.Replace("\\", "/")
                    : "file://" + path.Replace("\\", "/");

                if (i == index)
                    startIndex = imageViewerUrls.Count;

                imageViewerUrls.Add(url);
            }

            if (imageViewerUrls.Count == 0)
                return;

            imageViewerRoot.SetActive(true);
            ShowMediaViewerPage(startIndex);
        }

        private void RemoveAttachment(int index)
        {
            if (index < 0 || index >= attachedFiles.Count)
                return;

            var info = attachedFiles[index];
            if (info.thumbnail != null)
            {
                UnityEngine.Object.Destroy(info.thumbnail);
                info.thumbnail = null;
            }

            attachedFiles.RemoveAt(index);
            RebuildAttachmentSlots();
            RefreshAttachmentGrid();
        }

        private void RebuildAttachmentSlots()
        {
            for (int i = 0; i < attachmentSlotInstances.Count; i++)
            {
                var slot = attachmentSlotInstances[i];
                if (slot == null)
                    continue;

                if (i < attachedFiles.Count)
                {
                    var info = attachedFiles[i];
                    int capturedIndex = i;
                    slot.gameObject.SetActive(true);
                    slot.Setup(info.filePath, info.thumbnail, info.isVideo, () => RemoveAttachment(capturedIndex), () => OpenWriteAttachmentViewer(capturedIndex));
                }
                else
                {
                    slot.Clear(false);
                    slot.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshAttachmentGrid()
        {
            bool canAddMore = attachedFiles.Count < maxAttachmentCount;

            if (attachmentAddButton != null)
                attachmentAddButton.gameObject.SetActive(canAddMore);

            if (attachmentAddButton != null && attachmentGridRoot != null)
                attachmentAddButton.transform.SetAsLastSibling();

            if (attachmentGridRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(attachmentGridRoot);

                var contentRoot = attachmentGridRoot.parent as RectTransform;
                if (contentRoot != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

                Canvas.ForceUpdateCanvases();
            }
        }

        private void ClearAttachments()
        {
            foreach (var info in attachedFiles)
            {
                if (info.thumbnail != null)
                    UnityEngine.Object.Destroy(info.thumbnail);
            }

            attachedFiles.Clear();

            foreach (var slot in attachmentSlotInstances)
            {
                if (slot != null)
                {
                    slot.Clear();
                    slot.gameObject.SetActive(false);
                }
            }

            RefreshAttachmentGrid();
        }

        #endregion

        private static void BindButton(CPButton button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}