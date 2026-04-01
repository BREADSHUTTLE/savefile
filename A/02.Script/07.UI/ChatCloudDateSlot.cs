using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class ChatCloudDateSlot : Poolable
    {
        public TMP_Text dateText;
        
        public void SetDate(string date)
        {
            if (dateText != null)
                dateText.text = date;
        }
    }
}
