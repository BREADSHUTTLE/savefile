/// Camp.cs
/// Author : KimJuHee
/// 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DFramework.DataPool;
using DFramework.Common;

namespace DFramework.Game
{
    public class Camp : CharacterBase
    {
        [SerializeField] private HPBar _hPBar;
        [SerializeField] private GameObject _missilePrefab;

        private float _distTimer = 0f;
        private float _distDelay = 1f;

        private Transform _target;

        private Coroutine _coroutineAttack = null;

        //private Collider

        private void Update()
        {
            if (_currentState == State.Spawn)
            {
                _distTimer += Time.deltaTime;
                if (_distTimer >= _distDelay)
                {
                    _distTimer = 0;
                    if (CheckRecognition())
                        ChangeState(State.Attack);
                }
            }
        }

        private bool CheckRecognition()
        {
            List<Character> list;
            if (_isEnemy)
                list = GameSystem.Instance._gameManager._myCharacterList;
            else
                list = GameSystem.Instance._gameManager._enemyCharacterList;

            if (list == null || list.Count == 0)
                return false;

            return RecognitionTarget(list);
        }

        private bool RecognitionTarget(List<Character> list)
        {
            float currentDist = 0f;
            int targetIndex = -1;
            float targetDist = _characterInfo.statInfo.AttackRange + (5f);  // 임의의 값 지정. 테이블 정해지면 교체

            for (int i = 0; i < list.Count; i++)
            {
                currentDist = Vector3.Distance(transform.position, list[i].transform.position) + list[i].Collider.radius;

                if (currentDist <= targetDist)
                {
                    targetIndex = i;
                    targetDist = currentDist;
                }
            }

            if (targetIndex > -1f)
            {
                _target = list[targetIndex].transform;

                return true;
            }

            return false;
        }

        public override void Init(CharacterDataInfo info, Transform target, bool isEnemy)
        {
            base.Init(info, target, isEnemy);

            ChangeState(State.Spawn);
        }

        public override void Spawn()
        {
            // 여기에 스킨같은거 변경하는거 있지 않을까
        }

        public override void Attack()
        {
            //Debug.Log("타워가 공격 할 것입니다!!");

            //return;

            if (_coroutineAttack == null)
                _coroutineAttack = StartCoroutine(OnAttack());
        }

        private IEnumerator OnAttack()
        {
            Missile missile = CommonTools.CreateGameObject<Missile>(_missilePrefab, transform.position, new Vector3(0.3f,0.3f,0.3f), gameObject);

            //float atk = DataPool.CharacterInfo.Instance.GetATK(_characterInfo);
            missile.Init(_characterInfo.statInfo.AttackSpeed, _characterInfo.statInfo.ATK, _isEnemy, _target);

            yield return new WaitForSeconds(_characterInfo.statInfo.AttackSpeed);

            _coroutineAttack = null;

            if (CheckRecognition())
            {
                Attack();

                yield break;
            }
            else
            {
                ChangeState(State.Spawn);
            }

            StopAttack();
        }

        private void StopAttack()
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

            if (_hPBar != null)
                _hPBar.SetHP((float)_characterInfo.statInfo.HP);

            if (_characterInfo.statInfo.HP <= 0)
                ChangeState(State.Dead);
        }

        public override void Passive()
        {
            
        }

        public override void Dead()
        {
            // Debug.Log(_isEnemy ? "적군이 죽었습니다." : "아군이 죽었습니다.");

            GameSystem.Instance._gameManager.ChangeGameState(GameManager.State.EndAndResult, _isEnemy ? true : false);
        }
    }
}