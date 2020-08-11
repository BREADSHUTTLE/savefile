using UnityEngine;
using System.Collections.Generic;

public class XWidget_Button : MonoBehaviour
{
    public UILabel lblTitle;
    public UILabel[] lblDesc;
    public XUISound sound;
    public UI2DSprite sprBg;
    public UI2DSprite sprDisable;
    public UI2DSprite[] sprIcon;

    protected System.Action m_onClick = null;
    protected System.Action m_onDisableClick = null;
    protected bool m_isDisable = false;
    public bool IsDisable { get { return m_isDisable; } }


    private UIWidget[] m_widgets;
    private Color[] m_colors;

    private int widgetDepth = -1000;
    public int WidgetDepth
    {
        get
        {
            if(widgetDepth == -1000)
            {
                var topWidget = GetComponent<UIWidget>();
                if (topWidget != null)
                    widgetDepth = topWidget.depth;
            }
            return widgetDepth;
        }
    }
    

    void Awake()
    {
        m_widgets = GetComponentsInChildren<UIWidget>();
        if (m_widgets != null && m_widgets.Length > 0)
        {
            m_colors = new Color[m_widgets.Length];
            for (int i = 0; i < m_widgets.Length; ++i)
            {
                m_colors[i] = m_widgets[i].color;
            }
        }
    }

    private void InitWidgetColors()
    {
        if (m_widgets != null)
        {
            for (int i = 0; i < m_widgets.Length; ++i)
            {
                m_widgets[i].color = m_colors[i];
            }
        }
    }


    public virtual void Build(string title, System.Action onClick, bool isDisable = false, System.Action onDisableClick = null)
    {
        InitWidgetColors();

        SetTitle(title);
        m_onClick = onClick;
        m_onDisableClick = onDisableClick;
        SetDisable(isDisable);
    }

    public void SetTitle(string title)
    {
        if (lblTitle != null && title != null)
            lblTitle.text = title;
    }

    public void SetDisable(bool disable)
    {
        m_isDisable = disable;
        if (sprDisable != null)
        {
            sprDisable.gameObject.SetActive(disable);
            if (lblTitle != null)
            {
                SetLabelColor(lblTitle, disable);
            }

            if (lblDesc.Length > 0)
            {
                foreach (var each in lblDesc)
                    SetLabelColor(each, disable);
            }

            if (sprIcon.Length > 0)
            {
                foreach (var each in sprIcon)
                    SetIconAlpha(each, disable);
            }

            if (sprBg != null)
                sprBg.gameObject.SetActive(!disable);

            //콜백이 있는 경우는 버튼 눌리는 효과를 그대로 적용하게 하고
            //콜백이 없는 경우는 버튼눌리는 느낌 없도록 수정.
            if (m_onDisableClick == null)
                sprDisable.depth = WidgetDepth + 1;
            else
                sprDisable.depth = WidgetDepth - 1;
        }

        if (sound == null)
            return;

        if (disable)
        {
            sound.type = XUISound.UISound.UIButtonTouchFail;
        }
        else
        {
            sound.type = XUISound.UISound.UIButtonTouch;
        }
    }

    private void SetLabelColor(UILabel lblLabel, bool disable)
    {
        lblLabel.color = disable ? XUIHelper.RGBToColor(141, 141, 141) : Color.white;
        lblLabel.effectColor = disable ? XUIHelper.RGBToColor(32, 34, 42) : Color.black;
    }

    private void SetIconAlpha(UI2DSprite sprIcon, bool disable)
    {
        sprIcon.alpha = disable ? 0.3f : 1f;
    }

    public void Show(bool bShow)
    {
        gameObject.SetActive(bShow);
    }

    protected virtual void OnClick()
    {
        if(m_isDisable)
        {
            if (m_onDisableClick != null)
                m_onDisableClick();
            return;
        }

        if (m_onClick != null)
            m_onClick();
    }
}
