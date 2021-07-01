/// Character.cs
/// Author : KimJuHee

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DFramework.DataPool;
using DFramework.Common;

namespace DFramework.Game
{
    public class Character : CharacterBase
    {
        [SerializeField] private Skill _skill;

        [SerializeField] private Animator _animator;
        [SerializeField] private NavMeshAgent _myAgent;
        [SerializeField] private GameObject _model;
        [SerializeField] private HPBar _hpBar;
        
        [SerializeField] private CapsuleCollider _collider;
        public CapsuleCollider Collider
        {
            get { return _collider; }
        }

        private Coroutine _coroutineAttack = null;
        private Coroutine _coroutineDeath = null;
        private Coroutine _coroutineWithdraw = null;

        private Coroutine _coroutineRecovery = null;

        private Transform _target = null;
        //private float _targetRadius = 1f;

        //private bool _isTargetEnemy = false;
        //private float _distTimer = 0f;
        //private float _distDelay = 0.2f;

        private float _sightRange = 0f;

        //private bool _isTargetWithdraw = false;

        private ClassValue _classValue;

        private void Update()
        {
            if (_currentState == State.Move)
            {
                //_distTimer += Time.deltaTime;
                //if (_distTimer >= _distDelay)
                //{
                //    _distTimer = 0;
                //    CheckRecognition();
                //}
                _skill._checkTarget = true;
            }
            else
            {
                _skill._checkTarget = false;
            }

            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
            {
                if (_myAgent.enabled == true && _myAgent.isStopped == true)
                {
                    LookTarget();
                    _myAgent.isStopped = false;  // 이동 시작
                }
            }
        }

        public override void Init(CharacterDataInfo info, Transform targetCamp, bool isEnemy)
        {
            base.Init(info, targetCamp, isEnemy);

            _target = targetCamp;

            _classValue = ClassStatInfo.Instance.FindClassValue(info.classType);
            //_sightRange = _classValue.SightRange;

            if (_animator == null)
                _animator = gameObject.GetComponent<Animator>();

            if (_myAgent == null)
                _myAgent = gameObject.GetComponent<NavMeshAgent>();

            _myAgent.enabled = false;

            // 스킬 세팅
            //_skill.SightRange = _classValue.SightRange;
            _skill.SightRange = 3;  // 테이블 값 변경 요망
            _skill.Init(_characterInfo, isEnemy, targetCamp, _myAgent, UpdateTarget);

            // 패시브 스킬 세팅
            //_skill.Add(_characterInfo.skillGroupId,);

            ChangeState(State.Spawn);
        }

        private void UpdateTarget(State state, Transform target, float redius)
        {
            _target = target;
            //_targetRadius = redius;

            if (state == State.None) 
                UpdateTarget(0);
            else
                ChangeState(state);
        }

        private void LookTarget()
        {
            Vector3 vec = _target.position - transform.position;
            vec.Normalize();
            vec.y = 0;  // y축으로만 회전해야함. 안그러면 애니메이션 튄다!

            Quaternion q = Quaternion.LookRotation(vec);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, 1);
        }

        public override void Spawn()
        {
            _animator.SetBool(Constants.CHARACTER_STATE_SPAWN, true);

            LookTarget();

            SetActiveCharacter(true);
            ChangeState(State.Move);
        }

        public override void Move()
        {
            _animator.SetBool(Constants.CHARACTER_STATE_RUN, true);
            _animator.SetBool(Constants.CHARACTER_STATE_ATTACK, false);

            UpdateTarget(0);
        }

        private void UpdateTarget(float stopRange)
        {
            //TODO:: 동적 생성시, 이미 navi 에서 종속되어 활동을 시작했기 때문에 임의로 pos 값을 변경할 수 없음
            // 그래서 생성시 enable을 false 로 시작한 뒤, 변경된 pos로 초기화 한 뒤, navi를 켜주는 것으로 변경
            _myAgent.enabled = false;
            _myAgent.Warp(transform.position);
            _myAgent.enabled = true;

            _myAgent.speed = _characterInfo.statInfo.MoveSpeed;
            _myAgent.stoppingDistance = stopRange;

            _myAgent.SetDestination(_target.position);

            _myAgent.isStopped = true;
        }

        public override void Attack()
        {
            _myAgent.enabled = false;

            LookTarget();

            StopAttackCoroutine();

            _animator.SetBool(Constants.CHARACTER_STATE_RUN, false);
            _animator.SetBool(Constants.CHARACTER_STATE_ATTACK, true);
            //_animator.speed = _classValue.aSPD_base;
            //_animator.speed = _characterInfo.statInfo.AttackSpeed;

            _animator.Play(Constants.CHARACTER_STATE_ATTACK, -1, 0f);

            var targetBase = _target.GetComponent<CharacterBase>().CharacterDataInfo;

            if (_coroutineAttack == null)
                _coroutineAttack = StartCoroutine(OnAttack(targetBase));
        }

        private IEnumerator OnAttack(CharacterDataInfo dataInfo)
        {
            yield return new WaitForSeconds(0.3f);  // 0.3초 뒤 피격 데미지 발생 해달라고 기획적으로 얘기 한 적이 있음.(추후 변경될 여지있음)

            //float atk = DataPool.CharacterInfo.Instance.GetATK(_characterInfo);
            Debug.Log("ATK : " + _characterInfo.statInfo.ATK);
            GameSystem.Instance._gameManager.SetHit(_characterInfo.statInfo.ATK, dataInfo, _isEnemy);

            _myAgent.enabled = true;
            _myAgent.isStopped = true;

            float length = _animator.GetCurrentAnimatorStateInfo(0).length;
            //Debug.Log(_animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
            yield return new WaitForSeconds(length - 0.3f);

            ChangeState(State.AttackReady);
        }

        public override void AttackReady()
        {
            // 여기에 뭔가 어택 준비 전 idle 애니나.. 이런게 필요 해 질수도 있으니 나눠둠
            StopAttackCoroutine();

            //_animator.SetBool(Constants.CHARACTER_STATE_RUN, false);
            //_animator.SetBool(Constants.CHARACTER_STATE_ATTACK, false);

            if (_skill.StopAttack())
            {
                _target = _targetCamp;

                _skill.isTargetEnemy = false;

                ChangeState(State.Move);
            }
            else
            {
                ChangeState(State.Attack);
            }
        }

        private void StopAttackCoroutine()
        {
            if (_coroutineAttack != null)
            {
                StopCoroutine(_coroutineAttack);
                _coroutineAttack = null;
            }
        }

        public override void Damage(float damage)
        {
            _characterInfo.statInfo.HP -= damage;
            //Debug.Log("id : " + _characterInfo.id + " / " + "hp : " + _characterInfo.hp);

            SetHPBar();

            if (_characterInfo.statInfo.HP <= 0)
                ChangeState(State.Dead);
        }

        public override void Withdraw(Action action)
        {
            Debug.Log("character withdraw");

            _myAgent.enabled = false;

            StopAttackCoroutine();

            _animator.SetBool(Constants.CHARACTER_STATE_SPAWN, true);
            _animator.SetBool(Constants.CHARACTER_STATE_RUN, false);
            _animator.SetBool(Constants.CHARACTER_STATE_ATTACK, false);

            if (_coroutineWithdraw == null)
                _coroutineWithdraw = StartCoroutine(OnWithdraw(action));
        }

        private IEnumerator OnWithdraw(Action action)
        {
            yield return new WaitForSeconds(3f);

            _target = _targetCamp;
            _skill.isTargetEnemy = false;

            //gameObject.SetActive(false);
            SetActiveCharacter(false);

            if (action != null)
                action();

            Recovery();
            StopWithdrawCoroutine();
        }

        public void StopWithdrawCoroutine()
        {
            if (_coroutineWithdraw != null)
            {
                StopCoroutine(_coroutineWithdraw);
                _coroutineWithdraw = null;
            }
        }

        public override void ActiveSkill()
        {
            Debug.Log("character attack");
        }

        public override void Passive()
        {   
        }

        public override void Dead()
        {
            Debug.Log("id : " + _characterInfo.id + " dead");

            StopAttackCoroutine();
            StopWithdrawCoroutine();

            _animator.SetBool(Constants.CHARACTER_STATE_DEATH, true);
            _animator.SetBool(Constants.CHARACTER_STATE_RUN, false);
            _animator.SetBool(Constants.CHARACTER_STATE_ATTACK, false);

            DataPool.CharacterInfo.Instance.RemoveCharacterInfo(_characterInfo.id, _isEnemy);

            if (_coroutineDeath == null)
                _coroutineDeath = StartCoroutine(OnDead());
        }

        private IEnumerator OnDead()
        {
            SetColliderEnable(false);

            yield return new WaitForSeconds(2f);    // 죽는거 보려고 (나중에 애니메이션 시간 보면 될듯)

            Destroy(gameObject);
        }

        public void Recovery()
        {
            if (_coroutineRecovery == null)
                _coroutineRecovery = StartCoroutine(OnRecovery());
        }

        private IEnumerator OnRecovery()
        {
            while (true)
            {
                if (_currentState != State.Withdraw)
                {
                    _coroutineRecovery = null;
                    yield break;
                }

                yield return new WaitForSeconds(2f);

                _characterInfo.statInfo.HP += 4;
                Debug.Log(_characterInfo.statInfo.HP);

                if (_characterInfo.statInfo.HP >= 100)
                {
                    _characterInfo.statInfo.HP = 100;
                    yield break;
                }
            }
        }

        public void Skill()
        {
            // 임의로 스킬 1번 사용하도록 함 (테스트)
            
            //_skill.Add(1);
        }

        private void SetColliderEnable(bool enabled)
        {
            Collider[] collider = gameObject.GetComponentsInChildren<Collider>();
            foreach (var co in collider)
                co.enabled = enabled;
        }

        private void SetHPBar()
        {
            if (_hpBar != null)
                _hpBar.SetHP((float)_characterInfo.statInfo.HP);
        }

        public void SetCharacterState(State state, Action actionWithdraw = null)
        {
            ChangeState(state, actionWithdraw);
        }

        public void SetActiveCharacter(bool active)
        {
            _model.SetActive(active);
            _hpBar.gameObject.SetActive(active);

            if (active)
                SetHPBar();
        }

        public bool OnActiveCharacter()
        {
            return _model.activeSelf && _hpBar.gameObject.activeSelf;
        }

        public bool isEnemy()
        {
            return _isEnemy;
        }
    }
}