using System;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class LocalizeText : MonoBehaviour
    {
        [SerializeField] private string descKeyName;
        [SerializeField] private TMP_Text myText;
        [SerializeField] private Text myText2;

        private void Awake()
        {
            if (!System.Enum.TryParse<LocalizeDescKeys>(descKeyName, out var key) || key == LocalizeDescKeys.None)
                return;

            var localized = StaticData.Wrapper.localizeddescDict[key].StringToLocal;
            if (myText  != null) myText.text  = localized;
            if (myText2 != null) myText2.text = localized;
        }
    }

}
