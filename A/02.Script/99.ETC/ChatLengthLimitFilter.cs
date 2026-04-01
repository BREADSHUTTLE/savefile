using System;
using AdvancedInputFieldPlugin;
using CAPYBARA.Core;
using UnityEngine;

namespace CAPYBARA
{
    public class ChatLengthLimitFilter : LiveProcessingFilter
    {
        [SerializeField] private int characterLimit = 100;
        [SerializeField] private string toastMessage;

        private void Awake()
        {
            toastMessage = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Max100Chars].StringToLocal;
        }

        public void Configure(int limit)
        {
            characterLimit = limit;
        }

        public override TextEditFrame ProcessTextEditUpdate(TextEditFrame textEditFrame, TextEditFrame lastTextEditFrame)
        {
            if (textEditFrame.text == null)
                return textEditFrame;

            bool hasLast = lastTextEditFrame.text != null;

            if (textEditFrame.text.Length > 0 && char.IsWhiteSpace(textEditFrame.text[0]))
                return hasLast ? lastTextEditFrame : new TextEditFrame("", 0, 0);

            if (characterLimit <= 0)
                return textEditFrame;

            if (textEditFrame.text.Length <= characterLimit)
                return textEditFrame;

            bool shouldToast = hasLast && lastTextEditFrame.text.Length <= characterLimit;
            if (shouldToast)
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(toastMessage, false));

            if (hasLast)
            {
                int selectionLength = Mathf.Abs(lastTextEditFrame.selectionEndPosition - lastTextEditFrame.selectionStartPosition);
                int insertAmount = textEditFrame.text.Length - (lastTextEditFrame.text.Length - selectionLength);

                // 붙여넣기/대량 입력일 때만 100자까지 잘라서 반영
                if (insertAmount > 1)
                {
                    string trimmed = textEditFrame.text.Substring(0, characterLimit);
                    int caretPos = Mathf.Min(textEditFrame.selectionStartPosition, trimmed.Length);
                    return new TextEditFrame(trimmed, caretPos, caretPos);
                }

                // 일반 입력은 아예 반영하지 않음
                return lastTextEditFrame;
            }

            string fallbackTrimmed = textEditFrame.text.Substring(0, characterLimit);
            int fallbackCaret = Mathf.Min(textEditFrame.selectionStartPosition, fallbackTrimmed.Length);
            return new TextEditFrame(fallbackTrimmed, fallbackCaret, fallbackCaret);
        }
    }
}
