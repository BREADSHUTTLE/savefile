using UnityEngine;
using System.Collections;
using JaykayFramework;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;
using TangentFramework;
using System.Collections.Generic;

public class ListItemRanking : ListItem
{

    #region Enum
    public enum RankingType
    {
        Total,
        Friends,
        PrevPage,
        NextPage
    }

    RankingType m_type;
    #endregion

    #region UI Property
    public Image sp_img_BG { get; set; }
    public Image sp_img_RankingMedal { get; set; }
    public Text sp_txt_RankingNumber { get; set; }
    public Image sp_img_CharIcon { get; set; }
    public GameObject sp_obj_KaKaoImg { get; set; }
    public GameObject sp_obj_FaceBookImg { get; set; }
    public Text sp_txt_NickName { get; set; }
    public Text sp_txt_Score { get; set; }
    public Text sp_txt_Level { get; set; }
    public Image sp_img_Runner01 { get; set; }
    public Image sp_img_Runner02 { get; set; }
    public Image sp_img_Runner03 { get; set; }
    public GameObject sp_obj_RunnerBg { get; set; }

    public GameObject sp_obj_PrevPage { get; set; }
    public GameObject sp_obj_NextPage { get; set; }

    public Text sp_txt_PrevPageText { get; set; }
    public Text sp_txt_NextPageText { get; set; }

    #endregion

    #region Member variables
    PlayerData.UserFriend FriendsData;
     public int index;
    List<Image> m_RunnerIconList = new List<Image>();
    #endregion

    #region CommonFunction

    public override void init()
    {
        m_RunnerIconList.Add(sp_img_Runner01);
        m_RunnerIconList.Add(sp_img_Runner02);
        m_RunnerIconList.Add(sp_img_Runner03);
    }

    public override void OnListItemActivated()
    {
        SetInfo(m_type,index, FriendsData);
    }

    public override void OnToggleValueChangedEvent(GameObject go, bool state)
    {
        CLog.Log(go.name + " / " + state);
    }

    public override void OnBtnClickEvent(GameObject go)
    {
        base.OnBtnClickEvent(go);
        switch (go.name)
        {
            case "sp_img_BG":
                StageMgr.instance.OpenPopup("StageRankingRunnerDetailInfo", UTIL.Hash("Info", FriendsData,"RankType", m_type), (string str) => { });
                break;

            case "sp_obj_PrevPageBtn":
                StageAdventureRunLobby.Instance.SetPageNumber(StageAdventureRunLobby.Instance.GetPageNumber() - 1);
                StageAdventureRunLobby.Instance.RefreshRankingTab(StageAdventureRunLobby.Instance.sp_obj_TotalRankingOn.activeSelf ? "TotalTab" : "FriendRankingTab");
                break;

            case "sp_obj_NextPageBtn":
                StageAdventureRunLobby.Instance.SetPageNumber(StageAdventureRunLobby.Instance.GetPageNumber() + 1);
                StageAdventureRunLobby.Instance.RefreshRankingTab(StageAdventureRunLobby.Instance.sp_obj_TotalRankingOn.activeSelf ? "TotalTab" : "FriendRankingTab");
                break;
        }
    }

    #endregion

    #region Function

    public void SetInfo(RankingType type, int indexx, PlayerData.UserFriend friendsData = null )
    {
        if (indexx != -1)
            index = indexx;
        m_type = type;

        if (m_type == RankingType.PrevPage)
        {
            if (sp_obj_PrevPage != null)
            {
                sp_obj_PrevPage.SetActive(true);
                sp_obj_NextPage.SetActive(false);
                sp_txt_PrevPageText.text = LocalizationMgr.GetMessage(10961);
            }

            if (sp_img_BG != null)
                sp_img_BG.gameObject.SetActive(false);
        }
        else if (m_type == RankingType.NextPage)
        {
            if (sp_obj_NextPage != null)
            {
                sp_obj_PrevPage.SetActive(false);
                sp_obj_NextPage.SetActive(true);
                sp_txt_NextPageText.text = LocalizationMgr.GetMessage(10962);
            }

            if (sp_img_BG != null)
                sp_img_BG.gameObject.SetActive(false);
        }
        else
        {
            if (sp_obj_PrevPage != null)
            {
                sp_obj_PrevPage.SetActive(false);
                sp_obj_NextPage.SetActive(false);
            }

            FriendsData = friendsData;

            if (sp_txt_Score != null)
            {
                if (sp_img_BG != null)
                    sp_img_BG.gameObject.SetActive(true);

                sp_txt_Score.text = string.Format("{0:#,##0}", FriendsData.BestScroe);

                byte[] date;

                if (FriendsData.UserId == PlayerData.instance.m_UserId)
                    date = Utils.ConvertToByteList(StringConfigTableMgr.instance.GetStringConfigData("MyRankingBGColor"));
                else
                    date = Utils.ConvertToByteList(StringConfigTableMgr.instance.GetStringConfigData("BasicRankingBGColor"));

                sp_img_BG.color = new Color32(date[0], date[1], date[2], date[3]);


                if (FriendsData.BestScroe == 0 && FriendsData.Rank != 0)
                {
                    sp_img_RankingMedal.gameObject.SetActive(false);
                    sp_txt_RankingNumber.gameObject.SetActive(true);

                    if (m_type == RankingType.Total)
                    {
                        sp_txt_RankingNumber.text = LocalizationMgr.GetMessage(10934, PlayerData.instance.GetTotalRankList().Count);

                    }
                    else
                    {
                        sp_txt_RankingNumber.text = LocalizationMgr.GetMessage(10934, PlayerData.instance.GetFriendRankList().Count);
                    }
                }
                else
                {
                    switch (FriendsData.Rank)
                    {
                        case 0:
                            sp_img_RankingMedal.gameObject.SetActive(false);
                            sp_txt_RankingNumber.gameObject.SetActive(false);
                            break;
                        case 1:
                            sp_txt_RankingNumber.gameObject.SetActive(false);
                            sp_img_RankingMedal.gameObject.SetActive(true);
                            sp_img_RankingMedal.sprite = DataMgr.instance.GetSprite(StringConfigTableMgr.instance.GetStringConfigData("RankingMedalGold"));
                            break;
                        case 2:
                            sp_txt_RankingNumber.gameObject.SetActive(false);
                            sp_img_RankingMedal.gameObject.SetActive(true);
                            sp_img_RankingMedal.sprite = DataMgr.instance.GetSprite(StringConfigTableMgr.instance.GetStringConfigData("RankingMedalSilver"));
                            break;
                        case 3:
                            sp_txt_RankingNumber.gameObject.SetActive(false);
                            sp_img_RankingMedal.gameObject.SetActive(true);
                            sp_img_RankingMedal.sprite = DataMgr.instance.GetSprite(StringConfigTableMgr.instance.GetStringConfigData("RankingMedalBronze"));
                            break;
                        default:
                            sp_img_RankingMedal.gameObject.SetActive(false);
                            sp_txt_RankingNumber.gameObject.SetActive(true);
                            sp_txt_RankingNumber.text = LocalizationMgr.GetMessage(10934, FriendsData.Rank);
                            break;

                    }
                }

                if (GameDefine.BuildType == GameDefine.eBuildType.Korea_Kakao)
                {
                    sp_obj_FaceBookImg.SetActive(false);

                    if (FriendsData.UserId == PlayerData.instance.m_UserId)
                        sp_obj_KaKaoImg.SetActive(true);
                    else
                        sp_obj_KaKaoImg.SetActive(!(PlayerData.instance.GetPlatformFriendList().Find(item => item.UserId == FriendsData.UserId) == null));
                }
                else if (GameDefine.bUseFaceBook && NativeHelperContainer.facebookHelper.CheckFaceBookLogin())
                {
                    //페이스북 이미지 On
                    sp_obj_KaKaoImg.SetActive(false);
                    sp_obj_FaceBookImg.SetActive(!(PlayerData.instance.GetPlatformFriendList().Find(item => item.UserId == FriendsData.UserId) == null));
                }
                else
                {
                    sp_obj_FaceBookImg.SetActive(false);
                    sp_obj_KaKaoImg.SetActive(false);
                }

                sp_txt_NickName.text = FriendsData.NickName;
                sp_txt_Level.text = LocalizationMgr.GetMessage(10102, FriendsData.PlayerLevel);

                int CharacterId = PlayerData.instance.CheckCharacterIcon(FriendsData.ItemId);
                sp_img_CharIcon.sprite = DataMgr.instance.GetSprite(ItemTableMgr.instance.GetItemData(CharacterId).Icon + "nr");
                SetRelayCharacter();
            }
        }
     
    }


    void SetRelayCharacter()
    {
        //m_RunnerIconList

        if (FriendsData != null && FriendsData.RunningCharacterIndex != null)
        {
            sp_obj_RunnerBg.SetActive(true);
            for (int i = 0; i < FriendsData.RunningCharacterIndex.Length; i++)
            {
                int CharacterId = PlayerData.instance.CheckCharacterIcon(FriendsData.RunningCharacterIndex[i]);
                ItemInfo CharItemInfo = ItemTableMgr.instance.GetItemData(CharacterId);

                if (m_RunnerIconList.Count > i)
                {
                    m_RunnerIconList[i].gameObject.SetActive(true);

                    if (CharItemInfo != null)
                        m_RunnerIconList[i].sprite = DataMgr.instance.GetSprite(CharItemInfo.Icon + "nr");
                    else
                    {
                        //유저가 업데이트 받지않아서 캐릭터가 정보에 없을경우 / 핀으로 고정.
                        CharItemInfo = ItemTableMgr.instance.GetItemData(101);
                        m_RunnerIconList[i].sprite = DataMgr.instance.GetSprite(CharItemInfo.Icon + "dis");
                    }
                }

            }


            for (int i = FriendsData.RunningCharacterIndex.Length; i < m_RunnerIconList.Count; i++)
            {
                m_RunnerIconList[i].gameObject.SetActive(false);
            }
        }
        else
            sp_obj_RunnerBg.SetActive(false);
    }

    //public int GetIndex()
    //{
    //    return index;
    //}


    #endregion







}
