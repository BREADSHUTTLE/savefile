using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System;
using I2.Loc;

public class XPanel_WaitingMatch : MonoBehaviour, IUIBackbutton
{
    public UILabel lblEstimated;
    public UILabel lblElapsed;
    public UILabel lblTitle;

    private Stopwatch m_stopWatch = new Stopwatch();
    private Action m_onCancle;

    public void Show(string title, int avgWaitTime, Action onCancle)
    {
        Reset();
        gameObject.SetActive(true);
        lblTitle.text = title;
        m_stopWatch.Start();
        lblEstimated.text = string.Format("{0}: {1}", ScriptLocalization.Get("Glossary/EstimatedTime"), ToMinSecString(avgWaitTime));
        m_onCancle = onCancle;
        StartCoroutine(RefreshTime());
    }

    private string ToMinSecString(int sec)
    {
        return string.Format("{0}:{1}", (sec / 60).ToString("00"), (sec % 60).ToString("00"));
    }

    IEnumerator RefreshTime()
    {
        while(true)
        {
            lblElapsed.text = string.Format("{0}: {1}", ScriptLocalization.Get("Glossary/CurrentProgress"), ToMinSecString((int)(m_stopWatch.ElapsedMilliseconds / 1000 % 60)));
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void Reset()
    {
        m_onCancle = null;
        StopAllCoroutines();
        m_stopWatch.Reset();
    }

    public void Hide()
    {
        Reset();
        gameObject.SetActive(false);
    }

    public void TimeOutCancle()
    {
        var title = ScriptLocalization.Get("Code/UI_DUEL_MATCH_TIME_OUT_POPUP_TITLE");
        var msg = ScriptLocalization.Get("Code/UI_DUEL_MATCH_TIME_OUT_POPUP");
        XGlobal.ui.common.ShowFlatMessageBox(title, msg, XMessageBoxButtons.OK);
    }

    public void OnClickCancle()
    {
        if (m_onCancle != null)
        {
            m_onCancle();
            return;
        }
            
        Hide();
    }

    public bool IsActive()
    {
        return this.gameObject.activeSelf;
    }

    public void OnHide()
    {
        OnClickCancle();
    }
}
