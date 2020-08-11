using UnityEngine;
using System.Collections;
using JaykayFramework;
using System.Reflection;
//using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;
using TangentFramework;

public class ListItemShop : ListItem, IListener
{
    #region Enum

    enum GachaType
    {
        Treasure = 2,
        Character,
    }

    enum GachaPriceType
    {
        Free = 0,
        CoinAndCash,


    }

    #endregion

    #region UI Property
    RectTransform rt_img;
    public GameObject sp_obj_DrawItem { get; set; }
    public GameObject sp_obj_NotDrawItem { get; set; }

    /// <summary>
    /// Draw
    /// </summary>

    public Text sp_txt_DrawItemName { get; set; }
    public Text sp_txt_DrawDesc { get; set; }
    public Image sp_img_DrawIcon { get; set; }
    public GameObject sp_obj_DrawLimited { get; set; }
    public Text sp_txt_DrawCoolTime { get; set; }
    public Text sp_txt_DrawCostDigit { get; set; }
    public Image sp_img_DrawCostImg { get; set; }

    public Image sp_img_DrawShelfImg { get; set; }
    public GameObject sp_obj_DrawSaleImg { get; set; }

    /// <summary>
    /// Not Draw
    /// </summary>
    /// 
    public Image sp_img_ShelfImg { get; set; }
    public Image sp_img_Icon { get; set; }
    public Text sp_txt_ItemName { get; set; }
    public Text sp_txt_ItemDesc { get; set; }
    public Image sp_img_ItemPriceIcon { get; set; }
    public Text sp_txt_ItemCostDigit { get; set; }
    public Image sp_img_FirstBuy { get; set; }
    public GameObject sp_obj_SaleImag { get; set; }
    public Text sp_txt_FirstTitle { get; set; }

    public Image sp_img_QuantityBg { get; set; }
    public Text sp_txt_QuantityTitle { get; set; }
    public Text sp_txt_Quantity { get; set; }

    #endregion

    #region Member variables

    string RShelfImgName = "image_shop_shelfR";
    string LShelfImgName = "image_shop_shelfL";

    ShopInfo m_ShopInfo;
    GachaInfo m_DarwInfo;
    InAppPurchaseInfo m_DiamondInfo;

    StageShop.TabState m_ShopType;
    InAppPurchaseTableMgr.InAppPurchase_Type m_InAppItemType;

    bool FreeByNow = false;

    int ItemIndex = 1;


    Image shelf = null;

    System.DateTime _NowTime;

    GameObject BoxEffect;
    GameObject WhilteEffect;

    public delegate void CallBack();

    #endregion

    #region CommonFunction

    public override void init()
    {
        ListenerManager.instance.AddListener(this as IListener);
        //  m_InAppRefresh_Handler = refreshInApp;
        sp_txt_FirstTitle.text = LocalizationMgr.GetMessage(10614);
        BoxEffect = StageLobby.instance.GetEffect_NeedToCommonEffect_Handler("UI_Box", new Vector3(0.9f, 14.2f, 77.7f), new Vector3(5, 5, 5));
        WhilteEffect = StageLobby.instance.GetEffect_NeedToCommonEffect_Handler("FX_white_out_ingame", new Vector3(0, 12.7f,0), Vector3.one);
        sp_txt_QuantityTitle.text = LocalizationMgr.GetMessage(11302);
    }


    public override void OnListItemActivated()
    {
        Set(m_ShopType, m_ShopInfo, ItemIndex, m_DarwInfo, m_InAppItemType, m_DiamondInfo);

        if(m_DarwInfo != null &&
            ( m_DarwInfo.GachaId == ConfigTableMgr.instance.GetConfigData("TutorialGachaListItemId") && TutorialMgr.instance.GetCurTutorialInfo() != null))
            ListenerManager.instance.SendDirectEvent("OutGameShopOpenClick");
    }

    bool IListener.HandleEvent(string eventName, object data)
    {
        if(eventName == "TutorialGachaBtnClick" && m_DarwInfo.GachaId == ConfigTableMgr.instance.GetConfigData("TutorialGachaListItemId"))
        {
            ItemInfo PieceItemInfo = ItemTableMgr.instance.GetItemData(ConfigTableMgr.instance.GetConfigData("GetTutorialItemId"));
            int Count = ConfigTableMgr.instance.GetConfigData("GetTutorialItemIdCount");
            PlayerData.instance.GachaEffectStart(BoxEffect, WhilteEffect, PieceItemInfo, Count, StageShop.m_GetBackGround(), GetGachaEffect());

        }

        return true;
    }

    public override void OnBtnClickEvent(GameObject go)
    {
        base.OnBtnClickEvent(go);
        switch (go.name)
        {
            case "sp_obj_DrawItem":
                string popup = "";
                int price = m_DarwInfo.Price;

                string infoText = "";

                // 상점에서 특수하게 전체화면을 가리는 팝업 구성 - 별도의 구매 팝업이 필요 한 경우.
                // 캐릭터만 뽑을 수 있는 특별한 가챠 상품! (테이블에 id 지정되어 있습니다.)
                if (m_DarwInfo.GachaId > 10000 && 20000 >= m_DarwInfo.GachaId)
                {
                    //popup = string.Format("<color=#059cff>{0} 다이아몬드</color>로 캐릭터 확정뽑기를 구매하시겠습니까?", price.ToString());
                    popup = LocalizationMgr.GetMessage(12309, price);
                    infoText = LocalizationMgr.GetMessage(12308);
                    string image = "";
                    if (GameDefine.SelectedLanguage == LocalizationMgr.ELanguage.Korean)
                        image = "image_gacha_char";
                    else
                        image = "image_gacha_char_en";

                    OnGachaShopPopup("StageGachaCharacterPopup", image, price, popup, infoText);
                }
                else if (m_DarwInfo.GachaId > 400000 && 500000 >= m_DarwInfo.GachaId)   // 마스터 버디 뽑기
                {
                    //popup = LocalizationMgr.GetMessage(50102, m_DarwInfo.Name, price, ItemTableMgr.instance.GetItemsByType(m_DarwInfo.PriceType)[0].Name);
                    //popup = string.Format("<color=#059cff>{0} 다이아몬드</color>로 마스터 버디를 구매하시겠습니까?", price.ToString());
                    popup = LocalizationMgr.GetMessage(12312, price);
                    infoText = LocalizationMgr.GetMessage(12311);
                    string image = "";
                    if (GameDefine.SelectedLanguage == LocalizationMgr.ELanguage.Korean)
                        image = "image_gacha_buddy";
                    else
                        image = "image_gacha_buddy_en";

                    OnGachaShopPopup("StageGachaMasterBuddyPopup", image, price, popup, infoText);
                }
                else
                {
                    if (m_DarwInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Material)
                    {
                        popup = LocalizationMgr.GetMessage(50110,
                                                price, 
                                                ItemTableMgr.instance.GetItemsByType(m_DarwInfo.PriceType)[0].Name,
                                                m_DarwInfo.Name,
                                                GetMaterialCount());
                    }
                    else
                    {
                        if (FreeByNow)
                            popup = LocalizationMgr.GetMessage(50077, m_DarwInfo.Name);
                        else
                            popup = LocalizationMgr.GetMessage(50013, m_DarwInfo.Name, price, ItemTableMgr.instance.GetItemsByType(m_DarwInfo.PriceType)[0].Name);
                    }
                    PopUpUpdate(popup);
                }
                break;

            case "sp_obj_NotDrawItem":

                string popupText = "";

                if (m_ShopType == StageShop.TabState.Diamond)
                {
#if !UNITY_EDITOR
                    StageMgr.instance.SetLoadingIndicator(true, StageMgr.LoadingIndicatorLayer.UI);
#endif
                    popupText = LocalizationMgr.GetMessage(50013, m_DiamondInfo.Name, sp_txt_ItemCostDigit.text, "");
                }
                else
                {
                    ItemInfo BuddyCheck = ItemTableMgr.instance.GetItemData(m_ShopInfo.ItemId);
                    if(BuddyCheck.ItemType == ItemTableMgr.ItemInfo_ItemType.Buddy)
                    {
                        StageMgr.instance.OpenPopup("StageGachaAndUnLockResult", UTIL.Hash("Type", BuddyCheck, "Count", 1, "ResultType", "BuddyBuyPopup", "BuddyAbility", 0,"BuyCallback",(CallBack)BuyingItem), (string str) =>
                        {
                        });

                        return;
                    }

                    popupText = LocalizationMgr.GetMessage(50013, m_ShopInfo.Name, m_ShopInfo.Price, ItemTableMgr.instance.GetItemsByType(m_ShopInfo.PriceType)[0].Name);
                }

                PopUpUpdate(popupText);

                break;

        }
    }

    #endregion

    #region Function


    public void Clear()
    {

    }

    bool IsMasterBuddyDraw(StageShop.TabState state)
    {
        return state == StageShop.TabState.BuddyItem && m_DarwInfo != null;
    }

    public void Set(StageShop.TabState state, ShopInfo info, int index, GachaInfo drawinfo = null, InAppPurchaseTableMgr.InAppPurchase_Type inapptype = InAppPurchaseTableMgr.InAppPurchase_Type.Cash, InAppPurchaseInfo diamondinfo = null)
    {
        if(info != null)
        {
            m_ShopInfo = info;
        }
        else if(drawinfo != null)
        {
            m_DarwInfo = drawinfo;
        }
        else if (diamondinfo != null)
        {
            m_DiamondInfo = diamondinfo;
        }

        if (sp_txt_DrawItemName == null)
        {
            m_ShopType = state;
            ItemIndex = index;
            m_InAppItemType = inapptype;
            return;
        }

        if (rt_img == null)
            rt_img = sp_img_Icon.transform.GetComponent<RectTransform>();

        sp_img_QuantityBg.gameObject.SetActive(state == StageShop.TabState.BuddyItem && m_ShopInfo != null);

        if (state == StageShop.TabState.Buddy)
        {
            rt_img.sizeDelta = Vector2.one * 90f;
        }
        else if (state == StageShop.TabState.BuddyItem && m_ShopInfo != null)
        {
            PlayerData.UserItem item = PlayerData.instance.GetUserConsumableInfo_Id(m_ShopInfo.ItemId);
            sp_txt_Quantity.text = item == null ? "0" : item.Number.ToString();
            rt_img.sizeDelta = Vector2.one * 102f;
        }

        if (state == StageShop.TabState.Draw || IsMasterBuddyDraw(state))
        {
            sp_obj_DrawItem.SetActive(true);
            shelf = sp_img_DrawShelfImg;

            SetDrawMode(drawinfo);
        }
        else
        {
            sp_obj_NotDrawItem.SetActive(true);
            shelf = sp_img_ShelfImg;


            if (state != StageShop.TabState.Diamond && info != null)
            {
                sp_img_Icon.sprite = DataMgr.instance.GetSprite(m_ShopInfo.Icon);
                sp_txt_ItemName.text = m_ShopInfo.Name;
                sp_txt_ItemDesc.text = m_ShopInfo.Description;
                sp_img_ItemPriceIcon.gameObject.SetActive(true);

                if(m_ShopInfo.Prices_SaleFactor != 0)
                {                  
                    sp_obj_SaleImag.SetActive(true);
                }
                else
                {
                    sp_obj_SaleImag.SetActive(false);
                }

                sp_txt_ItemCostDigit.text = string.Format("{0:#,##0}", m_ShopInfo.PriceApplyEventFactor);

                //if (m_ShopInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Cash)
                //{
                //    sp_img_ItemPriceIcon.sprite = DataMgr.instance.GetSprite(StringConfigTableMgr.instance.GetStringConfigData("ShopCashIcon"));
                //}
                //else if (m_ShopInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Coin)
                //{
                //    sp_img_ItemPriceIcon.sprite = DataMgr.instance.GetSprite(StringConfigTableMgr.instance.GetStringConfigData("ShopCoinIcon"));

                //}
                //else

                sp_img_ItemPriceIcon.sprite = DataMgr.instance.GetSprite(ItemTableMgr.instance.GetItemsByType(m_ShopInfo.PriceType)[0].Icon);


            }
            else {
                SetDiamondMode(diamondinfo);
            }


        }

        if (ItemIndex % 2 == 0)
            shelf.sprite = DataMgr.instance.GetSprite(RShelfImgName);
        else
            shelf.sprite = DataMgr.instance.GetSprite(LShelfImgName);
    }

    void SetDiamondMode(InAppPurchaseInfo diamondinfo)
    {
        if(diamondinfo != null)
        {
#if UNITY_IPHONE
            sp_txt_ItemCostDigit.text = LocalizationMgr.GetMessage(10606, m_DiamondInfo.PriceUs);
#else
            if (GameDefine.SelectedLanguage == LocalizationMgr.ELanguage.Korean)
            {
                int index = 0;
                string str = m_DiamondInfo.PriceKr.ToString();
                for (int i = str.Length - 1; i >= 0; i--)
                {
                    index++;
                    if (index % 3 == 0 && i != 0)
                    {
                        str = str.Insert(i, ",");
                    }
                }

                sp_txt_ItemCostDigit.text = LocalizationMgr.GetMessage(10613, str);

            }
            else
                sp_txt_ItemCostDigit.text = LocalizationMgr.GetMessage(10606, string.Format("{0:#,##0}", m_DiamondInfo.PriceUs));
#endif

            sp_img_Icon.sprite = DataMgr.instance.GetSprite(m_DiamondInfo.Icon);
            sp_txt_ItemName.text = m_DiamondInfo.Name;
            if (m_DiamondInfo.Description != null)
                sp_txt_ItemDesc.text = m_DiamondInfo.Description;
            else
                sp_txt_ItemDesc.text = " ";

            CheckFirches();
        }
    }

    void CheckFirches()
    {
        if (PlayerData.instance.IsFirstPurchase())
        {
            sp_img_FirstBuy.gameObject.SetActive(true);
        }
        else
            sp_img_FirstBuy.gameObject.SetActive(false);
    }

    void SetOutLine(Text txt, Color32 color)
    {
        Outline[] line1 = txt.gameObject.GetComponents<Outline>();

        for (int i = 0; i < line1.Length; i++)
        {
            line1[i].effectColor = color;
        }
    }

    void SetDrawMode(GachaInfo drawinfo)
    {
        if(drawinfo != null)
        {

            sp_img_DrawIcon.sprite = DataMgr.instance.GetSprite(m_DarwInfo.Icon);
            sp_txt_DrawItemName.text = m_DarwInfo.FreeCoolTime == 0 ? "<color=#00FFFCFF>" + m_DarwInfo.Name +"</color>":  m_DarwInfo.Name;
            sp_txt_DrawDesc.text = m_DarwInfo.FreeCoolTime == 0 ? "<color=#FFF000FF>" + m_DarwInfo.Description + "</color>" : m_DarwInfo.Description;

            sp_img_DrawIcon.transform.localPosition = new Vector3(0f, -0.5f, 0f);

            // 캐릭터 가챠일 경우, 특별하게 이미지 크기가 달라야 한다.
            if ((drawinfo.GachaId > 10000 &&  20000 >= drawinfo.GachaId) || drawinfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Material)
            {
                sp_img_DrawIcon.SetNativeSize();
                sp_img_DrawIcon.transform.localPosition = new Vector3(0f, 23f, 0f);
            }

            if(drawinfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Material)
            {
                sp_img_DrawCostImg.rectTransform.sizeDelta = new Vector2(54f, 54f);
                sp_img_DrawCostImg.transform.localPosition = new Vector3(sp_img_DrawCostImg.transform.localPosition.x,
                                                                         -73f,
                                                                         sp_img_DrawCostImg.transform.localPosition.z);
            }

            if(m_DarwInfo.FreeCoolTime == 0)
            {
                SetOutLine(sp_txt_DrawItemName, new Color32(0, 55, 80,255));
                SetOutLine(sp_txt_DrawDesc, new Color32(112, 47, 0, 255));
            }

            if (m_DarwInfo.Prices_SaleFactor != 0)
            {
                sp_obj_DrawSaleImg.SetActive(true);
            }
            else
            {
                sp_obj_DrawSaleImg.SetActive(false);

            }
            sp_txt_DrawCostDigit.text = string.Format("{0:#,##0}", m_DarwInfo.PriceApplyEventFactor);

            if (m_DarwInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Cash)
            {
                sp_obj_DrawLimited.SetActive(true);
              //  sp_img_DrawCostImg.sprite = DataMgr.instance.GetSprite(StringConfigTableMgr.instance.GetStringConfigData("ShopCashIcon"));

            }

            sp_img_DrawCostImg.sprite = DataMgr.instance.GetSprite(ItemTableMgr.instance.GetItemsByType(m_DarwInfo.PriceType)[0].Icon);

            CoolTime();
        }

    }

    //이함수로 쿨타임계산.
    public void CoolTime()
    {
        if (sp_txt_DrawCoolTime == null) return;

        if (m_DarwInfo.FreeCoolTime == 0)
        {
            FreeByNow = false;
            sp_txt_DrawCoolTime.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            System.DateTime time = PlayerData.instance.GetFreeGachaCoolTime(m_DarwInfo.GachaId);

            if(time == System.DateTime.MinValue)
            {
                FreeByNow = true;
                sp_txt_DrawCoolTime.transform.parent.gameObject.SetActive(false);
                sp_txt_DrawCostDigit.text = LocalizationMgr.GetMessage(11704);
            }
            else
            {

                System.DateTime _coolTime = time;
                _coolTime = _coolTime.AddSeconds((double)m_DarwInfo.FreeCoolTime);

                if (PlayerData.instance.m_ServerDateTime < _coolTime)
                {
                    System.TimeSpan _remainTime = _coolTime - PlayerData.instance.m_ServerDateTime;

                    if(_NowTime == System.DateTime.MinValue)
                    {
#if GSP_PLATFORM_GLOBAL
                        _NowTime = System.DateTime.UtcNow;
#else
                        _NowTime = System.DateTime.Now;
#endif
                    }
#if GSP_PLATFORM_GLOBAL
                    System.TimeSpan _remainTime2 = System.DateTime.UtcNow - PlayerData.instance.m_ServerDateTime;
#else
                    System.TimeSpan _remainTime2 = System.DateTime.Now - PlayerData.instance.m_ServerDateTime;
#endif

                    if (_remainTime2 < _remainTime)
                    {
                        _remainTime = _remainTime - _remainTime2;

                        

                        int Hour = _remainTime.Hours;
                        int Minute = _remainTime.Minutes;
                        int Day = _remainTime.Days;
                        int seconds = _remainTime.Seconds;

                      
                        if (Day > 0)
                        {
                            sp_txt_DrawCoolTime.text = LocalizationMgr.GetMessage(10608, Day, Hour);
                           
                        }
                        else if (Hour > 0)
                        {

                            sp_txt_DrawCoolTime.text = LocalizationMgr.GetMessage(10609, Hour, Minute);

                        }
                        else if (Minute > 0)
                        {
                            sp_txt_DrawCoolTime.text = LocalizationMgr.GetMessage(10611, Minute, seconds);

                        }
                        else if (seconds > 0)
                        {
                            sp_txt_DrawCoolTime.text = LocalizationMgr.GetMessage(10610, seconds);
                        }

                        if (sp_txt_DrawCoolTime.transform.parent.gameObject.activeSelf == false)
                        {
                            sp_txt_DrawCoolTime.transform.parent.gameObject.SetActive(true);
                            if (m_DarwInfo.Prices_SaleFactor != 0)
                            {                        
                                sp_obj_DrawSaleImg.SetActive(true);
                            }
                            else
                            {
                                sp_obj_DrawSaleImg.SetActive(false);

                            }

                            sp_txt_DrawCostDigit.text = string.Format("{0:#,##0}", m_DarwInfo.PriceApplyEventFactor);
                        }


                    }
                    else
                    {
                        FreeByNow = true;
                        sp_txt_DrawCoolTime.transform.parent.gameObject.SetActive(false);
                        sp_txt_DrawCostDigit.text = LocalizationMgr.GetMessage(11704);

                        

                    }

                }
                else
                {
                    FreeByNow = true;
                    sp_txt_DrawCoolTime.transform.parent.gameObject.SetActive(false);
                    sp_txt_DrawCostDigit.text = LocalizationMgr.GetMessage(11704);
                }


            }
        }

    }

    //이 변수가 true가 나오면 update그만돌기.
    public bool GetFreeByNow()
    {
        if (m_DarwInfo.FreeCoolTime == 0)
            return true;

        return FreeByNow;
    }

    void PopUpUpdate(string popupText)
    {
        if (m_ShopType == StageShop.TabState.Diamond)
        {
            InAppBuying();
        }
        else
        {
            StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "TwoBtn", "Text", popupText), (string str) =>
            {
                if (str == "Yes")
                {
                    if (m_ShopType == StageShop.TabState.Draw || IsMasterBuddyDraw(m_ShopType))
                    {
                        Gacha();
                    }
                    else
                    {
                        BuyingItem();
                    }
                }
                else if (str == "No")
                {
                    CLog.Log(str);
                }

            });
        }
    }

    /// <summary>
    /// 가챠 상품 - 확정 캐릭터 뽑기 팝업 / 마스터 버디 뽑기 팝업
    /// </summary>
    /// <param name="popupText"></param>
    private void OnGachaShopPopup (string stageName, string imageName, int price, string priceText, string infoText)
    {
        StageMgr.instance.OpenPopup(stageName, UTIL.Hash("Price", price, "Text", priceText, "InfoText", infoText, "Image", imageName), (string str) =>
        {
            if(str == "Yes")
            {
                Gacha();
            }
            else if(str == "No")
            {
                CLog.Log(str);
            }
        });
    }

    void InAppBuying()
    {
      //  if (StageShop.m_UserInfoRefresh != null)
       // StageShop.m_UserInfoRefresh();

       // refreshInApp();

        switch (m_InAppItemType) {
            case InAppPurchaseTableMgr.InAppPurchase_Type.Cash:
                if(!UTIL.IsEditor() && m_DiamondInfo != null)
                {
#if UNITY_ANDROID
                    NativeHelperContainer.iapHelper.PurchaseProduct(m_DiamondInfo.Id.ToString());
#elif UNITY_IPHONE
                    NativeHelperContainer.iapHelper.PurchaseProduct(m_DiamondInfo.Id.ToString());
#endif

                }
                break;
            case InAppPurchaseTableMgr.InAppPurchase_Type.Package:
                break;

        }

    }


    private int GetMaterialCount ()
    {
        List<PlayerData.UserItem> userItem = PlayerData.instance.GetUserMaterialList();
        for (int i = 0; i < userItem.Count; i++)
        {
            // 12 : 신비한 물질
            if (userItem[i].ItemId == 12)
            {
                return userItem[i].Number;
            }
        }
        return 0;
    }

    private void CheckSetCharacterGuidQuest()
    {
        bool clearCheck = PlayerData.instance.CheckOpenLimit(60010);
        if (clearCheck)
        {
            List<QuestInfo> list = QuestTableMgr.instance.GetQuestListByType(QuestTableMgr.Quest_Type.Guide);
            QuestInfo info = list.Find(item => item.Id == 60010);
            if (info != null)
            {
                bool userGuidCheck = CheckGuidQuest(info);
                if (userGuidCheck)
                {
                    List<UserBehavior> questList = new List<UserBehavior>();
                    questList.Add(new UserBehavior(info.Postcondition.Type, info.Id, info.Postcondition.Id, info.Postcondition.Value));
                    PlayerDataMgr.instance.UpdateQuest(questList, (TangentFramework.HttpNetwork.Packet p, bool success) =>
                    {
                        if (success)
                        {
                        }
                    });
                }
            }
        }

    }

    private bool CheckGuidQuest(QuestInfo Info)
    {
        if (PlayerData.instance.IsCompleteAbleQuest(Info.Id))
            return false;

        int charCount = 0;
        int curID = PlayerData.instance.GetCurCharacterID();

        if (curID > 0)
            charCount++;

        List<PlayerData.UserCharacterItem> relayList = PlayerData.instance.m_RelayCharacterList;
        foreach (var list in relayList)
        {
            if (list != null)
                charCount++;
        }

        if (Info.Postcondition.Type == QuestTableMgr.PostconditionType.SelectCharacter && Info.Type == QuestTableMgr.Quest_Type.Guide)
        {
            if (Info.Postcondition.Id == 0)
            {
                if (charCount >= Info.Postcondition.Value)
                    return true;
            }
        }

        return false;
    }

    void Gacha()
    {
        if ((m_DarwInfo.GachaId <= (int)GachaType.Treasure || m_DarwInfo.GachaId == 5) && PlayerData.instance.m_TreasureInvenNumber <= PlayerData.instance.GetUserTreasureList().Count) // 인벤에 여유가 없으면.
        {
            //Local : 인벤이 꽉찼습니다.
            StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(50005)), (string str) =>
            {

            });

        }
        else
        {
            long PlayerMoney = 0;
            int price = m_DarwInfo.Price;
            string strr = "";
            int itemtype = 0;
            GachaPriceType type = GachaPriceType.CoinAndCash;
            StageShop.TabState tabstate = StageShop.TabState.Gold;

            if (m_DarwInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Cash)
            {
                PlayerMoney = PlayerData.instance.Cash;
                tabstate = StageShop.TabState.Diamond;
                strr = "Diamond"; // Local :보석
                itemtype = (int)ItemTableMgr.ItemInfo_ItemType.Cash;
            }
            else if (m_DarwInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Coin)
            {
                PlayerMoney = PlayerData.instance.Coin;
                strr = "Gold"; // Local : 코인
                itemtype = (int)ItemTableMgr.ItemInfo_ItemType.Coin;
            }
            else if (m_DarwInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Material)
            {
                PlayerMoney = GetMaterialCount();

                tabstate = StageShop.TabState.Draw;
                strr = "Material";
                itemtype = 11; // 신비한 물질 - 1
            }

            if (FreeByNow) price = 0;

            if (PlayerMoney < price)
            {
                if (m_DarwInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Material)
                {
                    StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "OneBtn", "Text", LocalizationMgr.GetMessage(50111, ItemTableMgr.instance.GetItemData(itemtype + 1).Name)), (string str) => {});
                }
                else
                {
                    // Local : 부족
                    StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", "TwoBtn", "Text", LocalizationMgr.GetMessage(50025, ItemTableMgr.instance.GetItemData(itemtype + 1).Name)), (string str) =>
                    {
                        if (str == "Yes")
                        {
                            if (StageShop.m_SetShopUIOn != null)
                            {
                                StageShop.m_SetShopUIOn(tabstate);
                            }
                        }
                    });
                }
            }
            else
            {               

                if (FreeByNow)
                    type = GachaPriceType.Free;


                PlayerDataMgr.instance.Gacha(m_DarwInfo.GachaId, (int)type, (TangentFramework.HttpNetwork.Packet p, bool success) =>
                {
                    if (success)
                    {
                        string _strLabel = "";
                        if (m_DarwInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.Coin)
                            _strLabel = "Coin";
                        else
                            _strLabel = "Cash";
                        GAHelper.GALogEvent(GAHelper.GAEventCategory.Shop, "Draw", _strLabel, m_DarwInfo.GachaId);

                        if (StageShop.m_UserInfoRefresh != null)
                            StageShop.m_UserInfoRefresh();

                     
                        CustomNetwork.ExtractTreasureData treasure = p as CustomNetwork.ExtractTreasureData;

						if (m_DarwInfo.GachaId > 400000 && 500000 >= m_DarwInfo.GachaId)
                        {
#if NEW_NETWORK_MODULE
							string buddyUID = treasure.getitemlist.i;
#else
							string buddyUID = treasure.getitemlist.ItemId;
#endif

							PlayerDataMgr.instance.GetBuddyList((TangentFramework.HttpNetwork.Packet p2, bool success2) =>
                            {
                                if (success2)
                                {
                                    PlayerData.UserBuddyItem buddyInfo = PlayerData.instance.GetBuddyDataByUID(long.Parse(buddyUID));
                                    ItemInfo itemInfo = ItemTableMgr.instance.GetItemData(buddyInfo.ItemId);
                                    PlayerData.instance.ShopGachaEffectStart(BoxEffect, WhilteEffect, itemInfo, buddyInfo, 1, StageShop.m_GetBackGround(), GetGachaEffect(), true, 0);
                                }
                            });
                        }
                        else
                        {
#if NEW_NETWORK_MODULE
							int[] itemlist = Utils.ConvertToIntList(treasure.getitemlist.i);
							int[] countlist = Utils.ConvertToIntList(treasure.getitemlist.c);
							int itemCount = int.Parse(treasure.getitemlist.c);
							int ChangeID = treasure.getitemlist.n;
#else
							int[] itemlist = Utils.ConvertToIntList(treasure.getitemlist.ItemId);
							int[] countlist = Utils.ConvertToIntList(treasure.getitemlist.Count);
                            int itemCount = int.Parse(treasure.getitemlist.Count);
                            int ChangeID = treasure.getitemlist.ChangeItem;

#endif
							
							for (int i = 0; i < itemlist.Length; i++)
                            {
                                ItemInfo info = ItemTableMgr.instance.GetItemData(itemlist[i]);

                                CLog.Log("item info : " + info.ItemType);

                                if (info != null)
                                {
                                    if (info.ItemType == ItemTableMgr.ItemInfo_ItemType.Character)
                                    {
                                        PlayerDataMgr.instance.GetCharacterSkillList((TangentFramework.HttpNetwork.Packet p2, bool success2) =>
                                        {
                                            if (success2)
                                            {
                                                CLog.Log("success : " + i);
                                                PlayerData.instance.ShopGachaEffectStart(BoxEffect, WhilteEffect, info, null, itemCount, StageShop.m_GetBackGround(), GetGachaEffect(), true, ChangeID);


                                                CLog.Log("effect success : " + i);
                                                UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.CollectCount_Characters, 1, info.ItemId, "", false); // 캐릭터 획득 갯수 요구 Quest : soojin

                                                CheckSetCharacterGuidQuest();
                                            }

                                        });
                                    }
                                    else if (info.ItemType == ItemTableMgr.ItemInfo_ItemType.Buddy)
                                    {
                                        PlayerDataMgr.instance.GetBuddyList((TangentFramework.HttpNetwork.Packet p2, bool success2) =>
                                        {
                                            if (success2)
                                            {
                                            // 2016.12.26 마스터 버디 연출 관련 이전 버전으로 롤백함.
                                            /*
											GameObject effect1 = StageLobby.instance.GetEffect_NeedToCommonEffect_Handler("UI_Egg", new Vector3(1f, 15.3f, 77.5f), Vector3.one * 5);
											GameObject effect2 = StageLobby.instance.GetEffect_NeedToCommonEffect_Handler("FX_MasterBuddy_egg", new Vector3(1f, 15.3f, 0f), Vector3.one);
											PlayerData.instance.GachaEffectStart(effect1, effect2, info, 1, StageShop.m_GetBackGround(), GetGachaEffect());
											*/
                                                PlayerData.instance.GachaEffectStart(BoxEffect, WhilteEffect, info, 1, StageShop.m_GetBackGround(), GetGachaEffect());
                                            }
                                        });
                                    }
                                    else
                                    {
                                        PlayerData.instance.GachaEffectStart(BoxEffect, WhilteEffect, info, countlist[i], StageShop.m_GetBackGround(), GetGachaEffect());
                                    }

                                    if (info.ItemType == ItemTableMgr.ItemInfo_ItemType.Treasure)
                                    {
                                        UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.CollectCount_Treasures, countlist[i], info.ItemId, "", false); // 보물 획득 갯수 요구 Quest : soojin

                                    }

                                    if (m_DarwInfo.GachaId <= (int)GachaType.Treasure)
                                        UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.GetGachaTreasure, countlist[i], info.ItemId, "", false);
                                }


                            }
                        }

                        _NowTime = System.DateTime.Now;

                        if (FreeByNow)
                            FreeByNow = false;

                        JaykayFramework.ListenerManager.instance.SendDirectEvent("ShopAlarm", (object)StageShop.TabState.Draw);
                    }
                });
            }

        }
    }

    List<GameObject> GetGachaEffect()
    {
        int gachaId = m_DarwInfo.GachaId;
        if (gachaId >= 5)
        {
            gachaId = 4;
        }

        GameObject obj1 = BoxEffect.transform.FindChild("UI_box/Dummy002/Dummy003/UI_box_basic_up" + gachaId).gameObject;
        GameObject obj2 = BoxEffect.transform.FindChild("UI_box/Dummy002/Dummy004/UI_box_basic_skull" + gachaId).gameObject;
        GameObject obj3 = BoxEffect.transform.FindChild("UI_box/Dummy002/Dummy005/UI_box_basic_handle" + gachaId).gameObject;
        GameObject obj4 = BoxEffect.transform.FindChild("UI_box/Dummy002/Dummy006/UI_box_basic_skull_R" + gachaId).gameObject;
        GameObject obj5 = BoxEffect.transform.FindChild("UI_box/Dummy002/Dummy007/UI_box_basic_skull_L" + gachaId).gameObject;
        GameObject obj6 = BoxEffect.transform.FindChild("UI_box/Dummy002/Dummy008/UI_box_basic_gold" + gachaId).gameObject;
        GameObject obj7 = BoxEffect.transform.FindChild("UI_box/Dummy002/UI_box_basic_down" + gachaId).gameObject;

        List<GameObject> effecobj = new List<GameObject>();
        effecobj.Add(obj1);
        effecobj.Add(obj2);
        effecobj.Add(obj3);
        effecobj.Add(obj4);
        effecobj.Add(obj5);
        effecobj.Add(obj6);
        effecobj.Add(obj7);

        for (int i = 0; i < effecobj.Count; i++)
        {
            effecobj[i].SetActive(true);
        }

        return effecobj;

    }


    //IEnumerator GachaEffect(ItemInfo info, int count)
    //{
    //    BoxEffect.SetActive(true);

    //    yield return new WaitForSeconds(1f);

    //    WhilteEffect.SetActive(true);


    //    yield return new WaitForSeconds(1.7f);

    //    BoxEffect.SetActive(false);
    //    WhilteEffect.SetActive(false);

    //    StageMgr.instance.OpenPopup("StageGachaAndUnLockResult", UTIL.Hash("Type", info, "Count", count, "ResultType", "Gacha"), (string str) =>
    //    {

    //    });


    //}

    void BuyingItem()
    {
        long PlayerMoney = 0;
        string strr = "";
        StageShop.TabState tabstate = StageShop.TabState.Gold;

        switch (m_ShopInfo.PriceType) {
            case ItemTableMgr.ItemInfo_ItemType.Cash:
                PlayerMoney = PlayerData.instance.Cash;
                strr = ItemTableMgr.instance.GetItemData((int)ItemTableMgr.ItemInfo_ItemType.Cash + 1).Name; // Local :보석
                tabstate = StageShop.TabState.Diamond;
                break;

            case ItemTableMgr.ItemInfo_ItemType.SocialPoint:
                PlayerMoney = PlayerData.instance.m_SocialPoint;
                strr = LocalizationMgr.GetMessage(50014); // Local :소셜포인트
                tabstate =  StageShop.TabState.None;

                break;

            case ItemTableMgr.ItemInfo_ItemType.Coin:
                PlayerMoney = PlayerData.instance.Coin;
                strr = ItemTableMgr.instance.GetItemData((int)ItemTableMgr.ItemInfo_ItemType.Coin + 1).Name; // Local :소셜포인트
                tabstate = StageShop.TabState.Gold;
                break;

        }

        if (m_ShopInfo.Price <= PlayerMoney)
        {
            //구매함수.
            PlayerDataMgr.instance.BuyShopItem(m_ShopInfo.ShopId,(TangentFramework.HttpNetwork.Packet p, bool success) =>
            {
                if (success)
                {
                    string _strAction = "";
                    if (m_ShopInfo.Type == ShopTableMgr.ShopType.Coin)
                        _strAction = "Coin";
                    else if (m_ShopInfo.Type == ShopTableMgr.ShopType.Stamina)
                    {
                        if(m_ShopInfo.PriceType == ItemTableMgr.ItemInfo_ItemType.SocialPoint)
                            UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.SocialBuyHeart, 1, 3);
                        _strAction = "Heart";
                    }
                    else if(m_ShopInfo.Type == ShopTableMgr.ShopType.BuddyItem)
                    {
                        PlayerData.UserItem item = PlayerData.instance.GetUserConsumableInfo_Id(m_ShopInfo.ItemId);
                        sp_txt_Quantity.text = item == null ? "0" : item.Number.ToString();
                    }
                    else if(m_ShopInfo.Type == ShopTableMgr.ShopType.Buddy)
                    {
                        CustomNetwork.BuyShopInfo buyshop = p as CustomNetwork.BuyShopInfo;

                        if(buyshop.buddy_info != null && buyshop.buddy_info.Length > 0)
                        {
#if NEW_NETWORK_MODULE
							ItemInfo info = ItemTableMgr.instance.GetItemData(buyshop.buddy_info[0].i);
#else
							ItemInfo info = ItemTableMgr.instance.GetItemData(buyshop.buddy_info[0].ItemId);
#endif

							PlayerDataMgr.instance.GetBuddyList((TangentFramework.HttpNetwork.Packet p1, bool success1) =>
                            {
                                if (success1)
                                {
#if NEW_NETWORK_MODULE
									StageMgr.instance.OpenPopup("StageGachaAndUnLockResult", UTIL.Hash("Type", info, "Count", 1, "ResultType", "Buddy", "BuddyAbility", buyshop.buddy_info[0].a0), (string str) =>
#else
									StageMgr.instance.OpenPopup("StageGachaAndUnLockResult", UTIL.Hash("Type", info, "Count", 1, "ResultType", "Buddy", "BuddyAbility", buyshop.buddy_info[0].Level0_AbilityID), (string str) =>
#endif
									{
                                    });
                                }
                            });
                           
                        }
                    }
                      
                    if (m_ShopInfo.Type == ShopTableMgr.ShopType.ExpPotion)
                        _strAction = "Exp";
                    if(!string.IsNullOrEmpty(_strAction))
                        GAHelper.GALogEvent(GAHelper.GAEventCategory.Shop, _strAction, "", m_ShopInfo.ShopId);

                    switch (m_ShopType)
                    {
                        case StageShop.TabState.Diamond:
                            InAppPurchaseInfo purchaseInfo = InAppPurchaseTableMgr.instance.GetInAppPurchaseInfo(m_ShopInfo.ShopId);
                            UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.BuyCount_SpecificItem, purchaseInfo.ProductItems[0].Number, 2);
                            break;

                        case StageShop.TabState.Draw:
                            break;

                        default:
                            ShopInfo shopInfo = ShopTableMgr.instance.GetShopInfo(m_ShopInfo.ShopId);
                            UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.BuyCount_SpecificItem, shopInfo.Number, shopInfo.ItemId);
                            break;
                    }

                    JaykayFramework.ListenerManager.instance.SendDirectEvent("ShopAlarm", (object)m_ShopType);

                    if (StageShop.m_UserInfoRefresh != null)
                        StageShop.m_UserInfoRefresh();
                }
            });

        }
        else
        {
            string btntype = "";
            int Docsnum = 0;
            if(tabstate == StageShop.TabState.None)
            {
                btntype = "OneBtn";
                Docsnum = 50044;
            }
            else
            {
                btntype = "TwoBtn";
                Docsnum = 50025;
            }

            StageMgr.instance.OpenPopup("StageBasicPopup", UTIL.Hash("Type", btntype, "Text", LocalizationMgr.GetMessage(Docsnum, strr)), (string str) =>
            {
                if (tabstate != StageShop.TabState.None)
                {
                    if (str == "Yes")
                    {
                        if (StageShop.m_SetShopUIOn != null)
                        {
                            StageShop.m_SetShopUIOn(tabstate);
                        }
                    }
                }
            });

        }

    }

#endregion
}
