using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[ExecuteAlways]
[DisallowMultipleComponent]
public class UISequencePlayer : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        [Tooltip("이 delay 시점에 동시에 재생할 트윈 컴포넌트들")]
        public List<Component> actions = new();

        [Min(0f)]
        public float delay;
    }

    [Header("Timeline Items")]
    public List<Item> items = new();

    [Header("Auto Delay")]
    public bool autoDelayByIndex = true;
    [Min(0f)] public float delayStep = 1f;

    [Header("Sequence Options")]
    public bool useUnscaledTime = true;

    Sequence _seq;

    private void OnValidate()
    {
        if (!autoDelayByIndex) return;
        for (int i = 0; i < items.Count; i++)
            if (items[i] != null)
                items[i].delay = i * delayStep;
    }

    public void Play()
    {
        Kill();

        _seq = DOTween.Sequence();
        _seq.SetUpdate(useUnscaledTime);

        foreach (var item in items)
        {
            if (item == null || item.actions == null) continue;

            foreach (var comp in item.actions)
            {
                if (comp == null) continue;

                Tween tw = BuildTweenFromComponent(comp);
                if (tw == null) continue;

                _seq.Insert(item.delay, tw);
            }
        }

        _seq.Play();
    }

    Tween BuildTweenFromComponent(Component comp)
    {
        if (comp is ISequenceTweenItem builder)
            return builder.BuildTween();

        Debug.LogWarning($"UISequencePlayer: {comp.name} 는 ISequenceTweenItem이 아닙니다.");
        return null;
    }

    public void Kill()
    {
        if (_seq != null && _seq.IsActive())
        {
            _seq.Kill();
            _seq = null;
        }

        // 각 컴포넌트가 Kill(bool) 있으면 복구까지 하려면 아래 호출(선택)
        // RestoreAll();
    }

    public void RestoreAll()
    {
        foreach (var item in items)
        {
            if (item?.actions == null) continue;
            foreach (var comp in item.actions)
            {
                if (comp == null) continue;
                InvokeKillBool(comp, true);
            }
        }
    }

    static void InvokeKillBool(Component comp, bool reset)
    {
        var m = comp.GetType().GetMethod("Kill",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(bool) }, null);

        if (m != null)
        {
            try { m.Invoke(comp, new object[] { reset }); }
            catch { /* ignore */ }
        }
    }

    private void OnDisable()
    {
        Kill();
    }
}
