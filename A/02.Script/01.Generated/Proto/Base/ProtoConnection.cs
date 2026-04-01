using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Collections;
using UnityEngine;


namespace CAPYBARA 
{
    public class ProtoConnection<TPacket> : IDisposable where TPacket:class,IMessage<TPacket>,new()
    {
        private readonly string _host;
        private readonly int _port;
        private readonly MessageParser<TPacket> _parser;
        private TcpClient _tcp;
        private NetworkStream _ns;
        private CancellationTokenSource _connCts;

        //송수신 검사를 위한 비동기 dictionary(스레드가 달라서 concurrent로)
        private readonly ConcurrentDictionary<string, UniTaskCompletionSource<TPacket>> _pending = new();
        private int _requestIdCounter = 0;

        //private readonly ConcurrentDictionary<string, UniTaskCompletionSource<TPacket>> _pending = new();
        //송신x수신용으로만 받는것 이건 따른곳에서 dequeue해서 사용할것
        public readonly ConcurrentQueue<TPacket> Unsolicited = new();
        private readonly Func<TPacket, string> _getTraceId;
        
        public bool isConnected { get { try { return _tcp?.Connected ?? false; } catch { return false; } } }
        
        string connectionName=string.Empty;
        public ProtoConnection(string host, int port, MessageParser<TPacket> parser,Func<TPacket,string> getTraceId,string _connectionName="")
        {
            _host = host;
            _port=port;
            _parser=parser;
            _getTraceId = getTraceId;
            connectionName=_connectionName;
        }

        public async UniTask ConnectAsync()
        {
            _tcp=new TcpClient();
            await _tcp.ConnectAsync(_host, _port);

            // TCP Keep-Alive: 20분 idle 후 서버/NAT가 연결을 끊는 문제 방지
            // 60초 idle 후 10초 간격으로 probe 전송
            _tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            try
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                // Windows: SIO_KEEPALIVE_VALS IOControl (on=1, idle=60000ms, interval=10000ms)
                byte[] keepAliveValues = new byte[12];
                BitConverter.GetBytes(1u).CopyTo(keepAliveValues, 0);
                BitConverter.GetBytes(60000u).CopyTo(keepAliveValues, 4);
                BitConverter.GetBytes(10000u).CopyTo(keepAliveValues, 8);
                _tcp.Client.IOControl(IOControlCode.KeepAliveValues, keepAliveValues, null);
#elif UNITY_IOS
                // iOS/macOS: TCP_KEEPALIVE=0x10, TCP_KEEPINTVL=0x101, TCP_KEEPCNT=0x102
                _tcp.Client.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)0x10,  60);
                _tcp.Client.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)0x101, 10);
                _tcp.Client.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)0x102,  3);
#else
                // Android/Linux: TCP_KEEPIDLE=4, TCP_KEEPINTVL=5, TCP_KEEPCNT=6
                _tcp.Client.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)4, 60);
                _tcp.Client.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)5, 10);
                _tcp.Client.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)6,  3);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProtoConnection] TCP Keep-Alive 세부 설정 실패 (기본값 사용): {ex.Message}");
            }

            _ns=_tcp.GetStream();
            _connCts=new CancellationTokenSource();
            ReceiveLoop(_connCts.Token).Forget();
        }
        public void Dispose()
        {
            Extension.eLog($"{connectionName} connection disposed",Color.cyan);
            try{_connCts?.Cancel();}catch{}
            try{_ns?.Close();}catch{}
            try{_tcp?.Close();}catch{}
            _tcp = null;
            _ns = null;
            _connCts?.Dispose();
            _connCts = null;
        }
        
        //수신(송신 후 수신시 tcs.TrySetResult로 완료, 단방향수신시 unsolicited에 넣어줌
        private async UniTaskVoid ReceiveLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var data = await ProtoFraming.ReadMessageAsync(_ns, ct);
                    
                    if (CPPlayer.Server._waitingFirstPacketAfterResume)
                    {
                        CPPlayer.Server._waitingFirstPacketAfterResume = false;
                        
                        CPPlayer.Server._loadingShown = false;
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(false));
                    }
                    
                    // CodedInputStream을 사용하여 정확한 길이만 파싱
                    TPacket pkt;
                    try
                    {
                        using (var stream = new CodedInputStream(data))
                        {
                            pkt = new TPacket();
                            pkt.MergeFrom(stream);
                            
                            // 스트림에 남은 데이터가 있는지 확인 (디버깅용)
                            if (!stream.IsAtEnd)
                            {
                                Debug.LogWarning($"[ProtoConnection] 파싱 후 남은 데이터 있음: {data.Length - stream.Position} bytes");
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Debug.LogError($"[ProtoConnection] 파싱 에러: {parseEx.Message}, 데이터 길이: {data.Length}");
                        continue; // 파싱 실패 시 다음 메시지 처리
                    }
                    
                    var trace = _getTraceId(pkt)??string.Empty;

                    if (!string.IsNullOrEmpty(trace) && _pending.TryRemove(trace, out var tcs))
                    {
                        tcs.TrySetResult(pkt);
                    }
                    else
                    {
                        Unsolicited.Enqueue(pkt);
                    }
                }
            }
            catch (Exception e)
            {
                if (ct.IsCancellationRequested)
                    return;
                
                foreach (var kv in _pending)
                {
                    kv.Value.TrySetException(e);
                }
                _pending.Clear();
                Dispose();
                Debug.LogError($"[ProtoConnection] ReceiveLoop 에러: {e.Message}");
            }
        }
        
        // 송신만 (응답 안 기다림)
        public void SendOnly(TPacket packet, Func<TPacket, byte[]> toBytes)
        {
            try
            {
                var bytes = toBytes(packet);
                ProtoFraming.WriteMessageSync(_ns, bytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProtoConnection] SendOnly 실패: {e.Message}");
            }
        }

        //송신용
        public async UniTask<TPacket> RequestAsync(TPacket packet, Func<TPacket, byte[]> toBytes, int timeoutMs = 3000,
            CancellationToken ct = default)
        {
            var trace=_getTraceId(packet);
            if (string.IsNullOrEmpty(trace))
            {
                throw new Exception("No trace found. set TraceID!");
            }

            var tcs = new UniTaskCompletionSource<TPacket>();
            if (!_pending.TryAdd(trace, tcs))
            {
                throw new InvalidOperationException("Duplicated trace_id");
            }

            CancellationTokenRegistration extReg = default;
            if (ct.CanBeCanceled)
                extReg = ct.Register(()=>tcs.TrySetCanceled());

            try
            {
                var bytes=toBytes(packet);
                await ProtoFraming.WriteMessageAsync(_ns,bytes,_connCts?.Token?? CancellationToken.None);

                var (leftWon,resp) = await UniTask.WhenAny(
                    tcs.Task,
                    UniTask.Delay(timeoutMs,cancellationToken:_connCts?.Token??CancellationToken.None)
                );
                if (leftWon)
                    return resp;
                
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"timeOut!! traceID:{trace}"));
                throw new TimeoutException($"timeOut!! traceID:{trace}");

            }
            finally
            {
                extReg.Dispose();
                _pending.TryRemove(trace,out _);
            }
        }
        
        
    }

}
