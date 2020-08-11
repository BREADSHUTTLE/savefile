// Startup.cs - Startup implementation file
//
// Description      : Startup
// Author           : icoder
// Maintainer       : icoder, uhrain7761
// How to use       : 
// Created          : 2018/03/15
// Last Update      : 2018/08/21
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using ST.MARIA.UI;
using ST.MARIA.DataPool;
using ST.MARIA.Network;


namespace ST.MARIA
{
    public sealed class Startup : UIBaseLayout<Startup>
    {
        [SerializeField] private ProgressFX progressFX;
        [SerializeField] private List<StatusObject> statuses;
        [SerializeField] private GameObject loginFacebook;
        [SerializeField] private GameObject loginGuest;
        [SerializeField] private GameObject loginGSN;
        [SerializeField] private InputField inputGSN;
        [SerializeField] private GameObject retryButton;
        [SerializeField] private GameObject csButton;
        [SerializeField] private Text errorMessage;
        [SerializeField] private Text version;

        private bool launcher = false;
        public Action ActionLoadingCompleted = delegate { };

        public bool IsLoadingCompleted
        {
            get;
            private set;
        }

        [Serializable] private class ProgressFX
        {
            public enum Speed
            {
                Stop,
                Slow,
                Normal,
                Fast
            }

            public Animation target;
            public float slowSpeed = 0.2f;
            public float normalSpeed = 0.5f;
            public float fastSpeed = 1f;
        }

        [Serializable] private class StatusObject
        {
            public string name;
            public Session.State state = Session.State.Ready;
            public GameObject targetObject;
        }

        protected override void Awake()
        {
            base.Awake();

            UserTracking.Instance.Initialize();
            RestClient.Instance.Initialize();
            SettingSystem.Instance.Initialize();
            Environment.Instance.Initialize();
            SoundSystem.Instance.Initialize();
            ApplicationSystem.Instance.Initialize();
            GraphicSystem.Instance.Initialize();
            Vibration.Instance.Initialize();
            CachedImage.Instance.Initialize();
            AdiscopeSystem.Instance.Initialize();

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            UniWebViewPlugin.Initialize();
#endif
            GraphicSystem.Instance.SetupFrameRate(true);
        }

        private void Start()
        {
            Log();
            SetupUI();
            SetupSession();
        }

        private void SetupSession()
        {
            Log();

            SessionLobby.Instance.Initialize();
            SessionGame.Instance.Initialize();
            Session.Instance.Initialize(OnStartup);
        }

        private void OnStartup()
        {
            Log();
            SetupUI();

            RestClient.Instance.Initialize();

            switch (Session.Instance.CurrentState)
            {
                case Session.State.Ready:
                case Session.State.FacebookLink:
                {
                    if (launcher)
                    {
                        launcher = false;
                        Login(Session.LoginType.GSN, long.Parse(inputGSN.text));
                    }
                    else
                    {
                        if (Session.Instance.LoggedOut)
                            ShowLogin();
                        else
                            Login(Session.LoginType.Auto);
                    }
                    break;
                }
                case Session.State.FacebookSignin:
                {
                    Login(Session.LoginType.Facebook);
                    break;
                }
                case Session.State.LoginFail:
                {
                    ShowError();
                    break;
                }
                default:
                {
                    if (Session.Instance.LoggedOut)
                        ShowLogin();
                    else
                        ShowError();
                    break;
                }
            }
        }

        private void Login(Session.LoginType type, long GSN = 0)
        {
            HideButtons();
            StartProgress();
            Session.Instance.Open(type, GSN);
        }

        private void OnEnable()
        {
            Session.Instance.ActionOpen += OnOpenSession;
            Session.Instance.ActionClose += OnCloseSession;
            Session.Instance.ActionChangeState += OnChangeSessionState;
        }

        private void OnDisable()
        {
            Session.Instance.ActionOpen -= OnOpenSession;
            Session.Instance.ActionClose -= OnCloseSession;
            Session.Instance.ActionChangeState -= OnChangeSessionState;
        }

        private void SetupUI()
        {
#if !UNITY_DEBUG && !UNITY_EDITOR
            CommonTools.SetActive(loginGSN, false);
            CommonTools.SetActive(inputGSN, false);
#endif
            HideButtons();

            foreach (StatusObject status in statuses)
                CommonTools.SetActive(status.targetObject, false);

            if (version != null)
            {
                string prefix =
                    (Environment.Instance.Target != EnvironmentField.Target.Live) ?
                    (Environment.Instance.Target.ToString() + " ") : "version ";

                version.text = prefix + Environment.Instance.Version;
            }

            if (inputGSN != null)
            {
                if (string.IsNullOrEmpty(Environment.Instance.LauncherGSN) == false)
                {
                    launcher = true;
                    inputGSN.text = Environment.Instance.LauncherGSN;
                }
            }
        }

        private void StartProgress()
        {
            if (progressFX.target != null)
            {
                CommonTools.SetActive(progressFX.target, true);
                CommonTools.PlayAnimation(progressFX.target);
                UpdateProgress(ProgressFX.Speed.Slow);
            }
        }

        private void StopProgress()
        {
            if (progressFX.target != null)
            {
                CommonTools.StopAnimation(progressFX.target);
                CommonTools.SetActive(progressFX.target, false);
            }
        }

        private void UpdateProgress(ProgressFX.Speed speed)
        {
            if (progressFX.target != null)
            {
                progressFX.target[progressFX.target.clip.name].speed =
                    (speed == ProgressFX.Speed.Fast) ? progressFX.fastSpeed :
                    (speed == ProgressFX.Speed.Normal) ? progressFX.normalSpeed :
                    (speed == ProgressFX.Speed.Slow) ? progressFX.slowSpeed :
                    (speed == ProgressFX.Speed.Stop) ? 0f : 0f;
            }
        }

        private void OnUpdateProgress()
        {
            // NOTE: this method will be called from the ProgressFX animation.

            if (Session.Instance.CurrentState < Session.State.InLobby)
                UpdateProgress(ProgressFX.Speed.Stop);
        }

        private void OnFinishedProgress()
        {
            // NOTE: this method will be called from the ProgressFX animation.

            IsLoadingCompleted = true;
            ActionLoadingCompleted();
        }

        private void OnOpenSession()
        {
            // NOTE: DO nothing.
        }

        private void OnCloseSession()
        {
            OnStartup();
            StopProgress();
        }

        private void OnChangeSessionState(Session.State state)
        {
            StatusObject found = statuses.Find(x => x.state == state);
            if (found != null)
            {
                CommonTools.SetActive(found.targetObject, true);

                foreach (StatusObject status in statuses)
                {
                    if (found.targetObject != status.targetObject)
                        CommonTools.SetActive(status.targetObject, false);
                }
            }

            switch (state)
            {
                case Session.State.ProgressAuth:
                case Session.State.ProgressConnection:
                    UpdateProgress(ProgressFX.Speed.Slow);
                    break;

                case Session.State.ProgressLobby:
                case Session.State.ProgressGame:
                    UpdateProgress(ProgressFX.Speed.Normal);
                    break;

                case Session.State.InLobby:
                case Session.State.InGame:
                    UpdateProgress(ProgressFX.Speed.Fast);
                    break;
            }
        }

        public void OnClickLoginGSN()
        {
            Login(Session.LoginType.GSN, long.Parse(inputGSN.text));
        }

        public void OnClickLoginFacebook()
        {
            Login(Session.LoginType.Facebook);
        }

        public void OnClickLoginGuest()
        {
            Login(Session.LoginType.Guest);
        }

        public void OnClickRetry()
        {
            Login(Session.LoginType.Retry);
        }

        public void OnClickCS()
        {
            Application.OpenURL(string.Format(
                LocalizationSystem.Instance.Localize("SUPPORT.Mail"),
                MyInfo.Instance.GSN,
                Environment.Instance.Version,
                ClientInfo.Instance.DeviceInfo));
        }

        private void HideButtons()
        {
            CommonTools.SetActive(loginFacebook, false);
            CommonTools.SetActive(loginGuest, false);
            CommonTools.SetActive(retryButton, false);
            CommonTools.SetActive(csButton, false);
            CommonTools.SetActive(errorMessage, false);
        }

        private void ShowLogin()
        {
            CommonTools.SetActive(csButton, true);
            CommonTools.SetActive(loginFacebook, true);
            CommonTools.SetActive(loginGuest, true);

            StopProgress();
        }

        private void ShowError()
        {
            CommonTools.SetActive(csButton, true);
            CommonTools.SetActive(errorMessage, true);
            CommonTools.SetActive(retryButton, !Session.Instance.LoggedOut);
            CommonTools.SetActive(loginFacebook, Session.Instance.LoggedOut);
            CommonTools.SetActive(loginGuest, Session.Instance.LoggedOut);

            StopProgress();
        }
    }
}
