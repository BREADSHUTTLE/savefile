using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using I2.Loc;

public class XUIQuestItem : MonoBehaviour
{
    public UILabel lblTitle;
    public UI2DSprite sprQuestIcon;
    public UILabel lblQuestTitle;
    public UILabel lblQuestCondition;
    public UILabel lblQuestDesc;
    public UILabel lblClearGroupProgress;

    public XWiget_GridBuilder gridRewards;

    public UIButton btnGo;
    public UIButton btnComplete;
    public UI2DSprite sprBackground;
    public UI2DSprite sprUnShow;
    public UI2DSprite sprDisable;
    public GameObject goCompleteButton;

    public CS.QuestInfo m_QuestItem;
    public List<CS.CSContent> m_rewardsList;

    public string UIQuestRewardFailSoundKey = "UIQuestRewardFail";

    public UIProgressBar progressbar;

    private Action m_hideAction;

    public void Build(CS.QuestInfo QuestItem, Action hideAction)
    {
        var logic = XGlobal.m_questLogic;
        m_hideAction = hideAction;
        m_QuestItem = QuestItem;

        int progressCount, totalCount = 0;
        logic.GetProgressCount(XGlobal.MyInfo, QuestItem.nGroup, QuestItem.nID, QuestItem.nShowOrder, out progressCount, out totalCount);

        if (QuestItem.HasClearGroup)
        {
            lblClearGroupProgress.gameObject.SetActive(true);
            var clearGroup = XGlobal.proto.Quest.GetClearGroup(QuestItem.clearGroupUID);
            lblClearGroupProgress.text = string.Format("({0}/{1})", QuestItem.clearGroupIndex, clearGroup.Count);
        }
        else
        {
            lblClearGroupProgress.gameObject.SetActive(false);
        }

        if (sprQuestIcon != null)
        {
            XNGUIHelper.SetSpriteAspect(sprQuestIcon, XGlobal.spriteLoader.LoadFromAtlas(QuestItem.strIcon, QuestItem.atlasIcon));
        }

        if (lblQuestTitle != null)
        {
            lblQuestTitle.text = QuestItem.strTitle;
        }

        if (lblQuestDesc != null)
        {
            lblQuestDesc.text = string.Format("{0}", QuestItem.strDesc);
        }

        if (progressbar != null)
        {
            //if (QuestItem.nGroup == CS.QuestGroup.events)
            //{
            //    progressbar.gameObject.SetActive(false);
            //}
            //else
            //{
                progressbar.gameObject.SetActive(true);
                progressbar.value = 1.0f;
            //}
        }

        m_rewardsList = QuestItem.RewardContentList;

        var gos = gridRewards.Build<XWidget_RewardIcon>(m_rewardsList.Count);
        var idx = gos.Length - 1;

        for (int i = 0; i < gos.Length; ++i)
        {
            gos[i].Build(m_rewardsList[idx - i], true);
        }


        if (lblQuestCondition != null)
        {
            lblQuestCondition.gameObject.SetActive(true);
        }

        if (sprDisable != null)
            sprDisable.gameObject.SetActive(false);

        var myInfo = XGlobal.MyInfo;
        var ownCharList = XGlobal.MyInfo.m_CharacterInventory.OwnCharIdList();
        var complete = logic.IsCompletable(XGlobal.MyInfo, ownCharList, m_QuestItem.nID, XGlobal.m_Time.ServerDateTime.ToLocalTime());

        SetProgressbar(m_QuestItem, progressCount, totalCount, complete);

        if (XGlobal.m_questLogic.IsClearQuest(myInfo, QuestItem.nID, QuestItem.nGroup))
        {
            SetLinkButton("");
            lblQuestCondition.text = ScriptLocalization.Get("Glossary/Completed");
            sprUnShow.gameObject.SetActive(true);
            progressbar.value = 1.0f;
            goCompleteButton.SetActive(false);
        }
        else
        {
            goCompleteButton.SetActive(complete);
            btnComplete.gameObject.SetActive(complete);
            sprDisable.gameObject.SetActive(!complete);

            if (complete == true)
            {
                SetLinkButton("");
            }
            else
            {
                var questGroup = QuestItem.nGroup == CS.QuestGroup.events || QuestItem.nGroup == CS.QuestGroup.combat;
                SetLinkButton(questGroup ? "" : QuestItem.strLink);
                goCompleteButton.SetActive(questGroup);
            }
            sprUnShow.gameObject.SetActive(false);
        }

        if (lblTitle != null)
            lblTitle.color = XUIHelper.GetTextDisableColor(!complete);
    }

    private void SetProgressbar(CS.QuestInfo questInfo, int progressCount, int totalCount, bool complete)
    {
        if (questInfo.nGroup != CS.QuestGroup.events && !questInfo.strCompleteIf.StartsWith("buff="))
        {
            lblQuestCondition.text = string.Format("{0}/{1}", progressCount.ToString("N0"), totalCount.ToString("N0"));

            if (progressCount >= totalCount)
                lblQuestCondition.text = ScriptLocalization.Get("Glossary/Completed");

            progressbar.value = (float)progressCount / (float)totalCount;
        }
        else
        {
            if (complete)
            {
                lblQuestCondition.text = ScriptLocalization.Get("Glossary/Completed");
                progressbar.value = 1.0f;
            }
            else
            {
                lblQuestCondition.text = string.Format("{0}/{1}", 0, 1);
                progressbar.value = 0.0f;
            }
        }
    }

    private void SetLinkButton(string link)
    {
        if (btnGo != null)
        {
            if (string.IsNullOrEmpty(link))
            {
                btnGo.gameObject.SetActive(false);
                return;
            }

            btnGo.onClick.Clear();
            btnGo.onClick.Add(new EventDelegate ( () =>
            {
                var action = XGlobal.ui.lobby.MoveSceneLink(link);
                if(action != null)
                {
                    OnClick_HideLink();
                    action();                    
                }
            }));

            btnGo.gameObject.SetActive(true);
        }
    }

    private void OnClick_HideLink()
    {
        m_hideAction();
    }

    public void OnClick_Complete()
    {
        var ownCharList = XGlobal.MyInfo.m_CharacterInventory.OwnCharIdList();
        if (XGlobal.m_questLogic.IsCompletable(XGlobal.MyInfo, ownCharList, m_QuestItem.nID, XGlobal.m_Time.ServerDateTime.ToLocalTime()))
        {
            SendMessageUpwards("RequestComplete", m_QuestItem, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void OnClick_RewardFail()
    {
        XGlobal.ui.common.ShowNotification(ScriptLocalization.Get("Code/UI_DAILY_QUEST_ITEM_CANNOT_RECEIVE_ITEM_YET"), 2.0f);
    }
}