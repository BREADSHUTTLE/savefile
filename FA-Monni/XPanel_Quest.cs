using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using I2.Loc;

public class XPanel_Quest : MonoBehaviour 
{
    public XUIQuestList m_daiyQuestList = null;
    public GameObject allClearItem;
    public UIScrollView m_itemScrollView;

    public void Show(bool bShow)
    {
        if (bShow == true)
        {
            m_isRequesting = false;
            gameObject.SetActive(true);
            BuildDailyQuests();
        }
        else
        {
            gameObject.SetActive(false);
        }
        HideComplete();
    }

    public void BuildDailyQuests()
    {
        InitTabsBadge();
        m_daiyQuestList.ReserveRefresh();

        // tab list
        BuildList();
        
        m_daiyQuestList.BuildList();
    }

    #region tab list

    private CS.QuestGroup m_group = CS.QuestGroup.day;
    public XWidget_ToggleTab[] m_Tabs;
    public XUIQuestList m_dailyQuest = null;

    public void BuildList()
    {
        BuildTabList(m_group);
    }

    public void ShowDayGroupList()
    {
        BuildTabList(CS.QuestGroup.day);
    }

    public void ShowWeekGroupList()
    {
        BuildTabList(CS.QuestGroup.week);
    }

    public void ShowEventGroupList()
    {
        BuildTabList(CS.QuestGroup.events);
    }

    public void ShowCombatGroupList()
    {
        BuildTabList(CS.QuestGroup.combat);
    }

    public void ShowAllClearItem(bool bShow)
    {
        allClearItem.gameObject.SetActive(bShow);
        // scroll view panel vector4 = positionX / positionY / sizeX / sizeY
        if (bShow)
        {
            m_itemScrollView.panel.baseClipRegion = new Vector4(
                0f, -63f, 980f, 354f
            );
        }
        else
        {
            m_itemScrollView.panel.baseClipRegion = new Vector4(
                0f, -8f, 980f, 473f
            );
        }
    }

    private void BuildTabList(CS.QuestGroup type)
    {
        m_group = type;

        int typeNumber = 0;

        //if (type > CS.QuestGroup.events)
        //    return;

        if (type == CS.QuestGroup.events || type == CS.QuestGroup.combat)
            ShowAllClearItem(false);
        else
            ShowAllClearItem(true);
        
        typeNumber = ((int)m_group - 1);

        // TODO : 리스트 갱신하는 부분 추가 해야 함.
        m_dailyQuest.Build(type, OnButtonClick_CloseWindow);
        //

        if (m_Tabs != null)
            m_Tabs[typeNumber].OnClickThisTab();

    }

    public void InitTabsBadge()
    {
        for (int i = 0; i < m_Tabs.Count(); i++)
            m_Tabs[i].transform.FindChild("Badge").gameObject.SetActive(false);
    }

    public void SetTabsBadge(int number)
    {
        GameObject go = m_Tabs[number].transform.FindChild("Badge").gameObject;

        if (!go.activeSelf)
            m_Tabs[number].transform.FindChild("Badge").gameObject.SetActive(true);
    }


    #endregion

    #region handler

    public void OnButtonClick_CloseWindow()
    {
        XGlobal.ui.lobby.GoBack();
    }
    #endregion

    #region completePopup
    public XWiget_GridBuilder gridReward;
    public UILabel lblMessage;
    public string soundKey = "UIQuestReward";

    //public void ShowComplete(CS.QuestInfo questInfo)
    //{
    //    XGlobal.ui.lobby.PauseTopBanner(true);
    //    if (questInfo == null)
    //        return;
    //    if (questInfo.RewardContentList.Count < 1)
    //        return;

    //    completePopup.SetActive(true);
    //    XGlobal.sound.Play(soundKey);

    //    string strMsg = I2.Loc.ScriptLocalization.Get("Code/UI_DAILY_QUEST_ITEM_COMPLETED");
    //    lblMessage.text = string.Format(strMsg, questInfo.strTitle);

    //    var lists = gridReward.Build<XWidget_RewardIcon>(questInfo.RewardContentList.Count);
    //    for (int i = 0; i < lists.Length; ++i)
    //    {
    //        lists[i].Build(questInfo.RewardContentList[i], false);
    //    }

    //    BuildDailyQuests();
    //}

    private bool m_isRequesting = false;
    public void RequestComplete(CS.QuestInfo questInfo)
    {
        if (questInfo == null)
        {
            return;
        }

        if (m_isRequesting)
            return;

        m_isRequesting = true;

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_QuestComplete>(
               param: new CS.CSNetParameters {
            { CS.ApiParamKeys.Id, questInfo.nID},},
               callback:
           delegate(CS.ApiReturnCode nErrorCode, string strErrorMessage, JObject json)
           {
               m_isRequesting = false;
               if (nErrorCode == CS.ApiReturnCode.Success)
               {
                   //ShowComplete(questInfo);
                   string strTitle = ScriptLocalization.Get("Code/UI_DAILY_QUEST_ITEM_COMPLETED");

                   if (questInfo.RewardContentList[0].ContentType == CS.CSContentType.gacha)
                   {
                       if (json == null)
                           return;

                       var jObj = json[CS.Netdata_Acquisitions.String];
                       CS.Netdata_Acquisitions acInfo = null;

                       if (jObj != null)
                       {
                           acInfo = jObj.ToObject<CS.Netdata_Acquisitions>();
                           if (acInfo.contents.Count > 0)
                           {
                               XGlobal.ui.lobby.ShowGachaConfirm(questInfo.RewardContentList[0], acInfo.contents[0], acInfo.toMail, string.Format(strTitle, questInfo.strTitle));
                               BuildDailyQuests();
                           }
                       }
                   }
                   else
                   {    
                       XGlobal.ui.common.ShowRewardBox(string.Format(strTitle, questInfo.strTitle), "", XMessageBoxButtons.OK, null, questInfo.RewardContentList.ToArray(), false, true);

                       BuildDailyQuests();
                   }
               }
               else if (nErrorCode == CS.ApiReturnCode.NotEnoughCondition)
               {
                   XGlobal.ui.common.ShowNotification(ScriptLocalization.Get("Code/UI_DAILY_QUEST_ITEM_CANNOT_RECEIVE_ITEM_YET"), 2.0f);
               }

               XGlobal.ui.lobby.RefreshMailbox();
           });
    }

    public void HideComplete()
    {
        XGlobal.ui.common.ShowDisableInput(true);
        XGlobal.ui.lobby.PauseTopBanner(false);
    }
    #endregion

}

