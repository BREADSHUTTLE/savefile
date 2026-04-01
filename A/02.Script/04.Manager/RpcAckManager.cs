// RpcAckManager.cs
// Photon PUN2용 RPC-ACK 집계 매니저 (byte[] payload, Ev=AckType)
// - RPC 호출: (byte[] payload, int messageId) 시그니처 권장
// - ACK 수신: RaiseEvent(code=(byte)AckType, data=[msgId,int actor])
// - 결과: 누가 응답/타임아웃/퇴장했는지까지 상세 반환
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mkey;
using UnityEngine;

namespace CAPYBARA
{
  

}

