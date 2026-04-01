using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace CAPYBARA
{
    [CustomEditor(typeof(CurvedMoverSequence))]
    public class CurvedMoverSequenceEditor : UnityEditor.Editor
    {
        private CurvedMoverSequence _seq;

        // ── 일반 재생 상태 ───────────────────────────────────────────
        private class GroupState
        {
            public bool  InStartDelay;
            public int   CurrentIndex;
            public bool  InDelay;
            public float PhaseElapsed;
            public bool  Done;
        }

        private bool _isPlaying;
        private List<GroupState> _groupStates = new List<GroupState>();

        // ── 인터리브 재생 상태 ───────────────────────────────────────
        private struct InterleavedItem
        {
            public CurvedMover Mover;
            public float       StartTime;   // 재생 시작 시각
            public bool        Started;
        }

        private bool _isInterleavedPlaying;
        private float _interleavedElapsed;
        private List<InterleavedItem> _interleavedSchedule = new List<InterleavedItem>();

        // ── 공통 ─────────────────────────────────────────────────────
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private float _interleavedInterval = 0.06f;
        private Vector3 _unifyStartPos;
        private float   _unifyDuration = 1f;

        private void OnEnable()  => _seq = (CurvedMoverSequence)target;
        private void OnDisable() => StopAll();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("에디터 미리보기 (플레이 모드 불필요)", EditorStyles.boldLabel);

            int groupCount = _seq.groups != null ? _seq.groups.Count : 0;
            float totalTime = CalcTotalTime();
            EditorGUILayout.LabelField($"그룹 수: {groupCount}  |  최대 소요 시간: {totalTime:F2}초", EditorStyles.miniLabel);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            bool anyPlaying = _isPlaying || _isInterleavedPlaying;

            if (anyPlaying)
            {
                DrawPlayingUI();
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("■  정지", GUILayout.Height(36)))
                    StopAll();
            }
            else
            {
                GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
                if (GUILayout.Button("▶  전체 그룹 동시 재생", GUILayout.Height(36)))
                    StartPreview();

                EditorGUILayout.Space(4);

                // 인터리브 재생 버튼 + 인터벌 필드
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button("◈  인터리브 재생", GUILayout.Height(36)))
                    StartInterleavedPreview();
                GUI.backgroundColor = Color.white;
                EditorGUILayout.BeginVertical(GUILayout.Width(90));
                GUILayout.Space(8);
                _interleavedInterval = EditorGUILayout.FloatField(_interleavedInterval, GUILayout.Width(54));
                EditorGUILayout.LabelField("간격(초)", EditorStyles.miniLabel, GUILayout.Width(54));
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("일괄 설정", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(anyPlaying);

            EditorGUILayout.BeginHorizontal();
            _unifyStartPos = EditorGUILayout.Vector3Field(GUIContent.none, _unifyStartPos);
            if (GUILayout.Button("모든 시작점 통일", GUILayout.Width(110)))
                UnifyAllStartPositions(_unifyStartPos);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _unifyDuration = EditorGUILayout.FloatField(_unifyDuration);
            if (GUILayout.Button("모든 duration 통일", GUILayout.Width(110)))
                UnifyAllDurations(_unifyDuration);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("➖  모든 제어점 직선으로 세팅", GUILayout.Height(28)))
                SetAllLinearControlPoints();

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
            if (GUILayout.Button("↺  시작 위치로 초기화", GUILayout.Height(28)))
                ResetAllToStart();
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;
        }

        private void DrawPlayingUI()
        {
            if (_isPlaying)
            {
                for (int g = 0; _seq.groups != null && g < _seq.groups.Count; g++)
                {
                    if (g >= _groupStates.Count) break;
                    var group  = _seq.groups[g];
                    var state  = _groupStates[g];
                    float groupTotal   = CalcGroupTime(group);
                    float groupElapsed = CalcGroupElapsed(group, state);
                    float progress     = groupTotal > 0f ? Mathf.Clamp01(groupElapsed / groupTotal) : 1f;

                    string label = state.Done       ? $"{group.groupName}  완료"
                        : state.InStartDelay        ? $"{group.groupName}  시작 대기 중..."
                        : GetCurrentEntryName(group, state);

                    Rect rect = GUILayoutUtility.GetRect(0, 8, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(rect, progress, label);
                    EditorGUILayout.Space(1);
                }
            }
            else if (_isInterleavedPlaying)
            {
                // 전체 진행 바 하나
                float total = _interleavedSchedule.Count > 0
                    ? _interleavedSchedule[_interleavedSchedule.Count - 1].StartTime + GetLastMoverDuration()
                    : 1f;
                float progress = total > 0f ? Mathf.Clamp01(_interleavedElapsed / total) : 1f;
                int fired = 0;
                foreach (var item in _interleavedSchedule)
                    if (item.Started) fired++;

                Rect rect = GUILayoutUtility.GetRect(0, 8, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(rect, progress, $"인터리브  {fired}/{_interleavedSchedule.Count}");
            }
            EditorGUILayout.Space(2);
        }

        // ── 일반 재생 ────────────────────────────────────────────────

        private void StartPreview()
        {
            StopAll();
            if (_seq.groups == null || _seq.groups.Count == 0) return;

            _groupStates.Clear();
            foreach (var group in _seq.groups)
            {
                _groupStates.Add(new GroupState
                {
                    InStartDelay = group.delayBeforeStart > 0f,
                    CurrentIndex = 0,
                    InDelay      = true,
                    PhaseElapsed = 0f,
                    Done         = false
                });
            }

            _isPlaying = true;
            _stopwatch.Restart();
            EditorApplication.update += EditorUpdate;
        }

        // ── 인터리브 재생 ────────────────────────────────────────────

        private void StartInterleavedPreview()
        {
            StopAll();
            if (_seq.groups == null || _seq.groups.Count == 0) return;

            _interleavedSchedule.Clear();
            _interleavedElapsed = 0f;

            int maxEntries = 0;
            foreach (var g in _seq.groups)
                if (g.entries != null && g.entries.Count > maxEntries)
                    maxEntries = g.entries.Count;

            float cursor = 0f;
            for (int ei = 0; ei < maxEntries; ei++)
            {
                foreach (var group in _seq.groups)
                {
                    if (group.entries == null || ei >= group.entries.Count) continue;
                    var entry = group.entries[ei];
                    if (entry?.mover == null) continue;

                    _interleavedSchedule.Add(new InterleavedItem
                    {
                        Mover     = entry.mover,
                        StartTime = cursor,
                        Started   = false
                    });
                    cursor += _interleavedInterval;
                }
            }

            if (_interleavedSchedule.Count == 0) return;

            _isInterleavedPlaying = true;
            _stopwatch.Restart();
            EditorApplication.update += EditorUpdate;
        }

        // ── 공통 업데이트 ────────────────────────────────────────────

        private void StopAll(bool resetAll = true)
        {
            EditorApplication.update -= EditorUpdate;
            _isPlaying           = false;
            _isInterleavedPlaying = false;

            if (_seq != null && resetAll && _seq.groups != null)
            {
                foreach (var group in _seq.groups)
                    foreach (var e in group.entries)
                        if (e?.mover != null)
                        {
                            e.mover.transform.position = e.mover.startPosition;
                            e.mover.gameObject.SetActive(false);
                        }
            }

            SceneView.RepaintAll();
            Repaint();
        }

        private void EditorUpdate()
        {
            if (_seq == null) { StopAll(); return; }

            float delta = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            if (_isPlaying)
                UpdateNormal(delta);
            else if (_isInterleavedPlaying)
                UpdateInterleaved(delta);
        }

        private void UpdateNormal(float delta)
        {
            bool anyRunning = false;
            for (int g = 0; g < _seq.groups.Count && g < _groupStates.Count; g++)
            {
                var group = _seq.groups[g];
                var state = _groupStates[g];
                if (state.Done) continue;

                anyRunning = true;
                state.PhaseElapsed += delta;
                TickGroup(group, state);
            }

            if (!anyRunning) { StopAll(resetAll: false); return; }
            SceneView.RepaintAll();
            Repaint();
        }

        private void UpdateInterleaved(float delta)
        {
            _interleavedElapsed += delta;

            // 시작 시각이 된 mover 활성화
            for (int i = 0; i < _interleavedSchedule.Count; i++)
            {
                var item = _interleavedSchedule[i];
                if (!item.Started && _interleavedElapsed >= item.StartTime)
                {
                    item.Mover.gameObject.SetActive(true);
                    item.Mover.transform.position = item.Mover.startPosition;
                    item.Started = true;
                    _interleavedSchedule[i] = item;
                }
            }

            // 각 mover 위치 업데이트
            bool anyRunning = false;
            foreach (var item in _interleavedSchedule)
            {
                if (!item.Started || item.Mover == null) continue;
                float elapsed = _interleavedElapsed - item.StartTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, item.Mover.duration));
                item.Mover.transform.position = item.Mover.EvaluatePath(t);
                if (t < 1f) anyRunning = true;
            }

            // 마지막 mover도 끝났으면 종료
            if (!anyRunning && _interleavedElapsed >= (_interleavedSchedule.Count > 0
                    ? _interleavedSchedule[_interleavedSchedule.Count - 1].StartTime + GetLastMoverDuration()
                    : 0f))
            {
                StopAll(resetAll: false);
                return;
            }

            SceneView.RepaintAll();
            Repaint();
        }

        private float GetLastMoverDuration()
        {
            if (_interleavedSchedule.Count == 0) return 0f;
            var last = _interleavedSchedule[_interleavedSchedule.Count - 1];
            return last.Mover != null ? last.Mover.duration : 0f;
        }

        private void TickGroup(CurvedMoverGroup group, GroupState state)
        {
            if (state.InStartDelay)
            {
                if (state.PhaseElapsed < group.delayBeforeStart) return;
                state.PhaseElapsed -= group.delayBeforeStart;
                state.InStartDelay = false;
            }

            if (state.CurrentIndex >= group.entries.Count) { state.Done = true; return; }

            var entry = group.entries[state.CurrentIndex];
            if (entry?.mover == null) { AdvanceGroup(group, state); return; }

            if (state.InDelay)
            {
                if (state.PhaseElapsed < entry.delayAfterPrevious) return;
                state.PhaseElapsed -= entry.delayAfterPrevious;
                state.InDelay = false;
                entry.mover.gameObject.SetActive(true);
                entry.mover.transform.position = entry.mover.startPosition;
            }

            float t = Mathf.Clamp01(state.PhaseElapsed / Mathf.Max(0.001f, entry.mover.duration));
            entry.mover.transform.position = entry.mover.EvaluatePath(t);

            if (state.PhaseElapsed >= entry.mover.duration)
                AdvanceGroup(group, state);
        }

        private void AdvanceGroup(CurvedMoverGroup group, GroupState state)
        {
            state.CurrentIndex++;
            if (state.CurrentIndex >= group.entries.Count) { state.Done = true; return; }
            state.InDelay      = true;
            state.PhaseElapsed = 0f;
        }

        // ── 헬퍼 ────────────────────────────────────────────────────

        private void UnifyAllDurations(float duration)
        {
            if (_seq?.groups == null) return;
            foreach (var group in _seq.groups)
                foreach (var e in group.entries)
                {
                    if (e?.mover == null) continue;
                    Undo.RecordObject(e.mover, "Unify Durations");
                    e.mover.duration = duration;
                    EditorUtility.SetDirty(e.mover);
                }
        }

        private void SetAllLinearControlPoints()
        {
            if (_seq?.groups == null) return;
            foreach (var group in _seq.groups)
                foreach (var e in group.entries)
                {
                    if (e?.mover == null) continue;
                    Undo.RecordObject(e.mover, "Set Linear Control Points");
                    e.mover.controlPoint1 = Vector3.Lerp(e.mover.startPosition, e.mover.endPosition, 1f / 3f);
                    e.mover.controlPoint2 = Vector3.Lerp(e.mover.startPosition, e.mover.endPosition, 2f / 3f);
                    EditorUtility.SetDirty(e.mover);
                }
            SceneView.RepaintAll();
        }

        private void UnifyAllStartPositions(Vector3 pos)
        {
            if (_seq?.groups == null) return;
            foreach (var group in _seq.groups)
                foreach (var e in group.entries)
                {
                    if (e?.mover == null) continue;
                    Undo.RecordObject(e.mover, "Unify Start Positions");
                    e.mover.startPosition = pos;
                    EditorUtility.SetDirty(e.mover);
                }
            SceneView.RepaintAll();
        }

        private void ResetAllToStart()
        {
            if (_seq?.groups == null) return;
            foreach (var group in _seq.groups)
                foreach (var e in group.entries)
                {
                    if (e?.mover == null) continue;
                    Undo.RecordObject(e.mover.transform, "Reset to Start");
                    Undo.RecordObject(e.mover.gameObject, "Reset to Start");
                    e.mover.transform.position = e.mover.startPosition;
                    e.mover.gameObject.SetActive(false);
                }
            SceneView.RepaintAll();
        }

        private float CalcTotalTime()
        {
            float max = 0f;
            if (_seq?.groups == null) return max;
            foreach (var group in _seq.groups)
                max = Mathf.Max(max, CalcGroupTime(group));
            return max;
        }

        private float CalcGroupTime(CurvedMoverGroup group)
        {
            float t = group?.delayBeforeStart ?? 0f;
            if (group?.entries == null) return t;
            foreach (var e in group.entries)
            {
                if (e == null) continue;
                t += e.delayAfterPrevious;
                if (e.mover != null) t += e.mover.duration;
            }
            return t;
        }

        private float CalcGroupElapsed(CurvedMoverGroup group, GroupState state)
        {
            if (state.InStartDelay) return state.PhaseElapsed;
            float t = group.delayBeforeStart;
            for (int i = 0; i < state.CurrentIndex && i < group.entries.Count; i++)
            {
                var e = group.entries[i];
                if (e == null) continue;
                t += e.delayAfterPrevious;
                if (e.mover != null) t += e.mover.duration;
            }
            if (!state.Done && state.CurrentIndex < group.entries.Count)
            {
                var cur = group.entries[state.CurrentIndex];
                if (cur != null)
                    t += state.InDelay ? state.PhaseElapsed : cur.delayAfterPrevious + state.PhaseElapsed;
            }
            return t;
        }

        private string GetCurrentEntryName(CurvedMoverGroup group, GroupState state)
        {
            if (state.CurrentIndex >= group.entries.Count) return group.groupName;
            var e = group.entries[state.CurrentIndex];
            string moverName = e?.mover != null ? e.mover.name : "?";
            return $"{group.groupName}  [{state.CurrentIndex + 1}/{group.entries.Count}] {moverName}";
        }
    }
}