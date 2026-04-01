using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using UnityEngine.SceneManagement;

namespace CAPYBARA
{
    public class ConnectionManager : MonoSingleton<ConnectionManager>
    {
        [Header("Proto Noti Updater")] [SerializeField]
        LobbyDispatchPushHub lobbyDispatcher;

        [SerializeField] HoldemDispatchPushHub holdemDispatcher;
        [SerializeField] BadugiDispatchPushHub badugiDispatcher;
        [SerializeField] SPokerDispatchPushHub spokerDispatcher;

        private System.Threading.CancellationTokenSource _cts;
        private static bool isReconnecting = false;
        private static int maxRetryCount = 3;
        private static int reconnectTimeoutMs = 5000; // 10초 타임아웃

        private bool _isInBackground = false;

        protected override void Init()
        {
            base.Init();
            _cts = new System.Threading.CancellationTokenSource();

            if (lobbyDispatcher == null)
                lobbyDispatcher = FindFirstObjectByType<LobbyDispatchPushHub>();
            if (holdemDispatcher == null)
                holdemDispatcher = FindFirstObjectByType<HoldemDispatchPushHub>();
            if (badugiDispatcher == null)
                badugiDispatcher = FindFirstObjectByType<BadugiDispatchPushHub>();
            if (spokerDispatcher == null)
                spokerDispatcher = FindFirstObjectByType<SPokerDispatchPushHub>();


            StartLobbyConnectionMonitoring().Forget();
            HeartBeatToMaintainServer().Forget();
        }

        public void LobbyDispatcherInit()
        {
            lobbyDispatcher.Init();
        }

        public async UniTask HoldemConnect()
        {
            int maxRetryCount = 3; // 최대 재시도 횟수
            int currentTry = 0;
            isReconnecting = true;

            while (currentTry < maxRetryCount)
            {
                try
                {
                    bool isFirstConnect = CPPlayer.Server.holdemConnection == null;
                    UnityEngine.Debug.LogWarning($"홀덤 서버 리스트 가져오기");
                    var serverListpacket = await Services.Lobby.GetServerListAsync(Common.GameType.GtHoldem);
                    if (!serverListpacket.IsSuccess)
                    {
                        CPPlayer.Server.lobbyConnection?.Dispose();
                        CPPlayer.Server.holdemConnection?.Dispose();
                        await UniTask.WaitUntil(() => CPPlayer.Server.lobbyConnection?.isConnected == true);
                        await UniTask.WaitUntil(() => isReLoginComplete == true);
                        UnityEngine.Debug.LogWarning($"로비 서버 연결 및 로그인 성공");
                    }

                    var holdemServerInfo = serverListpacket.Data.List[0];

                    UnityEngine.Debug.LogWarning($"홀덤 연결");
                    await ProtoBootStrap.InitHoldem(holdemServerInfo.Host, (int)holdemServerInfo.Port);
                    var holdemconnectRes = await Services.Holdem.ConnectAsync(Services.Lobby.Token);

                    if (!holdemconnectRes.IsSuccess)
                    {
                        currentTry++;
                        UnityEngine.Debug.LogWarning($"홀덤 서버 접속 실패. 재로그인 시도 중... ({currentTry}/{maxRetryCount})");

                        CPPlayer.Server.lobbyConnection?.Dispose();
                        CPPlayer.Server.holdemConnection?.Dispose();
                        await UniTask.WaitUntil(() => CPPlayer.Server.lobbyConnection?.isConnected == true);
                        await UniTask.WaitUntil(() => isReLoginComplete == true);
                        UnityEngine.Debug.LogWarning($"로비 서버 연결 및 로그인 성공");

                        if (currentTry >= maxRetryCount)
                        {
                            UnityEngine.Debug.LogError("홀덤 서버 접속 최대 재시도 횟수 초과. 로비로 이동하거나 에러 팝업을 띄워야 합니다.");
                            return;
                        }
                    }
                    else
                    {
                        DateTime hserverUtc = holdemconnectRes.Data.Ts.ToDateTime();
                        CPPlayer.Holdem.serverTime = hserverUtc;
                        DateTime hclientUtc = DateTime.UtcNow;
                        CPPlayer.Holdem.timeGap = hclientUtc - hserverUtc;

                        holdemDispatcher.Init();

                        CPPlayer.Server.currentConnectedGameType = GameType.HOLDEM;
                        break;
                    }
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    currentTry++;
                    UnityEngine.Debug.LogWarning($"홀덤 연결 중 SocketException (idle 후 연결 끊김 추정). 로비 재연결 시도 중... ({currentTry}/{maxRetryCount})\n{ex.Message}");

                    CPPlayer.Server.lobbyConnection?.Dispose();
                    CPPlayer.Server.holdemConnection?.Dispose();
                    await UniTask.WaitUntil(() => CPPlayer.Server.lobbyConnection?.isConnected == true);
                    await UniTask.WaitUntil(() => isReLoginComplete == true);
                    UnityEngine.Debug.LogWarning($"로비 서버 재연결 및 로그인 성공");

                    if (currentTry >= maxRetryCount)
                    {
                        UnityEngine.Debug.LogError("홀덤 서버 접속 최대 재시도 횟수 초과 (SocketException).");
                        return;
                    }
                }
                catch (System.IO.IOException ex)
                {
                    currentTry++;
                    UnityEngine.Debug.LogWarning($"홀덤 연결 중 IOException (idle 후 연결 끊김 추정). 로비 재연결 시도 중... ({currentTry}/{maxRetryCount})\n{ex.Message}");

                    CPPlayer.Server.lobbyConnection?.Dispose();
                    CPPlayer.Server.holdemConnection?.Dispose();
                    await UniTask.WaitUntil(() => CPPlayer.Server.lobbyConnection?.isConnected == true);
                    await UniTask.WaitUntil(() => isReLoginComplete == true);
                    UnityEngine.Debug.LogWarning($"로비 서버 재연결 및 로그인 성공");

                    if (currentTry >= maxRetryCount)
                    {
                        UnityEngine.Debug.LogError("홀덤 서버 접속 최대 재시도 횟수 초과 (IOException).");
                        return;
                    }
                }
            }
        }

        public async UniTask BadugiConnect()
        {
            int maxRetryCount = 3; // 최대 재시도 횟수
            int currentTry = 0;
            isReconnecting = true;

            while (currentTry < maxRetryCount)
            {
                bool isFirstConnect = CPPlayer.Server.badugiConnection == null;
                var serverBadugiRes = await Services.Lobby.GetServerListAsync(Common.GameType.GtBadugi);
                var badugiServerInfo = serverBadugiRes.Data.List[0];

                await ProtoBootStrap.InitBadugi(badugiServerInfo.Host, (int)badugiServerInfo.Port);

                var badugiConnectRes = await Services.Badugi.ConnectAsync(Services.Lobby.Token);

                if (!badugiConnectRes.IsSuccess)
                {
                    currentTry++;
                    UnityEngine.Debug.LogWarning($"바둑이 서버 접속 실패. 재로그인 시도 중... ({currentTry}/{maxRetryCount})");

                    CPPlayer.Server.lobbyConnection?.Dispose();
                    CPPlayer.Server.badugiConnection?.Dispose();
                    await UniTask.WaitUntil(() => CPPlayer.Server.lobbyConnection?.isConnected == true);
                    await UniTask.WaitUntil(() => isReLoginComplete == true);

                    if (currentTry >= maxRetryCount)
                    {
                        UnityEngine.Debug.LogError("바둑이 서버 접속 최대 재시도 횟수 초과. 로비로 이동하거나 에러 팝업을 띄워야 합니다.");
                        return;
                    }
                }
                else
                {
                    CPPlayer.Badugi.ingameUid = badugiConnectRes.Data.Uid;

                    DateTime bserverUtc = badugiConnectRes.Data.Ts.ToDateTime();
                    CPPlayer.Badugi.serverTime = bserverUtc;
                    DateTime bclientUtc = DateTime.UtcNow;
                    CPPlayer.Badugi.timeGap = bclientUtc - bserverUtc;

                    badugiDispatcher.Init();

                    CPPlayer.Server.currentConnectedGameType = GameType.LOW_BADUGI;
                    break;
                }
            }
        }


        public async UniTask SevenPokerConnect()
        {
            int maxRetryCount = 3; // 최대 재시도 횟수
            int currentTry = 0;
            isReconnecting = true;

            while (currentTry < maxRetryCount)
            {
                bool isFirstConnect = CPPlayer.Server.sevenPokerConnection == null;
                var serverListSpokerpacket = await Services.Lobby.GetServerListAsync(Common.GameType.GtSevenPoker);
                var spokerServerInfo = serverListSpokerpacket.Data.List[0];

                if (isFirstConnect)
                {
                    await ProtoBootStrap.InitSPoker(spokerServerInfo.Host, (int)spokerServerInfo.Port);
                }
                else
                {
                    if (CPPlayer.Server.sevenPokerConnection.isConnected == false)
                    {
                        await ProtoBootStrap.InitSPoker(spokerServerInfo.Host, (int)spokerServerInfo.Port);
                    }
                }

                var spokerConnectRes = await Services.SevenPoker.ConnectAsync(Services.Lobby.Token);

                if (!spokerConnectRes.IsSuccess)
                {
                    currentTry++;
                    UnityEngine.Debug.LogWarning($"세븐포커 서버 접속 실패. 재로그인 시도 중... ({currentTry}/{maxRetryCount})");

                    CPPlayer.Server.lobbyConnection?.Dispose();
                    CPPlayer.Server.sevenPokerConnection?.Dispose();
                    await UniTask.WaitUntil(() => CPPlayer.Server.lobbyConnection?.isConnected == true);
                    await UniTask.WaitUntil(() => isReLoginComplete == true);

                    if (currentTry >= maxRetryCount)
                    {
                        UnityEngine.Debug.LogError("세븐포커 서버 접속 최대 재시도 횟수 초과. 로비로 이동하거나 에러 팝업을 띄워야 합니다.");
                        return;
                    }
                }
                else
                {
                    CPPlayer.SPoker.ingameUid = spokerConnectRes.Data.Uid;

                    DateTime sserverUtc = spokerConnectRes.Data.Ts.ToDateTime();
                    CPPlayer.SPoker.serverTime = sserverUtc;
                    DateTime sclientUtc = DateTime.UtcNow;
                    CPPlayer.SPoker.timeGap = sclientUtc - sserverUtc;

                    spokerDispatcher.Init();
                    CPPlayer.Server.currentConnectedGameType = GameType.SEVEN_POKER;
                    break;
                }
            }
        }

        public void CloseHoldemConnection()
        {
            CPPlayer.Server.holdemConnection.Dispose();
            holdemDispatcher.Dispose();
        }

        public void CloseBadugiConnection()
        {
            CPPlayer.Server.badugiConnection.Dispose();
            badugiDispatcher.Dispose();
        }

        public void CloseSevenPokerConnection()
        {
            CPPlayer.Server.sevenPokerConnection.Dispose();
            spokerDispatcher.Dispose();
        }

        private bool isReLoginComplete = false;

        /// <summary>
        /// 로비 연결 상태를 지속적으로 모니터링하고 필요시 재연결을 시도합니다
        /// </summary>
        private async UniTask StartLobbyConnectionMonitoring()
        {
            var token = _cts.Token;
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;
                isReconnecting = false;

                if (CPPlayer.Server.lobbyConnection?.isConnected == false && !isReconnecting)
                {
                   
                    isReconnecting = true;
                    isReLoginComplete = false;
                    try
                    {
                        // 로딩 팝업 표시
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(true));
                        if (_cts == null)
                            break;
                        // 재연결 시도
                        bool reconnected = await TryReconnectLobbyWithRetry(5, token: token);
                    
                        if (_cts == null)
                        {
                            CPPlayer.Server.lobbyConnection?.Dispose();
                            break;
                        }
                    
                        if (reconnected)
                        {
                            Debug.Log("로비 재연결 성공");
                        }
                        else
                        {
                            Debug.LogError("로비 재연결 실패 - 최대 재시도 횟수 초과");
                    
                            //
                            PopupManager.Instance.Open<PopupNetworkCheck>();
                    
                            //Dispose();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"로비 재연결 중 예외 발생: {ex.Message}");
                    }
                    finally
                    {
                        //로딩 팝업 숨김
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(false));
                        isReconnecting = false;
                    }
                }

                // 1초마다 연결 상태 확인
                await UniTask.Delay(300, cancellationToken: token);
            }
        }

        private void LogOutAndOpenLoadingScene()
        {
            CPPlayer.Dispose();
            PoolManager.Clear();

            SceneManager.LoadScene("Loading");
        }

        private int pingelapsedTime = 320000;

        private async UniTask HeartBeatToMaintainServer()
        {
            var token = _cts.Token;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                await UniTask.Delay(pingelapsedTime, DelayType.Realtime, cancellationToken: token);
                int elapsedMilSec = (int)sw.ElapsedMilliseconds;
                sw.Restart();
                if (CPPlayer.Server.lobbyConnection?.isConnected == true)
                {
                    try
                    {
                        await Services.Lobby.PingReqAsync(elapsedMilSec, token);
                        Debug.LogWarning($"Lobby Ping 성공");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Lobby Ping 실패: {e.Message}");
                    }
                }


                if (CPPlayer.Server.holdemConnection?.isConnected == true)
                {
                    try
                    {
                        await Services.Holdem.PingReqAsync(elapsedMilSec, token);
                        Debug.LogWarning($"Holdem Ping 성공");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Holdem Ping 실패: {e.Message}");
                    }
                }

                if (CPPlayer.Server.badugiConnection?.isConnected == true)
                {
                    try
                    {
                        await Services.Badugi.PingReqAsync(elapsedMilSec, token);
                        Debug.LogWarning($"Badugi Ping 성공");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Badugi Ping 실패: {e.Message}");
                    }
                }

                if (CPPlayer.Server.sevenPokerConnection?.isConnected == true)
                {
                    try
                    {
                        await Services.SevenPoker.PingReqAsync(elapsedMilSec, token);
                        Debug.LogWarning($"SevenPoker Ping 성공");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"SevenPoker Ping 실패: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 로비 재연결을 여러 차례 시도합니다
        /// </summary>
        private async UniTask<bool> TryReconnectLobbyWithRetry(int maxTryCount = 5, CancellationToken token = default)
        {
            int maxRetryCount = maxTryCount; // 최대 재시도 횟수
            int currentTry = 0;
            isReconnecting = true;

            while (currentTry < maxRetryCount)
            {
                if (token.IsCancellationRequested) return false; // 추가   
                try
                {
                    Debug.Log($"로비 재연결 시도 {currentTry + 1}/{maxRetryCount}");

                    // 기존 연결 정리 - null로 설정해서 새 ProtoConnection 생성 유도
                    CPPlayer.Server.lobbyConnection?.Dispose();
                    CPPlayer.Server.lobbyConnection = null;
                    Services.Lobby = null;
                    Services.LobbyDispatcher = null;

                    // 타임아웃과 함께 연결 시도
                    var connectTask = ProtoBootStrap.InitLobby(CPPlayer.IpPortData.ipportinfos.LobbyInfo.ip, CPPlayer.IpPortData.ipportinfos.LobbyInfo.port);

                    var timeoutTask = UniTask.Delay(reconnectTimeoutMs, cancellationToken: token);

                    var hasCompleted = await UniTask.WhenAny(connectTask, timeoutTask);

                    if (hasCompleted == 0) // 연결 성공
                    {
                        LobbyDispatcherInit();
                        
                        if (LoginData.Cloud.loginValue == null)
                            return true;
                        if (LoginData.Cloud.loginValue.loginres == null)
                            return true;
                        
                        // 연결이 실제로 되었는지 확인
                        if (CPPlayer.Server.lobbyConnection?.isConnected == true)
                        {
                            bool loginSuccess = await ReLoginProccess();
                            if (loginSuccess)
                            {
                                isReLoginComplete = true;
                                break; // 성공
                            }
                            else
                            {
                                isReLoginComplete = false;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"로비 연결 타임아웃 (시도 {currentTry + 1})");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"로비 재연결 시도 {currentTry + 1} 실패: {ex.Message}");
                }

                currentTry++;

                // 마지막 시도가 아니면 잠시 대기
                if (currentTry < maxRetryCount)
                {
                    await UniTask.Delay(1000 * currentTry, cancellationToken: token); // 점진적 백오프
                }
            }

            if (isReLoginComplete)
            {
                Debug.LogError($"로비 재연결 시도 {currentTry} 성공");
                return true;
            }
            else
            {
                Debug.LogError($"로비 재연결 시도 {currentTry} 실패");
                return false; // 모든 시도 실패    
            }
        }

        public async UniTask<bool> TryReconnectLobbyOnce()
        {
            try
            {
                // 기존 연결 정리 - null로 설정해서 새 ProtoConnection 생성 유도
                CPPlayer.Server.lobbyConnection?.Dispose();
                CPPlayer.Server.lobbyConnection = null;
                Services.Lobby = null;
                Services.LobbyDispatcher = null;
                
                // 연결 시도
                var connectTask = ProtoBootStrap.InitLobby(CPPlayer.IpPortData.ipportinfos.LobbyInfo.ip, CPPlayer.IpPortData.ipportinfos.LobbyInfo.port);
        
                var hasCompleted = await UniTask.WhenAny(connectTask);

                if (hasCompleted == 0) // 연결 성공
                {
                    LobbyDispatcherInit();
                    // 연결이 실제로 되었는지 확인
                    if (CPPlayer.Server.lobbyConnection?.isConnected == true)
                    {
                        bool loginSuccess = await ReLoginProccess();
                        if (loginSuccess)
                        {
                            isReLoginComplete = true;
                        }
                        else
                        {
                            isReLoginComplete = false;
                        }
                    }
                }
                else
                {
                    isReLoginComplete = false;
                    Debug.LogWarning($"로비 연결 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"로비 재연결 시도 실패: {ex.Message}");
            }

            if (isReLoginComplete)
            {
                Debug.LogError($"로비 재연결 시도 성공");
                return true;
            }
            else
            {
                Debug.LogError($"로비 재연결 시도 실패");
                return false; // 모든 시도 실패    
            }
        }

        public async UniTask<bool> ReLoginProccess()
        {
            var usersInfos = await Services.Lobby.GetUserListInfoAsync(LoginData.Cloud.loginValue.userAutoToken);
            if (usersInfos.IsSuccess == false)
            {
                // CPPlayer.InGame.errorToastPopup?.Invoke($"로그인에 실패하였습니다. 고객센터로 문의해주세요.");
                return false;
            }

            var targetUser = usersInfos.Data.Users.FirstOrDefault(o => o.Uid == LoginData.Cloud.loginValue.loginres.Uid);
            if (targetUser == null)
            {
                return false;
            }


            string autoLoginToken = LoginData.Cloud.loginValue.userAutoToken;

            LobbyClient.PacketResult<LoginRes> loginResPacket;
            if (LoginData.Cloud.loginValue.loginType == LoginType.ATOZ)
            {
                loginResPacket = await Services.Lobby.AutoLoginAsync(autoLoginToken);
            }
            else
            {
                string loginType = LoginData.Cloud.loginValue.loginType.ToString();
                string socialEmail = LoginData.Cloud.loginValue.userSocialEmail;
                string socialToken = LoginData.Cloud.loginValue.userSocialToken;
                string accessToken = LoginData.Cloud.loginValue.accessToken;
                loginResPacket = await Services.Lobby.SocialLoginAsync(loginType, LoginData.Cloud.loginValue.userAccountID, socialEmail, socialToken, accessToken);
            }

            if (loginResPacket.IsSuccess)
            {
                LoginData.Cloud.loginValue.UID = loginResPacket.Data.Uid;
                LoginData.Cloud.loginValue.userAutoToken = loginResPacket.Data.Token;
                LoginData.Cloud.loginValue.loginres = loginResPacket.Data;
            }

            LocalSaveLoader.SaveLoginCloudData();

            if (!loginResPacket.IsSuccess)
            {
                if (loginResPacket.Error.Code == ErrorCode.EUserNotExist)
                {
                    // CPPlayer.InGame.errorToastPopup?.Invoke($"서버 연결에 실패하였습니다 게임을 재시작해주세요.");
                }
                else if (loginResPacket.Error.Code == ErrorCode.EAlreadyLogin)
                {
                    // CPPlayer.InGame.errorToastPopup?.Invoke($"서버 연결에 실패하였습니다 게임을 재시작해주세요.");
                }

                Debug.LogError(loginResPacket.Error);
                return false;
            }

            await UniTask.Yield();


            return true;
        }

        public async UniTask StartInGameConnectionMonitoring()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                // 현재 연결된 게임 타입에 따라 추가 모니터링
                if (CPPlayer.Server.currentConnectedGameType == GameType.HOLDEM)
                {
                    if (CPPlayer.Server.holdemConnection?.isConnected == false && !isReconnecting)
                    {
                        await HandleReconnection(CPPlayer.Server.currentConnectedGameType);
                    }
                }

                if (CPPlayer.Server.currentConnectedGameType == GameType.LOW_BADUGI)
                {
                    if (CPPlayer.Server.badugiConnection?.isConnected == false && !isReconnecting)
                    {
                        await HandleReconnection(CPPlayer.Server.currentConnectedGameType);
                    }
                }

                if (CPPlayer.Server.currentConnectedGameType == GameType.SEVEN_POKER)
                {
                    if (CPPlayer.Server.sevenPokerConnection?.isConnected == false && !isReconnecting)
                    {
                        await HandleReconnection(CPPlayer.Server.currentConnectedGameType);
                    }
                }

                await UniTask.Delay(1000);
            }
        }

        private bool IsGameConnectionActive(GameType gameType)
        {
            return gameType switch
            {
                GameType.HOLDEM => CPPlayer.Server.holdemConnection?.isConnected == true,
                GameType.LOW_BADUGI => CPPlayer.Server.badugiConnection?.isConnected == true,
                GameType.SEVEN_POKER => CPPlayer.Server.sevenPokerConnection?.isConnected == true,
                _ => false
            };
        }

        /// <summary>
        /// 게임 타입별 재연결 처리
        /// </summary>
        private async UniTask HandleReconnection(GameType gameType)
        {
            isReconnecting = true;
            try
            {
                string gameTypeName = GetGameTypeName(gameType);
                Debug.Log($"{gameTypeName} 연결이 끊어짐, 재연결 시도 시작");

                PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(true));

                bool reconnected = await TryReconnectWithRetry(gameType);

                if (reconnected)
                {
                    Debug.Log($"{gameTypeName} 재연결 성공");
                }
                else
                {
                    Debug.LogError($"{gameTypeName} 재연결 실패 - 최대 재시도 횟수 초과");
                    // CPPlayer.InGame.errorToastPopup?.Invoke($"{gameTypeName} 서버 연결에 실패했습니다. 앱을 재시작해주세요.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{GetGameTypeName(gameType)} 재연결 중 예외 발생: {ex.Message}");
            }
            finally
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(false));
                isReconnecting = false;
            }
        }

        /// <summary>
        /// 게임 타입별 재연결을 여러 차례 시도합니다
        /// </summary>
        private async UniTask<bool> TryReconnectWithRetry(GameType gameType)
        {
            for (int attempt = 1; attempt <= maxRetryCount; attempt++)
            {
                try
                {
                    string gameTypeName = GetGameTypeName(gameType);
                    Debug.Log($"{gameTypeName} 재연결 시도 {attempt}/{maxRetryCount}");

                    bool success = await ReconnectByGameType(gameType);

                    if (success)
                    {
                        return true;
                    }

                    Debug.LogWarning($"{gameTypeName} 연결 실패 (시도 {attempt})");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{GetGameTypeName(gameType)} 재연결 시도 {attempt} 실패: {ex.Message}");
                }

                if (attempt < maxRetryCount)
                {
                    await UniTask.Delay(1000 * attempt); // 점진적 백오프
                }
            }

            return false;
        }

        /// <summary>
        /// 게임 타입별 실제 재연결 로직
        /// </summary>
        private async UniTask<bool> ReconnectByGameType(GameType gameType)
        {
            switch (gameType)
            {
                case GameType.HOLDEM:
                    return await ReconnectHoldem();

                case GameType.LOW_BADUGI:
                    return await ReconnectBadugi();

                case GameType.SEVEN_POKER:
                    return await ReconnectSevenPoker();
                default:
                    Debug.LogError($"지원하지 않는 게임 타입: {gameType}");
                    return false;
            }
        }

        /// <summary>
        /// 홀덤 재연결
        /// </summary>
        private async UniTask<bool> ReconnectHoldem()
        {
            // 기존 연결 정리
            CPPlayer.Server.holdemConnection?.Dispose();

            // 서버 정보 가져오기
            var serverListPacket = await Services.Lobby.GetServerListAsync(Common.GameType.GtHoldem);
            if (!serverListPacket.IsSuccess) return false;

            var serverInfo = serverListPacket.Data.List[0];

            // 타임아웃과 함께 연결 시도
            var connectTask = ProtoBootStrap.InitHoldem(serverInfo.Host, (int)serverInfo.Port);
            var timeoutTask = UniTask.Delay(1000);

            var hasCompleted = await UniTask.WhenAny(connectTask, timeoutTask);

            if (hasCompleted == 0 && CPPlayer.Server.holdemConnection?.isConnected == true)
            {
                // 재연결 후 서버 인증
                var connectRes = await Services.Holdem.ConnectAsync(Services.Lobby.Token);
                if (connectRes.IsSuccess)
                {
                    // 서버 시간 동기화
                    SyncServerTime(connectRes.Data.Ts.ToDateTime(),
                        time => CPPlayer.Holdem.serverTime = time,
                        gap => CPPlayer.Holdem.timeGap = gap);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 바둑이 재연결
        /// </summary>
        private async UniTask<bool> ReconnectBadugi()
        {
            // 기존 연결 정리
            CPPlayer.Server.badugiConnection?.Dispose();

            // 서버 정보 가져오기
            var serverListRes = await Services.Lobby.GetServerListAsync(Common.GameType.GtBadugi);
            if (!serverListRes.IsSuccess) return false;

            var serverInfo = serverListRes.Data.List[0];

            // 타임아웃과 함께 연결 시도
            var connectTask = ProtoBootStrap.InitBadugi(serverInfo.Host, (int)serverInfo.Port);
            var timeoutTask = UniTask.Delay(1000);

            var hasCompleted = await UniTask.WhenAny(connectTask, timeoutTask);

            if (hasCompleted == 0 && CPPlayer.Server.badugiConnection?.isConnected == true)
            {
                // 재연결 후 서버 인증
                var connectRes = await Services.Badugi.ConnectAsync(Services.Lobby.Token);
                if (connectRes.IsSuccess)
                {
                    CPPlayer.Badugi.ingameUid = connectRes.Data.Uid;

                    // 서버 시간 동기화
                    SyncServerTime(connectRes.Data.Ts.ToDateTime(),
                        time => CPPlayer.Badugi.serverTime = time,
                        gap => CPPlayer.Badugi.timeGap = gap);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 세븐포커 재연결
        /// </summary>
        private async UniTask<bool> ReconnectSevenPoker()
        {
            // 기존 연결 정리
            CPPlayer.Server.sevenPokerConnection?.Dispose();

            // 서버 정보 가져오기
            var serverListPacket = await Services.Lobby.GetServerListAsync(Common.GameType.GtSevenPoker);
            if (!serverListPacket.IsSuccess) return false;

            var serverInfo = serverListPacket.Data.List[0];

            // 타임아웃과 함께 연결 시도
            var connectTask = ProtoBootStrap.InitSPoker(serverInfo.Host, (int)serverInfo.Port);
            var timeoutTask = UniTask.Delay(1000);

            var hasCompleted = await UniTask.WhenAny(connectTask, timeoutTask);

            if (hasCompleted == 0 && CPPlayer.Server.sevenPokerConnection?.isConnected == true)
            {
                // 재연결 후 서버 인증
                var connectRes = await Services.SevenPoker.ConnectAsync(Services.Lobby.Token);
                if (connectRes.IsSuccess)
                {
                    CPPlayer.SPoker.ingameUid = connectRes.Data.Uid;

                    // 서버 시간 동기화
                    SyncServerTime(connectRes.Data.Ts.ToDateTime(),
                        time => CPPlayer.SPoker.serverTime = time,
                        gap => CPPlayer.SPoker.timeGap = gap);
                    return true;
                }
            }

            return false;
        }

        private void SyncServerTime(DateTime serverUtc, Action<DateTime> setServerTime, Action<TimeSpan> setTimeGap)
        {
            DateTime clientUtc = DateTime.UtcNow;
            setServerTime(serverUtc);
            setTimeGap(clientUtc - serverUtc);
        }

        /// <summary>
        /// 게임 타입명 반환 (한글)
        /// </summary>
        private string GetGameTypeName(GameType gameType)
        {
            return gameType switch
            {
                GameType.HOLDEM => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Holdem].StringToLocal,
                GameType.LOW_BADUGI => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Badugi].StringToLocal,
                GameType.SEVEN_POKER => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SevenPoker].StringToLocal,
                _ => gameType.ToString()
            };
        }

        /// <summary>
        /// Dispose 후 게임씬 재진입 시 모니터링 재시작 (로딩씬 → 게임씬 복귀 시 호출)
        /// </summary>
        public void Reinitialize()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new System.Threading.CancellationTokenSource();

            isReconnecting = false;
            isReLoginComplete = false;

            StartLobbyConnectionMonitoring().Forget();
            HeartBeatToMaintainServer().Forget();
        }

        private const double BackgroundTimeoutSeconds = 320.0;

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _isInBackground = true;
            }
            else
            {
                if (!_isInBackground) return;
                _isInBackground = false;

                ForceCloseGameServerConnections();
            }
        }

        private void ForceCloseGameServerConnections()
        {
            if (CPPlayer.Server.holdemConnection?.isConnected == true)
            {
                var lastRequest = Services.Holdem?.LastRequestAt ?? DateTime.MinValue;
                bool timedOut = lastRequest == DateTime.MinValue ||
                                (DateTime.UtcNow - lastRequest).TotalSeconds >= BackgroundTimeoutSeconds;

                if (timedOut)
                {
                    CPPlayer.Server.holdemConnection.Dispose();
                    holdemDispatcher?.Dispose();
                    Debug.LogWarning("[ConnectionManager] 홀덤 백그라운드 320초 초과 - 연결 종료");
                }
                else
                {
                    Debug.Log("[ConnectionManager] 홀덤 백그라운드 복귀 - 320초 미경과, 연결 유지");
                }
            }

            if (CPPlayer.Server.badugiConnection?.isConnected == true)
            {
                var lastRequest = Services.Badugi?.LastRequestAt ?? DateTime.MinValue;
                bool timedOut = lastRequest == DateTime.MinValue ||
                                (DateTime.UtcNow - lastRequest).TotalSeconds >= BackgroundTimeoutSeconds;

                if (timedOut)
                {
                    CPPlayer.Server.badugiConnection.Dispose();
                    badugiDispatcher?.Dispose();
                    Debug.LogWarning("[ConnectionManager] 바두기 백그라운드 320초 초과 - 연결 종료");
                }
                else
                {
                    Debug.Log("[ConnectionManager] 바두기 백그라운드 복귀 - 320초 미경과, 연결 유지");
                }
            }

            if (CPPlayer.Server.sevenPokerConnection?.isConnected == true)
            {
                var lastRequest = Services.SevenPoker?.LastRequestAt ?? DateTime.MinValue;
                bool timedOut = lastRequest == DateTime.MinValue ||
                                (DateTime.UtcNow - lastRequest).TotalSeconds >= BackgroundTimeoutSeconds;

                if (timedOut)
                {
                    CPPlayer.Server.sevenPokerConnection.Dispose();
                    spokerDispatcher?.Dispose();
                    Debug.LogWarning("[ConnectionManager] 세븐포커 백그라운드 320초 초과 - 연결 종료");
                }
                else
                {
                    Debug.Log("[ConnectionManager] 세븐포커 백그라운드 복귀 - 320초 미경과, 연결 유지");
                }
            }

            CPPlayer.Server.currentConnectedGameType = GameType.END;
        }

        /// <summary>
        /// 연결 매니저 리소스 정리
        /// </summary>
        public void Dispose()
        {
            Debug.LogError("connectionManager Dispose");
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            LoginData.Cloud.loginValue.loginres = null;

            CPPlayer.Server.lobbyConnection?.Dispose();
            CPPlayer.Server.holdemConnection?.Dispose();
            CPPlayer.Server.badugiConnection?.Dispose();
            CPPlayer.Server.sevenPokerConnection?.Dispose();

            lobbyDispatcher?.Dispose();
            holdemDispatcher?.Dispose();
            badugiDispatcher?.Dispose();
            spokerDispatcher.Dispose();
        }

        public void AfterLoginFailProcess()
        {
        }
    }
}