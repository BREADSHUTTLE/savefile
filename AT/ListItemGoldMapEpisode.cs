using UnityEngine;
using System.Collections;
using JaykayFramework;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;

public class ListItemGoldMapEpisode : ListItem
{

    #region Enum
    #endregion

    #region UI Property

    public Text sp_txt_EpisodeNumber { get; set; }
    public Text sp_txt_EpisodeName { get; set; }

    public GameObject sp_obj_Lock { get; set; }
    public Text sp_txt_OpenInfo { get; set; }
    public Text sp_txt_CurrentCount { get; set; }

    public Image sp_img_ArrowcharIcon { get; set; }
    public GameObject sp_obj_ArrowObj { get; set; }
    public GameObject sp_obj_ArrowBest { get; set; }

    //Episord Image.
    public GameObject sp_obj_EpisordGold_1 { get; set; }
    public GameObject sp_obj_GlowEffect { get; set; }

    #endregion

    #region Member variables

    EpisodeInfo m_ItemInfo;

    List<GameObject> m_EpisordList = new List<GameObject>();

    List<GameObject> m_OnMapPoint = new List<GameObject>();
    List<Image> m_MainCharacterIcon = new List<Image>();

    #endregion

    #region CommonFunction

    public override void init()
    {
        m_EpisordList.Add(sp_obj_EpisordGold_1);

        InfoInit();

    }

    public override void OnListItemActivated()
    {

    }

    public override void OnBtnClickEvent(GameObject go)
    {
        base.OnBtnClickEvent(go);
        switch (go.name)
        {
            case "Image_ListItemGoldMap":

                if (sp_obj_Lock.activeSelf)
                    return;

                PlayerData.instance.CurEpisodeID = m_ItemInfo.Id;
                StageMgr.instance.CloseCurPopup(true, false);

                if (!StageEpisodeMapAdv.returnStageAdventureRun)
                {
                    if (StageMgr.instance.CurPopupName != "StageAdventureRunLobby")
                    {
                        StageMgr.instance.OpenPopup("StageAdventureRunLobby", null, (string str) => { });
                    }
                }
                break;

  
        }
    }


    #endregion

    #region Function


    public void Set(EpisodeInfo info)
    {
        m_ItemInfo = info;
        gameObject.SetActive(true);
        InfoInit();
    }

    public void Clear()
    {
        gameObject.SetActive(false);
    }

    public int GetMapId()
    {
        return m_ItemInfo.Id;
    }

    public void PercentQuastController()
    {
        int count = QuestTableMgr.instance.GetQuestClearCountByAchieveEpisode(m_ItemInfo.Id);

        //for(int i = 0; i < m_StageToggleList.Count; i++)
        //{
        //    if(i < count)
        //        m_StageToggleList[i].isOn = true;
        //    else
        //        m_StageToggleList[i].isOn = false;

        //}


    }

    void SetEpisodeMapOn(int EpisodeId)
    {
        for (int i = 0; i < m_EpisordList.Count; i++)
        {
            if (EpisodeId - 1 == i)
                m_EpisordList[i].gameObject.SetActive(true);
            else
                m_EpisordList[i].gameObject.SetActive(false);
        }
    }

    void InfoInit()
    {
        if (sp_txt_EpisodeNumber != null)
        {
            string str = LocalizationMgr.GetMessage(10517);
            sp_txt_EpisodeNumber.text = str;
            sp_txt_EpisodeName.text = m_ItemInfo.Name;


            SetEpisodeMapOn(m_ItemInfo.Id - (EpisodeTableMgr.instance.GetGoldMapStartID() - 1));

            PercentQuastController();
            SetLock();
            if (m_OnMapPoint.Count <= 0)
                GetPoint();
            SetCharIcon();
            OnMapPoint();

        }

    }


    void SetLock()
    {

        if (m_ItemInfo.Id <= 1)
        {
            sp_obj_Lock.SetActive(false);
            sp_obj_GlowEffect.SetActive(false);
            return;
        }
        EpisodeInfo info = EpisodeTableMgr.instance.GetEpisodeInfo(m_ItemInfo.Id - (EpisodeTableMgr.instance.GetGoldMapStartID() -1));

        int episordquestcount = info.AchievementMaxCount;
        int clearcount = QuestTableMgr.instance.GetQuestClearCountByEpisode(info.Id);
        bool EpisodeOpen = info.OpenCondition.Length <= 0 ? false : true;

        for (int i = 0; i < info.OpenCondition.Length; i++)
        {
            QuestInfo QuestInfo = QuestTableMgr.instance.GetQuestInfo(info.OpenCondition[i]);

            if (QuestInfo != null)
            {
                PlayerData.UserQuest Info = PlayerData.instance.GetQuestData(QuestInfo.Id);
                if (Info == null || Info.Value < QuestInfo.Postcondition.Value)
                {
                    EpisodeOpen = false;
                    break;
                }
            }

        }

        if (episordquestcount > clearcount && !EpisodeOpen)
        {

            sp_obj_Lock.SetActive(true);
            sp_obj_GlowEffect.SetActive(false);
            sp_txt_OpenInfo.text = LocalizationMgr.GetMessage(10706, info.Id, episordquestcount);
            sp_txt_OpenInfo.text += "\n" + LocalizationMgr.GetMessage(10708, info.Id);

            sp_txt_CurrentCount.text = LocalizationMgr.GetMessage(10707, clearcount);
        }
        else
        {
            sp_obj_Lock.SetActive(false);
            sp_obj_GlowEffect.SetActive(true);

        }
    }

  
    void GetPoint()
    {
        int mapid = m_ItemInfo.Id - (EpisodeTableMgr.instance.GetGoldMapStartID());


        Transform childs = m_EpisordList[mapid].transform.FindChild("MapLine/Map_road_line2/MapPoint");


        foreach (Transform trans in childs)
        {
            if (trans.gameObject.name.Contains("Large_Points"))
            {
                m_OnMapPoint.Add(trans.FindChild("Image_LPoint_on").gameObject);
                m_MainCharacterIcon.Add(trans.FindChild("Image_LPoint_on/Icon_CharHead/Image_Char").GetComponent<Image>());
            }
            else
            {
                m_OnMapPoint.Add(trans.FindChild("On").gameObject);
            }
        }
        sp_obj_ArrowBest.transform.SetParent(childs);
        sp_obj_ArrowObj.transform.SetParent(childs);

    }

    void SetCharIcon()
    {
        int charid = PlayerData.instance.GetCurCharacterID();
        ItemInfo info = ItemTableMgr.instance.GetItemData(charid);
        string iconname = "";

        switch (info.OriginalCharacterName)
        {
            case ItemTableMgr.OriginalCharacter.Finn:
                iconname = StringConfigTableMgr.instance.GetStringConfigData("EpisordCharIconFinn");
                break;
            case ItemTableMgr.OriginalCharacter.Jake:
                iconname = StringConfigTableMgr.instance.GetStringConfigData("EpisordCharIconJake");
                break;
            case ItemTableMgr.OriginalCharacter.burble:
                iconname = StringConfigTableMgr.instance.GetStringConfigData("EpisordCharIconBubble");
                break; //
            case ItemTableMgr.OriginalCharacter.Marceline:
                iconname = StringConfigTableMgr.instance.GetStringConfigData("EpisordCharIconMarceline");
                break;

            case ItemTableMgr.OriginalCharacter.flameprincess:
                iconname = StringConfigTableMgr.instance.GetStringConfigData("EpisordCharIconFlame");
                break;

            default:
                iconname = StringConfigTableMgr.instance.GetStringConfigData("EpisordCharIconFinn");
                break;
        }

        sp_img_ArrowcharIcon.sprite = DataMgr.instance.GetSprite(iconname);

        for (int i = 0; i < m_MainCharacterIcon.Count; i++)
        {
            m_MainCharacterIcon[i].sprite = DataMgr.instance.GetSprite(iconname);
        }
    }

    void OnMapPoint()
    {
        int Activecount = 0;
        string[] mapname = EpisodeTableMgr.instance.GetEpisodeMapNames(m_ItemInfo.Id);
        float mapDistance = 0;

        for (int i = 0; i < mapname.Length - 1; i++)
        {
            mapDistance += PlatformPoolManager.GetPlatformDistance(mapname[i]);
        }

        PlayerData.EpisodeBestScore distance = PlayerData.instance.GetMyEpisodeDistance(m_ItemInfo.Id);

        float OnLengh = 0;
        float Lengh = 0;
        float bestLengh = 0;
        bool posset = false;

        Vector3 objpos = new Vector3();
        Vector3 Bestpos = new Vector3();

        if (distance != null)
        {
            Lengh = distance.LastDistance;
            bestLengh = m_OnMapPoint.Count < distance.LongestDistance ? m_OnMapPoint.Count - 1 : distance.LongestDistance;
        }

        List<QuestInfo> EpisodeClearQuest = QuestTableMgr.instance.GetQuestListByEpisode(m_ItemInfo.Id);

        for (int i = 0; i < EpisodeClearQuest.Count; i++)
        {
            if (EpisodeClearQuest[i].Postcondition.Type == QuestTableMgr.PostconditionType.Achieve_Episode && EpisodeClearQuest[i].Postcondition.EpisodeId == m_ItemInfo.Id)
            {
                PlayerData.UserQuest Info = PlayerData.instance.GetQuestData(EpisodeClearQuest[i].Id);
                if (Info != null && Info.Value < EpisodeClearQuest[i].Postcondition.Value && EpisodeClearQuest[i].Postcondition.Value <= 1) // 첫번째 퀘스트만 해당.
                {
                    bestLengh = m_OnMapPoint.Count - 1 == bestLengh ? bestLengh - 1 : bestLengh;
                    break;
                }
            }
        }

        for (int i = 0; i < m_OnMapPoint.Count; i++)
        {
            OnLengh += mapDistance / m_OnMapPoint.Count;

            //if(OnLengh >= Lengh && posset == false)
            //{
            //    objpos = m_OnMapPoint[i].transform.parent.transform.localPosition;
            //    objpos.y += 50;
            //    sp_obj_ArrowObj.transform.localPosition = objpos;  
            //    posset = true;
            //}
            m_OnMapPoint[i].SetActive(true);
            if (i >= bestLengh)
            {
                Activecount = i;

                Bestpos = m_OnMapPoint[i].transform.parent.transform.localPosition;
                objpos = m_OnMapPoint[i].transform.parent.transform.localPosition;

                int plusY = 50;
                if (i >= m_OnMapPoint.Count - 1)
                    plusY = 20;
                Bestpos.y += (plusY + 35);
                objpos.y += plusY;

                sp_obj_ArrowBest.transform.localPosition = Bestpos;
                sp_obj_ArrowObj.transform.localPosition = objpos;

                break;

            }



        }



        for (int i = Activecount + 1; i < m_OnMapPoint.Count; i++)
        {
            m_OnMapPoint[i].SetActive(false);
        }
    }

    #endregion



}
