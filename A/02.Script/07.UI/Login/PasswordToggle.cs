using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PasswordToggle : MonoBehaviour
    {
        public Toggle toggle;
        public TMP_InputField input;
        private bool notVisible = false;
        
        private void Awake()
        {
            toggle.onValueChanged.AddListener(TogglePassword);
            toggle.isOn = true;
        }

        public void TogglePassword(bool _notVisible)
        {
            notVisible = _notVisible;

            if (notVisible)
            {
                // 비밀번호 보이기
                input.inputType = TMP_InputField.InputType.Password;
            }
            else
            {
                // 비밀번호 숨기기
                input.inputType = TMP_InputField.InputType.Standard;
            }

            // ⚠ 이거 안 하면 화면 갱신 안 됨
            input.ForceLabelUpdate();
        }
    }

}
