using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace CAPYBARA
{
    [CustomEditor(typeof(CurvedMover))]
    public class CurvedMoverEditor : UnityEditor.Editor
    {
        private CurvedMover _mover;
        private bool _isPlaying;
        private float _previewT;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        // 핸들 색상
        private static readonly Color StartColor   = new Color(0.2f, 1f, 0.2f);
        private static readonly Color EndColor     = new Color(1f, 0.3f, 0.3f);
        private static readonly Color CP1Color     = new Color(0.2f, 0.8f, 1f);
        private static readonly Color CP2Color     = new Color(1f, 0.8f, 0.2f);
        private static readonly Color CurveColor   = Color.white;

        private void OnEnable()
        {
            _mover = (CurvedMover)target;
        }

        private void OnDisable()
        {
            StopPreview();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("포인트 설정", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📍 현재 위치 → 시작점"))
            {
                Undo.RecordObject(_mover, "Set Start Position");
                _mover.startPosition = _mover.transform.position;
                EditorUtility.SetDirty(_mover);
            }
            if (GUILayout.Button("📍 현재 위치 → 끝점"))
            {
                Undo.RecordObject(_mover, "Set End Position");
                _mover.endPosition = _mover.transform.position;
                EditorUtility.SetDirty(_mover);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("⬅ 시작점으로 이동"))
            {
                Undo.RecordObject(_mover.transform, "Move to Start");
                _mover.transform.position = _mover.startPosition;
            }
            if (GUILayout.Button("➡ 끝점으로 이동"))
            {
                Undo.RecordObject(_mover.transform, "Move to End");
                _mover.transform.position = _mover.endPosition;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("제어점 프리셋", EditorStyles.boldLabel);
            if (GUILayout.Button("➖ 직선으로 세팅"))
            {
                Undo.RecordObject(_mover, "Set Linear Control Points");
                _mover.controlPoint1 = Vector3.Lerp(_mover.startPosition, _mover.endPosition, 1f / 3f);
                _mover.controlPoint2 = Vector3.Lerp(_mover.startPosition, _mover.endPosition, 2f / 3f);
                EditorUtility.SetDirty(_mover);
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("에디터 미리보기 (플레이 모드 불필요)", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            if (!_isPlaying)
            {
                GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
                if (GUILayout.Button("▶  미리보기 재생", GUILayout.Height(36)))
                    StartPreview();
            }
            else
            {
                // 진행 바
                float progress = _previewT;
                Rect rect = GUILayoutUtility.GetRect(0, 8, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(rect, progress, "");
                EditorGUILayout.Space(2);

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("■  정지", GUILayout.Height(36)))
                    StopPreview();
            }

            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();
        }

        // ── 에디터 미리보기 ──────────────────────────────────────────

        private void StartPreview()
        {
            if (_isPlaying) StopPreview();
            _previewT = 0f;
            _isPlaying = true;
            _mover.gameObject.SetActive(true);
            _mover.transform.position = _mover.startPosition;
            _stopwatch.Restart();
            EditorApplication.update += EditorUpdate;
        }

        private void StopPreview(bool resetPosition = true)
        {
            EditorApplication.update -= EditorUpdate;
            _isPlaying = false;
            if (_mover != null)
            {
                if (resetPosition)
                {
                    _mover.transform.position = _mover.startPosition;
                    _mover.gameObject.SetActive(false);
                }
                SceneView.RepaintAll();
            }
            Repaint();
        }

        private void EditorUpdate()
        {
            if (_mover == null) { StopPreview(); return; }

            float delta = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            _previewT += delta / Mathf.Max(0.001f, _mover.duration);

            if (_previewT >= 1f)
            {
                _previewT = 1f;
                _mover.transform.position = CubicBezier(1f);
                StopPreview(resetPosition: false); // 끝점에서 유지
                return;
            }

            _mover.transform.position = CubicBezier(_previewT);
            SceneView.RepaintAll();
            Repaint();
        }

        private Vector3 CubicBezier(float t) => _mover.EvaluatePath(t);

        // ── Scene 핸들 ───────────────────────────────────────────────

        private void OnSceneGUI()
        {
            if (_mover == null) return;

            serializedObject.Update();

            bool changed = false;
            changed |= DrawPointHandle(ref _mover.startPosition,    "시작",   StartColor);
            changed |= DrawPointHandle(ref _mover.endPosition,      "끝",     EndColor);
            changed |= DrawPointHandle(ref _mover.controlPoint1,    "제어1",  CP1Color);
            changed |= DrawPointHandle(ref _mover.controlPoint2,    "제어2",  CP2Color);

            if (changed) EditorUtility.SetDirty(_mover);

            // 제어선 (점선)
            Handles.color = new Color(1, 1, 1, 0.4f);
            Handles.DrawDottedLine(_mover.startPosition, _mover.controlPoint1, 4f);
            Handles.DrawDottedLine(_mover.endPosition,   _mover.controlPoint2, 4f);

            // 베지어 곡선 미리보기
            Handles.DrawBezier(
                _mover.startPosition,
                _mover.endPosition,
                _mover.controlPoint1,
                _mover.controlPoint2,
                CurveColor, null, 2.5f);
        }

        // Shift 드래그 축 고정 상태 (핸들 하나만 드래그되므로 공유)
        private Vector3 _shiftDragStart;
        private bool _isDragging;
        private int _axisLock; // 0=자유, 1=수평(X), 2=수직(Y)

        /// <returns>변경됐으면 true</returns>
        private bool DrawPointHandle(ref Vector3 position, string label, Color color)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseUp)
            {
                _isDragging = false;
                _axisLock = 0;
            }

            Handles.color = color;
            float size = HandleUtility.GetHandleSize(position) * 0.12f;

            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.FreeMoveHandle(position, size, Vector3.zero, Handles.SphereHandleCap);

            Handles.Label(position + Vector3.up * size * 2.5f, label,
                new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = color } });

            if (!EditorGUI.EndChangeCheck()) return false;

            // 드래그 시작 위치 기록
            if (!_isDragging)
            {
                _isDragging = true;
                _shiftDragStart = position;
                _axisLock = 0;
            }

            if (e.shift)
            {
                Vector3 delta = newPos - _shiftDragStart;

                // 처음 유의미한 이동 방향으로 축 결정
                if (_axisLock == 0 && delta.sqrMagnitude > 0.0001f)
                    _axisLock = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? 1 : 2;

                if (_axisLock == 1)       // 수평 고정
                    newPos = new Vector3(newPos.x, _shiftDragStart.y, _shiftDragStart.z);
                else if (_axisLock == 2)  // 수직 고정
                    newPos = new Vector3(_shiftDragStart.x, newPos.y, _shiftDragStart.z);

                // 축 가이드 라인
                if (_axisLock != 0)
                {
                    float lineLen = HandleUtility.GetHandleSize(_shiftDragStart) * 20f;
                    Vector3 dir = _axisLock == 1 ? Vector3.right : Vector3.up;
                    Handles.color = new Color(1f, 1f, 0f, 0.6f);
                    Handles.DrawLine(_shiftDragStart - dir * lineLen, _shiftDragStart + dir * lineLen);
                }
            }
            else
            {
                _axisLock = 0;
            }

            Undo.RecordObject(_mover, $"Move {label}");
            position = newPos;
            return true;
        }
    }
}