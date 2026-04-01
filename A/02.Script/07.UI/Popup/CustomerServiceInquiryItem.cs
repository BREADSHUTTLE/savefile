using CAPYBARA.Bundles;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class CustomerServiceInquiryItem : MonoBehaviour
    {
        [SerializeField] private CPButton selectButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private GameObject newBadge;
        [SerializeField] private GameObject selectedIndicator;

        public void Bind(string status, string time, string title, bool isNew, UnityAction onClick)
        {
            if (statusText != null)
                statusText.text = status ?? string.Empty;

            if (timeText != null)
                timeText.text = time ?? string.Empty;

            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (newBadge != null)
                newBadge.SetActive(isNew);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(onClick);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(selected);
        }
    }
}
