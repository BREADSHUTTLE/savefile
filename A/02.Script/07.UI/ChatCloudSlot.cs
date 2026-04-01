using System;
using System.Collections.Generic;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CAPYBARA
{
    public enum ChatCloudType
    {
        Other,
        Me,
    }

    public enum EmotionType
    {
        NONE = 0,
        HAPPY,
        SAD,
        MAD,
        SMILE,
        DIFFICULT,
    }

    public class ChatCloudSlot : Poolable
    {
        private static GameObject currentOpenEmotionBox = null;
        public static Action<long, string, string, string> OnEmotionReaction;       // messageId, newEmotionId, originalMessage, existingEmotions
        public static Action<long, string, string, string> OnEmotionRemove;         // messageId, emotionIdToRemove, originalMessage, existingEmotions
        
        public RectTransform rectTransform;
        public GameObject[] cloudObject;
        public TMP_Text[] cloudText;

        public TMP_Text[] timeText;

        public string emotion = "";

        public GameObject[] emotionBox;

        public GameObject[] chatObject;
        
        public LongPressButton longPressButtonLeft;   // 상대방 메시지용
        public LongPressButton longPressButtonRight;  // 내 메시지용
        
        public Button[] emotionButtonsLeft;   // 상대방 메시지용 이모지 버튼
        public Button[] emotionButtonsRight;  // 내 메시지용 이모지 버튼
        public EmotionType[] emotionTypes;
        
        public GameObject[] emotionDisplayObject;       // 0: Left, 1: Right
        
        public Emoji emotionPrefab;
        
        [HideInInspector]public string messegeId = "";

        [HideInInspector]public bool isMe;

        public lobby.Message myChatData;
        
        public Animation[] chatAnimation;
        
        private bool isInitialized = false;
        private List<Emoji> currentEmotionInstances = new List<Emoji>();
        
        private void Awake()
        {
            InitLongPressButtons();
            InitEmotionButtons();
        }
        
        private void InitLongPressButtons()
        {
            if (isInitialized)
                return;
            
            if (longPressButtonLeft != null)
                longPressButtonLeft.onLongPress.AddListener(OpenEmotionBox);
            
            if (longPressButtonRight != null)
                longPressButtonRight.onLongPress.AddListener(OpenEmotionBox);
        }
        
        private void InitEmotionButtons()
        {
            if (emotionTypes == null)
                return;
            
            if (emotionButtonsLeft != null)
            {
                for (int i = 0; i < emotionButtonsLeft.Length && i < emotionTypes.Length; i++)
                {
                    if (emotionButtonsLeft[i] == null) continue;
                    
                    int index = i;
                    emotionButtonsLeft[i].onClick.AddListener(() => OnEmotionButtonClicked(emotionTypes[index]));
                }
            }
            
            if (emotionButtonsRight != null)
            {
                for (int i = 0; i < emotionButtonsRight.Length && i < emotionTypes.Length; i++)
                {
                    if (emotionButtonsRight[i] == null) continue;
                    
                    int index = i;
                    emotionButtonsRight[i].onClick.AddListener(() => OnEmotionButtonClicked(emotionTypes[index]));
                }
            }
            
            isInitialized = true;
        }
        
        private void OnEmotionButtonClicked(EmotionType emotionType)
        {
            string emotionId = emotionType.ToString();
            string existingEmotions = myChatData.Emotion ?? "";
            OnEmotionReaction?.Invoke(myChatData.MessageId, emotionId, myChatData.Message_, existingEmotions);
            
            CloseAllEmotionBox();
        }

        public void OpenEmotionBox()
        {
            if (isMe)
                return;
            
            if (currentOpenEmotionBox != null && currentOpenEmotionBox != GetActiveEmotionBox())
                currentOpenEmotionBox.SetActive(false);
            
            GameObject activeBox = GetActiveEmotionBox();
            if (activeBox != null)
            {
                activeBox.SetActive(true);
                currentOpenEmotionBox = activeBox;
                
                var layoutElement = activeBox.GetComponent<LayoutElement>();
                int displayIndex = isMe ? 1 : 0;
                
                // 이모지가 있으면 Layout 무시하고 EmojiList 위에 덮어서 띄움
                if (currentEmotionInstances.Count > 0 && emotionDisplayObject != null && displayIndex < emotionDisplayObject.Length)
                {
                    var emojiListObj = emotionDisplayObject[displayIndex];
                    if (emojiListObj != null && layoutElement != null)
                    {
                        layoutElement.ignoreLayout = true;
                        
                        var emojiListRect = emojiListObj.GetComponent<RectTransform>();
                        var boxRect = activeBox.GetComponent<RectTransform>();
                        if (emojiListRect != null && boxRect != null)
                            boxRect.anchoredPosition = emojiListRect.anchoredPosition;
                        
                        activeBox.transform.SetAsLastSibling();
                        
                        var overrideCanvas = activeBox.GetComponent<Canvas>();
                        if (overrideCanvas == null)
                        {
                            overrideCanvas = activeBox.AddComponent<Canvas>();
                            overrideCanvas.overrideSorting = true;
                            overrideCanvas.sortingOrder = 101;
                            activeBox.AddComponent<GraphicRaycaster>();
                        }
                    }
                }
                else
                {
                    if (layoutElement != null)
                        layoutElement.ignoreLayout = false;
                }
            }
        }
        
        private GameObject GetActiveEmotionBox()
        {
            if (emotionBox == null || emotionBox.Length < 2)
                return null;
                
            return isMe ? emotionBox[1] : emotionBox[0];    // isMe 기준으로 Right(1) 또는 Left(0) 선택
        }
        
        public static void CloseAllEmotionBox()
        {
            if (currentOpenEmotionBox != null)
            {
                currentOpenEmotionBox.SetActive(false);
                currentOpenEmotionBox = null;
            }
        }
        
        private void Update()
        {
            // 이모지 박스가 열려있을 때 밖을 클릭하면 닫기
            if (currentOpenEmotionBox != null && currentOpenEmotionBox.activeSelf)
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    if (!IsPointerOverEmotionBox())
                        CloseAllEmotionBox();
                }
            }
        }
        
        private bool IsPointerOverEmotionBox()
        {
            if (EventSystem.current == null)
                return false;
            
            var pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = Input.mousePosition;
            
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);
            
            foreach (var result in raycastResults)
            {
                // 클릭한 UI가 이모지 박스 자체이거나 자식인지 확인
                if (result.gameObject == currentOpenEmotionBox || 
                    result.gameObject.transform.IsChildOf(currentOpenEmotionBox.transform))
                    return true;
            }
            
            return false;
        }
        
        public void SetChat(bool isMe, lobby.Message chatData, bool skipAnimation = false, bool skipEmojiAnimation = true, string newEmotionId = null, bool showTime = true)
        {
            this.isMe = isMe;
            myChatData = chatData;
            cloudObject[0].SetActive(!isMe);
            cloudObject[1].SetActive(isMe);
            
            if (emotionBox != null)
            {
                foreach (var box in emotionBox)
                {
                    if (box != null)
                        box.SetActive(false);
                }
            }

            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(chatData.CreatedAt).LocalDateTime;
            Debug.Log($"[ChatCloudSlot] CreatedAt: {chatData.CreatedAt}, UTC: {DateTimeOffset.FromUnixTimeSeconds(chatData.CreatedAt).UtcDateTime}, Local: {dateTime}");
            string formattedTime = FormatTimeStyle(dateTime);

            if (isMe)
            {
                if (chatObject != null && chatObject.Length > 1 && chatObject[1] != null)
                    chatObject[1].SetActive(true);
                    
                cloudText[1].text = chatData.Message_;
                timeText[1].text = showTime ? formattedTime : "";
                if (timeText[1] != null)
                    timeText[1].gameObject.SetActive(showTime);
            }
            else
            {
                if (chatObject != null && chatObject.Length > 0 && chatObject[0] != null)
                    chatObject[0].SetActive(true);
                    
                cloudText[0].text = chatData.Message_;
                timeText[0].text = showTime ? formattedTime : "";
                if (timeText[0] != null)
                    timeText[0].gameObject.SetActive(showTime);
            }
            
            DisplayEmotion(chatData.Emotion, isMe, skipEmojiAnimation, newEmotionId);

            if (chatAnimation != null && chatAnimation.Length > 0)
            if (skipAnimation)
            {
                for (int i = 0; i < chatAnimation.Length; i++)
                    chatAnimation[i].enabled = !skipAnimation;
            }
            
            gameObject.SetActive(true);
        }

        private void DisplayEmotion(string emotionId, bool isMe, bool skipAnimation = true, string newEmotionId = null)
        {
            // 기존 인스턴스 제거 (gameObject 단위로)
            foreach (var instance in currentEmotionInstances)
            {
                if (instance != null)
                    Destroy(instance.gameObject);
            }

            currentEmotionInstances.Clear();
            
            if (emotionDisplayObject != null)
            {
                foreach (var obj in emotionDisplayObject)
                    if (obj != null)
                        obj.SetActive(false);
            }

            string upperEmotionId = emotionId?.ToUpper();
            if (string.IsNullOrEmpty(upperEmotionId) || upperEmotionId == EmotionType.NONE.ToString())
                return;
            
            if (emotionPrefab == null)
                return;
            
            int displayIndex = isMe ? 1 : 0;
            Transform parent = null;
            
            if (emotionDisplayObject != null && displayIndex < emotionDisplayObject.Length && emotionDisplayObject[displayIndex] != null)
                parent = emotionDisplayObject[displayIndex].transform;
            
            if (parent == null)
                return;
            
            string upperNewEmotionId = newEmotionId?.ToUpper();
            
            string[] emotionIds = upperEmotionId.Split(',');
            foreach (var emoId in emotionIds)
            {
                string trimmedId = emoId.Trim();
                if (string.IsNullOrEmpty(trimmedId) || trimmedId == EmotionType.NONE.ToString())
                    continue;
                
                var emojiSprite = BlackTree.Bundles.SocialBundle.Loaded?.GetEmojiSprite(trimmedId);
                if (emojiSprite == null)
                    continue;
                
                var instance = Instantiate(emotionPrefab, parent);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localScale = Vector3.one;
                instance.SetSprite(emojiSprite);
                instance.emotionId = trimmedId;
                
                bool shouldSkipAnimation;
                if (!string.IsNullOrEmpty(upperNewEmotionId))
                    shouldSkipAnimation = (trimmedId != upperNewEmotionId);
                else
                    shouldSkipAnimation = skipAnimation;
                
                if (shouldSkipAnimation && instance.emojiAnimation != null)
                    instance.emojiAnimation.enabled = false;
                
                instance.OnClick = (clickedEmotionId) => OnEmojiClicked(clickedEmotionId);
                
                if (isMe)
                    instance.transform.SetAsFirstSibling();
                
                currentEmotionInstances.Add(instance);
            }
            
            if (currentEmotionInstances.Count > 0)
                emotionDisplayObject[displayIndex].SetActive(true);
        }
        
        private void OnEmojiClicked(string emotionIdToRemove)
        {
            string existingEmotions = myChatData.Emotion ?? "";
            OnEmotionRemove?.Invoke(myChatData.MessageId, emotionIdToRemove, myChatData.Message_, existingEmotions);
        }
        
        private string FormatTimeStyle(DateTime dateTime)
        {
            string amPm = dateTime.Hour < 12 ? StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AM].StringToLocal : StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PM].StringToLocal;
            int hour = dateTime.Hour % 12;
            if (hour == 0) hour = 12;
            return $"{amPm} {hour}:{dateTime.Minute:D2}";
        }

        public void UpdateTimeDisplay(bool showTime)
        {
            if (myChatData == null)
                return;
                
            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(myChatData.CreatedAt).LocalDateTime;
            string formattedTime = FormatTimeStyle(dateTime);
            
            int index = isMe ? 1 : 0;
            if (timeText != null && index < timeText.Length && timeText[index] != null)
            {
                timeText[index].text = showTime ? formattedTime : "";
                timeText[index].gameObject.SetActive(showTime);
            }
        }
    }
}