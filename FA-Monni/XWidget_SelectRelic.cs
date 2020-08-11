using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class XWidget_SelectRelic : MonoBehaviour
{
    private bool m_isSelect = false;
    private bool m_isEnable = true;
    private bool m_isLock = false;

    public XUISound sound = null;

    public UI2DSprite sprSkillIcon = null;
    public GameObject goLock = null;
    public GameObject goHighlight = null;
    public GameObject goCheckMark = null;
    public GameObject goDisable = null;

    private Action<int> m_onClick = null;
    private int m_relicID;

    void Awake()
    {
        if (goCheckMark != null)
            goCheckMark.SetActive(false);

        if (goHighlight != null)
            goHighlight.SetActive(false);

        if (goDisable != null)
            goDisable.SetActive(false);

        m_isSelect = false;
        m_isEnable = true;
        m_isLock = false;
    }

    public void Build(int selectRelicID, int relicID, Action<int> onClick)
    {
        m_onClick = onClick;

        m_relicID = relicID;
        BuildLock(relicID);
        BuildSkillInfo(relicID);

        if (m_isLock || !m_isEnable)
            SetNullRelic();

        RefreshSelectRelic(selectRelicID);
    }

    private void BuildSkillInfo(int relicID)
    {
        m_isEnable = true;
        ShowDisable(false);

        // 수정 필요
        List<CS.RelicInfo> relicData;
        if (!XGlobal.proto.Relic.TryGetValue(relicID, out relicData))
        {
            m_isEnable = false;
            return;
        }

        string strIconName = relicData[0].m_icon;   // 수정필요
        if (strIconName.Length > 0)
        {
            sprSkillIcon.gameObject.SetActive(true);
            sprSkillIcon.sprite2D = XGlobal.spriteLoader.LoadInPool(strIconName);
        }
    }

    private void BuildLock(int relicID)
    {
        if (XGlobal.MyInfo.Level < GetOpenRelicLevel())
            m_isLock = true;
        else
            m_isLock = relicID == -1;
        goLock.SetActive(m_isLock);
    }

    public void RefreshSelectRelic(int selectRelicID)
    {
        //if (XGlobal.MyInfo.Level < GetOpenRelicLevel())
        //    m_isSelect = true;
        //else
        if (selectRelicID == -1)
            m_isSelect = false;
        else
            m_isSelect = selectRelicID == m_relicID;
        ShowCheckMark(m_isSelect);
    }

    private int GetOpenRelicLevel()
    {
        var LockLevel = 0;
        if (XGlobal.proto.gameConst.TryGetValue(CS.Constant.key.open_relic_page, out LockLevel) == false)
            CS.CSLog.Error("not gameConst key || open relic level");

        return LockLevel;
    }

    private void SetNullRelic()
    {
        sprSkillIcon.gameObject.SetActive(false);
        ShowDisable(true);
        ShowCheckMark(false);
    }

    public void OnClick()
    {
        if (m_isEnable == false)
            return;

        m_isSelect = !m_isSelect;
        SetSound(m_isSelect, m_isEnable);

        if (m_onClick != null)
            m_onClick(m_relicID);
    }

    public void ShowCheckMark(bool isShow)
    {
        if (goCheckMark != null)
            goCheckMark.SetActive(isShow);

        if (goHighlight != null)
            goHighlight.SetActive(isShow);
    }

    public void ShowDisable(bool isShow)
    {
        if (goDisable != null)
            goDisable.SetActive(isShow);
    }

    private void SetSound(bool isChecked, bool isEnable)
    {
        if (sound == null)
            return;

        if (!isEnable)
            sound.type = XUISound.UISound.None;
        else if (isChecked)
            sound.type = XUISound.UISound.UIButtonTouch;
        else
            sound.type = XUISound.UISound.UIButtonTouchFail;
    }
}
