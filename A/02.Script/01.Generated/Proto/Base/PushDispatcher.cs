
using System;
using CAPYBARA;
using Google.Protobuf;
using UnityEngine;
using Extension = CAPYBARA.Extension;

namespace CPAYBARA
{
    public sealed class PushDispatcher<TPacket>where TPacket:class,IMessage<TPacket>,new()
    {
        private readonly ProtoConnection<TPacket> _conn;
        private readonly Func<TPacket,int> _getPayloadCaseId;
        private readonly System.Collections.Generic.Dictionary<int, Action<TPacket>> _routes = new();

        // 큐 안정화 감지용
        private int _lastQueueCount = 0;
        private float _lastQueueChangeTime = 0f;
        private const float QUEUE_STABLE_DELAY = 0.03f; // 100ms 안정화 대기
        private bool _isWaitingForStable = false;
        
        public PushDispatcher(ProtoConnection<TPacket> conn, Func<TPacket, int> getPayloadCaseId)
        {
            _conn = conn;
            _getPayloadCaseId = getPayloadCaseId;
        }
        
         // Packet.PayloadOneofCase을 통한 이벤트 등록
        public void AddEvent(int caseId, Action<TPacket> handler)=>_routes[caseId] = handler;
        
        public void Pump(int maxPerFrame = 32,bool canLog=false)
        {
            //Debug.LogError("PushDispatcher Pump");
            int currentQueueCount = _conn.Unsolicited.Count;

            // 큐 크기 변화 감지
            if (currentQueueCount != _lastQueueCount)
            {
                _lastQueueCount = currentQueueCount;
                _lastQueueChangeTime = Time.time;
                _isWaitingForStable = currentQueueCount > 0; // 큐가 비어있지 않으면 대기 모드
             
                if(canLog)
                    Debug.LogError($"큐 변화 감지: {currentQueueCount}개 패킷, 안정화 대기 시작");
                return; // 변화가 있으면 처리하지 않고 대기
            }
            // 안정화 대기 중인 경우
            if (_isWaitingForStable)
            {
                float timeSinceLastChange = Time.time - _lastQueueChangeTime;
                if (timeSinceLastChange < QUEUE_STABLE_DELAY)
                {
                    // 아직 안정화 시간이 안 됨
                    if(canLog)
                        Debug.LogError($"큐 안정화 대기! {currentQueueCount}개 패킷 대기 중");
                    return;
                }
                // 안정화 완료, 처리 시작
                _isWaitingForStable = false;
                if(canLog)
                    Debug.LogError($"큐 안정화 완료! {currentQueueCount}개 패킷 처리 시작");
            }

            // 큐가 비어있으면 처리할 것 없음
            if (currentQueueCount == 0)
                return;

            // 패킷 처리 (한 번에 모든 패킷 처리)
            int processedCount = 0;
            int totalPackets = currentQueueCount;

          
            
            // 패킷 처리할 때 Dequeue 대신 일반 Queue 사용
            while (processedCount < maxPerFrame &&_conn.Unsolicited.TryDequeue(out var pkt))
            {
                processedCount++;
                
                //Extension.eLog($"현재 패킷 처리량:{processedCount}",Color.chartreuse);
                var id = _getPayloadCaseId(pkt);
                
                if (_routes.TryGetValue(id, out var _action))
                {
                    SetCurrentBatchInfo(processedCount, totalPackets);
                    _action(pkt);
                    if(canLog)
                        Debug.LogError($" {pkt.ToString()} 패킷 관련 함수 실행 시작");
                }
            }

        }
        
        // 현재 처리 중인 패킷의 배치 정보
        private static int _currentPacketIndex = 0;
        private static int _totalBatchSize = 0;
    
        private void SetCurrentBatchInfo(int currentIndex, int totalSize)
        {
            _currentPacketIndex = currentIndex;
            _totalBatchSize = totalSize;
        }
    
        public static bool IsLastPacketInBatch()
        {
            return _currentPacketIndex == _totalBatchSize;
        }
    
        public static int GetCurrentBatchInfo()
        {
            return _totalBatchSize;
        }
    }
    
}
