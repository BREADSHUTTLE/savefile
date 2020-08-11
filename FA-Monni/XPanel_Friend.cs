using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using I2.Loc;

enum FriendTabType
{
    Friend = 0,
    Socialize = 1,
}

/// <summary>
/// 친구 목록 > 친구 요청 목록 > 추천 친구 목록
/// 내 친구 / 요청&추천
/// </summary>
public class XPanel_Friend : MonoBehaviour
{
    public XWidget_ToggleTab[] tabs;
    public GameObject gameFriend;
    public GameObject gameFriendMake;

    private FriendTabType m_type = FriendTabType.Friend;

    public XUIFriendList m_friendList;
    public XUIFriendRecommendList m_recommendList;
    public XUIFriendRequestList m_requestList;

    private List<CS.Friend> friend_list = new List<CS.Friend>();
    private List<CS.Friend> recommanded_list = new List<CS.Friend>();
    private List<CS.Friend> request_list = new List<CS.Friend>();

    private CS.UserMetaData search_user = new CS.UserMetaData();

    public UILabel lblFriendCount;
    public UILabel lblRequestCount;

    public UIInput InputName;

    private string user_name = "";

    public UI2DSprite sprLevelSortArrow;
    public UI2DSprite sprLevelSortArrowActive;

    public UI2DSprite sprTimeSortArrow;
    public UI2DSprite sprTimeSortArrowActive;

    public XTutorialObjContainer tutoObjContainer;

    public UILabel lblIsFriendList;
    public UILabel lblIsRequestList;

    public void Build()
    {
        gameObject.SetActive(true);

        ShowGameFriend();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnClickTabs(int m_type)
    {
        tabs[m_type].OnClickThisTab();
    }

    #region 친구목록

    // 게임친구 목록
    public void ShowGameFriend()
    {
        OnClickTabs((int)FriendTabType.Friend);
        gameFriend.SetActive(true);
        gameFriendMake.SetActive(false);

        m_friendList.CoolTimeFriendListClear();

        RefreshFriendList();
    }

    // 나의 친구 목록
    private void RefreshFriendList()
    {
        friend_list.Clear();

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_Friend_List>(
            param: new CS.CSNetParameters
            {
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                if (result == CS.ApiReturnCode.Success)
                {
                    friend_list.Clear();

                    var resJson = json[CS.ApiParamKeys.List];
                    var response = resJson.ToObject<List<CS.Friend>>();

                    foreach (var each in response)
                    {
                        friend_list.Add(each);
                    }

                    SetLevelSorting(levelSort);
                }
            });
    }

    private bool friendRemove = false;

    // 친구 삭제하기
    public void MyFriendRemove(string aid)
    {
        if (friendRemove == true)
            return;

        friendRemove = true;

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_RemoveFriend>(
            param: new CS.CSNetParameters
            {
                { CS.ApiParamKeys.AccountId, aid }
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                if (result == CS.ApiReturnCode.Success)
                {
                    // 내 친구 목록
                    RefreshFriendList();
                }

                friendRemove = false;
            });
    }

    public void SetLevelSorting(bool Sortlevel)
    {
        lblFriendCount.text = string.Format("{0}  /  {1}", friend_list.Count.ToString(), "50");
        if (levelSort == false)
            levelSort = true;

        sprTimeSortArrowActive.gameObject.SetActive(false);
        sprLevelSortArrowActive.gameObject.SetActive(true);

        List<CS.Friend> sortedList = new List<CS.Friend>();

        var rotZ = Sortlevel ? 90f : 270f;

        // true = 오름차순, false = 내림차순
        if(Sortlevel)
        {
            // 레벨 낮은 순 -> 높은 순
            sortedList = friend_list.OrderBy(o => o.Level).ToList();
        }
        else
        {
            // 레벨 높은 순 -> 낮은 순
            sortedList = friend_list.OrderByDescending(o => o.Level).ToList();
        }

        SetArrowRotation(sprLevelSortArrow, sprLevelSortArrowActive, rotZ);

        lblIsFriendList.gameObject.SetActive(friend_list.Count == 0);
        friend_list = sortedList;
        m_friendList.Build(friend_list);
    }

    private bool levelSort = false;
    private bool onClick = false;

    public void OnClick_SortedLevel()
    {
        if (levelSort == true)
        {
            if (onClick)
                onClick = false;
            else
                onClick = true;
        }

        else if (levelSort == false)
        {
            timeSort = false;
            levelSort = true;
        }

        SetLevelSorting(onClick);
    }

    private bool timeSort = false;
    private bool onClick2 = false;

    public void OnClick_SortedLastTime()
    {
        if (timeSort == true)
        {
            if (onClick2)
                onClick2 = false;
            else
                onClick2 = true;
        }
        else if (timeSort == false)
        {
            levelSort = false;
            timeSort = true;
        }

        SetLastTimeSorting(onClick2);
    }

    private void SetLastTimeSorting(bool timeSort)
    {
        sprTimeSortArrowActive.gameObject.SetActive(true);
        sprLevelSortArrowActive.gameObject.SetActive(false);

        List<CS.Friend> sortedList = new List<CS.Friend>();

        var rotZ = timeSort ? 90f : 270f;

        if (timeSort)
        {
            // 최근 -> 예전
            sortedList = friend_list.OrderByDescending(o => o.LastLoginTime).ToList();
        }
        else
        {
            // 예전 -> 최근
            sortedList = friend_list.OrderBy(o => o.LastLoginTime).ToList();
            
        }

        SetArrowRotation(sprTimeSortArrow, sprTimeSortArrowActive, rotZ);

        friend_list = sortedList;
        m_friendList.Build(friend_list);
    }

    private void SetArrowRotation(UI2DSprite sprArrow, UI2DSprite sprActiveArrow, float rZ)
    {
        XUnityHelper.SetLocalRotation(sprArrow.transform, 0f, 0f, rZ);
        XUnityHelper.SetLocalRotation(sprActiveArrow.transform, 0f, 0f, rZ);
    }

    public void OnClick_AllFriendPoint()
    {    
        SendAllFriendPoint();
    }


    private bool sendAllPoint = false;

    // 친구 포인트 전체 보내기
    private void SendAllFriendPoint()
    {
        if (sendAllPoint == true)
            return;

        sendAllPoint = true;

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_FriendPoint>(
            param: new CS.CSNetParameters
            {
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                if (result == CS.ApiReturnCode.Success)
                {
                    var resJson = json[CS.ApiParamKeys.List];
                    var dic = resJson.ToObject<Dictionary<string, CS.ApiReturnCode>>();

                    //m_friendList.TempListClear();
                    bool state = false;
                    foreach (var e in dic)
                    {
                        if (e.Value != CS.ApiReturnCode.FriendPointCooltime)
                        {
                            m_friendList.BuildCoolTimeFriendList(e.Key);
                            state = true;
                        }
                    }

                    if (!state)
                    {
                        var title = ScriptLocalization.Get("Code/UI_TITLE_FRIEND");
                        var msg = ScriptLocalization.Get("Code/UI_FRIEND_ALL_POINT");
                        XGlobal.ui.common.ShowMessageBox(title, msg, XMessageBoxButtons.OK);
                    }

                    m_friendList.Build(friend_list);

                    XGlobal.MyInfo.FP = (int)json[CS.ApiParamKeys.AfterFriendPoint];
                    XGlobal.ui.NotifyToUI("OnNetdataUpdated_UserResource", XGlobal.MyInfo);
                }

                sendAllPoint = false;
            });
    }
    #endregion

    #region 추천친구

    // 추천친구 및 친구신청 목록
    public void ShowGameFriendSocialize()
    {
        OnClickTabs((int)FriendTabType.Socialize);
        gameFriend.SetActive(false);
        gameFriendMake.SetActive(true);

        // 요청 수락 대기 친구
        RefreshFriendRequest();
    }

    // 추천 친구 목록
    private void RefreshFriendSocializeList()
    {
        // 친구 패널에 있을 경우에는 새로고침 외엔 불러오지 않음
        //if (recommend)
        //    return;

        recommanded_list.Clear();

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_RecommandedFriend_List>(
            param: new CS.CSNetParameters
            {
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                if (result == CS.ApiReturnCode.Success)
                {
                    var resJson = json[CS.ApiParamKeys.List];
                    var response = resJson.ToObject<List<CS.Friend>>();

                    foreach (var each in response)
                    {
                        if(XGlobal.LoginInfo.AID != each.AccountId)
                            recommanded_list.Add(each);
                    }
                    RefreshRecommandedFriendList();
                }
            });
    }

    // 추천 친구 새로고침
    public void OnClick_RecommendFriendList()
    {
        RefreshFriendSocializeList();
    }

    private void RefreshRecommandedFriendList()
    {
        m_recommendList.Build(recommanded_list);
        SetFriendTutorialObject();
    }


    private bool friendSearch = false;

    // 친구 검색
    public void OnClick_SearchFriend()
    {
        if (friendSearch == true)
            return;

        friendSearch = true;

        user_name = InputName.value;
        if (user_name == null || user_name == "")
        {
            XGlobal.ui.common.ShowMessageBox(ScriptLocalization.Get("Code/UI_FRIEND_SEARCH_TITLE"),
                                             ScriptLocalization.Get("Code/UI_FRIEND_SEARCH_NOT_DESC"), 
                                             XMessageBoxButtons.OK);
            friendSearch = false;
            return;
        }

        SearchFriend();
    }

    private void SearchFriend()
    {
        recommanded_list.Clear();

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_SearchFriend>(
            param: new CS.CSNetParameters
            {
                { CS.ApiParamKeys.Username, user_name }
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                if (result == CS.ApiReturnCode.Success)
                {
                    var resJson = json[CS.ApiParamKeys.Userdata];
                    var response = resJson.ToObject<CS.UserMetaData>();

                    search_user = response;

                    // 추천친구 리스트
                    RefreshRecommandedSearchFriendList();
                }
                else if (result == CS.ApiReturnCode.NotExistsUserName)
                {
                    XGlobal.ui.common.ShowMessageBox(ScriptLocalization.Get("Code/UI_FRIEND_SEARCH_TITLE"),
                                                   ScriptLocalization.Get("Code/UI_FRIEND_SEARCH_DESC"), XMessageBoxButtons.OK);
                }

                // 검색 초기화
                InputName.value = "";
                friendSearch = false;
            });
    }

    private void RefreshRecommandedSearchFriendList()
    {
        m_recommendList.SearchBuild(search_user);
    }

    #endregion

    #region 친구 요청

    // 친구 요청 목록
    private void RefreshFriendRequest()
    {
        request_list.Clear();

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_GetWatingFriend_List>(
            param: new CS.CSNetParameters
            {
            },
            callback: (CS.ApiReturnCode result_1, string errmsg_1, JObject json_1) =>
            {
                if (result_1 == CS.ApiReturnCode.Success)
                {
                    var resJson = json_1[CS.ApiParamKeys.List];
                    var response = resJson.ToObject<List<CS.Friend>>();

                    foreach (var each in response)
                        request_list.Add(each);

                    RefreshRequestFriendList();

                    // 추천친구
                    RefreshFriendSocializeList();


                    // N 마크 제거
                    //XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_FriendshipNewMarkClear>(
                    //    param: new CS.CSNetParameters
                    //    {
                    //    },
                    //    callback: (CS.ApiReturnCode result_2, string errmsg_2, JObject json_2) =>
                    //    {
                    //        if (result_2 == CS.ApiReturnCode.Success)
                    //        {
                    //            if (XGlobal.MyInfo.Notifications.ContainsKey(CS.CSNotificationType.friendRequest) == true)
                    //                XGlobal.MyInfo.Notifications.Remove(CS.CSNotificationType.friendRequest);
                    //        }
                    //    });
                }
            });
    }

    private void RefreshRequestFriendList()
    {
        lblRequestCount.text = string.Format("{0}  /  {1}", request_list.Count.ToString(), "50");
        lblIsRequestList.gameObject.SetActive(request_list.Count == 0);
        m_requestList.Build(request_list);
    }


    private bool requestFriendShip = false;

    // 친구 요청 하기
    public void SetRequestFriendShip(string aid)
    {
        if (requestFriendShip == true)
            return;

        requestFriendShip = true;

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_RequestFriendship>(
            param: new CS.CSNetParameters
            {
                { CS.ApiParamKeys.AccountId, aid }
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                string titleStr = ScriptLocalization.Get("Code/UI_FRIEND_REQUEST_TITLE");
                string msg = null;

                if (result == CS.ApiReturnCode.Success)
                {
                    // 추천친구
                    //RefreshFriendSocializeList();

                    // 해당 추천 리스트 목록에서 요청한 유저 삭제
                    SetRefrechRecommandList(aid);

                    // 리스트 다시 그려줌
                    RefreshRecommandedFriendList();
                    msg = ScriptLocalization.Get("Code/UI_FRIEND_REQUEST_DESC");
                }
                else if (result == CS.ApiReturnCode.AlreadyFriend)
                {
                    // 이미 친구로 추가 된 유저
                    msg = ScriptLocalization.Get("Code/UI_FRIEND_NOT_REQUEST_DESC");
                }
                else if (result == CS.ApiReturnCode.FulledFriendRequest)
                {
                    //상대방의 대기요청이 가득참
                    msg = ScriptLocalization.Get("Code/UI_FRIEND_TARGET_REQUEST_FULL");
                }
                else if (result == CS.ApiReturnCode.AlreadyRequestedFriendship)
                {
                    //이미 요청을 보냈던 유저
                    msg = ScriptLocalization.Get("Code/UI_FRIEND_ALREADY_FREIND_REQUESTED");
                }
                else if (result == CS.ApiReturnCode.TargetAndSenderIsEqual)
                {
                    //나에게 요청을 보낸 경우
                    msg = ScriptLocalization.Get("Code/UI_FRIEND_REQUESTED_ME");
                }
                else
                {
                    //ERROR_CODE_PREFIX
                    msg = string.Format(ScriptLocalization.Get("Code/ERROR_CODE_PREFIX"), result);
                }

                XGlobal.ui.common.ShowMessageBox(titleStr,msg,XMessageBoxButtons.OK);
                requestFriendShip = false;
            });
    }

    private CS.Friend GetFriendData(List<CS.Friend> list, string aid)
    {
        foreach (var each in list)
        {
            if (aid == each.AccountId)
                return each;
        }

        return null;
    }

    private void SetRefrechRecommandList(string aid)
    {
        CS.Friend data = GetFriendData(recommanded_list, aid);
        if (data != null)
            recommanded_list.Remove(data);
    }

    private bool acceptFriendShip = false;


    // 친구 요청 수락
    public void AcceptRequestFriend(string aid)
    {
        if (acceptFriendShip == true)
            return;

        acceptFriendShip = true;

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_AcceptFriendship>(
            param: new CS.CSNetParameters
            {
                { CS.ApiParamKeys.AccountId, aid }
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                if (result == CS.ApiReturnCode.Success)
                {
                    // 요청 수락 대기 친구
                    RefreshFriendRequest();
                }
                else if (result == CS.ApiReturnCode.MyFriendIsFulled)
                {
                    // 내 친구목록 가득참
                    XGlobal.ui.common.ShowMessageBox(ScriptLocalization.Get("Code/UI_FRIEND_NOT_ACCEPT_TITLE"),
                                                    ScriptLocalization.Get("Code/UI_FRIEND_NOT_ACCEPT_DESC"),
                                                    XMessageBoxButtons.OK);
                }

                acceptFriendShip = false;
            });
    }

    private bool rejectFriendShip = false;
    
    // 친구 요청 거부
    public void UserRejectFriend(string aid)
    {
        if (rejectFriendShip == true)
            return;

        rejectFriendShip = true;


        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_RejectFriendship>(
            param: new CS.CSNetParameters
            {
                { CS.ApiParamKeys.AccountId, aid }
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                if (result == CS.ApiReturnCode.Success)
                {
                    // 요청 수락 대기 친구
                    RefreshFriendRequest();
                }

                rejectFriendShip = false;
            });
    }

    #endregion

    public void ChackOneFriendPoint(string aid)
    {
        m_friendList.BuildCoolTimeFriendList(aid);
    }


    private void SetFriendTutorialObject()
    {
        var recommendItem = m_recommendList.GetFirstItem();
        var tutoObj = recommendItem == null ? null : recommendItem.goFriendRequest;
        tutoObjContainer.Add("button_friend_request", tutoObj, true);

        if (tutoObj != null)
            tutoObj.SetActive(true);
    }

    public void OnClick_Help()
    {
        XGlobal.ui.lobby.ShowHelp(HelpLink.Friend);
    }
}
