using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public interface ISequenceTweenItem
{
    Tween BuildTween();
}

[DisallowMultipleComponent]
public class UIScaleCurveSequencePlayer : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        public UIScaleCurveTween tween;
        [Min(0f)] public float delay;
    }

    [Header("Items (order matters)")]
    public List<Item> items = new();

    [Header("Auto Delay")]
    public bool autoDelayByIndex = true;
    [Min(0f)] public float delayStep = 1f; // 0,1,2,3... 만들려면 1

    [Header("Sequence Options")]
    public bool useUnscaledTime = true;

    Sequence _seq;

    private void OnValidate()
    {
        if (autoDelayByIndex)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                    items[i].delay = i * delayStep;
            }
        }
    }

    public void Play()
    {
        Kill();

        _seq = DOTween.Sequence();
        _seq.SetUpdate(useUnscaledTime);

        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it == null || it.tween == null) continue;

            // 개별 Tween을 만들고, delay에 “삽입”해서 동시에 한 시퀀스에서 스케줄링
            Tween tw = it.tween.BuildTween();
            _seq.Insert(it.delay, tw);
        }

        _seq.Play();
    }

    public void Kill()
    {
        if (_seq != null && _seq.IsActive())
        {
            _seq.Kill();
            _seq = null;
        }
    }
}