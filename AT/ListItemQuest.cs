using UnityEngine;
using System.Collections;
using JaykayFramework;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;

public class ListItemQuest : ListItem
{
    public enum ButtonState
    {
        InComplete,
        Progress,
        ButtonReward,
        Complete,
    }

    public Text sp_txt_RewardBtnTitle { get; set; }
    public Text sp_txt_IncompleteBtnTitle { get; set; }
    public Text sp_txt_CompleteBtnTitle { get; set; }

    public Image sp_img_IconQuest { get; set; }
    public Text sp_txt_QuestTitle { get; set;}
    public Text sp_txt_QuestDescription { get; set; }
    public Image sp_img_QuestGuage { get; set; }
    public Text sp_txt_QuestCount { get; set; }
    public Text sp_txt_sp_txt_Reward { get; set; }


    // reward
    public Image sp_img_IconReward1 { get; set; }
    public Text sp_txt_RewardDigit1 { get; set; }
    public Image sp_img_IconReward2 { get; set; }
    public Text sp_txt_RewardDigit2 { get; set; }
    public Image sp_img_IconReward3 { get; set; }
    public Text sp_txt_RewardDigit3 { get; set; }

    // State
    public GameObject sp_obj_Incomplete { get; set; }
    public GameObject sp_obj_Progress { get; set; }
    public GameObject sp_obj_ButtonReward { get; set; }
    public GameObject sp_obj_Complete { get; set; }

    // State Value
    public Text sp_txt_ProgressDigit { get; set; }

    public Text sp_txt_IconText { get; set; }

    private PlayerData.UserQuest itemData;
    private StageQuest.CallBack refreshFunc;
    private PlayerData.QuestType questType;

    bool settingtime = true;

    public override void init()
    {
        LocalInit();
    }

    public override void OnListItemActivated()
    {
        SetItemQuestData();
    }

    public void SyncItemData(PlayerData.UserQuest item, StageQuest.CallBack callback, PlayerData.QuestType type, bool refresh = false)
    {
        itemData = item;
        refreshFunc = callback;
        questType = type;

        if(refresh)
        {
            gameObject.SetActive(true);
            SetItemQuestData();
        }
    }

    void LocalInit()
    {
        sp_txt_sp_txt_Reward.text = LocalizationMgr.GetMessage(10514);
        sp_txt_RewardBtnTitle.text = LocalizationMgr.GetMessage(10505);
        sp_txt_IncompleteBtnTitle.text = LocalizationMgr.GetMessage(10510);
        sp_txt_CompleteBtnTitle.text = LocalizationMgr.GetMessage(10509);
    }

    public void SetItemQuestData()
    {
        const int ITEMCOUNT = 3;
        if(sp_img_IconQuest != null && itemData != null&& transform.parent.gameObject.activeInHierarchy == true)
        {
            QuestInfo info = QuestTableMgr.instance.GetQuestInfo(itemData.QuestId);
            string questicon = (info.Postcondition.IsCumulative == 0 ? "QuestOneTime" : "QuestContinue");
            //sp_img_IconQuest.sprite = DataMgr.instance.GetSprite("icon_unknown_nr");
            sp_img_IconQuest.sprite = DataMgr.instance.GetSprite(StringConfigTableMgr.instance.GetStringConfigData(questicon));
            int questNumber =( questicon == "QuestOneTime" ? 10515 : 10516);

            sp_txt_IconText.text = LocalizationMgr.GetMessage(questNumber);
            sp_txt_QuestTitle.text = info.Name;
            sp_txt_QuestDescription.text = info.Description;
                                 
            for(int i = 0; i < ITEMCOUNT; i++)
            {
                if(info.ListQuestReward.Count > i)
                {
                   
                    string IconName = "";
                    if(info.ListQuestReward[i].IsGacha == 1)
                    {
                        GachaInfo GachaItemInfo = GachaTableMgr.instance.GetGachaInfo(info.ListQuestReward[i].ItemId);
                        IconName = GachaItemInfo != null ? GachaItemInfo.Icon : "";
                    }
                    else
                    {
                        ItemInfo iteminfo = ItemTableMgr.instance.GetItemData(info.ListQuestReward[i].ItemId);
                        IconName = iteminfo != null ? iteminfo.Icon : "";
                    }
                   
                    SetRewardUIData(i, IconName, string.Format("X {0}", info.ListQuestReward[i].Number));
                }
                else
                {
                    SetRewardUIData(i, string.Empty, string.Empty);
                }
            }

            long maxvalue = info.Postcondition.Value + info.StartCumulativeValue;
            long achievevalue = itemData.Value + info.StartCumulativeValue;

            long value = (achievevalue >= maxvalue ? maxvalue : achievevalue);
            sp_txt_QuestCount.text = string.Format("{0:#,##0} / {1:#,##0}", value, maxvalue);
            float progress = ((float)achievevalue / (float)maxvalue);
            sp_img_QuestGuage.fillAmount = progress;

            if (itemData.IsTakeReward == 0)
            {
                

                if(progress >= 1)
                {
                    SetProgressStateUI(ButtonState.ButtonReward);
                }
                else
                {
                    if (info.Postcondition.IsCumulative == 0)
                    {
                        SetProgressStateUI(ButtonState.InComplete);
                    }
                    else
                    {
                        SetProgressStateUI(ButtonState.Progress);
                        sp_txt_ProgressDigit.text = string.Format("{0}%", (int)(progress * 100));
                    }
                }
            }
            else
            {
                SetProgressStateUI(ButtonState.Complete);
            }
        }

        settingtime = false;
    }

    void SetProgressStateUI(ButtonState state)
    {
        switch(state)
        {
            case ButtonState.InComplete:
                sp_obj_Incomplete.SetActive(true);
                sp_obj_Progress.SetActive(false);
                sp_obj_ButtonReward.SetActive(false);
                sp_obj_Complete.SetActive(false);
                break;

            case ButtonState.Progress:
                sp_obj_Incomplete.SetActive(false);
                sp_obj_Progress.SetActive(true);
                sp_obj_ButtonReward.SetActive(false);
                sp_obj_Complete.SetActive(false);
                break;

            case ButtonState.ButtonReward:
                sp_obj_Incomplete.SetActive(false);
                sp_obj_Progress.SetActive(false);
                sp_obj_ButtonReward.SetActive(true);
                sp_obj_Complete.SetActive(false);
                break;

            case ButtonState.Complete:
                sp_obj_Incomplete.SetActive(false);
                sp_obj_Progress.SetActive(false);
                sp_obj_ButtonReward.SetActive(false);
                sp_obj_Complete.SetActive(true);
                break;                        
        }    
    }

    public bool GetSettingTime()
    {
        return settingtime;
    }

    public void SetSettingItme(bool isActive)
    {
        settingtime = isActive;
    }

    void SetRewardUIData(int index, string icon, string text)
    {
        switch(index)
        {
            case 0:
                sp_img_IconReward1.transform.parent.gameObject.SetActive(icon != string.Empty);
                if (sp_img_IconReward1.transform.parent.gameObject.activeSelf)
                {
                    sp_img_IconReward1.sprite = DataMgr.instance.GetSprite(icon);
                    sp_txt_RewardDigit1.text = text;
                }
                break;

            case 1:
                sp_img_IconReward2.transform.parent.gameObject.SetActive(icon != string.Empty);
                if (sp_img_IconReward2.transform.parent.gameObject.activeSelf)
                {
                    sp_img_IconReward2.sprite = DataMgr.instance.GetSprite(icon);
                    sp_txt_RewardDigit2.text = text;
                }
                break;

            case 2:
                sp_img_IconReward3.transform.parent.gameObject.SetActive(icon != string.Empty);
                if (sp_img_IconReward3.transform.parent.gameObject.activeSelf)
                {
                    sp_img_IconReward3.sprite = DataMgr.instance.GetSprite(icon);
                    sp_txt_RewardDigit3.text = text;
                }
                break;

            default:
                break;
        }
    }

    public override void OnBtnClickEvent(GameObject go)
    {
        base.OnBtnClickEvent(go);
        switch (go.name)
        {
            case "sp_obj_ButtonReward":
                QuestInfo info = QuestTableMgr.instance.GetQuestInfo(itemData.QuestId);
                int OldLevel = PlayerData.instance.PlayerLevel;

                StageMgr.instance.SetLoadingIndicator(true, StageMgr.LoadingIndicatorLayer.UI);

                if (info.Type == QuestTableMgr.Quest_Type.Weekly)
                    UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.BehaviorCount_WeeklyQuestComplete, 1);
                else if (info.Type == QuestTableMgr.Quest_Type.FixedDaily || info.Type == QuestTableMgr.Quest_Type.RandomDaily)
                    UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.BehaviorCount_DailyQuestComplete, 1);

                int OldEpisodeId = PlayerData.instance.OpenEpisodeCheck();

                PlayerDataMgr.instance.CompleteQuest(info.Type.ToString(), info.Id, (TangentFramework.HttpNetwork.Packet p, bool success) =>
                {

                    if (p.ERROR == 0)
                    {
                        GAHelper.GALogEvent(GAHelper.GAEventCategory.Quest, "GetReward", info.Type.ToString(), info.Id);

                        StageMgr.instance.SetLoadingIndicator(false, StageMgr.LoadingIndicatorLayer.UI);
                        CustomNetwork.QUESTINFO quest = p as CustomNetwork.QUESTINFO;

                        // need to do (팝업 띄우기)
                        StageMgr.instance.OpenPopup("StageQuestGetReward", UTIL.Hash("QuestInfo", info), (string str) =>
                        {
                            if(str == "Yes")
                            {
                                refreshFunc(questType);

                                if (OldLevel < PlayerData.instance.PlayerLevel)
                                {
                                    StageMgr.instance.OpenPopup("StagePlayerLevelUp", null, (string str2) => {
                                        if(str == "No")
                                        {
                                            OpenToastMessage(info, OldEpisodeId);
                                        }
                                    });
                                }
                                else
                                    OpenToastMessage(info, OldEpisodeId);
                            }
                        });

                        if (PlayerPrefs.GetInt("QuestRenewal") == 0 && quest != null && quest.complaterefresh > 0)
                        {
                            StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(50092)), (string str) => { });

                            if (StageQuest.m_RefreshQuestTab != null)
                                StageQuest.m_RefreshQuestTab();
                            else
                                StageMgr.instance.SetCurStage("StageLobby");

                        }
                    }
                });
                break;

            
        }
    }

    public override void OnToggleValueChangedEvent(GameObject go, bool state)
    {
        CLog.Log(go.name + " / " + state);
    }

    void OpenToastMessage(QuestInfo info, int OldEpisodeId)
    {
        int NowUnLockEpisodeId = PlayerData.instance.OpenEpisodeCheck();
        if (info.Type == QuestTableMgr.Quest_Type.Normal && OldEpisodeId != NowUnLockEpisodeId)
        {
            string text = LocalizationMgr.GetMessage(50083, NowUnLockEpisodeId);
            StageMgr.instance.OpenPopup("StageToast", UTIL.Hash("TextInfo", text, "LocalPosition", new Vector3(-2.9f, 65.5f, 0)), (string str) =>
            {
                if (str == "No")
                {
                }
            });
        }
    }

}
