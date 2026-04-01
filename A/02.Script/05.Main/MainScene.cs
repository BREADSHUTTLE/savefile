using System;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Screen = UnityEngine.Screen;
using ScreenOrientation = UnityEngine.ScreenOrientation;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CAPYBARA
{
    public class MainScene : MonoBehaviour
    {
        public System.Threading.CancellationTokenSource _cts;
        [SerializeField] CAPYBARA.CPSafeArea safearea;

        ControllerLobby lobbyController;
        ControllerInGame ingameController;

        private void Awake()
        {
            Debug.Log("메인씬 진입");
            StartCoroutine(EnableLandscapeAutoRotationNextFrame());
            Main().Forget();
        }

        
        private void OnDestroy()
        {
            lobbyController.Dispose();
            ingameController.Dispose();
            
            //cpplayer static data dispose
            CPPlayer.Dispose();
            
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
        }

        IEnumerator EnableLandscapeAutoRotationNextFrame()
        {
            // 1단계: LandscapeLeft로 강제 설정
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            // 2단계: 2프레임 대기 (렌더링 강제 갱신)
            yield return null;
            yield return null;

            // 3단계: 다시 AutoRotation으로 전환
            Screen.orientation = ScreenOrientation.AutoRotation;

            // 4단계: UI 레이아웃 강제 갱신 (일부 디바이스에서 필요)
            Canvas.ForceUpdateCanvases();
            Screen.fullScreen = false; // 👈 강제 리프레시 (일부 디바이스에서 필요)
            Screen.fullScreen = true;
        }

        async UniTask Main()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }

            _cts = new System.Threading.CancellationTokenSource();

            BackButtonManager.Instance.Enable();
            //model data init
            CPPlayer.Server.Init();

            var values = new Dictionary<string, string> { { "GameStart", LoginData.Cloud.loginValue.userAutoToken.ToString() }, };
            AppsFlyerSDK.AppsFlyer.sendEvent("deep_link_opened", values);

            var userinfo = await Services.Lobby.GetUserInfoAsync();
            if (userinfo.IsSuccess)
                CPPlayer.UserInfo.userDatabase = userinfo.Data;

            //controllerSetting
            lobbyController = new ControllerLobby(transform,_cts);
            await lobbyController.InitLobbyDisplay();
            await lobbyController.GameStartSet();
            
            ingameController = new ControllerInGame(transform,_cts);
            ingameController.GameStartSet();

            lobbyController.UpdateRoomHistoryAfterLogin();
            
            
            
            
            var usersInfo = await Services.Lobby.GetUserListInfoAsync( LoginData.Cloud.loginValue.userAutoToken);
            if (usersInfo.IsSuccess && usersInfo.Data != null)
            {
                CPPlayer.UserInfo.userDatabaseList = usersInfo.Data.Users.ToList();
            }

            var purchaseHistoryRes = await Services.Lobby.PurchaseMonthlyInfoAsync();
            if (purchaseHistoryRes.IsSuccess)
                CPPlayer.UserInfo.purchaseMonthlyDatabase = purchaseHistoryRes.Data;

            await ConfigDataManager.InitializeAsync();
            CommonIAPManager.Instance.Initialize();

            // AppStateManager 초기화 (백그라운드/포어그라운드 상태 관리)
            _ = AppStateManager.Instance;

            CPPlayer.UserInfo.Init();

            float _volume = CPPlayer.Cloud.optionValue.allSoundOnOff ? 1 : 0;
            AudioManager.Instance.SetAllVolume(_volume);
            AudioManager.Instance.SetVolume(SoundType.BGM, CPPlayer.Cloud.optionValue.bgmVolum);
            AudioManager.Instance.SetVolume(SoundType.Effect, CPPlayer.Cloud.optionValue.effectVolum);
            AudioManager.Instance.SetVolume(SoundType.Voice, CPPlayer.Cloud.optionValue.voiceVolum);

            AudioManager.Instance.SetMute(SoundType.BGM, CPPlayer.Cloud.optionValue.bgmSoundOnOff);
            AudioManager.Instance.SetMute(SoundType.Effect, CPPlayer.Cloud.optionValue.effectSoundOnOff);
            AudioManager.Instance.SetMute(SoundType.Voice, CPPlayer.Cloud.optionValue.voiceSoundOnOff);
            CPPlayer.Server.currentConnectedGameType = GameType.END;

     

          
            
            CPPlayer.Option.SafeAreaActive += SafeAreaActivate;
            //PopupManager.Instance.Open<PopupToast>();
        }


        void SafeAreaActivate(bool _active)
        {
            safearea.gameObject.SetActive(_active);
            safearea.AreaUpdate();
        }

#if UNITY_EDITOR
        [InitializeOnLoad]
        public class PlayStateWatcher
        {
            static PlayStateWatcher()
            {
                EditorApplication.playModeStateChanged += (state) =>
                {
                    if (state == PlayModeStateChange.ExitingPlayMode)
                    {
                        CPPlayer.Server.lobbyConnection?.Dispose();
                        CPPlayer.Server.holdemConnection?.Dispose();
                        CPPlayer.Server.badugiConnection?.Dispose();
                        CPPlayer.Server.sevenPokerConnection?.Dispose();
                        Debug.Log("Editor에서 Play모드 종료됨");
                    }
                };
            }
        }
#else
           private void OnApplicationQuit()
        {
            CPPlayer.Server.lobbyConnection.Dispose();
            CPPlayer.Server.holdemConnection.Dispose();
            CPPlayer.Server.badugiConnection.Dispose();
            CPPlayer.Server.sevenPokerConnection.Dispose();
        }

#endif

        [ContextMenu("test")]
        public void Test()
        {
            var sdsd = LocalSaveLoader.LoadUserCloudData();
            Debug.LogError($"예약베팅 값{sdsd.optionValue.reserveBet}");
        }


        void OnApplicationPause(bool pause)
        {
            Debug.LogError("pause 호출");
            // if (!pause)
            //     BeginResumeWindow();
        }


        void BeginResumeWindow()
        {
            Debug.LogError("로딩이미지 활성화 되야함");
            Services.Lobby.AppStateFireAndForget(true);
            // 복귀 후 3초 동안만 “첫 패킷 기다리는 로딩”을 허용
            CPPlayer.Server._resumeUntilMs = CPPlayer.Server.NowMs + 3000;

            CPPlayer.Server._waitingFirstPacketAfterResume = true;

            // 즉시 로딩을 켜도 되고, 200~300ms 딜레이 후 켜도 됨(깜빡임 방지)
            ShowLoadingIfNeeded();
        }

        async void ShowLoadingIfNeeded()
        {
            if (CPPlayer.Server._loadingShown) return;

            Debug.LogError("잠시 대기중");
            // 너무 짧게 멈춘 경우 로딩이 깜빡이는 게 더 구림 -> 200ms 지연 후 still waiting이면 켜기
            await UniTask.Yield();

            Debug.LogError("로딩 이미지 오픈 준비");
            if (!CPPlayer.Server._waitingFirstPacketAfterResume) return;
            if (CPPlayer.Server.NowMs > CPPlayer.Server._resumeUntilMs) return;

            CPPlayer.Server._loadingShown = true;

            Debug.LogError("로딩 이미지 함수 호출");
            
            PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(true));
        }

//#if UNITY_EDITOR
        private void Update()
        {
            //test용!
            if (Input.GetKeyDown(KeyCode.I))
            {
                Debug.Log("frame 조정");
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 80;
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 120;
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                CPPlayer.Server.lobbyConnection.Dispose();
            }
            // if (Input.GetKeyDown(KeyCode.Q))
            // {
            //     ConnectionManager.Instance.Dispose();
            //     CPPlayer.InGame.toastTitlePopupWithCallback?.Invoke("중복 접속","중복 접속으로 인해 게임이 종료됩니다.",()=>
            //     {
            //         LogoutForThisAccount();
            //     });
            // }
        }
        
        void LogoutForThisAccount()
        {
            LocalSaveLoader.DeleteCloudData();
            
            ConnectionManager.Instance.Dispose();
            
            PoolManager.Clear();
            PopupManager.Instance.CloseAll();
            
            SceneManager.LoadScene("Loading");
        }
//#endif
    }
}