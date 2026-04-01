using CAPYBARA.Bundles;
using DG.Tweening;
using System;
using CAPYBARA;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarEquipSlot : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] private Image imgAvatar;
    
    [Header("Select")]
    [SerializeField] private GameObject[] objSelectOn;
    [SerializeField] private GameObject[] objSelectOff;

    [SerializeField] private TMP_Text txtRemainTime;
    [SerializeField] private TMP_Text txtAvatarName;
    [SerializeField] private GameObject objAvatarName;
    [SerializeField] private CPButton btnEquip;

    public Action<AvatarEquipSlot> onClickEquip;
    public bool hideNameOnDeselect;
    public bool skipSiblingReorder;

    private static readonly Color ColorSelected = Color.white;
    private static readonly Color ColorUnselected = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    private DOTweenAnimation[] dotweenAnims;
    
    // 아바타 정보
    public string AvatarId { get; private set; }
    public int DurationSeconds { get; private set; }

    private void Awake()
    {
        btnEquip.onClick.AddListener(() => onClickEquip?.Invoke(this));
        
        dotweenAnims = GetComponentsInChildren<DOTweenAnimation>(true);
        foreach (var anim in dotweenAnims)
            anim.CreateTween();
        
        SetEquip(false);
    }

    public void SetAvatar(Sprite avatarSprite, string avatarId = null, int durationSeconds = 0, string avatarName = null, Vector2 offset = default)
    {
        AvatarId = avatarId;
        DurationSeconds = durationSeconds;
        hideNameOnDeselect = false;
        
        const float scale = 0.45f;
        
        imgAvatar.sprite = avatarSprite;
        imgAvatar.SetNativeSize();
        imgAvatar.transform.localScale = new Vector3(scale, scale, scale);
        imgAvatar.rectTransform.anchoredPosition = offset;
        
        SetAvatarName(avatarName);
        UpdateRemainTimeText();
    }
    
    public void SetAvatarName(string name)
    {
        if (txtAvatarName != null)
            txtAvatarName.text = name ?? "";
    }
    
    private void UpdateRemainTimeText()
    {
        if (txtRemainTime == null)
            return;
        
        if (DurationSeconds <= 0)
        {
            txtRemainTime.gameObject.SetActive(true);
            txtRemainTime.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PermanentOwnership].StringToLocal;
            return;
        }
        
        txtRemainTime.gameObject.SetActive(true);
        
        int remainDays = DurationSeconds / 86400; // 1일 = 86400초
        int remainHours = (DurationSeconds % 86400) / 3600;
        
        if (remainDays > 0)
        {
            txtRemainTime.text = $"{remainDays}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.DaysRemaining].StringToLocal}";
        }
        else if (remainHours > 0)
        {
            txtRemainTime.text = $"{remainHours}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.HoursRemaining].StringToLocal}";
        }
        else
        {
            int remainMinutes = (DurationSeconds % 3600) / 60;
            txtRemainTime.text = remainMinutes > 0 ? $"{remainMinutes}분 남음" : StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ExpiringSoon].StringToLocal;
        }
    }

    public void SetEquip(bool isEquip)
    {
        imgAvatar.color = isEquip ? ColorSelected : ColorUnselected;

        if (objSelectOn != null && objSelectOn.Length > 0)
        {
            for (int i = 0; i < objSelectOn.Length; i++)
                objSelectOn[i].SetActive(isEquip);
        }

        if (objSelectOff != null && objSelectOff.Length > 0)
        {
            for (int i = 0; i < objSelectOff.Length; i++)
                objSelectOff[i].SetActive(!isEquip);
        }

        if (hideNameOnDeselect)
        {
            if (objAvatarName != null)
                objAvatarName.SetActive(isEquip);
            if (txtRemainTime != null)
                txtRemainTime.gameObject.SetActive(isEquip);
        }

        if (!skipSiblingReorder)
        {
            if (isEquip)
                transform.SetAsLastSibling();
            else
                transform.SetAsFirstSibling();
        }
    }
}
