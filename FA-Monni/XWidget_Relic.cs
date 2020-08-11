using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using I2.Loc;

public class XWidget_Relic : MonoBehaviour
{
    public List<XUIRelicSlotItem> m_slotItem = new List<XUIRelicSlotItem>();

    public XUIRelicList relicList = null;

    private Action m_relicBadgeAction = null;

    private bool netdata_list = true;

    public string UIRelicQuickSlotEquipSoundKey = "UIItemEquip";

    public XWidget_Button btnRelicOpen;
    public XWidget_Button btnRelicUpgrade;
    public XWidget_Button btnRelicSetSlot;

    public void Build(Action relicBadgeAction)
    {
        if(relicBadgeAction != null)
            m_relicBadgeAction = relicBadgeAction;

        gameObject.SetActive(true);

        BuildSlot();

        var m_list = XGlobal.proto.Relic.GetRelicInfoBy();
        m_list = m_list.OrderBy(o => o.nGrade).ToList();    // 등급 순으로 정렬

        CS.RelicInfo relicInit = null;
        if (selRelic == null)
            relicInit = XGlobal.proto.Relic.FindRelicInfoBy(m_list[0].m_id, 1);
        else
            relicInit = selRelic;

        if (netdata_list == true)
        {
            relicList.Build(m_list, RefreshDetail, relicInit, SelectRelicObject);
        }
        else
        {
            netdata_list = true;
        }

        goSlotHighLight.gameObject.SetActive(false);

        btnRelicOpen.Build(ScriptLocalization.Get("UI/pnlRelic_Open"), OnClick_RelicOpen);
        btnRelicUpgrade.Build("", OnClick_RelicUpgrade);
        btnRelicSetSlot.Build("", OnClick_SetSlot);
        
        RefreshDetail(relicInit, CS.RelicLogic.IsExistRelic(XGlobal.MyInfo, relicInit.m_id));

        SetParticleEffect(false);
    }

    private void OnNetdataUpdated_Userdata(object arg)
    {
        Build(null);
        // inventory
    }

    private void BuildSlot()
    {
        for (int i = 0; i < m_slotItem.Count; i++)
        {
            m_slotItem[i].Build(SetSelectSlotIndex, SetQuickSlot, OpenBuySlotPopup);
            m_slotItem[i].ChangeAction(ChangeSlot);
            m_slotItem[i].RefreshSlotItem(i);
        }
    }

    public void HideRelicAllEffect()
    {
        //SetParticleEffect(false);
        goEffect.gameObject.SetActive(false);
    }

    public void Hide()
    {
        selRelic = null;
        gameObject.SetActive(false);
    }

    #region Set Data

    private CS.RelicInfo selRelic;
    private XUIRelicItem m_selObject = null;

    public void OnClick_RelicOpen()
    {
        //if (CS.RelicLogic.Unlock(XGlobal.MyInfo, selRelic.m_id, XGlobal.proto.Relic) == false)
        //    return;


        //GetRelicUnlock();

        OnClick_RelicUpgrade();
    }

    //private void GetRelicUnlock()
    //{
    //    XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_Relic_Unlock>(
    //       param: new CS.CSNetParameters
    //        {
    //            { CS.ApiParamKeys.relic_id, selRelic.m_id},
    //        },
    //       callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
    //       {
    //           if (result == CS.ApiReturnCode.Success)
    //           {
    //               SetParticleEffect(true);
    //               var slotRelic = NoSetSlotRelic();
    //               // 슬롯에 아무 스킬도 장착되어 있지 않을 경우,
    //               // 해당 봉인 해제 한 스킬을 무조건 첫 번째로 세팅한다.
    //               if (!slotRelic)
    //               {
    //                   SetSelectSlotIndex(0);
    //                   SetItemQuickSlot();
    //                   SaveFirstSelectedRelic(selRelic.m_id);
    //               }

    //               m_selObject.RefreshPieceBar();
    //               RefreshDetail(selRelic, true);
    //               RefreshUnlock(m_selObject);

    //               //GetRelicPopup();

    //               if (getRelicAction != null)
    //                   getRelicAction();
    //           }
    //           else if (result == CS.ApiReturnCode.InternalLogicError)
    //           {
    //               // 실패
    //           }
    //       });
    //}

    private int m_selIndex = 0;

    public void SetSelectSlotIndex(int index)
    {
        m_selIndex = index;
    }

    private void SetQuickSlot()
    {
        //if (change_slot_index > -1)
        //{
        //    // 슬롯 교체
        //    change_slot_state = false;
        //    SetQuickSlotChange();
        //}
        //else
        //{
            // 일반 장착
            SetItemQuickSlot();
        //}
    }

    private void SaveFirstSelectedRelic(int selectRelicId)
    {
        foreach (var each in XGlobal.LocalData.partyLocalData)
            each.Value.SetRelicID(selectRelicId);

        XLocalDataSaver.Save();
    }

    private void SaveSelectedRelic(int selectRelicId)
    {
        CS.CharPartyId currPartyId = XGlobal.MyInfo.m_SelCharParty.CurrentCharPartyId;

        XGlobal.LocalData.GetPartyLocalData(currPartyId).SetRelicID(selectRelicId);

        XLocalDataSaver.Save();
    }


    private void SetItemQuickSlot()
    {
        if (CS.RelicLogic.ValidateSlotId(m_selIndex) == false)
            return;

        var relic_id = 0;
        if (change_slot_index > -1)
        {
            change_slot_state = false;
            relic_id = XGlobal.MyInfo.RelicQuickSlot[change_slot_index];
        }
        else
        {
            if (XGlobal.MyInfo.RelicQuickSlot[m_selIndex] == selRelic.m_id)
                return;
            relic_id = selRelic.m_id;
        }

        netdata_list = false;

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_Relic_Set_Quick_Slot>(
        param: new CS.CSNetParameters
            {
                { CS.ApiParamKeys.relic_quick_slot_id, m_selIndex },
                { CS.ApiParamKeys.relic_id, relic_id },
            },
        callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
        {
            if (result == CS.ApiReturnCode.Success)
            {
                //m_slotItem[m_selIndex].RefreshSlotItem(m_selIndex);
                goSlotHighLight.gameObject.SetActive(false);
                change_slot_index = -1;
                RefreshAllSlotItem();
                SetSlotButtonText(true);
                ChangeHideSetting(false);
                SaveSelectedRelic(relic_id);
                SingleQuickSlotSetButtonHide(selRelic.m_id);
                selectState = false;
                m_setSlot = false;
                XGlobal.sound.Play(UIRelicQuickSlotEquipSoundKey);
            }
            else if (result == CS.ApiReturnCode.InternalLogicError)
            {
                // 실패
            }
        });
    }

    private void RefreshAllSlotItem()
    {
        for (int i = 0; i < m_slotItem.Count; i++)
        {
            m_slotItem[i].InItItem();
            m_slotItem[i].RefreshSlotItem(i);
        }
    }

    public void OnClick_RelicUpgrade()
    {
        var level = GetCurrentlyRelicLevel(selRelic.m_id);
        var nextRelic = GetNextRelic(selRelic.m_id, level);

        if (nextRelic == null)
            return;

        if (XGlobal.MyInfo.Gold < nextRelic.m_learn_gold)
        {
            //m_effectPool.HideAllEffect();
            SetParticleEffect(false);
            XGlobal.ui.ShowNotEnoughGoldPopUp();
            return;
        }

        if (XGlobal.MyInfo.m_ItemInventory.GetRelicPieceCount(selRelic.m_materialid) < nextRelic.m_materialcount)
        {
            return;
        }

        var itemId = XGlobal.MyInfo.m_ItemInventory.GetRelicPieceIID(selRelic.m_materialid);


        netdata_list = false;
        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_Relic_Upgrade>(
        param: new CS.CSNetParameters
            {
                { CS.ApiParamKeys.relic_id, selRelic.m_id},
                { CS.ApiParamKeys.relic_piece_id, itemId},
            },
        callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
        {
            if (result == CS.ApiReturnCode.Success)
            {
                var slotRelic = NoSetSlotRelic();
                // 슬롯에 아무 스킬도 장착되어 있지 않을 경우,
                // 해당 봉인 해제 한 스킬을 무조건 첫 번째로 세팅한다.
                if (!slotRelic)
                {
                    SetSelectSlotIndex(0);
                    SetItemQuickSlot();
                    SaveFirstSelectedRelic(selRelic.m_id);
                }

                GetUpgradeEffect();
                
                RefreshDetail(selRelic, true);
                RefreshAllSlotItem();

                RefreshUnlock(m_selObject);
                m_selObject.BuildType();

                if(m_relicBadgeAction != null)
                    m_relicBadgeAction();

                XGlobal.tutoManager.NextTutorial(CS.TutorialType.RelicGuide, goEffect);
            }
            else if (result == CS.ApiReturnCode.InternalLogicError)
            {
                // 실패
            }
        });
    }

    #endregion

    #region popup

    public void OpenBuySlotPopup()
    {
        if (change_slot_state)
            return;

        if (m_setSlot)
            return;

        var cry = GetGameConstValue(CS.Constant.key.relic_quick_slot_open_price);
        XGlobal.ui.common.ShowMessageBox(CS.Currency.Crystal, cry, "UI/pnlRelic_Buy_Slot_Title", "UI/pnlRelic_Buy_Slot_Desc", XMessageBoxButtons.OKCancel,
            () =>
            {
                BuySlot();
            }, null, null, "", false, null, null, null, UIWidget.Pivot.Center);
    }

    private void BuySlot()
    {
        if (XGlobal.MyInfo.Crystal < GetGameConstValue(CS.Constant.key.relic_quick_slot_open_price))
        {
            //m_effectPool.HideAllEffect();
            SetParticleEffect(false);
            XGlobal.ui.ShowNotEnoughCrystalPopUp();
            return;
        }

        var slotIndex = CS.RelicLogic.GetBuySlot(XGlobal.MyInfo);

        // 슬롯 없음 에러
        if (slotIndex < 0)
            return;

        XNetworkManager.Instance.processor.ScheduleProcess<XNetProcess_Relic_Buy_Quick_Slot>(
        param: new CS.CSNetParameters
            {
                {CS.ApiParamKeys.relic_quick_slot_id, slotIndex},
            },
        callback: (CS.ApiReturnCode result, string errmsg, JObject json) =>
        {
            if (result == CS.ApiReturnCode.Success)
            {
                RefreshAllSlotItem();
                SingleQuickSlotSetButtonHide(selRelic.m_id);
                XGlobal.sound.Play(XUISound.GetSoundKey(XUISound.UISound.UIEventPop));
            }
            else if (result == CS.ApiReturnCode.InternalLogicError)
            {
                // 실패
            }
        });
    }

    private bool m_setSlot = false;
    public void OnClick_SetSlot()
    {
        // 슬롯간의 교체 상태에서 취소버튼 활성화.
        if (change_slot_state)
        {
            ChangeSlot(change_slot_index);
            return;
        }

        if (!m_setSlot)
        {
            // 자신 외의 장착 할 수 있는 슬롯이 있을 경우
            if (GetSetSlotCount(selRelic.m_id) > 0)
            {
                m_setSlot = true;
                RefreshSlotArrow(true, selRelic.m_id);
                goSlotHighLight.gameObject.SetActive(true);
                SetSlotButtonText(false);
            }
        }
        else
        {
            m_setSlot = false;
            RefreshSlotArrow(false);
            goSlotHighLight.gameObject.SetActive(false);
            SetSlotButtonText(true);
        }
    }
    #endregion

    #region Detail

    public GameObject goSlotHighLight;
    public UILabel lblSlotSetAndCancel;

    public UI2DSprite sprDetailIcon;
    public UILabel lblDetailName;
    public UILabel lblDetailCoolTime;
    public UILabel lblDetailSkillName;
    public UILabel lblDetailSkillDesc;
    public UILabel lblDetailNow;
    public UILabel lblDetailNext;
    public UILabel lblDetailLevel;

    public UIButton btnUnlock;
    public UIButton btnUpgrade;
    public UIButton btnSetSlot;
    public UILabel lblUpgradePrice;
    public UILabel lblMax;

    public UI2DSprite sprPhysic;
    public UI2DSprite sprMagic;
    public UILabel lblType;

    public UILabel lblDetailRelicDesc;

    public GameObject goEffect;


    public bool RelicUnlockCheck(CS.RelicInfo item)
    {
        return CS.RelicLogic.IsExistRelic(XGlobal.MyInfo, item.m_id);
    }

    public void RefreshUnlock(XUIRelicItem relicItem)
    {
        if (relicItem == null)
            return;

        relicItem.Build(relicItem.m_relicInfo, SelectRelicObject, selRelic);
    }

    private bool NoSetSlotRelic()
    {
        var myinfo = XGlobal.MyInfo;
        bool relic = false;

        for(int i = 0; i < myinfo.RelicQuickSlot.Count(); i++)
        { 
            if (myinfo.RelicQuickSlot[i] != 0 && myinfo.RelicQuickSlot[i] > 0)
            {
                // 슬롯에 스킬 존재
                relic = true;
            }
        }

        return relic;
    }

    // 선택된 아이템
    public void SelectRelicObject(XUIRelicItem selObject)
    {
        // 슬롯간의 교체 상태에서 다른 아이템 클릭을 막는다.
        if (change_slot_state)
            return;

        if (m_selObject != null && m_selObject != selObject)
        {
            selectState = false;
            m_selObject.SetSelectSprite(false);
            RefreshSlotArrow(false);
            m_setSlot = false;
        }
        else
        {
            if (!selectState)
                selectState = true;
            else
                selectState = false;
        }

        m_selObject = selObject;
        m_selObject.SetSelectSprite(true);
    }

    private bool selectState = false;
    // m_open == 이미 배운 스킬인가?
    public void RefreshDetail(CS.RelicInfo relicItem, bool m_open)
    {
        // 슬롯간의 교체 상태에서 다른 아이템 클릭을 막는다.
        if (change_slot_state)
            return;

        if (!m_open)
            selectState = false;

        selRelic = relicItem;

        if (sprDetailIcon != null)
        {
            if (relicItem.m_icon != null && relicItem.m_icon != "")
            {
                sprDetailIcon.gameObject.SetActive(true);
                XNGUIHelper.SetSpriteAspect(sprDetailIcon, XGlobal.spriteLoader.LoadInPool(relicItem.m_icon));
            }
            else
            {
                sprDetailIcon.gameObject.SetActive(false);
            }
        }
        lblDetailRelicDesc.text = relicItem.m_desc;
        lblDetailName.text = relicItem.m_name;        
        SetSlotButtonText(true);

        XUIHelper.DamageTypeIcon(m_selObject.m_damageType, lblType, sprMagic, sprPhysic);


        var level = GetCurrentlyRelicLevel(relicItem.m_id);

        if (level < 1)
            lblDetailLevel.text = "";
        else
            lblDetailLevel.text = string.Format("Lv. {0}", level);

        btnRelicOpen.SetDisable(!m_open);

        btnRelicUpgrade.SetDisable(false);

        SingleQuickSlotSetButtonHide(relicItem.m_id);
        RefreshDetailStat(relicItem, m_open, level);
    }

    public void RefreshDetailStat(CS.RelicInfo relicItem, bool m_open, int level)
    {
        btnUnlock.gameObject.SetActive(!m_open);
        btnUpgrade.gameObject.SetActive(m_open);
        btnSetSlot.gameObject.SetActive(m_open);

        SetDetailSkillBuff(relicItem, level);


        if (level >= CS.CSConst.RELIC_MAX_LEVEL)
        {
            btnRelicUpgrade.SetDisable(true);
            lblUpgradePrice.gameObject.SetActive(false);
            lblMax.text = "LEVEL MAX";
            return;
        }


        var inven = XGlobal.MyInfo.m_ItemInventory;
        var nextRelic = GetNextRelic(relicItem.m_id, level);
        int progressCount = inven.GetRelicPieceCount(relicItem.m_materialid);
        int totalCount = nextRelic.m_materialcount;
        if (m_open)
        {
            if (level < CS.CSConst.RELIC_MAX_LEVEL)
            {
                if (progressCount < totalCount)
                    btnRelicUpgrade.SetDisable(true);
                lblUpgradePrice.gameObject.SetActive(true);
                lblUpgradePrice.text = nextRelic.m_learn_gold.ToString("N0");
                lblMax.text = "";
            }
        }
        else
        {
            if (progressCount >= totalCount)
                btnRelicOpen.SetDisable(false);
        }
    }

    private void SingleQuickSlotSetButtonHide(int id)
    {
        btnRelicSetSlot.SetDisable(false);
        if (GetSetSlotCount(id) < 1)
            btnRelicSetSlot.SetDisable(true);
    }

    private XMakeDescription skillDesc = new XMakeDescription();

    private void SetDetailSkillBuff(CS.RelicInfo relicItem, int level)
    {
        // finish 조건 때문에 실제 level에 대한 정보의 스킬 아이디를 가져옴
        var m_level = level;
        if (m_level == 0)
            m_level = 1;
        var exist = XGlobal.proto.Relic.FindRelicInfoBy(relicItem.m_id, m_level);
        var relic_skill = GetRelicSkill(exist.m_skill_id);

        //lblDetailSkillDesc.text = relic_skill.m_szDesc;
        lblDetailSkillDesc.text = skillDesc.GetDescription(relic_skill, relic_skill.m_szDesc, 1);
        lblDetailSkillName.text = relic_skill.szName;
        lblDetailCoolTime.text = string.Format(ScriptLocalization.Get("Code/UI_RELIC_COOLTIME"), relic_skill.CoolTime.value);

        //// 이 부분 추후 수정 할 수 있음.
        
        if (level != 0)
        {
            lblDetailNow.text = skillDesc.GetDescription(relic_skill, relic_skill.m_szSpecDesc, 1);
        }
        else
        {
            lblDetailNow.text = "";
        }

        if (level >= CS.CSConst.RELIC_MAX_LEVEL)
        {
            lblDetailNext.text = ScriptLocalization.Get("Code/UI_SKILL_MAX_LEVEL");
        }
        else
        {
            var nextRelic = GetNextRelic(relicItem.m_id, level);
            var nextRelic_skill = GetRelicSkill(nextRelic.m_skill_id);

            lblDetailNext.text = skillDesc.GetDescription(nextRelic_skill, nextRelic_skill.m_szSpecDesc, 1);
        }
        /////// 
    }

    private CS.CSSkillInfo GetRelicSkill(int relic_SkillID)
    {
        foreach (var each in XGlobal.proto.skill)
        {
            if (relic_SkillID == each.Value.nID)
                return each.Value;
        }

        return null;
    }

    private CS.RelicInfo GetNextRelic(int id, int level)
    {
        var nextRelic = XGlobal.proto.Relic.FindNextRelicInfoBy(id, level);
        return nextRelic;
    }

    public void RefreshSlotArrow(bool state, int id = -1)
    {
        for (int i = 0; i < m_slotItem.Count; i++)
        {
            if (XGlobal.MyInfo.RelicQuickSlot[i] != id)
                m_slotItem[i].RefreshArrow(state);
        }
    }


    /// <summary>
    /// 슬롯 체인지 관련
    /// </summary>
    private int change_slot_index = -1;
    private bool change_slot_state = false;

    public void ChangeSlot(int index)
    {
        if (m_setSlot)
            return;

        // 교체 할 슬롯 없을경우 활성화X
        bool changeState = false;
        for (int i = 0; i < XGlobal.MyInfo.RelicQuickSlot.Count(); i++)
        {
            if (i != index)
            {
                if (XGlobal.MyInfo.RelicQuickSlot[i] > -1)
                    changeState = true;
            }
        }

        if (changeState == false)
            return;

        if(change_slot_index == index)
        {
            change_slot_index = -1;
            change_slot_state = false;
            RefreshSlotArrow(false);
            SetSlotButtonText(true);
            ChangeHideSetting(false);
            goSlotHighLight.gameObject.SetActive(false);
            return;
        }

        goSlotHighLight.gameObject.SetActive(true);
        change_slot_index = index;
        change_slot_state = true;
        SetSlotButtonText(false);
        ChangeHideSetting(true);

        for (int i = 0; i < m_slotItem.Count; i++)
        {
            if( i != index)
                m_slotItem[i].RefreshArrow(true);
        }
    }

    private void ChangeHideSetting(bool state)
    {
        if (selRelic != null)
        {
            var level = GetCurrentlyRelicLevel(selRelic.m_id);

            if (level <= 0)
            {
                btnUnlock.gameObject.SetActive(!state);
                btnSetSlot.gameObject.SetActive(state);
            }
        }
        else
        {
            // state true - 교체모드 , false 취소모드
            // 바로 슬롯교체 모드로 들어갔을 때, 디폴트 값 때문에 버튼이 안보임.
            btnSetSlot.gameObject.SetActive(state);
            btnUnlock.gameObject.SetActive(!state);
            btnRelicOpen.SetDisable(!state);
        }
    }

    #endregion

    private int GetCurrentlyRelicLevel(int id)
    {
        var level = 0;
        if (XGlobal.MyInfo.Relic.TryGetValue(id, out level) == false)
            return 0;

        return level;
    }

    private int GetSetSlotCount(int id)
    {
        var quickSlot = XGlobal.MyInfo.RelicQuickSlot;
        int count = 0;
        for (int i = 0; i < quickSlot.Count(); i++)
        {
            if (quickSlot[i] >= 0 && quickSlot[i] != id)
                count++;
        }

        return count;
    }

    private int GetGameConstValue(CS.Constant.key key)
    {
        var count = 0;
        if (XGlobal.proto.gameConst.TryGetValue(key, out count) == false)
            count = 0;

        return count;
    }

    public void GetRelicPopup()
    {
        int nRelicId = selRelic.m_id;

        var relic_reward = new CS.CSContent[]
        {  
            new CS.CSContent() { ContentType = CS.CSContentType.relic,
                                 ContentCount = 1,
                                 ContentId = nRelicId }
        };

        if (relic_reward != null)
        {
            XGlobal.ui.common.ShowRewardBox(ScriptLocalization.Get("Code/UI_RELIC_REWARD_TITLE"), "", 
                                                XMessageBoxButtons.OK, () => { }, relic_reward);
        }
    }

    private void SetSlotButtonText(bool state)
    {
        if(state)
            lblSlotSetAndCancel.text = ScriptLocalization.Get("Glossary/Enrollment");
        else
            lblSlotSetAndCancel.text = ScriptLocalization.Get("UI/Cancel");
            
    }


    public XUIEffectPool m_effectPool;
    public string effectSoundKey = "UISkillLevelUp";

    private void GetUpgradeEffect()
    {
        //if (m_effectPool == null)
        //    return;

        //m_effectPool.GetEffect();

        SetParticleEffect(true);

        XGlobal.sound.Play(effectSoundKey);
    }

    private void SetParticleEffect(bool bShow)
    {
        goEffect.gameObject.SetActive(bShow);
        var particle = goEffect.GetComponentsInChildren<ParticleSystem>();
        foreach (var each in particle)
        {
            if (bShow == true)
                each.Play();
            else
                each.Stop();
        }
    }
}