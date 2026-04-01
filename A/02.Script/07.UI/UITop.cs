using System.Collections;
using CAPYBARA;
using CAPYBARA.Bundles;
using TMPro;
using UnityEngine;

public class UITop : MonoBehaviour
{
    [SerializeField] private TMP_Text myMoneyText;
    [SerializeField] private CPButton vaultOpen;
    [SerializeField] private float chipAnimationDuration = 0.6f;

    private bool _vaultListenerAdded;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        CPPlayer.Balance.MyBalTextAnimEvent += OnMyBalanceAnim;
        CPPlayer.OutGame.callbackAfterGetMoneyAndBox += RefreshMyMoney;
        RefreshMyMoney();
    }

    private void OnDisable()
    {
        CPPlayer.Balance.MyBalTextAnimEvent -= OnMyBalanceAnim;
        CPPlayer.OutGame.callbackAfterGetMoneyAndBox -= RefreshMyMoney;
    }

    private void OnDestroy()
    {
        if (vaultOpen != null && _vaultListenerAdded)
        {
            vaultOpen.onClick.RemoveListener(OpenVault);
            _vaultListenerAdded = false;
        }
    }

    private void ResolveReferences()
    {
        if (myMoneyText == null)
        {
            var currentGold = transform.Find("SafeArea/CurrentGold");
            if (currentGold != null)
            {
                myMoneyText = currentGold.GetComponent<TMP_Text>();
            }

            if (myMoneyText == null)
            {
                myMoneyText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (vaultOpen == null)
        {
            var vaultButton = transform.Find("SafeArea/VaultBtn");
            if (vaultButton != null)
            {
                vaultOpen = vaultButton.GetComponent<CPButton>();
            }

            if (vaultOpen == null)
            {
                vaultOpen = GetComponentInChildren<CPButton>(true);
            }
        }
    }

    private void BindButtons()
    {
        if (vaultOpen != null && !_vaultListenerAdded)
        {
            vaultOpen.onClick.AddListener(OpenVault);
            _vaultListenerAdded = true;
        }
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf == visible)
        {
            return;
        }

        gameObject.SetActive(visible);
        if (visible)
        {
            RefreshMyMoney();
        }
    }

    private void OpenVault()
    {
        CPPlayer.OutGame.openVaultUI?.Invoke();
    }

    private void RefreshMyMoney()
    {
        if (myMoneyText == null)
        {
            return;
        }

        if (CPPlayer.UserInfo.userDatabase == null || CPPlayer.UserInfo.userDatabase.User == null)
        {
            return;
        }

        var myMoney = CPPlayer.UserInfo.userDatabase.User.Gold;
        myMoneyText.text = Extension.ToKoreanFormat(myMoney, Extension.KoreanFormatMode.Planning);
    }

    private void OnMyBalanceAnim(long startValue, long endValue)
    {
        if (myMoneyText == null)
        {
            return;
        }

        MoneyAnimation(myMoneyText, startValue, endValue, chipAnimationDuration);
    }

    public void MoneyAnimation(TMP_Text label, long startValue, long endValue, float duration)
    {
        StartCoroutine(AnimateChipText(label, startValue, endValue, duration));
    }

    private IEnumerator AnimateChipText(TMP_Text label, long startValue, long endValue, float duration)
    {
        if (label == null)
        {
            yield break;
        }

        if (startValue == endValue)
        {
            label.text = Extension.ToKoreanFormat(endValue, Extension.KoreanFormatMode.Planning);
            yield break;
        }

        float elapsed = 0f;
        int targetLength = endValue.ToString().Length;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            long currentValue = (long)Mathf.Lerp(startValue, endValue, t);

            string padded = currentValue.ToString().PadLeft(targetLength, '0');
            label.text = Extension.ToKoreanFormat(long.Parse(padded), Extension.KoreanFormatMode.Planning);

            yield return null;
        }

        label.text = Extension.ToKoreanFormat(endValue, Extension.KoreanFormatMode.Planning);
    }
}
