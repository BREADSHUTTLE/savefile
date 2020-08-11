#define UseFB
using UnityEngine;
using System.Collections;
using JaykayFramework;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;

public class ListItemFriend : ListItem
{
  
    public Image sp_img_MainIcon { get; set; }
    public Text sp_txt_Level { get; set; }
    public Text sp_txt_NicName { get; set; }
    public GameObject sp_obj_FaceBookImg { get; set; }

    public GameObject sp_obj_LastLoginBg { get; set; }
    public Text sp_txt_LastLoginTitle { get; set; }
    public Text sp_txt_LastLoginTime { get; set; }
    public GameObject sp_obj_HeartBg { get; set; }
    public Text sp_txt_HeartCount { get; set; }

    public GameObject sp_obj_DelBtn { get; set; }
    public GameObject sp_obj_SendBtn { get; set; }
    public Button sp_btn_InviteBtn { get; set; }
    public GameObject sp_obj_RejectBtn { get; set; }
    public GameObject sp_obj_AcceptBtn { get; set; }


    public Text sp_txt_InviteText { get; set; }
    public Text sp_txt_RejectText { get; set; }
    public Text sp_txt_AcceptText { get; set; }
    public Text sp_txt_SendRefreshTime { get; set; }

    public GameObject sp_obj_InvitePlusBtn { get; set; }
    public Text sp_txt_InvitePlusText { get; set; }
    public GameObject sp_obj_InviteFriendInfo { get; set; }
    public GameObject sp_obj_FaceBookInviteBtn { get; set; }

    public GameObject sp_obj_FaceBookFirst { get; set; }
    public GameObject sp_obj_FaceBook { get; set; }

    public GameObject sp_obj_FaceBookDisable { get; set; }
    public GameObject sp_obj_FaceBookDia { get; set; }

    public Text sp_txt_FaceBookInvite { get; set; }
    public Text sp_txt_FaceBookLogin { get; set; }
    public Text sp_txt_FaceBookDiaRewardNumber { get; set; }

    public Button SendBtnActive;

    public GameObject sp_obj_PrevPage{ get; set; }
    public GameObject sp_obj_NextPage { get; set; }

    public Text sp_txt_PrevPageText { get; set; }
    public Text sp_txt_NextPageText { get; set; }

    StageFriend.FriendsItemType m_FriendsType;
    public PlayerData.UserFriend m_FriendsInfo;
    KakaoFriendInfo m_KakaoInfo;
    Image rewardimg;
    int m_Index = 0;
    public static bool PlustBtnActive = true;

    public override void init()
    {
        rewardimg = sp_obj_HeartBg.GetComponent<Image>();
        SendBtnActive = sp_obj_SendBtn.GetComponent<Button>();

        if(m_FriendsType == StageFriend.FriendsItemType.KaKaoInvite)
            sp_img_MainIcon.gameObject.SetActive(false);

        if (GameDefine.bUseFaceBook)
        {
            QuestInfo info = QuestTableMgr.instance.GetQuestListByType(QuestTableMgr.Quest_Type.FaceBookLogin)[0];
            if (info.ListQuestReward != null && info.ListQuestReward.Count > 0)
            {
                sp_txt_FaceBookDiaRewardNumber.text = info.ListQuestReward[0].Number.ToString();
            }
        }

        LocalInit();
    }

    public void SetInActive(bool IsOn)
    {
      
    }

    public override void OnListItemActivated()
    {
        Refresh(m_FriendsType);
    }

   
    public void SetFriendsInfo(PlayerData.UserFriend info)
    {
        m_FriendsInfo = info;
    }

    public StageFriend.FriendsItemType GetFriendItemType ()
    {
        return m_FriendsType;
    }


    public void Set(StageFriend.FriendsItemType Type, PlayerData.UserFriend friends,int index , KakaoFriendInfo kakaoinfo = null)
    {
        m_FriendsType = Type;

        if (Type == StageFriend.FriendsItemType.FaceBookInvte)
        {
            if (sp_obj_FaceBookInviteBtn == null) return;

            sp_obj_FaceBookInviteBtn.SetActive(true);
            sp_obj_InviteFriendInfo.SetActive(false);
        }
        else
        {
            m_Index = index;

         
            m_KakaoInfo = kakaoinfo;
            if (m_FriendsType == StageFriend.FriendsItemType.AddFrends)
            {
                int a = 0;
            }
            m_FriendsInfo = friends;

        }
        if (sp_txt_LastLoginTitle == null) return;

        Refresh(Type);

    }

    public void Refresh(StageFriend.FriendsItemType Type)
    {

        sp_obj_FaceBookInviteBtn.SetActive(false);
        sp_obj_InviteFriendInfo.SetActive(true);
        sp_obj_PrevPage.SetActive(false);
        sp_obj_NextPage.SetActive(false);

        switch (Type)
        {
            case StageFriend.FriendsItemType.Nomal:
                SetFriendsMode();
                break;
            case StageFriend.FriendsItemType.Waiting:
                SetWatingMode();
                break;
            case StageFriend.FriendsItemType.AddFrends:
                SetAddFriendsMode();
                break;
            case StageFriend.FriendsItemType.KaKaoInvite:
                SetInviteMode();
                break;

            case StageFriend.FriendsItemType.PlusBtn:
                sp_img_MainIcon.gameObject.SetActive(false);
                break;

            case StageFriend.FriendsItemType.FaceBookInvte:
                sp_obj_FaceBookInviteBtn.SetActive(true);
                sp_obj_InviteFriendInfo.SetActive(false);

                int playerAge = PlayerPrefs.GetInt("Player_Age");
                if (playerAge < GameDefine.CheckMinAge)
                {
                    sp_obj_FaceBookDisable.SetActive(true);

                    sp_obj_FaceBookFirst.SetActive(true);
                    sp_txt_FaceBookLogin.text = LocalizationMgr.GetMessage(11726);
                    sp_obj_FaceBookDia.SetActive(false);
                }
                else
                {
                    sp_obj_FaceBookDisable.SetActive(false);
                    sp_obj_FaceBookDia.SetActive(true);

                    sp_txt_FaceBookLogin.text = sp_txt_FaceBookInvite.text = NativeHelperContainer.facebookHelper.CheckFaceBookLogin() ? LocalizationMgr.GetMessage(10436) : LocalizationMgr.GetMessage(11718);

                    bool IsFaceBookFirstClear = PlayerData.instance.IsClearFirstFaceBookLogin();

                    sp_obj_FaceBookFirst.SetActive(!IsFaceBookFirstClear && !NativeHelperContainer.facebookHelper.CheckFaceBookLogin());
                    sp_obj_FaceBook.SetActive(!sp_obj_FaceBookFirst.activeSelf);

                    sp_txt_FaceBookLogin.gameObject.SetActive(sp_obj_FaceBookFirst.activeSelf || sp_obj_FaceBook.activeSelf);
                }
                break;

            case StageFriend.FriendsItemType.nextPage:
                sp_obj_PrevPage.SetActive(false);
                sp_obj_NextPage.SetActive(true);
                sp_obj_FaceBookInviteBtn.SetActive(false);
                sp_obj_InviteFriendInfo.SetActive(false);
                sp_txt_PrevPageText.text = LocalizationMgr.GetMessage(10961);
                break;
            case StageFriend.FriendsItemType.prevPage:
                sp_obj_PrevPage.SetActive(true);
                sp_obj_NextPage.SetActive(false);
                sp_obj_FaceBookInviteBtn.SetActive(false);
                sp_obj_InviteFriendInfo.SetActive(false);
                sp_txt_NextPageText.text = LocalizationMgr.GetMessage(10962);
                break;

        }

        ButtonControll();
      
        if (Type != StageFriend.FriendsItemType.KaKaoInvite && m_FriendsInfo != null)
        LoginTime(m_FriendsInfo.LoginDate);
    }

    void LocalInit()
    {
        sp_txt_LastLoginTitle.text = LocalizationMgr.GetMessage(10424);
        sp_txt_InviteText.text = LocalizationMgr.GetMessage(10427);
        sp_txt_RejectText.text = LocalizationMgr.GetMessage(10426);
        sp_txt_AcceptText.text = LocalizationMgr.GetMessage(10425);
        sp_txt_InvitePlusText.text = LocalizationMgr.GetMessage(10433);
        sp_txt_FaceBookLogin.text = sp_txt_FaceBookInvite.text = LocalizationMgr.GetMessage(10436);
    }

    public void Clear()
    {
        m_FriendsInfo = null;
        m_KakaoInfo = null;
        gameObject.SetActive(false);
    }

    public System.DateTime SendGiftTime()
    {
        return m_FriendsInfo.SendGiftTime;
    }

    void SetFriendsMode()
    {
        sp_obj_LastLoginBg.SetActive(true);
        sp_txt_Level.gameObject.SetActive(true);
        sp_txt_NicName.gameObject.SetActive(true);
        sp_obj_SendBtn.SetActive(true);
        sp_obj_DelBtn.SetActive((PlayerData.instance.GetPlatformFriendList().Find(item => item.Friend_UserId == m_FriendsInfo.Friend_UserId) == null));

        int CharacterId = PlayerData.instance.CheckCharacterIcon(m_FriendsInfo.ItemId);

        sp_img_MainIcon.sprite = DataMgr.instance.GetSprite(ItemTableMgr.instance.GetItemData(CharacterId).Icon + "nr");
        sp_txt_Level.text = LocalizationMgr.GetMessage(10252, m_FriendsInfo.PlayerLevel); 
        sp_txt_NicName.text = LocalizationMgr.GetMessage(10309, m_FriendsInfo.NickName);

        if (GameDefine.bUseFaceBook && NativeHelperContainer.facebookHelper.CheckFaceBookLogin())
        {
            //페이스북 이미지 On
            sp_obj_FaceBookImg.SetActive(!(PlayerData.instance.GetPlatformFriendList().Find(item => item.Friend_UserId == m_FriendsInfo.Friend_UserId) == null));
        }
        else
            sp_obj_FaceBookImg.SetActive(false);

        if (m_FriendsInfo.SendGiftTime != null && PlayerData.instance.FriendGiftRefreshTime.AddSeconds(-ConfigTableMgr.instance.GetConfigData("CoolTime_SendSocialPointToFriend")) < m_FriendsInfo.SendGiftTime)
        {
            RefreshSendBtn(true);
        }
        else
        {
            RefreshSendBtn(false);
        }
    }

    public void RefreshSendBtn(bool IsOn)
    {
        if(sp_txt_SendRefreshTime != null)
        {
            sp_txt_SendRefreshTime.gameObject.SetActive(IsOn);
            SendBtnActive.interactable = !IsOn;

            if (IsOn)
            {
#if GSP_PLATFORM_GLOBAL
                System.TimeSpan time = PlayerData.instance.FriendGiftRefreshTime - System.DateTime.UtcNow;
#else
                System.TimeSpan time = PlayerData.instance.FriendGiftRefreshTime - System.DateTime.Now;
#endif

                sp_txt_SendRefreshTime.text = LocalizationMgr.GetMessage(10432, time.Hours + 1);
            }
        }
       
      
    }

    void SetInviteMode()
    {
        List<InvitePlatformRewardInfo> info = InvitePlatformRewardTableMgr.instance.GetInvitePlatformRewardInfo(0);
        rewardimg.sprite = DataMgr.instance.GetSprite(ItemTableMgr.instance.GetItemData(info[0].RewardItemId).Icon);
        sp_btn_InviteBtn.gameObject.SetActive(true);
        sp_obj_HeartBg.SetActive(true);
        sp_txt_NicName.gameObject.SetActive(true);
        sp_txt_NicName.gameObject.transform.position = sp_txt_Level.gameObject.transform.position;
        StartCoroutine(KakaoImageGet());
        sp_txt_NicName.text = m_KakaoInfo.profile_nickname; // 카톡닉네임으로 바꿔야됨.
        sp_txt_HeartCount.text = LocalizationMgr.GetMessage(10216, info[0].RewardItemNumber); // Local : X {0}
        sp_btn_InviteBtn.interactable = CheckSendInvite();
    }

    IEnumerator KakaoImageGet()
    {
        Sprite sprite = null;
        if (m_KakaoInfo.profile_thumbnail_image == "")
        {
            sprite = DataMgr.instance.GetSprite("image_katok");
            yield return null;
        }
        else
        {
            WWW www = new WWW(m_KakaoInfo.profile_thumbnail_image);
            yield return www;

            Rect rec = new Rect(0, 0, www.texture.width, www.texture.height);
            sprite = Sprite.Create(www.texture, rec, sp_img_MainIcon.rectTransform.pivot, 1);
        }
        sp_img_MainIcon.sprite = sprite;

        sp_img_MainIcon.gameObject.SetActive(true);

    }

    void SetWatingMode()
    {
        sp_obj_LastLoginBg.SetActive(true);
        sp_txt_Level.gameObject.SetActive(true);
        sp_txt_NicName.gameObject.SetActive(true);
   

        sp_obj_RejectBtn.SetActive(true);
        sp_obj_AcceptBtn.SetActive(true);
        int CharacterId = PlayerData.instance.CheckCharacterIcon(m_FriendsInfo.ItemId);
        sp_img_MainIcon.sprite = DataMgr.instance.GetSprite(ItemTableMgr.instance.GetItemData(CharacterId).Icon + "nr");
        sp_txt_Level.text = LocalizationMgr.GetMessage(10252, m_FriendsInfo.PlayerLevel); // Local : Level {0}
        sp_txt_NicName.text = LocalizationMgr.GetMessage(10309, m_FriendsInfo.NickName);//Local : Name {0}
     //   sp_txt_LastLoginTime.text = LocalizationMgr.GetMessage(10431, LoginTime(m_FriendsInfo.LoginDate).Days);
    }

    void LoginTime(System.DateTime time)
    {
#if GSP_PLATFORM_GLOBAL
        System.TimeSpan remaintime = System.DateTime.UtcNow - System.DateTime.UtcNow;
        if (System.DateTime.UtcNow > time)
            remaintime = System.DateTime.UtcNow - time;
#else
        System.TimeSpan remaintime = System.DateTime.Now - System.DateTime.Now;
        if (System.DateTime.Now > time)
            remaintime =  System.DateTime.Now - time;

#endif

        if (remaintime.Days < 1 && remaintime.Hours < 1)
        {
            sp_txt_LastLoginTime.text = LocalizationMgr.GetMessage(11809);
        }
        else if (remaintime.Days < 1)
        {
            sp_txt_LastLoginTime.text = LocalizationMgr.GetMessage(11810, remaintime.Hours);
        }
        else if (remaintime.Days < 2)
        {
            sp_txt_LastLoginTime.text = LocalizationMgr.GetMessage(11808);
        }
        else if (remaintime.Days < 30)
        {
            sp_txt_LastLoginTime.text = LocalizationMgr.GetMessage(11806, remaintime.Days);
        }
        else if (remaintime.Days >= 30)
        {
            sp_txt_LastLoginTime.text = LocalizationMgr.GetMessage(11807);
        }
        //else if(remaintime.Hours > 0)
        //    sp_txt_LastLoginTime.text = LocalizationMgr.GetMessage(10609, remaintime.Hours, remaintime.Minutes);
        //else
        //    sp_txt_LastLoginTime.text = LocalizationMgr.GetMessage(10611, remaintime.Minutes, remaintime.Seconds);



    }

    void SetAddFriendsMode()
    {
        sp_txt_InviteText.text = LocalizationMgr.GetMessage(10428);
        sp_btn_InviteBtn.gameObject.SetActive(true);
        sp_obj_LastLoginBg.SetActive(true);
        sp_txt_Level.gameObject.SetActive(true);
        sp_txt_NicName.gameObject.SetActive(true);
        int CharacterId = PlayerData.instance.CheckCharacterIcon(m_FriendsInfo.ItemId);
        sp_img_MainIcon.sprite = DataMgr.instance.GetSprite(ItemTableMgr.instance.GetItemData(CharacterId).Icon + "nr");
        sp_txt_Level.text = LocalizationMgr.GetMessage(10252, m_FriendsInfo.PlayerLevel); // Local : Level {0}
        sp_txt_NicName.text = LocalizationMgr.GetMessage(10309, m_FriendsInfo.NickName);//Local : Name {0}
       // sp_txt_LastLoginTime.text = LocalizationMgr.GetMessage(10431, m_FriendsInfo.LoginDate.Day);
    }

    public void SendBtnActiveUpdate(bool IsOn)
    {
        SendBtnActive.interactable = IsOn;
    }

    bool CheckSendInvite()
    {
        return (PlayerData.instance.GetSendedPlatformFriendList().Find(item => item.ToUserPlatformId == (m_KakaoInfo.id == 0 ? m_KakaoInfo.uuid : m_KakaoInfo.id.ToString())) == null);
    }

    void ButtonControll(){
        sp_obj_RejectBtn.SetActive(m_FriendsType == StageFriend.FriendsItemType.Waiting);
        sp_obj_AcceptBtn.SetActive(m_FriendsType == StageFriend.FriendsItemType.Waiting);
        sp_btn_InviteBtn.gameObject.SetActive(m_FriendsType == StageFriend.FriendsItemType.KaKaoInvite || m_FriendsType == StageFriend.FriendsItemType.AddFrends);
        sp_obj_HeartBg.SetActive(m_FriendsType == StageFriend.FriendsItemType.KaKaoInvite);

        sp_obj_LastLoginBg.SetActive(m_FriendsType != StageFriend.FriendsItemType.KaKaoInvite && m_FriendsType != StageFriend.FriendsItemType.PlusBtn);
        sp_txt_Level.gameObject.SetActive(m_FriendsType != StageFriend.FriendsItemType.KaKaoInvite && m_FriendsType != StageFriend.FriendsItemType.PlusBtn);
        sp_obj_SendBtn.SetActive(m_FriendsType == StageFriend.FriendsItemType.Nomal);
        sp_obj_DelBtn.SetActive(m_FriendsType == StageFriend.FriendsItemType.Nomal && (PlayerData.instance.GetPlatformFriendList().Find(item => item.Friend_UserId == m_FriendsInfo.Friend_UserId) == null));
        sp_obj_InvitePlusBtn.SetActive(PlustBtnActive && m_FriendsType == StageFriend.FriendsItemType.PlusBtn);

    }

    void SendSocialPoint()
    {
        List<PlayerData.UserFriend> Gamefrendsdata = new List<PlayerData.UserFriend>();
        Gamefrendsdata.Add(m_FriendsInfo);

        PlayerDataMgr.instance.SendSocialPoint(Gamefrendsdata, (TangentFramework.HttpNetwork.Packet p, bool success) =>
        {
            if (success)
            {
                GAHelper.GALogEvent(GAHelper.GAEventCategory.Friend, "SendSocialPoint", "", 0);
                //if (StageFriend.ScrollRefresh != null)
                //    StageFriend.ScrollRefresh(null, "sp_obj_FriendsOn");
                if (StageFriend._RefreshFriends != null)
                    StageFriend._RefreshFriends();

                if (PlayerData.instance.GetGameFriendList().Count > m_Index)
                    m_FriendsInfo = PlayerData.instance.GetGameFriendList()[m_Index];
                else
                    m_FriendsInfo = PlayerData.instance.GetPlatformFriendList()[m_Index - PlayerData.instance.GetGameFriendList().Count];

                //CLog.LogError("Index = " + m_Index);
                //CLog.LogError("Nicname = " + m_FriendsInfo.NickName);
                //CLog.LogError("SendGiftTime = " + m_FriendsInfo.SendGiftTime);
                UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.ReachCount_Gift, 1, 0);
                SendBtnActiveUpdate(false);

            }
        }, StageFriend.Instance.GetCurPage());
    }


    public override void OnBtnClickEvent(GameObject go)
    {
        base.OnBtnClickEvent(go);
        switch (go.name)
        {

            case "sp_obj_FaceBookFirst":
            case "sp_obj_FaceBook":
                if (NativeHelperContainer.facebookHelper.CheckFaceBookLogin())
                {
                    NativeHelperContainer.facebookHelper.AppInvite((Facebook.Unity.IAppInviteResult result) =>
                    {
                        if (result == null)
                        {
                            CLog.Log("FaceBook Invite");
                        }

                    }, GamePath.FacebookInvitingImageURL);
                }
                else
                {
                    NativeHelperContainer.facebookHelper.FacebookLogin((bool succuss) => 
                    {
                        if(succuss)
                            ListenerManager.instance.SendDirectEvent("UpdateFriendCount", null);

                    }, sp_txt_FaceBookInvite, 10436, sp_obj_FaceBookFirst, sp_obj_FaceBook);
                  
                }
              
                break;
            case "sp_obj_FaceBookDisable":
                StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(11727)), (string str) => { });
                break;
            case "sp_obj_DelBtn":

                StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "TwoBtn", "Text", LocalizationMgr.GetMessage(50037, m_FriendsInfo.NickName)), (string str) =>
                {
                    if(str == "Yes")
                    {
                      

                        PlayerDataMgr.instance.DeleteGameFriend(m_FriendsInfo.Friend_UserId, (TangentFramework.HttpNetwork.Packet p, bool success) =>
                        {
                            if (success)
                            {
                                PlayerData.instance.GetGameFriendList();
                                StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(50038)), (string str2) =>
                                {
                                    if(str2 == "Yes")
                                    {

                                    }
                                });
                                StageFriend.Instance.SetCurPage(1);
                                if (StageFriend._RefreshFriends != null)
                                    StageFriend._RefreshFriends();

                            }
                        });

                    }

                });


                break;

            case "sp_obj_SendBtn":
                if(PlayerData.instance.m_TodayReceiveSocialPoint + 1 > ConfigTableMgr.instance.GetConfigData("MaxSocialpointDaily"))
                {
                    StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "TwoBtn", "Text", LocalizationMgr.GetMessage(50098, ConfigTableMgr.instance.GetConfigData("MaxSocialpointDaily"))), (string str) =>
                    {
                        if (str == "Yes")
                        {
                            SendSocialPoint();
                        }
                    });
                }
                else
                {
                    SendSocialPoint();
                }
                break;

            case "sp_btn_InviteBtn":
                //add , invite 두개라서 type별로 해야댐
                if(m_FriendsType == StageFriend.FriendsItemType.AddFrends)
                {
                    List<PlayerData.UserFriend> friends1 = new List<PlayerData.UserFriend>();
                    friends1.Add(m_FriendsInfo);
                    if (PlayerData.instance.GetGameFriendList().Count + friends1.Count > ConfigTableMgr.instance.GetConfigData("MaxGameFriendNumber"))
                    {
                        StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(50040)), (string str) =>
                        {

                        });
                        return;
                    }

                    PlayerDataMgr.instance.ApplyGameFriend(friends1, (TangentFramework.HttpNetwork.Packet p, bool success) =>
                    {
                        if (success)
                        {
                            UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.RecommendFreidns, friends1.Count, 0);
                            if (StageFriend.ScrollRefresh != null)
                                StageFriend.ScrollRefresh(null, "AddFriends");
                        }
                    });
                }
                else if(m_FriendsType == StageFriend.FriendsItemType.KaKaoInvite)
                {
                    if (PlatformHelper.kakao_remaining_invite_count <= 0)
                    {
                        StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(50057)), (string str) =>
                        {

                        });
                        return;
                    }

                    if (PlayerData.instance.IsAllowToSendKakaoMsg(m_KakaoInfo))
                    {
                        StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "TwoBtn", "Text", LocalizationMgr.GetMessage(50059, m_KakaoInfo.profile_nickname)), (string str) =>
                        {
                            if(str == "Yes")
                            {
                                StageMgr.instance.CloseCurPopup(true);
                                PlayerDataMgr.instance.InviteFriend(m_KakaoInfo.uuid, (TangentFramework.HttpNetwork.Packet p, bool success) =>
                                {
                                    if (success)
                                    {
                                        StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(50058)), (string str2) =>
                                        {

                                        });
                                        sp_btn_InviteBtn.interactable = CheckSendInvite();
                                        if (StageFriend._RefreshInviteTab != null)
                                            StageFriend._RefreshInviteTab();
                                    }
                                });

                            }
                        });
                    }
                    else
                    {
                        int docskey = 50060;
                        if (m_KakaoInfo.talk_os == "ios" && ConfigTableMgr.instance.GetConfigData("AllowToSendKakaoInviteMsgToiOSUser") == 0)
                            docskey = 50074;

                        StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(docskey)), (string str2) =>
                        {

                        });
                    }
                   
                }
                break;

            case "sp_obj_RejectBtn":
                List<PlayerData.UserFriend> Denyfriends = new List<PlayerData.UserFriend>();
                Denyfriends.Add(m_FriendsInfo);
                PlayerDataMgr.instance.DenyGameFriend(Denyfriends, (TangentFramework.HttpNetwork.Packet p, bool success) =>
                {
                    if (success)
                    {
                        StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(50041)), (string str) =>
                        {

                        });
                        if (StageFriend._RefreshFriends != null)
                           StageFriend._RefreshFriends();

                        if (StageFriend.ScrollRefresh != null)
                            StageFriend.ScrollRefresh(null, "Recommended");
                    }
                });
                break;

            case "sp_obj_AcceptBtn":
                List<PlayerData.UserFriend> friends = new List<PlayerData.UserFriend>();
                friends.Add(m_FriendsInfo);

                if (PlayerData.instance.GetGameFriendList().Count + friends.Count <= ConfigTableMgr.instance.GetConfigData("MaxGameFriendNumber"))
                {
                    PlayerDataMgr.instance.AcceptGameFriend(friends, (TangentFramework.HttpNetwork.Packet p, bool success) =>
                    {
                        if (success)
                        {
                            int DocId = 0;
                            CustomNetwork.RecommendFriendList friend = p as CustomNetwork.RecommendFriendList;

                            if (friend.failCnt == 0)
                                DocId = 50039;
                            else DocId = 50091;

                            StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(DocId)), (string str) =>
                            {
                                if(str == "No")
                                {
                                    if (StageFriend._RefreshFriends != null)
                                        StageFriend._RefreshFriends();

                                    if (StageFriend.ScrollRefresh != null)
                                        StageFriend.ScrollRefresh(null, "Recommended");
                                }
                            });
                         
                        }
                    });

                }
                else
                {
                    StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(50040)), (string str) =>
                    {

                    });
                }

                break;

            case "sp_obj_InvitePlusBtn":

                int Oldcount = PlayerData.instance.GetNoGamePlatformFriendList().Count;
                PlayerData.instance.UpdatePlatformFriendList((bool success) =>
                {
                    if (success)
                    {
                        PlustBtnActive = Oldcount < PlayerData.instance.GetNoGamePlatformFriendList().Count ? true : false;

                        if (StageFriend.ScrollRefresh != null)
                            StageFriend.ScrollRefresh(null, "sp_obj_InviteOn");
                    }
                }, PlatformHelper.KakaoFriendsType.Invitable);

                break;

            case "sp_obj_NextPageBtn":

                StageFriend.Instance.AddCurPage(1);

                PlayerDataMgr.instance.GetFriendList((TangentFramework.HttpNetwork.Packet p, bool success) =>
                {
                    CustomNetwork.FriendList friendListInfo = p as CustomNetwork.FriendList;                    
                    if (success)
                    {
                        //StageFriend.Instance.ChangeFriendPage();
                        if (StageFriend._RefreshFriends != null)
                            StageFriend._RefreshFriends();
                    }
                }, StageFriend.Instance.GetCurPage());

                break;

            case "sp_obj_PrevPageBtn":

                StageFriend.Instance.AddCurPage(-1);

                PlayerDataMgr.instance.GetFriendList((TangentFramework.HttpNetwork.Packet p, bool success) =>
                {
                    CustomNetwork.FriendList friendListInfo = p as CustomNetwork.FriendList;
                    if (success)
                    {
                        //StageFriend.Instance.ChangeFriendPage();
                        if (StageFriend._RefreshFriends != null)
                            StageFriend._RefreshFriends();
                    }
                }, StageFriend.Instance.GetCurPage());

                break;
        }
    }
}
