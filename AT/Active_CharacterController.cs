using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JaykayFramework;
using Tangent.Game;

//이 스크립트는 캐릭터를 키고, 끄는 작업, 애니메이션 변경을 담당합니다.

public class Active_CharacterController : MonoSingleton<Active_CharacterController>
{
    //캐릭터가 나와야할 스크립트의 이름입니다.
    enum CharacterOnPopupName { StageSetCharacter, StageResult, StageCharacterLevelUp, StageCharacterBreakLimit, StageGachaAndUnLockResult, StageOutGameTutorial, StageGuideQuest, MAX }

    // 버디가 나와야할 스크립트 이름
    enum BuddyOnPopupName { StageSetCharacter }


    GameObject camera_displayedCharacter; // 캐릭터 카메라는 따로 있기때문에 얻어 와야합니다.
    GameObject displayedCharacterParent;
    GameObject displayedBuddyParent;
    GameObject lobbyDisplayedCharacter;
    Animator m_LobbyChatAnim;


    bool firstset = false;

    string nowPopupName = "";
    string nowStageName = "";

    static int CharacterIndex = 0; // 캐릭터 창에서 캐릭터 계속 변경해줘야 하니까 
    int nowCharacterIndex = 0;
    int oldcharacterIndex = 0;
    public static bool CharacterGachaUnLock = false; // 레어 이상 뽑기. -> 다른 재료들이랑 같이 써서. 캐릭터만 구별필요.

    Dictionary<int, GameObject> dicDisplayedCharacter = new Dictionary<int, GameObject>();
    Dictionary<int, GameObject> dicDisplayedBuddy = new Dictionary<int, GameObject>();
    GameObject curDisplayedBuddy = null;

    bool TutorialPopupOn = false;

    public bool ClickCharacterListItem = false; // 캐릭터 리스트에서 모델링 교체 시, update 로 인해 제어하기 위함
    // Use this for initialization
    void Start () {
      
    }
   
    // Update is called once per frame
    void Update () {
	
        if(StageLobby.instance != null && !StageMgr.instance.CurStageName.Equals("StageInGame"))
        {
            string popupname = StageMgr.instance.CurPopupName;

            if (firstset == false && dicDisplayedCharacter != null)
            {
                firstset = true;
                nowCharacterIndex = CharacterIndex = PlayerData.instance.GetCharacterID(PlayerData.instance.m_CurCharacterUID);
                setCreate_DisplayedCharacterCreate_Handler(CharacterIndex);
            }
            else if (nowCharacterIndex != CharacterIndex && nowPopupName == "StageSetCharacter" && CharacterIndex != 0)
            {
                nowCharacterIndex = CharacterIndex;
                setActive_DisplayedCharacter_Handler(true, CharacterIndex);
            }
            else if (StageLobby.instance.popupopenBtnClick || (firstset && (nowPopupName != popupname)) || ((nowStageName != StageMgr.instance.CurStageName) && CharacterIndex != 0) || (oldcharacterIndex != CharacterIndex && StageLobby.instance.IsOnTagUiMode) ||
               StageLobby.instance.ChangeTagCharacter || CharacterGachaUnLock)
            {
                StageLobby.instance.popupopenBtnClick = false;
                for (CharacterOnPopupName i = CharacterOnPopupName.StageSetCharacter; i < CharacterOnPopupName.MAX; i++)
                {
					if (popupname == i.ToString() || (!StageMgr.instance.CurOpenPopupCheck && popupname == "" && (StageMgr.instance.CurStageName == "StageLobby" || StageMgr.instance.CurStageName == "StageResult")))
					{
						if (popupname != CharacterOnPopupName.StageGachaAndUnLockResult.ToString() || (popupname == CharacterOnPopupName.StageGachaAndUnLockResult.ToString() && CharacterGachaUnLock))
						{
							setActive_DisplayedCharacter_Handler(true, CharacterIndex);
						}

						break;

					}
					else if (popupname != "StageToast")
					{
						if(!string.IsNullOrEmpty(popupname))
							setActive_DisplayedCharacter_Handler(false, CharacterIndex);
					}
                }

                nowPopupName = popupname;
                nowStageName = StageMgr.instance.CurStageName;
                oldcharacterIndex = CharacterIndex;

            }
            


        }
    }


    public void setActive_DisplayedCharacter_Handler(bool IsOn = true, int charId = 0)
    {
        if(charId != 0 && dicDisplayedCharacter.ContainsKey(charId) == false)
            setCreate_DisplayedCharacterCreate_Handler(charId);

        SetDisplayedCharacterParentActive(false);
        foreach (KeyValuePair<int,GameObject> dic in dicDisplayedCharacter)
        {
            if (charId != dic.Key || StageMgr.instance.CurPopupName != "")
                dic.Value.SetActive(false);
        }

        if (charId == 0)
            charId = PlayerData.instance.GetCharacterID(PlayerData.instance.m_CurCharacterUID);

        if (dicDisplayedCharacter[charId] == null) return;

        dicDisplayedCharacter[charId].SetActive(IsOn);

        Active_RelayCharacter(charId, IsOn); // 릴레이 캐릭터가 나와야 할때와 아닐때를 구분합니다.

        m_LobbyChatAnim = dicDisplayedCharacter[charId].GetComponent<Animator>();

        ItemTableMgr.OriginalCharacter Character = ItemTableMgr.instance.GetItemData(CharacterIndex).OriginalCharacterName;
        //m_LobbyChatAnim.SetBool("JakeIdle", Character == ItemTableMgr.OriginalCharacter.Jake);

        //if (Character == ItemTableMgr.OriginalCharacter.burble && StageMgr.instance.CurPopupName == "StageSetCharacter")
        //{
        //    dicDisplayedCharacter[charId].transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        //}
        //else if (Character == ItemTableMgr.OriginalCharacter.burble)
        //    dicDisplayedCharacter[charId].transform.localScale = Vector3.one;

        StageLobby.instance.ChangeTagCharacter = false;

        if(IsOn && (TutorialMgr.instance.GetCurTutorialInfo() != null) && !TutorialPopupOn)
        {
            StageMgr.instance.OpenPopup("StageOutGameTutorial", null, (string str) => { });
            TutorialPopupOn = true;
        }

        SetBuddy(charId);

        if (nowPopupName == "StageSetCharacter" || nowPopupName == "StageBasicPopup" || nowPopupName == "StageCharacterSkill")
        {
            if (ClickCharacterListItem == true)
                SetDisplayedCharacterParentActive(true);
        }
        else
        {
            if((StageMgr.instance.CurPopupName == "" && StageMgr.instance.CurStageName != "StageResult") ||
                nowPopupName == "StageGachaAndUnLockResult" || nowPopupName == "StageToast")
                SetDisplayedCharacterParentActive(true);
        }
    }

    public void SetDisplayedCharacterParentActive(bool state)
    {
        displayedCharacterParent.SetActive(state);
    }

    public void setCreate_DisplayedCharacterCreate_Handler(int itemid)
    {
        if (camera_displayedCharacter == null)
        {
            GameObject charcamara = GameObject.Find("CharacterCamera");
            camera_displayedCharacter = charcamara;
        }
        if (displayedCharacterParent == null)
        {
            displayedCharacterParent = new GameObject("CharacterParent");
            displayedCharacterParent.transform.localScale = new Vector3(1.85f, 1.85f, 1.85f);
            displayedCharacterParent.transform.position = new Vector3(0, -0.38f, 1.85f);
            displayedCharacterParent.transform.localRotation = Quaternion.Euler(0, 180, 0);
            displayedCharacterParent.layer = 8;

            displayedBuddyParent = new GameObject("BuddyParent");
            displayedBuddyParent.transform.localScale = Vector3.one * 1.85f;
            displayedBuddyParent.transform.position = new Vector3(-0.35f, 2.5f, 3.45f);
            displayedBuddyParent.transform.localRotation = Quaternion.Euler(0, 180, 0);
            displayedCharacterParent.layer = 8;
        }


        List<ItemInfo> info = ItemTableMgr.instance.GetItemsByType(ItemTableMgr.ItemInfo_ItemType.Character);


        ItemInfo charinfo = info.Find(item => item.ItemId == itemid);

        GameObject Lobbychar = ObjectMgr.instance.GetObject(ItemTableMgr.instance.GetItemData(charinfo.ItemId).PrefabName);

        if (Lobbychar != null && !dicDisplayedCharacter.ContainsKey(itemid))
        {
            GameObject Lobbyinst = Instantiate(Lobbychar, new Vector3(-0.5f, 0.25f, 0f), Quaternion.identity) as GameObject;
            Lobbyinst.SetParent(displayedCharacterParent);
            float size = ItemTableMgr.instance.GetItemData(itemid).ScaleSize * 0.01f;
            Lobbyinst.transform.localScale = new Vector3(size, size, size);
            Lobbyinst.SetActive(false);
            dicDisplayedCharacter.Add(charinfo.ItemId, Lobbyinst);
            lobbyDisplayedCharacter = Lobbyinst;

            if(itemid == PlayerData.instance.GetCurCharacterID())
            {
                m_LobbyChatAnim = Lobbyinst.GetComponent<Animator>();
                PlayAnimation();
            }
        
        }

        PlayerData.instance.SetdicDisplayedCharacter(dicDisplayedCharacter);


    }

    public void Active_RelayCharacter(int charId, bool IsOn)
    {
        List<PlayerData.UserCharacterItem> list = PlayerData.instance.m_RelayCharacterList;

        if (list.Find(item => item != null && item.ItemId == charId) == null)
            dicDisplayedCharacter[charId].transform.localPosition = new Vector3(-0.5f, 0.3f, 0);


        if (list.Count > 0 && (StageMgr.instance.CurPopupName == "StageGuideQuest" || StageMgr.instance.CurPopupName == "") && StageMgr.instance.CurStageName == "StageLobby")
        {

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    if (list[i].ItemId != 0 && dicDisplayedCharacter.ContainsKey(list[i].ItemId) == false)
                        setCreate_DisplayedCharacterCreate_Handler(list[i].ItemId);

                    dicDisplayedCharacter[list[i].ItemId].SetActive(IsOn);

                    if (i == 0)
                        dicDisplayedCharacter[list[i].ItemId].transform.localPosition = new Vector3(0.7f, 0.66f, -1.2f);
                    else if (i == 1)
                        dicDisplayedCharacter[list[i].ItemId].transform.localPosition = new Vector3(-1.55f, 1.2f, -1.7f);

                }

            }
        }
        else
            dicDisplayedCharacter[charId].transform.localPosition = new Vector3(-0.5f, 0.3f, 0);



    }


    void CharacterAnimation()
    {
        if (StageSetCharacter.Instance != null && StageSetCharacter.Instance.gameObject.activeSelf)
        {
            PlayAnimation();
            return;
        }

        if (m_LobbyChatAnim != null && m_LobbyChatAnim.gameObject.activeSelf != false && CharacterIndex != 0)
        {
            ItemTableMgr.OriginalCharacter Character = ItemTableMgr.instance.GetItemData(CharacterIndex).OriginalCharacterName;
			/*
            if (Character == ItemTableMgr.OriginalCharacter.burble)
            {
                m_LobbyChatAnim.SetInteger("Action", 1);
            }
            else
            {
                int Range = Character == ItemTableMgr.OriginalCharacter.Finn ? 3 : 4;
                m_LobbyChatAnim.SetInteger("Action", Random.Range(1, Range));

            }
            */

			int Range = (Character == ItemTableMgr.OriginalCharacter.Jake ? 4 : 3);
			m_LobbyChatAnim.SetInteger("Action", Random.Range(1, Range));
        }
            

        Invoke("PlayAnimation", 2.5f);

    }

    void PlayAnimation()
    {
        StopLobbyAnimation();
        Invoke("CharacterAnimation" , 10);
    }


    void StopLobbyAnimation()
    {
        if (m_LobbyChatAnim != null && m_LobbyChatAnim.gameObject.activeSelf != false)
        {
            m_LobbyChatAnim.SetInteger("Action", -1);
        }

    }

    public void ShowBuyBuddyShop(int buddyId)
    {
        if (curDisplayedBuddy != null)
            curDisplayedBuddy.SetActive(false);

        curDisplayedBuddy = null;

        bool active = AddBuddy(buddyId);
        if (!active)
        {
            if (dicDisplayedBuddy.ContainsKey(buddyId))
            {
                curDisplayedBuddy = dicDisplayedBuddy[buddyId];
                curDisplayedBuddy.SetActive(true);
            }
        }

        if (curDisplayedBuddy != null)
        {
            ATBuddy atBuddy = curDisplayedBuddy.GetComponent<ATBuddy>();
            if (atBuddy != null)
                atBuddy.SetLobbyAnimation();

            atBuddy.GetTransform().localPosition = new Vector3(-0.7f, 0, 0);
            atBuddy.GetTransform().localScale = Vector3.one * 1.5f;
        }
    }

    public void OffBuddy(int buddyId)
    {
        if (dicDisplayedBuddy.ContainsKey(buddyId))
            dicDisplayedBuddy[buddyId].SetActive(false);
    }

    void SetBuddy(int charId)
    {
        if(GameDefine.Version_Client > 3)
        {
            string popupname = StageMgr.instance.CurPopupName;
            if (popupname == "StageSetCharacter")
            {
                if (curDisplayedBuddy != null)
                    curDisplayedBuddy.SetActive(false);

                curDisplayedBuddy = null;
                PlayerData.UserBuddyItem buddy = PlayerData.instance.GetEquipBuddy(CharacterIndex);

                bool active = buddy != null ? AddBuddy(buddy.ItemId) : false;
                if (!active)
                {
                    if (buddy != null && dicDisplayedBuddy.ContainsKey(buddy.ItemId))
                    {
                        curDisplayedBuddy = dicDisplayedBuddy[buddy.ItemId];
                        curDisplayedBuddy.SetActive(true);
                    }
                }

                if(curDisplayedBuddy != null)
                {
                    ATBuddy atBuddy = curDisplayedBuddy.GetComponent<ATBuddy>();
                    if (atBuddy != null)
                        atBuddy.SetLobbyAnimation();

                    atBuddy.GetTransform().localPosition = Vector3.zero;
                    atBuddy.GetTransform().localScale = Vector3.one;
                }
            }
            else
            {
                if (curDisplayedBuddy != null)
                {
                    if (curDisplayedBuddy.activeSelf)
                        curDisplayedBuddy.SetActive(false);
                }
            }
        }
    }

    bool AddBuddy(int buddyId)
    {
        if (!dicDisplayedBuddy.ContainsKey(buddyId))
        {
            ItemInfo info = ItemTableMgr.instance.GetItemData(buddyId);
            if (info != null)
            {
                GameObject buddyobj = ObjectMgr.instance.GetObject(info.PrefabName);
                GameObject obj = Instantiate(buddyobj, Vector3.zero, Quaternion.identity) as GameObject;
                obj.SetParent(displayedBuddyParent);
                obj.transform.localScale = Vector3.one;
                obj.transform.localPosition = new Vector3(0, info.InGamePosY, 0);
                dicDisplayedBuddy[buddyId] = obj;
                curDisplayedBuddy = obj;
                return true;
            }
        }

        return false;
    }

    public static void SetCharacterIndex(int index)
    {
        CharacterIndex = index;
    }

    public static int GetCharacterIndex()
    {
        return CharacterIndex;
    }

  
}
