// TimerSystem.cs - TimerSystem implementation file
//
// Description      : TimerSystem
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/09/17
// Last Update      : 2020/04/17
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dorothy;
using Dorothy.DataPool;

public class TimerSystem : BaseSystem<TimerSystem>
{
    public Action<string> ActionOfferTimer = null;
    public Action<bool> ActionStoreBonusTimer = null;
    
    private long restOfferTime = 0;
    private Coroutine coroutineOfferTimer = null;

    private Coroutine coroutineFasterBoosterTimer = null;
    private Coroutine coroutineBoomBoosterTimer = null;
    private Coroutine coroutineBoomFasterBoosterTimer = null;

    private Coroutine coroutineRewardAdState = null;

    private Coroutine coroutineStoreBonusTimer = null;
    
    private bool isApplicationQuit = false;

    public long RestOfferTime
    {
        get { return restOfferTime; }
        set { restOfferTime = value; }
    }

    public void StartOfferTimer(DateTime dealTime)
    {
        if (coroutineOfferTimer != null)
            StopCoroutine(coroutineOfferTimer);

        coroutineOfferTimer = StartCoroutine("OfferDisplayTime", dealTime);
    }

    public void StopOfferTimer()
    {
        if (coroutineOfferTimer != null)
            StopCoroutine(coroutineOfferTimer);
    }

    private IEnumerator OfferDisplayTime(DateTime dealTime)
    {
        while (true)
        {
            if (isApplicationQuit == true)
                break;

            TimeSpan timeout = CommonTools.ToPSTTime(dealTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);

            if (timeout >= TimeSpan.Zero)
            {
                if (ActionOfferTimer != null)
                    ActionOfferTimer(CommonTools.DisplayDigitalTime(timeout));

                restOfferTime = (long)timeout.TotalSeconds;

                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return new WaitForSeconds(1f);

                restOfferTime = 0;

                SessionLobby.Instance.RequestHotDealOffer((e) =>
                {
                    if (e != null)
                        Error("Error Hot Deal Offer : " + e.Message);
                });

                StopOfferTimer();

                yield break;
            }
        }
    }

    public void StartFasterBoosterTimer(DateTime endTime, Text txtTimer)
    {
        //StopBoosterTimer();
        if (coroutineFasterBoosterTimer != null)
        {
            StopCoroutine(coroutineFasterBoosterTimer);
            coroutineFasterBoosterTimer = null;
        }

        Debug.Log("StartFasterBoosterTimer : " + endTime.ToString());
        
        coroutineFasterBoosterTimer = StartCoroutine(BoosterDisplayTime(endTime, txtTimer));
    }

    public void StartBoomBoosterTimer(DateTime endTime, Text txtTimer)
    {
        //StopBoosterTimer();
        if (coroutineBoomBoosterTimer != null)
        {
            StopCoroutine(coroutineBoomBoosterTimer);
            coroutineBoomBoosterTimer = null;
        }

        Debug.Log("StartBoomBoosterTimer : " + endTime.ToString());

        coroutineBoomBoosterTimer = StartCoroutine(BoosterDisplayTime(endTime, txtTimer));
    }

    public void StartBoomFasterBoosterTimer(DateTime endTime, Text txtTimer)
    {
        //StopBoosterTimer();
        if (coroutineBoomFasterBoosterTimer != null)
        {
            StopCoroutine(coroutineBoomFasterBoosterTimer);
            coroutineBoomFasterBoosterTimer = null;
        }

        Debug.Log("StartBoomFasterBoosterTimer : " + endTime.ToString());

        coroutineBoomFasterBoosterTimer = StartCoroutine(BoosterDisplayTime(endTime, txtTimer));
    }

    public void StopBoosterTimer()
    {
        if (coroutineFasterBoosterTimer != null)
        {
            StopCoroutine(coroutineFasterBoosterTimer);
            coroutineFasterBoosterTimer = null;
        }

        if (coroutineBoomBoosterTimer != null)
        {
            StopCoroutine(coroutineBoomBoosterTimer);
            coroutineBoomBoosterTimer = null;
        }

        if (coroutineBoomFasterBoosterTimer != null)
        {
            StopCoroutine(coroutineBoomFasterBoosterTimer);
            coroutineBoomFasterBoosterTimer = null;
        }
    }

    private IEnumerator BoosterDisplayTime(DateTime endTime, Text txtTimer)
    {
        while (true)
        {
            if (isApplicationQuit == true)
                break;

            TimeSpan timeout = CommonTools.ToPSTTime(endTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);

            if (timeout >= TimeSpan.Zero)
            {
                if (txtTimer != null)
                    txtTimer.text = CommonTools.DisplayDigitalTimeDay(timeout);

                yield return new WaitForSeconds(1f);
            }
            else
            {
                yield return new WaitForSeconds(1f);

                StopBoosterTimer();

                SessionLobby.instance.RequestUserEffect(() => { Log("Update User Effect"); });
                
                yield break;
            }
        }
    }

    public void StartRewardAdState()
    {
        DateTime curTime = CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
        DateTime nextTime = new DateTime(curTime.Year, curTime.Month, curTime.Day).AddDays(1);

        //DateTime nextTime = new DateTime(curTime.Year, curTime.Month, curTime.Day, curTime.Hour, curTime.Minute, curTime.Second).AddMinutes(1);

        if (coroutineRewardAdState != null)
        {
            StopCoroutine(coroutineRewardAdState);
            coroutineRewardAdState = null;
        }

        coroutineRewardAdState = StartCoroutine(RewardAdStateTime(nextTime));
    }

    public void StopRewardAdState()
    {
        if (coroutineRewardAdState != null)
        {
            StopCoroutine(coroutineRewardAdState);
            coroutineRewardAdState = null;
        }
    }

    private IEnumerator RewardAdStateTime(DateTime endTime)
    {
        while (true)
        {
            if (isApplicationQuit == true)
                break;

            TimeSpan timeout = endTime - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);

            if (timeout >= TimeSpan.Zero)
            {
                yield return new WaitForSeconds(1f);
            }
            else
            {
                yield return new WaitForSeconds(1f);

                SessionLobby.instance.RequestAdStatus(() => { Log("Update Reward Ad Status"); });

                yield break;
            }
        }
    }

    public void StartStoreBonusTimer(DateTime nextTime)
    {
        if (coroutineStoreBonusTimer != null)
        {
            StopCoroutine(coroutineStoreBonusTimer);
            coroutineStoreBonusTimer = null;
        }

        coroutineStoreBonusTimer = StartCoroutine(StoreBonusTime(nextTime));
    }

    public void StopStoreBonusTimer()
    {
        if (coroutineStoreBonusTimer != null)
        {
            StopCoroutine(coroutineStoreBonusTimer);
            coroutineStoreBonusTimer = null;
        }
    }

    private IEnumerator StoreBonusTime(DateTime nextTime)
    {
        while (true)
        {
            if (isApplicationQuit == true)
                break;

            TimeSpan timeout = CommonTools.ToPSTTime(nextTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);

            if (timeout >= TimeSpan.Zero)
            {
                yield return new WaitForSeconds(1f);
            }
            else
            {
                yield return new WaitForSeconds(0.2f);

                if (ActionStoreBonusTimer != null)
                    ActionStoreBonusTimer(true);

                yield break;
            }
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuit = true;

        StopOfferTimer();
        StopBoosterTimer();
        StopRewardAdState();
    }
}
