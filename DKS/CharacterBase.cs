using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DFramework.DataPool;

namespace DFramework.Game
{
    public abstract class CharacterBase : MonoBehaviour
    {
        // 우선순위로 나열함
        public enum State
        {
            None,
            //BeAttack = 0,   // 피격 -> 이거 체크
            Dead,           // 사망
            Passive,        // 상태이상 -> 이름 체크 / 기획서 확인
            ActiveSkill,    // 액티브 스킬
            Withdraw,       // 회수
            AttackReady,    // 어택 후 상태 체크
            Attack,
            Move,
            Spawn,
        }

        protected CharacterDataInfo _characterInfo;
        public CharacterDataInfo CharacterDataInfo
        {
            get { return _characterInfo; }
            set { _characterInfo = value; }
        }

        //protected CharacterInfo _targetInfo;
        protected Transform _targetCamp;

        protected bool _isEnemy;

        public virtual void Init(CharacterDataInfo info, Transform targetCamp, bool isEnemy)
        {
            _characterInfo = info;
            _targetCamp = targetCamp;
            _isEnemy = isEnemy;
        }

        //state 바꾸는 부분 구현 필요
        //pos 값 가진 부분 필요함
        // 타워 속성 어케 할건지?
        // sm 할당하겠다 알아보기

        protected State _currentState = State.Spawn;
        public State CurrentState
        {
            get { return _currentState; }
        }
        protected void ChangeState(State state, Action actionWithdraw = null)
        {
            //Debug.Log(MethodBase.GetCurrentMethod());
            //Debug.Log("ChangeState");

            _currentState = state;

            switch (state)
            {
                case State.Spawn:
                    Spawn();
                    break;
                case State.Move:
                    Move();
                    break;
                case State.Attack:
                    Attack();
                    break;
                case State.AttackReady:
                    AttackReady();
                    break;
                case State.Withdraw:
                    Withdraw(actionWithdraw);
                    break;
                case State.ActiveSkill:
                    ActiveSkill();
                    break;
                case State.Passive:
                    Passive();
                    break;
                case State.Dead:
                    Dead();
                    break;
                default:
                    break;
            }
        }

        public abstract void Spawn();
        public virtual void Move() { }
        public abstract void Attack();
        public virtual void AttackReady() { }
        public abstract void Damage(float damage);
        public virtual void Withdraw(Action action) { }
        public virtual void ActiveSkill() { }
        public abstract void Passive();
        public abstract void Dead();
    }
}