#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CAPYBARA
{
    public class InviteTestWindow : EditorWindow
    {
        private const string PREF_KEY = "ATOZPOKER_TestInviteCode";

        private string _input = "";
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.None;

        [MenuItem("ATOZPOKER/Test Invite DeepLink")]
        public static void Open()
        {
            var window = GetWindow<InviteTestWindow>("초대 딥링크 테스트");
            window.minSize = new Vector2(400, 280);
            window.Show();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RestorePendingCode()
        {
            string saved = EditorPrefs.GetString(PREF_KEY, "");
            if (string.IsNullOrEmpty(saved))
                return;

            DeepLinkData.PendingInviteCode = saved;
            EditorPrefs.DeleteKey(PREF_KEY);
            Debug.Log($"[InviteFriend][EditorTest] Play 모드 진입 - 저장된 초대 코드 복원: {saved}");
        }

        private static string ExtractInviteCode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            if (input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                int idx = input.IndexOf("af_sub1=", StringComparison.Ordinal);
                if (idx >= 0)
                {
                    string sub = input.Substring(idx + 8);
                    int end = sub.IndexOf('&');
                    return end >= 0 ? sub.Substring(0, end) : sub;
                }
                return "";
            }

            return input.Trim();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("초대 딥링크 수신 시뮬레이션", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (Application.isPlaying && CPPlayer.UserInfo.userDatabase?.User != null)
            {
                EditorGUILayout.LabelField("내 코드", CPPlayer.UserInfo.userDatabase.User.Code);
            }

            GUILayout.Space(5);
            GUILayout.Label("초대 코드 또는 초대 링크 URL을 입력하세요:");
            _input = EditorGUILayout.TextField("입력", _input);

            string code = ExtractInviteCode(_input);
            if (!string.IsNullOrEmpty(code) && code != _input.Trim())
                EditorGUILayout.LabelField("추출된 코드", code);

            GUILayout.Space(5);
            var hasPending = DeepLinkData.HasPendingInvite;
            EditorGUILayout.LabelField("현재 PendingInviteCode",
                hasPending ? DeepLinkData.PendingInviteCode : "(없음)");

            string savedPref = EditorPrefs.GetString(PREF_KEY, "");
            if (!string.IsNullOrEmpty(savedPref))
                EditorGUILayout.LabelField("Play 시 복원 예정", savedPref);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(code)))
            {
                if (GUILayout.Button("1. 코드 설정 (Play 모드 진입 → 로비에서 자동 처리)"))
                {
                    EditorPrefs.SetString(PREF_KEY, code);
                    DeepLinkData.PendingInviteCode = code;
                    SetStatus($"코드 저장 완료: \"{code}\"\nPlay 모드 진입 후 로비에서 자동 처리됩니다.", MessageType.Info);
                }
            }

            GUILayout.Space(5);

            bool canProcess = Application.isPlaying
                && !string.IsNullOrEmpty(code)
                && CPPlayer.UserInfo.userDatabase?.User != null;

            using (new EditorGUI.DisabledGroupScope(!canProcess))
            {
                if (GUILayout.Button("2. 즉시 처리 (바로 서버 호출)"))
                {
                    EditorPrefs.DeleteKey(PREF_KEY);
                    DeepLinkData.PendingInviteCode = code;
                    SetStatus($"처리 중... (코드: {code})", MessageType.Info);
                    InviteFriendManager.ProcessPendingInviteCode().ContinueWith(() =>
                    {
                        SetStatus($"처리 완료! (코드: {code})\nConsole 로그를 확인하세요.", MessageType.Info);
                    }).Forget();
                }
            }

            if (!Application.isPlaying)
            {
                GUILayout.Space(3);
                EditorGUILayout.HelpBox("'즉시 처리'는 플레이 모드에서만 사용 가능합니다.", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Debug.Log($"[InviteFriend][EditorTest] {message}");
            Repaint();
        }
    }
}
#endif
