using CAPYBARA.Bundles;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class CustomerServiceFaqItem : MonoBehaviour
    {
        [SerializeField] private CPButton toggleButton;
        [SerializeField] private RectTransform rectQuestion;
        [SerializeField] private TMP_Text questionText;

        [SerializeField] private RectTransform rectAnswer;
        [SerializeField] private TMP_Text answerText;
        [SerializeField] private GameObject answerRoot;
        [SerializeField] private RectTransform arrowRoot;

        public void BindToggle(UnityAction onClick)
        {
            if (toggleButton == null)
                return;

            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(onClick);
        }

        public void SetTexts(string question, string answer)
        {
            if (questionText != null)
                questionText.text = question ?? string.Empty;

            if (answerText != null)
                answerText.text = answer ?? string.Empty;

            if (answerRoot != null)
                answerRoot.SetActive(true);

            if (rectQuestion != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectQuestion);
            if (rectAnswer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectAnswer);

            if (answerRoot != null)
                answerRoot.SetActive(false);
        }

        public void SetExpanded(bool expanded)
        {
            if (answerRoot != null)
                answerRoot.SetActive(expanded);

            if (arrowRoot != null)
                arrowRoot.localEulerAngles = expanded ? new Vector3(0f, 0f, 180f) : Vector3.zero;

            if (expanded)
            {
                if (rectAnswer != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectAnswer);
            }
        }
    }
}
