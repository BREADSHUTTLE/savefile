using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CAPYBARA
{
    public interface IStateCallback
    {
        Action onEnter { get; }
        Action onExit { get; }
        Action onUpdate { get; }
    }

    public interface IStateCallbackListener
    {
        IStateCallback stateCallback { get; }
        bool stop { get; }
        int updateFrame { get; }
    }

    public class UnitStateMachineRunner
    {
        IStateCallbackListener _statemachine;
        private readonly CancellationTokenSource _cts;

        public UnitStateMachineRunner(IStateCallbackListener statemachine, CancellationTokenSource cts)
        {
            _statemachine = statemachine;
            _cts = cts;

            Main().Forget();
        }

        async UniTaskVoid Main()
        {
            while (true)
            {
                if (_statemachine.stop == false)
                {
                    _statemachine.stateCallback?.onUpdate?.Invoke();
                }

                await UniTask.Delay(_statemachine.updateFrame);
            }
        }
    }

    public class StateMachine<T> : IStateCallbackListener where T : struct
    {
        public bool Triggerevent;
        public T PreviousState;
        public T CurrentState;

        private bool IsstopState;
        private int updatewaitFrame = 0;

        public IStateCallback PreviousStatecallback;
        public IStateCallback currentStatecallback;
        public Dictionary<T, IStateCallback> stateLookup;

        UnitStateMachineRunner StateMachineRunner;

        public StateMachine(bool _istrigger, System.Threading.CancellationTokenSource cts)
        {
            Triggerevent = _istrigger;

            StateMachineRunner = new UnitStateMachineRunner(this, cts);

            stateLookup = new Dictionary<T, IStateCallback>();

            IsstopState = true;
        }

        public IStateCallback stateCallback
        {
            get { return currentStatecallback; }
        }

        public bool stop
        {
            get { return IsstopState; }
        }

        public int updateFrame
        {
            get { return updatewaitFrame; }
        }

        public void AddState(T state, IStateCallback statecallback)
        {
            stateLookup.Add(state, statecallback);
        }

        public void ChangeState(T state)
        {
            if (IsCurrentState(state))
                return;
            Extension.eLog($"change state {state}",Color.magenta);
            PreviousState = CurrentState;
            if (stateLookup.ContainsKey(PreviousState))
            {
                PreviousStatecallback = stateLookup[PreviousState];
                PreviousStatecallback?.onExit?.Invoke();
            }

            if (stateLookup.ContainsKey(state))
            {
                currentStatecallback = stateLookup[state];
            }

            CurrentState = state;
            currentStatecallback?.onEnter?.Invoke();
        }

        public bool IsCurrentState(T state)
        {
            if (stateLookup.ContainsKey(state))
            {
                return currentStatecallback == stateLookup[state];
            }
            else
                return false;
        }

        public bool IsStateExist(T state)
        {
            return stateLookup.ContainsKey(state);
        }

        public void StateStop(bool _stop)
        {
            IsstopState = _stop;
        }

        public void SetUpdateFrameDelay(int delaymillsec)
        {
            updatewaitFrame = delaymillsec;
        }
    }

    public enum eGameRoundState
    {
        None = -5,
        Enter=0,
        Idle,
        Start,
        RoundRunning,
        Result,
        End,
    }

    public abstract class InGameBase
    {
        public StateMachine<eGameRoundState> _state;
        public readonly  CancellationTokenSource _cts;
        
        public InGameBase(CancellationTokenSource cts)
        {
            _cts = cts;
        }
    }
    
    public abstract class GameRoundState : IStateCallback
    {
        public Action onEnter => OnEnter;
        public Action onUpdate => OnUpdate;
        public Action onExit => OnExit;

        protected abstract void OnEnter();
        protected abstract void OnUpdate();
        protected abstract void OnExit();

        public abstract eGameRoundState GetState();


    }
}