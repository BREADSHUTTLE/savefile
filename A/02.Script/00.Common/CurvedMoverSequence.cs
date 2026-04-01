using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CAPYBARA
{
    [Serializable]
    public class CurvedMoverEntry
    {
        public CurvedMover mover;
        [Tooltip("이전 항목이 시작된 뒤 대기 시간 (초)")]
        public float delayAfterPrevious = 0f;
    }

    [Serializable]
    public class CurvedMoverGroup
    {
        public string groupName = "Group";
        [Tooltip("Play() 호출 후 이 그룹이 시작되기까지의 대기 시간 (초)")]
        public float delayBeforeStart = 0f;
        public List<CurvedMoverEntry> entries = new List<CurvedMoverEntry>();
    }

    public class CurvedMoverSequence : MonoBehaviour
    {
        public List<CurvedMoverGroup> groups = new List<CurvedMoverGroup>();

        private readonly List<Sequence> _activeSequences = new List<Sequence>();

        /// <summary>모든 그룹을 동시에 재생</summary>
        public void Play()
        {
            Stop();
            foreach (var group in groups)
            {
                var seq = BuildSequence(group);
                if (seq != null) _activeSequences.Add(seq);
            }
        }

        /// <summary>
        /// 그룹1엔트리1 → 그룹2엔트리1 → ... → 그룹N엔트리1 → 그룹1엔트리2 → ...
        /// 순서로 interval 간격을 두고 순차 재생
        /// </summary>
        public void PlayInterleaved(float interval = 0.06f)
        {
            Stop();

            int maxEntries = 0;
            foreach (var g in groups)
                if (g.entries != null && g.entries.Count > maxEntries)
                    maxEntries = g.entries.Count;

            Sequence seq = DOTween.Sequence();
            float cursor = 0f;

            Debug.Log($"[PlayInterleaved] Start {System.DateTime.Now:HH:mm:ss.fff}");

            for (int ei = 0; ei < maxEntries; ei++)
            {
                foreach (var group in groups)
                {
                    if (group.entries == null || ei >= group.entries.Count) continue;
                    var entry = group.entries[ei];
                    if (entry?.mover == null) continue;

                    var captured = entry;
                    float insertAt = cursor;
                    seq.InsertCallback(insertAt, () => captured.mover.Play());
                    cursor += interval;
                }
            }

            seq.OnComplete(() => Debug.Log($"[PlayInterleaved] End {System.DateTime.Now:HH:mm:ss.fff}"));

            _activeSequences.Add(seq);
        }

        /// <summary>특정 인덱스의 그룹만 재생</summary>
        public void PlayGroup(int index)
        {
            if (index < 0 || index >= groups.Count) return;
            Stop();
            var seq = BuildSequence(groups[index]);
            if (seq != null) _activeSequences.Add(seq);
        }

        public void Stop()
        {
            foreach (var seq in _activeSequences)
                seq?.Kill();
            _activeSequences.Clear();

            foreach (var group in groups)
                foreach (var entry in group.entries)
                    entry.mover?.Stop();
        }

        private Sequence BuildSequence(CurvedMoverGroup group)
        {
            if (group == null || group.entries == null || group.entries.Count == 0)
                return null;

            Sequence seq = DOTween.Sequence();
            float cursor = group.delayBeforeStart;

            foreach (var entry in group.entries)
            {
                if (entry.mover == null) continue;
                cursor += entry.delayAfterPrevious;

                var captured = entry;
                float insertAt = cursor;
                seq.InsertCallback(insertAt, () => captured.mover.Play());
            }

            return seq;
        }

        private void OnDestroy() => Stop();
    }
}
