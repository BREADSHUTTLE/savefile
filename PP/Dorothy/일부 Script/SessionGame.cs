// SessionGame.cs - SessionGame implementation file
//
// Description      : SessionGame main instance
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/02/14
// Last Update      : 2019/06/11
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

namespace Dorothy
{
    public sealed class SessionGame : BaseSystem<SessionGame>
    {
        private State currentState = State.Ready;
        private string service = "/st-earth-grc/";

        public Action<State> ActionChangeState = delegate { };
        public Action ActionJoinGame = delegate { };
        public Action ActionLeaveGame = delegate { };

        public enum State
        {
            Ready,
            JoinGameWait,
            JoinGameSuccess,
            JoinGameFail,
            LeaveGameWait,
            LeaveGameSuccess,
            LeaveGameFail,
        }

        public State CurrentState
        {
            get
            {
                return currentState;
            }
            set
            {
                Log("The state changed: " + value.ToString());

                currentState = value;

                ActionChangeState(currentState);
            }
        }

        public long SessionKey
        {
            get; private set;
        }

        private Session.State SessionState
        {
            get { return Session.Instance.CurrentState; }
            set { Session.Instance.CurrentState = value; }
        }

        private SessionLobby.State LobbyState
        {
            get { return SessionLobby.Instance.CurrentState; }
            set { SessionLobby.Instance.CurrentState = value; }
        }

        public override void Initialize()
        {
            base.Initialize();

            // TODO: initialize.
        }

        protected override void Awake()
        {
            base.Awake();

            // TODO: initialize
        }

        public void JoinFreeGame(long id, string code)
        {
            Log();

            //if (Session.Instance.CurrentState != Session.State.InLobby)
            //{
            //    Log("invalid lobby state : " + Session.Instance.Location);
            //    return;
            //}
            
            //SessionLobby.Instance.ConsumeTicket
            //(
            //    id, code, true, (answer) =>
            //    {
            //        GameInfo.Instance.Ticket = answer.value;
            //        JoinGame(GameTools.SRL2MSN(answer.value.grcSlotCode));
            //    }
            //);
        }

        public void JoinGame(bool reconnect = false)
        {
            Log();

            //if (Session.Instance.CurrentState != Session.State.InLobby)
            //{
            //    Log("invalid lobby state : " + Session.Instance.Location);
            //    return;
            //}

            CurrentState = State.JoinGameWait;

            OnJoinGame();
        }

        private void OnJoinGame()
        {
            Log();

            
        }


        private bool CheckDownloadConfirm()
        {
            //return
            //(
            //    GameInfo.Instance.Reconnected == false &&
            //    Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork &&
            //    AssetBundleSystem.Instance.IsVersionCached(GameTools.GameSceneName(GameInfo.Instance.MSN)) == false
            //);

            return true;
        }

       

        private void OnDownload()
        {
            Log();

            OnLoadGame((result) =>
            {
                if (result == false)
                {
                    Log("Failed to load the game.");
                    CurrentState = State.JoinGameFail;
                    ReturnToLobby();
                }
                else
                {
                    ActionJoinGame();
                }
            });
        }

        private void OnLoadGame(Action<bool> callback)
        {
            Log();

            CurrentState = State.JoinGameSuccess;
            LogSession();
        }

        public void LogSession()
        {
            
        }

        private void ReturnTo(Action callback)
        {

        }

        public void ReturnToLobby()
        {
            Log();

            //ReturnTo(() =>
            //{
            //    SessionLobby.Instance.ReturnToLobby();
            //});
        }

        public void ReturnToMyIsland()
        {
            Log();

            //ReturnTo(() =>
            //{
            //    SessionLobby.Instance.ReturnToMyIsland();
            //});
        }

        public void LeaveGame(Action callback)
        {
            Log();

            CurrentState = State.LeaveGameWait;

            /*
            Request<Network.Answer>
            (
                "leave", new RequestSession()
                {
                    MSN = GameTools.MSN2SRL(GameInfo.Instance.MSN)
                },
                (result) =>
                {
                    CurrentState = State.LeaveGameSuccess;

                    if (callback != null)
                        callback();

                    ActionLeaveGame();
                }
            );
            */
        }

        public void Close()
        {
            Log();
        }

        public void Request(string command, Network.Request request, string method = UnityWebRequest.kHttpVerbPOST)
        {
            //RestClient.Request(service + command, request, method, true);
        }

        public void Request<T>(string command, Network.Request request, Action<T> callback, string method = UnityWebRequest.kHttpVerbPOST) where T : Network.Answer
        {
            /*
            RestClient.Request(service + command, request, (T answer) =>
            {
                if (answer == null)
                {
                    Error("invalid message");
                }
                else if (answer.resultCode != 0)
                {
                    if (OnError(command, answer) == false)
                        callback(answer);
                }
                else if (callback != null)
                {
                    callback(answer);
                }
            },
            method, true);
            */
        }

        private bool OnError(string command, Network.Answer answer)
        {
            Warning("error: " + answer.resultCode);

            return false;
        }
    }
}
