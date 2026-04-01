using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using CAPYBARA.badugi;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using Google.Protobuf;

namespace CAPYBARA
{
    public abstract class BaseClient<TPacket,TBase> 
        where TPacket:class,IMessage<TPacket>,new()
        where TBase:class,IMessage<TBase>
    {
        protected ProtoConnection<TPacket> _conn;
        protected readonly uint _protocolVersion;

        protected BaseClient(ProtoConnection<TPacket> conn, uint protocolVersion)
        {
            _conn = conn;
            _protocolVersion = protocolVersion;
        }

        protected virtual string NewTraceId() => Guid.NewGuid().ToString("N").Substring(0, 12);

        protected abstract TBase NewBase(string req,  uint ver, string trace);
        protected abstract void SetBase(TPacket pkt, TBase @base);
        protected abstract string GetError(TPacket res);
        protected abstract byte[] ToBytes(TPacket pkt);
        
        public string Token { get;protected set; }

        protected void ThrowIfError(TPacket res)
        {
            var err=GetError(res);
            if (!string.IsNullOrEmpty(err))
            {
                if (err != "EOk")
                {
                    Debug.LogError(err+$"//{res}");
                    
                    // 서버 에러 Slack 알림 (심각한 에러만)
                    if (ShouldAlertError(err))
                        SendServerErrorToSlack(err, res.ToString()).Forget();
                }
            }
                
        }
        
        // 알림 보낼 에러인지 확인 (EInternalError만 알림)
        private bool ShouldAlertError(string error)
        {
            if (error.Contains("CancellationToken") || error.Contains("ObjectDisposed") || error.Contains("OperationCanceled") ||
                error.Contains("SocketException") || error.Contains("Connection reset") || error.Contains("IOException") ||
                error.Contains("Unable to read data") || error.Contains("transport connection"))
                return false;
            
            // 서버 내부 에러(DB 에러 포함)만 알림
            return error.Contains("EInternalError") || error.Contains("E_INTERNAL_ERROR");
        }
        
        // 에러 중복 방지용 (에러키 - 마지막 전송 시간)
        private static Dictionary<string, float> _lastServerErrorSentTime = new Dictionary<string, float>();
        private const float SERVER_ERROR_COOLDOWN_SECONDS = 60f; // 동일 에러 쿨다운 (60초)
        
        // Slack으로 서버 에러 알림 (실제 기기에서만 전송)
        private static async UniTaskVoid SendServerErrorToSlack(string error, string detail)
        {
#if UNITY_EDITOR
            Debug.Log($"[Slack] 에디터에서는 알림 전송 생략 - 에러: {error}");
            return;
#else
            try
            {
                // 중복 에러 방지 동일 에러는 60초에 1번만
                var errorKey = $"{error}|{detail.GetHashCode()}";
                if (_lastServerErrorSentTime.TryGetValue(errorKey, out var lastTime))
                {
                    if ((Time.realtimeSinceStartup - lastTime) < SERVER_ERROR_COOLDOWN_SECONDS)
                    {
                        Debug.Log($"[Slack] 중복 에러 무시 (쿨다운 중): {error}");
                        return;
                    }
                }
                _lastServerErrorSentTime[errorKey] = Time.realtimeSinceStartup;
                
                var webhook = "https://hooks.slack.com/";
                
                // 빌드 정보
                var version = Application.version;
                var platform = Application.platform.ToString();
                
                // JSON 특수문자 escape
                var safeDetail = detail.Substring(0, Math.Min(detail.Length, 200))
                    .Replace("\\", "\\\\")
                    .Replace("\"", "'")
                    .Replace("\n", " ")
                    .Replace("\r", "");
                
                var message = $":octagonal_sign: *서버 에러 발생*\\n\\n*ATOZ POKER*\\n버전: `{version}` ({platform})\\n에러: `{error}`\\n상세: {safeDetail}";
                var payload = $"{{\"text\": \"{message}\"}}";
                
                using var request = new UnityWebRequest(webhook, "POST");
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
                request.SetRequestHeader("Content-Type", "application/json");
                await request.SendWebRequest();
                
                Debug.Log("[Slack] 알림 전송 완료!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Slack 알림 전송 실패: {e.Message}");
            }
#endif
        }
        

        // 응답 안 기다리고 전송만 
        protected void SendOnly(TPacket pkt)
        {
            _conn.SendOnly(pkt, ToBytes);
        }

        // protected async UniTask<TPacket> RequestAsync(TPacket pkt,int timeoutMs,CancellationToken ct)
        // {
        //     Extension.eLog(pkt.ToString(),Color.yellow);
        //     var res = await _conn.RequestAsync(pkt, ToBytes, timeoutMs, ct);
        //     ThrowIfError(res);
        //
        //     Extension.eLog(res.ToString(),Color.white);
        //     
        //     return res;
        // }
        
        
        private readonly Queue<RequestItem> _queue = new Queue<RequestItem>();
        private bool _isProcessing;

        /// <summary>
        /// 마지막으로 RequestAsync가 호출된 실시간(UTC). 백그라운드 타임아웃 판단에 사용.
        /// </summary>
        public DateTime LastRequestAt { get; private set; } = DateTime.MinValue;

        private class RequestItem
        {
            public TPacket Packet;
            public int TimeoutMs;
            public CancellationToken Ct;
            public UniTaskCompletionSource<TPacket> Tcs;
        }

        //한 프레임 내에 conn api 호출시 오류나므로
        public UniTask<TPacket> RequestAsync(TPacket pkt, int timeoutMs, CancellationToken ct, bool isErrorToast = true)
        {
            LastRequestAt = DateTime.UtcNow;

            var tcs = new UniTaskCompletionSource<TPacket>();

            _queue.Enqueue(new RequestItem
            {
                Packet = pkt,
                TimeoutMs = timeoutMs,
                Ct = ct,
                Tcs = tcs
            });

            if (!_isProcessing)
            {
                ProcessQueue(isErrorToast).Forget(); 
            }

            return tcs.Task;
        }

        private async UniTaskVoid ProcessQueue(bool isErrorToast = true)
        {
            _isProcessing = true;

            while (_queue.Count > 0)
            {
                var item = _queue.Dequeue();

                try
                {
                    Extension.eLog(item.Packet.ToString(), Color.yellow);

                    var res = await _conn.RequestAsync(item.Packet, ToBytes, item.TimeoutMs, item.Ct);
                    if (isErrorToast)
                        ThrowIfError(res);

                    Extension.eLog(res.ToString(), Color.white);

                    item.Tcs.TrySetResult(res);
                }
                catch (Exception ex)
                {
                    item.Tcs.TrySetException(ex);
                }
            }

            _isProcessing = false;
        }
    }

#if UNITY_EDITOR
    // 에디터 테스트용 - 별도 클래스 (제네릭 아님)
    public static class SlackTestMenu
    {
        [UnityEditor.MenuItem("Tools/Slack 알림 테스트")]
        public static void TestSlackAlert()
        {
            SendTestSlackMessage().Forget();
        }
        
        private static async UniTaskVoid SendTestSlackMessage()
        {
            try
            {
                var webhook = "https://hooks.slack.com/";
                var message = ":octagonal_sign: *알림 테스트*\\n\\n*ATOZ POKER*\\n환경: Unity Editor\\n상태: 정상 작동";
                var payload = $"{{\"text\": \"{message}\"}}";
                
                using var request = new UnityWebRequest(webhook, "POST");
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
                request.SetRequestHeader("Content-Type", "application/json");
                await request.SendWebRequest();
                
                Debug.Log("[Slack] 테스트 알림 전송 완료! Slack 채널 확인하세요.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Slack] 테스트 알림 전송 실패: {e.Message}");
            }
        }
    }
#endif

}
