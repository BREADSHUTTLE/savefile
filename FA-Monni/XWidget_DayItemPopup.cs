using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using I2.Loc;

public class XWidget_DayItemPopup : MonoBehaviour
{
    //public UILabel lblGroup;
    public XWiget_GridBuilder btnGridBuilder;
    public UI2DSprite sprOneItemIcon;
    public UI2DSprite sprMultiItemIcon;
    public XWidget_Button btnOne;

    //private List<CS.CSShopItem> m_popupList = new List<CS.CSShopItem>();
    private Dictionary<int, List<CS.CSShopItem>> m_popupList = new Dictionary<int, List<CS.CSShopItem>>();   // 팝업상품그룹, 해당아이템
    private Action m_queue = null;

    private CS.CSShopItem m_shopItem;
    private bool isOnButtonClick = false;

    public UIToggle checkNotDay_one;
    public UIToggle checkNotDay_multi;

    private XEventShopLogic eventShopLogic = new XEventShopLogic();

    public GameObject goOne;
    public GameObject goMulti;

    public void Show(Action queue)
    {
        m_queue = queue;
        checkNotDay_one.gameObject.SetActive(true);
        checkNotDay_multi.gameObject.SetActive(true);
        Build();
    }

    public void Show(List<CS.CSShopItem> itemInfo, Action queue)
    {
        m_queue = queue;
        checkNotDay_one.gameObject.SetActive(false);
        checkNotDay_multi.gameObject.SetActive(false);
        Build(itemInfo);
    }

    public void Build(List<CS.CSShopItem> itemInfo)
    {
        if (itemInfo == null)
            return;

        gameObject.SetActive(true);
        Refresh(itemInfo);
    }

    public void Build()
    {
        // TEST
        //XGlobal.LocalData.DayItemPopupLastTime = DateTime.Parse(CS.CSConst.INIT_DATE_TIME);
        //XGlobal.LocalData.PopupShopLastTimeList.Clear();
        //XGlobal.LocalData.PopupShopLastTimeList[700110] = new DateTime(2016, 4, 10, 0, 0, 0);
        //XLocalDataSaver.Save();

        BuildDayPopupList();

        if (m_popupList.Count > 0)
        {
            Refresh(m_popupList.First().Value);
            gameObject.SetActive(true);
        }
        else
        {
            if (m_queue != null)
                m_queue();
        }
    }

    private void Close()
    {
        if (m_popupList.Count <= 0)
        {
            if(m_queue != null)
                m_queue();
            Hide();
            return;
        }

        var first = m_popupList.First();
        if (first.Key == null)
            return;


        // 같은 그룹으로 묶인 아이템은 출력 초기화 시간이 동일하다는 전제 하에 첫 번째 것을 사용하도록 한다.
        if (m_popupList.First().Value[0].output_timing == CS.ShopPopupTime.Day)
            SaveDayTime();

        if (checkNotDay_one.value == true || checkNotDay_multi.value == true)
        {
            for (int i = 0; i < first.Value.Count; i++)
            {
                if (CheckBoxCondition(first.Value[i], true) == false)
                    CS.CSLog.Error("Failure Check Box Save");    
            }
            
        }

        m_popupList.Remove(first.Key);

        if (m_popupList.Count <= 0)
        {
            //XGlobal.ui.lobby.SetItemPopup(true);
            if (m_queue != null)
                m_queue();
            Hide();
        }
        else
        {
            Refresh(m_popupList.First().Value);
        }
    }

    public void ClosePopup()
    {
        //XGlobal.ui.common.ShowMessageBox("팝업 닫기", "정말로 팝업을 닫으시겠습니까?", XMessageBoxButtons.OKCancel,
        //            () =>
        //            {
                        Close();
                    //});
    }

    public void OnClick_BuyItem()
    {
        //ClosePopup();   // < 결제 시스템 붙기 전까지 임의로 추가

        if (m_shopItem == null)
            return;

        if (isOnButtonClick == true)
            return;

        isOnButtonClick = true;

        if (m_shopItem.strSaleType == "gold")
        {
            if(XGlobal.MyInfo.Gold < m_shopItem.PresentPrice)
            {
                isOnButtonClick = false;
                XGlobal.ui.common.ShowMessageBox(ScriptLocalization.Get("Code/UI_MANAGER_NOT_ENOUGH_GOLD_TITLE"),
                                                ScriptLocalization.Get("Code/UI_MANAGER_NOT_ENOUGH_GOLD_DESC"),
                                                XMessageBoxButtons.OK);
                return;
            }
        }

        if (m_shopItem.strSaleType == "crystal")
        {
            if (XGlobal.MyInfo.Crystal < m_shopItem.PresentPrice)
            {
                isOnButtonClick = false;
                XGlobal.ui.common.ShowMessageBox(ScriptLocalization.Get("Code/UI_MANAGER_NOT_ENOUGH_CRYSTAL_TITLE"),
                                                ScriptLocalization.Get("Code/UI_MANAGER_NOT_ENOUGH_CRYSTAL_DESC"),
                                                XMessageBoxButtons.OK);
                return;
            }
        }


#if !(UNITY_EDITOR)
        if (m_shopItem.strSaleType == "cash")
        {
            XGlobal.billing.PurchaseStoreProductItem(m_shopItem, (JObject json, string token) => 
            {
                //RequestAndReceiveBuyShopItem(item, token);
                //Callback_CrystalPurchased(json, token);
            });
        }
        else
#endif
        {
                string token = "";
#if (UNITY_EDITOR)
                token = "!UNITY_@EDITOR_#DEV";
#endif
                BuyItem(m_shopItem, false, token);
            }
    }

    private void Callback_CrystalPurchased(JObject json, string token)
    {
        var jObj = json[CS.Netdata_BuyShopItem.String];
        CS.Netdata_BuyShopItem acInfo = null;
        if (jObj != null)
            acInfo = jObj.ToObject<CS.Netdata_BuyShopItem>();

        isOnButtonClick = false;

        string strTitle = ScriptLocalization.Get("UI/UI_ShopBuy_Complete_Title");
        XGlobal.ui.common.ShowShopRewardPopup(strTitle, acInfo, acInfo.toMail);

        XNetworkManager.Instance.PurchaseLogToSlack(m_shopItem, "crystal_purchase_succeed", token);

        Close();
    }

    private void BuyItem(CS.CSShopItem item, bool bBuyFree = false, string token = "")
    {
        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_BuyShopItem>(
             param: new CS.CSNetParameters {
                { CS.ApiParamKeys.ProductId, item.nProductID },
                { CS.ApiParamKeys.BuyFree, bBuyFree },
                { CS.ApiParamKeys.Billing_Token, token },
            },
                 callback: (CS.ApiReturnCode errno, string errmsg, JObject json) =>
                 {
                     if (errno == CS.ApiReturnCode.Success)
                     {
                         var jObj = json[CS.Netdata_BuyShopItem.String];
                         CS.Netdata_BuyShopItem acInfo = null;
                         if (jObj != null)
                             acInfo = jObj.ToObject<CS.Netdata_BuyShopItem>();

                         isOnButtonClick = false;

                         if (item.strSaleType == "cash")
                             XNetworkManager.Instance.PurchaseLogToSlack(item, "crystal_adjust_succeed", token);

                         string strTitle = ScriptLocalization.Get("UI/UI_ShopBuy_Complete_Title");
                         XGlobal.ui.common.ShowShopRewardPopup(strTitle, acInfo, acInfo.toMail);

                         Close();
                     }
                 });
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Refresh(List<CS.CSShopItem> item)
    {
        if (item == null)
            return;

        if (goOne != null)
        {
            goOne.SetActive(false);
            goMulti.SetActive(false);
        }

        string btnTitle = ScriptLocalization.Get("UI/UI_BUTTON_ITEM_POPUP");

        if (item.Count <= 1)
        {
            btnOne.Build(btnTitle, BuyButton(item[0]));
            goOne.SetActive(true);
            if (sprOneItemIcon != null)
                XNGUIHelper.SetSpriteAspect(sprOneItemIcon, XGlobal.spriteLoader.LoadInPool(item[0].popup_image), true, false);
        }

        if (item.Count > 1)
        {
            goMulti.SetActive(true);
            var btnItem = btnGridBuilder.Build<XWidget_Button>(item.Count);

            // 어차피 그룹핑 되어있는 것이기에 어떤 것의 이미지를 가져와도 상관없음.
            if (sprMultiItemIcon != null)
                XNGUIHelper.SetSpriteAspect(sprMultiItemIcon, XGlobal.spriteLoader.LoadInPool(item[0].popup_image), true, false);

            for (int i = 0; i < btnItem.Length; i++)
            {
                bool disable = !ShopItemBuy(item[i]);
                SetBuyButtonDisableSprite(btnItem[i], disable, btnItem.Length);
                btnItem[i].Build(btnTitle, BuyButton(item[i]), disable);
            }

            BuildButtonGrid(item.Count);
        }
    }

    private void SetBuyButtonDisableSprite(XWidget_Button button, bool disable, int groupCount)
    {
        Transform design_two = null;
        Transform design_three = null;
        design_two = button.gameObject.transform.FindChild("disable_two");
        design_three = button.gameObject.transform.FindChild("disable_three");

        if (design_two != null)
            design_two.gameObject.SetActive(disable && groupCount < 3);

        if(design_three != null)
            design_three.gameObject.SetActive(disable && groupCount >= 3);
    }

    private void BuildButtonGrid(int groupCount)
    {
        if (groupCount < 3)
            btnGridBuilder.Grid.cellWidth = 475;
        else
            btnGridBuilder.Grid.cellWidth = 322;

        btnGridBuilder.Reposition();
    }

    private bool ShopItemBuy(CS.CSShopItem item)
    {
        if (string.IsNullOrEmpty(item.BuffID) == false)
        {
            string shopBuffId = CS.CSConst.SHOP_BUFF + item.BuffID;
            if (XGlobal.MyInfo.Buff.ContainsKey(shopBuffId))
            {
                return false;
            }
        }
        else
        {
            if (item.Available_count > 0)
            {
                var count = item.Available_count - GetBuyCount(item);
                if (count <= 0)
                    return false;
            }
        }

        return true;
    }

    private int GetBuyCount(CS.CSShopItem item)
    {
        var list = new List<CS.BoughtLimitedGoods>();
        foreach (var each in XGlobal.MyInfo.BoughtLimitedGoodsList)
        {
            if (each.ProductId == item.nProductID)
            {
                list.Add(each);
            }
        }

        return list.Count;
    }

    private Action BuyButton(CS.CSShopItem item)
    {
        return new Action(() =>
        {
            m_shopItem = item;
            OnClick_BuyItem();
        });
    }

    private List<CS.CSShopItem> GetGroupListCount(CS.CSShopItem item)
    {
        var list = GetShopPopupList();
        var groupList = new List<CS.CSShopItem>();
        foreach (var each in list)
        {
            if (each.nIineUp_id == item.nIineUp_id)
                groupList.Add(each);
        }

        return groupList;
    }

    private void BuildDayPopupList()
    {
        m_popupList.Clear();

        var list = GetShopPopupList();
        SortedDictionary<int, List<CS.CSShopItem>> popupList = new SortedDictionary<int, List<CS.CSShopItem>>();
        foreach (var each in list)
        {
            if (each.popup_group <= 0)      // 0 초기값
                continue;

            if (each.output_timing == CS.ShopPopupTime.Day)
            {
                if (CheckDayPopupList(each) == false)       // 오늘 시간 추가
                    continue;
            }

            if (CheckPopupList(each) == false)      // 전체 체크
                continue;


            if (popupList.ContainsKey(each.popup_group) == false)
                popupList.Add(each.popup_group, new List<CS.CSShopItem>());

            popupList[each.popup_group].Add(each);
        }

        var sortedList = popupList.ToArray();
        foreach (var each in sortedList)
        {
            var condition = GetBuyCondition(each.Value);
            if (each.Value.Count <= condition)
                popupList.Remove(each.Key);
        }


        m_popupList = new Dictionary<int,List<CS.CSShopItem>>(popupList);
    }

    private int GetBuyCondition(List<CS.CSShopItem> items)
    {
        int groupBuyCount = 0;
        foreach (var item in items)
        {
            if (item.nCategory != CS.ShopItemCategory.Recommend)
                continue;

            if (ShopItemBuy(item) == false)
                groupBuyCount++;
        }

        return groupBuyCount;
    }

    private void SaveDayTime()
    {
        XGlobal.LocalData.DayItemPopupLastTime = XGlobal.m_Time.ServerDateTime.ToLocalTime();
        XLocalDataSaver.Save();
    }

    private bool CheckDayPopupList(CS.CSShopItem shopItem)
    {
        if (LastDayPopupTime() == false)     // 하루 안지남
            return false;

        if (CheckPopupList(shopItem) == false)
            return false;

        return true;
    }

    private bool LastDayPopupTime()
    {
        DateTime LastTime = XGlobal.LocalData.DayItemPopupLastTime; // 마지막으로 팝업을 닫은 시간
        DateTime nowTime = XGlobal.m_Time.ServerDateTime.ToLocalTime();       // 현재 시간


        if (LastTime.Date < nowTime.Date)                
            return true;

        return false;
    }

    private bool CheckPopupList(CS.CSShopItem shopItem)
    {
        if (CheckBoxCondition(shopItem, false) == false)   // 오늘 그만보기 체크
            return false;

        if (CheckCondition(shopItem) == false)      // 로비에서 보여줘야하는건지?
            return false;

        if (eventShopLogic.ShopItemBuyCondition(shopItem, XGlobal.MyInfo, XGlobal.m_Time.ServerDateTime.ToLocalTime()) == false) // 구매한 상품인지? 판매 시간 맞는지?
            return false;

        return true;
    }

    private bool CheckCondition(CS.CSShopItem shopItem)
    {
        if (string.IsNullOrEmpty(shopItem.output_condition))    // output이 빈칸일경우 로그인시 화면 팝업 출력 X
            return false;

        var condition = shopItem.output_condition.Replace("access_", string.Empty);

        bool state = false;

        switch (condition)
        {
            case "lobby":
                if (XGlobal.GameFlow.GetFlowType() == GameFlowTypes.Lobby)
                {
                    if(XGlobal.ui.lobby.GetLastStateType() == XLobbyStateTypes.Main)
                        state = true;
                }
                break;
            default:
                state = false;
                throw new ArgumentException("invalid condition : " + condition);
        }

        return state;
    }

    private bool CheckBoxCondition(CS.CSShopItem shopItem, bool save)
    {
        var list = XGlobal.LocalData.PopupShopLastTimeList;
        var nowTime = XGlobal.m_Time.ServerDateTime.ToLocalTime();

        foreach (var each in list)
        {
            if (each.Key == shopItem.nProductID)
            {
                var lastTime = each.Value;

                if (lastTime.Date < nowTime.Date)
                {
                    if (save == true)
                    {
                        list[each.Key] = nowTime;
                        XLocalDataSaver.Save();
                    }
                    return true;
                }
                return false;
            }
        }

        if (save == true)
        {
            list.Add(shopItem.nProductID, nowTime);
            XLocalDataSaver.Save();
        }
        return true;
    }

    private bool CheckGroupList(CS.CSShopItem shopItem)
    {
        if (shopItem.require_puchased_product_id <= 0)
            return true;

        foreach (var each in XGlobal.MyInfo.BoughtLimitedGoodsList)
        {
            if (each.ProductId == shopItem.require_puchased_product_id)
                return true;
        }

        return false;
    }

    private List<CS.CSShopItem> GetShopPopupList()
    {
        var shoplist = XGlobal.proto.shop.Values;
        return shoplist.Where(x => (x.nCategory == CS.ShopItemCategory.Popup ||
                                    x.nCategory == CS.ShopItemCategory.Recommend)).ToList<CS.CSShopItem>();
    }


    public void OnClick_ShowRefund()
    {
        XGlobal.ui.lobby.ShowRefundDetail();
    }
}
