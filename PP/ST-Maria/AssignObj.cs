// AssignObj.cs             : AssignObj implementation file
//
// Description              : EditUI
// Author                   : gerndal
// Maintainer               : gerndal, uhrain7761
// How to use               : 
// Created                  : 2018/05/30
// Last Update              : 2018/10/01
// Known bugs               : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MyStrip;
using DG.Tweening;
using ST.MARIA;
using System;
using ST.MARIA.DataPool;
using ST.MARIA.UI.MYSTRIP;

public class AssignObj : MonoBehaviour, IDragHandler
{
    public  ObjCreatureData OCD  ;// {get; set;}
    public  Image           image {get; set;}
    public  CellAttribute   CA    {get; set;}
    public  List<int>       placeCellIdxList {get; set;}
    public  Vector2         v2Pivot  {get; set;}


    private float           startPressTime;
    private DateTime        remainDateTime = new DateTime(0);
    [SerializeField] private ParticleSystem harvestAlarmEffect = null;
    private Coroutine coroutineRemainTime;

    private bool rewardState = false;

    private void Start()
    {
        if (OCD.Name.ToLower().Contains("_basic_decor01"))
            SetVideoEffect();
    }

    public void OCDUpdate()
    {
        if (OCD.ObjType == OBJ_TYPE.DECORATOR)
            return;

        if (OCD.Name.ToLower().Contains("_basic_"))
            return;

        remainDateTime = new DateTime(OCD.RemainTime * 10000000);

        if (coroutineRemainTime != null)
            StopCoroutine(coroutineRemainTime);

        coroutineRemainTime = StartCoroutine(CoStructureRemainTime());
    }
    
    IEnumerator CoStructureRemainTime()
    {
        if (OCD.ObjType == OBJ_TYPE.DECORATOR) yield break;
        if (OCD.Name.ToLower().Contains("_basic_")) yield break;

        while (true)
        {
            yield return new WaitUntil(() => EditUI.I.isEditMode == false);

            if (remainDateTime.Ticks > 0)
            {
                remainDateTime = remainDateTime.AddSeconds(-1);
                --OCD.RemainTime;
                ShowEffect(false);
            }
            else
            {
                if (harvestAlarmEffect == null)
                    HarvestAlarm();
                else
                    ShowEffect(true);

                RewardEffectSound();
                break;
            }
            yield return new WaitForSeconds(1);
        }
    }

    public void OnDrag(PointerEventData data)
    {
        if (OCD.Name.ToLower().Contains("_basic_decor"))
            return;

        if (EditUI.Instance.isSelectMode == false && EditUI.Instance.isEditMode == false)
            return;


        EditUI.Instance.isDragMode = true;
        EditUI.Instance.ShowSelectModeClose(false);

        OnReplace();
    }

    public void OnClickInfo()
    {
        if (EditUI.Instance.isEditMode)
        {
            if (OCD.Name.ToLower().Contains("_basic_decor01") == false && OCD.Name.ToLower().Contains("_basic_") == false)
                SetSelectMode();
            return;
        }

        if (OCD.Name.ToLower().Contains("_basic_decor01"))
        {
            if (EditUI.I.isEditMode == false)
                AdiscopeSystem.I.Show();
        }
        else
        {
            if (OCD.Name.ToLower().Contains("_basic_"))
                return;

            if (OCD.RemainTime == 0 && rewardState == false && OCD.ObjType == OBJ_TYPE.STRUCTURE)
                Harvest();
            else
                SetSelectMode();
        }
    }

    public void ShowEffect(bool show)
    {
        if (harvestAlarmEffect == null)
            return;

        if (show == false)
            harvestAlarmEffect.Stop();

        CommonTools.SetActive(harvestAlarmEffect, show);
    }

    private void RewardEffectSound()
    {
        if (EditUI.Instance.rewardCompleteSound != null)
            EditUI.Instance.rewardCompleteSound.Play();
    }

    private void SetSelectMode()
    {
        if (EditUI.Instance.isSelectMode)
            return;

        if (SetupAttributeMap.Instance.currentAT != null)
            return;

        if (EditUI.Instance.isEditMode)
            EditUI.Instance.isEditSelectMode = true;

        if (OCD.Name.ToLower().Contains("_basic_"))
            return;

        SetupAttributeMap.Instance.currentAT = null;
        SetupAttributeMap.Instance.RemoveAssignList(this);
        if (EditUI.Instance.isEditSelectMode)
            EditUI.Instance.EditClick(false, false);

        EditUI.Instance.SelectMode(true);
        EditUI.Instance.BuildSelectMode(UIStructureSelectMode.State.Select, OCD);

        if (SetupAttributeMap.Instance.prevAT == null)
            SetupAttributeMap.Instance.prevAT = CA;

        EditUI.I.soundPlayer[3].Play();
        //SetupAttributeMap.Instance.SetMapColor(OCD.ObjType == OBJ_TYPE.STRUCTURE ? CellAttribute.State.Structure : CellAttribute.State.Deco);
        //SetupAttributeMap.Instance.ShowArrow(OCD, true, image);
        //SetupAttributeMap.Instance.SetArrowParent(true);
        SetupAttributeMap.Instance.draggingIMG = image;
        SetupAttributeMap.Instance.SelectImageParent(image, true);
        //SetupAttributeMap.Instance.ArrowCellPostion(CA);
        SetupAttributeMap.Instance.SelectAO = this;        
        SetupAttributeMap.Instance.AssignObjInfoPopupOnOff(false);
    }

    private void OnReplace()
    {
        if (OCD.Name.ToLower().Contains("_basic_")) return;

        EditUI.Instance.ShowSelectMode(false);
        //EditUI.Instance.ShowTipText(EditUI.EditState.DragMode);        
        SetupAttributeMap.Instance.ShowArrow(OCD, true);
        //SetupAttributeMap.Instance.SetArrowParent(true);
        SetupAttributeMap.I.New(OCD, true);
        SetupAttributeMap.I.ReplaceMouseUpCheck();
        SetupAttributeMap.I.MoveCell(CA);
        SetupAttributeMap.I.RemoveAssignObj(this);
    }
    
    private void HarvestAlarm()
    {
        if (gameObject.activeSelf == false) return;
        if (OCD.ObjType == OBJ_TYPE.DECORATOR) return;
        if (OCD.Name.ToLower().Contains("_basic_")) return;

        ShowHarvestAlarm();
    }

    private void ShowHarvestAlarm()
    {
        harvestAlarmEffect = MyStripEffect.Instance.Show("FX.Reward-Gold", Vector3.one, -1);

        if (harvestAlarmEffect != null)
            harvestAlarmEffect.Link(transform, new Vector3(0, 200, 0));
    }

    private void OnEnable()
    {
        AdiscopeSystem.Instance.ActionNoVideoAds += ShowVideoEffect;
    }
    
    private void OnDisable()
    {
        AdiscopeSystem.Instance.ActionNoVideoAds -= ShowVideoEffect;

        if (harvestAlarmEffect == null)
            return;

        ShowEffect(false);
        if (OCD.ObjType != OBJ_TYPE.VideoDeco)
            harvestAlarmEffect = null;
    }

    private void ShowVideoEffect()
    {
        if (OCD.Name.ToLower().Contains("_basic_decor01") == false)
            return;

        ShowEffect(false);
        var serverTime = ClientInfo.Instance.ServerTime;
        ST.MARIA.PlayerPrefs.SetString("VideoShowTime", new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, 0, 0, 0).Ticks.ToString());
    }

    private void SetVideoEffect()
    {
        var serverTime = ClientInfo.Instance.ServerTime;
        DateTime curTime = new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, 0, 0, 0);

        string time = ST.MARIA.PlayerPrefs.GetString("VideoShowTime");
        DateTime oldTime = new DateTime();
        Debug.Log(oldTime.ToString("yyyy-MM-dd HH:mm:ss"));

        if (String.IsNullOrEmpty(time) == false)
        {
            oldTime = new DateTime(long.Parse(time));
        }
        else
        {
            if (harvestAlarmEffect == null)
                ShowHarvestAlarm();
            else
                ShowEffect(true);

            return;
        }
        
        int result = DateTime.Compare(curTime, oldTime);
        if (result > 0)
        {
            if (harvestAlarmEffect == null)
                ShowHarvestAlarm();
            else
                ShowEffect(true);
        }
        else
        {
            ShowEffect(false);
        }
    }

    private void Harvest()
    {
        if (EditUI.I.isEditMode)
            return;

        if (rewardState == true)
            return;

        rewardState = true;

        SessionLobby.I.RequestMystripReward(OCD.ID , HarvestRequestResponse);
    }

    void  HarvestRequestResponse(ST.MARIA.Network.AnswerMystripReward AMR)
    {
        var v3Start = EditUI.I.CalcEffectPos(transform.position + new Vector3(0, 100, 0));
        if (AMR.value.curCoinAmount > 0)
            HarvestEffect("FX.RewardTrail-Gold"     , v3Start , MyStripEffect.I.Gold.position);
        if (AMR.value.curGemAmount > 0)
            HarvestEffect("FX.RewardTrail-Jewelry"  , v3Start , MyStripEffect.I.Gem .position);
        if (AMR.value.curExpAmount > 0)
            HarvestEffect("FX.RewardTrail-Xp"       , v3Start , MyStripEffect.I.XP  .position);

        EditUI.Instance.rewardMoveSound.Play();

        SessionLobby.I.RequestMyinfo();
        ShowEffect(false);
        harvestAlarmEffect = null;
        rewardState = false;
    }

    private void HarvestEffect(string effectName, Vector3 v3Start, Vector3 v3Dest)
    {
        var effect = MyStripEffect.I.Show(effectName, v3Start, Vector3.one, 1.1f);
        var newX = UnityEngine.Random.Range(10, 21) * UnityEngine.Random.Range(-1 , 2) * 10;
        var newY = UnityEngine.Random.Range(10, 16) * 10;

        Vector3[] v3PosArray =
        {
            v3Start + new Vector3(newX , newY , 0) ,
            v3Dest
        };

        effect.transform.DOPath(v3PosArray , 1 , PathType.CatmullRom).SetEase(Ease.InCubic)
        .OnComplete(() => 
        {
            MyStripEffect.I.Show("FX.Reward-Get", EditUI.I.CalcEffectPos(v3Dest), Vector3.one, 3);
            EditUI.Instance.rewardObtainSound.Play();
        });
    }
}
