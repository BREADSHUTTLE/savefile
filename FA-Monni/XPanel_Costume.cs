using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using I2.Loc;


public class XPanel_Costume : MonoBehaviour
{

    public XUIModelPreviewDragZone preview;
    public XUICostumeList costumeList = null;
    public string SelectedCharUid { set { m_selectedCharUid = value; } }

    public XWidget_ToggleTab[] toggleTab;

    private string m_selectedCharUid;
    private XUICostumeItem m_costumeItem;

    public enum tabList { all = 0, awakening, special }

    private tabList m_tab = tabList.all;

    private CS.CostumeInfo m_costumeInfo;
    private XModuleActor m_selectCharActor = null;


    // 캐릭터 연출
    private XPanel_CardDirectionRoot cardDirectionRoot = null;
    
    private void SetupSelectedCharModuleActor()
    {
        XModuleActor selectActor = GetSelectCharacterActor(m_selectedCharUid);
        m_selectCharActor = selectActor;
    }

    public void Build(bool bShow)
    {
        if (bShow == true)
        {
            //XGlobal.ui.lobby.TapBannerBackButtonHideState(true);
            gameObject.SetActive(true);

            SetupSelectedCharModuleActor();

            int selectCostumeId = -1;
            if (m_selectCharActor.CostumeInfo != null)
                selectCostumeId = m_selectCharActor.CostumeInfo.nID;


            costumeList.Init(m_selectedCharUid,
                (object obj) =>
                {
                    var o = obj as GameObject;
                    var item = o.GetComponent<XUICostumeItem>();
                    SelectCostumeItem(item);
                },
                (object obj) =>
                {
                    var o = obj as GameObject;
                    var item = o.GetComponent<XUICostumeItem>();
                    SetCheckCostumeItem(item);
                },
                RefreshDetail, selectCostumeId, m_selectCharActor.me.lobbyUI.Costumes);

            //BuildMainModel(m_selectCharActor);

            m_tab = tabList.all;
            SetClickTab();
            InitDetail();
        }
        else
        {
            preview.ResetRotate();
            gameObject.SetActive(false);
            //XGlobal.ui.lobby.TapBannerBackButtonHideState(false);
        }
    }


    //public void BuildMainModel(XModuleActor character)
    //{
    //    preview.ShowModelPreview(character, true);
    //}

    public void RefreshCostumeModel(XModuleActor character, int costumeId, bool bCenter)
    {
        preview.ShowCostumeModelPreview(character, costumeId, bCenter);
    }

    #region net process

    private void SetSelectCostume(int costumeID)
    {
        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_Costume_Select>(
            param: new CS.CSNetParameters
            {
                {CS.ApiParamKeys.CharacterId, m_selectedCharUid},
                {CS.ApiParamKeys.CostumeId, costumeID}
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                if (result == CS.ApiReturnCode.Success)
                {
                    SetupSelectedCharModuleActor();
                    if (m_CheckItem != null)
                        m_CheckItem.RefreshSelectCheck(costumeID);
                    m_item.RefreshSelectCheck(costumeID);
                    costumeList.RefreshCostumeState(m_selectCharActor.me.lobbyUI.Costumes, costumeID);
                    //BuildMainModel(m_selectCharActor);
                }
                else if (result == CS.ApiReturnCode.InternalLogicError)
                {

                }
            });
    }

    private void UnlockCostume(int costumeID)
    {
        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_Costume_Unlock>(
            param: new CS.CSNetParameters
            {
                {CS.ApiParamKeys.CharacterId, m_selectedCharUid},
                {CS.ApiParamKeys.CostumeId, costumeID}
            },
            callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
            {
                if (result == CS.ApiReturnCode.Success)
                {
                    SetupSelectedCharModuleActor();

                    RefreshCostumeModel(m_selectCharActor, costumeID, true);

                    RefreshButton(m_costumeInfo);
                    var state = IsUnlockCostume(m_costumeInfo.nID);
                    m_item.RefreshLockCostume(state);
                    m_item.RefreshUnlockState(state);
                    costumeList.RefreshCostumeState(m_selectCharActor.me.lobbyUI.Costumes, m_selectCharActor.CostumeInfo.nID);
                    ShowCardDirection(costumeID);
                }
                else if (result == CS.ApiReturnCode.InternalLogicError)
                {

                }
            });
    }

    private void ShowCardDirection(int costumeID)
    {
        // 캐릭터 획득 연출용 프리펩 로드
        XUIHelper.InitGetCharacterDirection(this.transform, ref cardDirectionRoot);

        var selectedCharacter = XGlobal.MyInfo.GetChar(m_selectedCharUid);

        XModuleActor.CreationDesc characterDescription = XCharHelper.CreateCharacterDescriptionFromActor(selectedCharacter);
        characterDescription.DefaultChar.costumeId = costumeID;

        cardDirectionRoot.gameObject.SetActive(true);
        cardDirectionRoot.Show(characterDescription);
    }

    #endregion


    #region Detaill

    public UILabel lblFightingMsg;
    public UILabel lblIntro;

    public UIButton btnEquip;
    public UIButton btnUnlock;
    public GameObject goGetInfo;
    public UILabel lblCondition;

    private void InitDetail()
    {
        if (m_item != null)
        {
            RefreshDetail(m_item.GetCostumeItemInfo());
            return;
        }

        //if (lblFightingMsg != null)
        //    lblFightingMsg.gameObject.SetActive(false);

        //if (lblIntro != null)
        //    lblIntro.gameObject.SetActive(false);

        if (btnEquip != null)
            btnEquip.gameObject.SetActive(false);

        if (btnUnlock != null)
            btnUnlock.gameObject.SetActive(false);

        if (goGetInfo != null)
            goGetInfo.SetActive(true);

        if (lblCondition != null)
            lblCondition.text = "";
    }

    public void RefreshDetail(CS.CostumeInfo costumeInfo)
    {
        if (costumeInfo == null)
            return;


        m_costumeInfo = costumeInfo;

        RefreshCostumeModel(m_selectCharActor, costumeInfo.nID, true);

        //if (lblFightingMsg != null)
        //{
        //    lblFightingMsg.gameObject.SetActive(true);
        //    lblFightingMsg.text =string.Format(ScriptLocalization.Get("Code/UI_MESSAGE_TITLE"), costumeInfo.strFightingMsg);
        //}

        //if (lblIntro != null)
        //{
        //    lblIntro.gameObject.SetActive(true);
        //    lblIntro.text = costumeInfo.strIntro;
        //}

        RefreshButton(costumeInfo);

        if (costumeInfo.nType == CS.CostumeType.Special)
            lblCondition.text = string.Format(": {0} 구매", costumeInfo.nPrice.ToString("N0"));
        else
            lblCondition.text = string.Format(": {0}", costumeInfo.m_FinishConditionDesc);
    }


    public void RefreshButton(CS.CostumeInfo costumeInfo)
    {
        var unlockState = IsUnlockCostume(costumeInfo.nID);
        if (unlockState == true)
        {
            btnEquip.gameObject.SetActive(true);
            goGetInfo.SetActive(false);
            btnUnlock.gameObject.SetActive(false);
        }
        else
        {
            var check = CheckUnlockCostume(costumeInfo.nID);

            btnUnlock.gameObject.SetActive(check);
            goGetInfo.gameObject.SetActive(!check);
            btnEquip.gameObject.SetActive(false);
        }
    }

    #endregion


    #region hendlr


    private void SetClickTab()
    {
        toggleTab[(int)m_tab].OnClickThisTab();
        costumeList.Build((int)m_tab);
    }

    public void OnClick_All()
    {
        m_tab = tabList.all;
        SetClickTab();
        TabSelectSetupModel();
    }

    public void OnClick_Awakening()
    {
        m_tab = tabList.awakening;
        SetClickTab();
        TabSelectSetupModel();
    }

    public void OnClick_Special()
    {
        m_tab = tabList.special;
        SetClickTab();
        TabSelectSetupModel();
    }

    private void TabSelectSetupModel()
    {
        var costumeInfo = m_item.GetCostumeItemInfo();
        RefreshCostumeModel(m_selectCharActor, costumeInfo.nID, true);
        RefreshDetail(costumeInfo);
    }

    public void OnClick_Shop()
    {
        XGlobal.ui.lobby.ShowNormalShopWindow();
    }

    public void OnClick_UnlockCostume()
    {
        UnlockCostume(m_costumeInfo.nID);
    }

    public void OnClick_SelectCostume()
    {
        SetSelectCostume(m_costumeInfo.nID);
    }

    public void OnClick_Help()
    {
        XGlobal.ui.lobby.ShowHelp(HelpLink.Costume);
    }

    #endregion


    private XModuleActor GetSelectCharacterActor(string selectedCharUid)
    {
        return XGlobal.MyInfo.m_CharacterInventory.Find(selectedCharUid);
    }

    private bool IsUnlockCostume(int costumeID)
    {
        return CS.CostumeLogic.IsHaveCostume(m_selectCharActor.me.lobbyUI.Costumes, costumeID);
    }

    private bool CheckUnlockCostume(int costumeID)
    {
        return CS.CostumeLogic.CheckUnlockable(XGlobal.MyInfo, costumeID, XGlobal.proto.Costume);
    }


    private XUICostumeItem m_item = null;

    public void SelectCostumeItem(XUICostumeItem item)
    {
        m_item = item;

        if (m_costumeItem != null)
            m_costumeItem.RefreshSelect(false);
        m_costumeItem = item;
        m_costumeItem.RefreshSelect(true);
    }

    private XUICostumeItem m_CheckItem = null;

    public void SetCheckCostumeItem(XUICostumeItem item)
    {
        m_CheckItem = item;
    }

    public void Hide()
    {
        //gameObject.SetActive(false);
        XGlobal.ui.lobby.GoBack();
    }
}
