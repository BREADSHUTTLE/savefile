using System;
using System.Collections;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CAPYBARA.Bundles
{
    public class PopupVault : BasePopup
    {
        public enum NumpadKey
        {
            Num1, Num2, Num3,
            Num4, Num5, Num6,
            Num7, Num8, Num9,
            Num0, Num00, Num000
        }

        public enum ControllKey
        {
            Backspace, Clear, Max,
        }
        
        public enum TransactionType
        {
            Deposit,
            Withdraw
        }

        [Serializable]
        public class NumberInfo
        {
            public NumpadKey key;
            public int amount;
            public CPButton btn;
        }

        [Serializable]
        public class ControllInfo
        {
            public ControllKey key;
            public CPButton btn;
        }

        [Space(10)]
        public CPButton btnClose;
        public UISegmentedControlGroup tabGroup;

        [Space(10)]
        public CPButton btnDepositAction;
        public CPButton btnWithdrawAction;

        [Space(10)]
        public TMP_Text txtMyGold;
        public TMP_Text txtVaultGold;
        public TMP_Text txtInput;

        [Space(10)]
        public GameObject[] deposit;
        public Image imageDosit;
        public TMP_Text[] txtDosit;
        public GameObject[] withdraw;
        public Image imageWithdraw;
        public TMP_Text[] txtWithdraw;

        [Space(10)]
        [Header("Numpad")]
        public List<NumberInfo> numpadKeys = new List<NumberInfo>();
        public List<ControllInfo> controllInfos = new List<ControllInfo>();

        protected override void Awake()
        {
            if (closeButton == null)
                closeButton = btnClose;

            base.Awake();
        }

        public void MoneyAnimation(Text label, long startValue, long endValue, float duration)
        {
            if (this.gameObject.activeInHierarchy == false)
                return;

            StartCoroutine(AnimateChipText(label, startValue, endValue, duration));
        }

        public void MoneyAnimation(TMP_Text label, long startValue, long endValue, float duration)
        {
            if (this.gameObject.activeInHierarchy == false)
                return;

            StartCoroutine(AnimateChipTextTMP(label, startValue, endValue, duration));
        }

        private IEnumerator AnimateChipText(Text label, long startValue, long endValue, float duration)
        {
            if (startValue == endValue)
            {
                label.text = Extension.ToKoreanFormat(endValue);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                long currentValue = (long)Mathf.Lerp(startValue, endValue, t);
                label.text = Extension.ToKoreanFormat(currentValue);
                yield return null;
            }

            label.text = Extension.ToKoreanFormat(endValue);
        }

        private IEnumerator AnimateChipTextTMP(TMP_Text label, long startValue, long endValue, float duration)
        {
            if (startValue == endValue)
            {
                label.text = Extension.ToKoreanFormat(endValue);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                long currentValue = (long)Mathf.Lerp(startValue, endValue, t);
                label.text = Extension.ToKoreanFormat(currentValue);
                yield return null;
            }

            label.text = Extension.ToKoreanFormat(endValue);
        }
    }
}
