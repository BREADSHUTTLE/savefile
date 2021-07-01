using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DFramework.DataPool;

namespace DFramework.Game
{
    public class GameManager : MonoBehaviour
    {
        public enum State
        {
            Enter,          // 진입
            Ready,          // 준비
            Start,          // 시작
            Play,           // 진행중
            Pause,          // 일시정지
            EndAndResult,   // 종료 및 결과
            Exit,           // 퇴장
        }

        public enum Mode
        {
            None,
            PVE,
            PVP,
        }


        public Action<bool> _actionResult = null;

        public GameUI _gameUI;
        [SerializeField] private NavMeshSurface navMeshSurface;

        public GameObject _myCampParent;
        public GameObject _enemyCampParent;

        public GameObject _characterListParent;
        public GameObject _enemyCharacterListParent;

        [HideInInspector] public List<Character> _myCharacterList = new List<Character>();
        [HideInInspector] public List<Character> _enemyCharacterList = new List<Character>();

        [HideInInspector] public Camp _myCamp;
        [HideInInspector] public Camp _enemyCamp;

        [HideInInspector] public List<Tower> _myTower = new List<Tower>();
        [HideInInspector] public List<Tower> _enemyTower = new List<Tower>();

        private Mode currentMode = Mode.None;


        //TODO:: 이거 씬 전환 진입시에 시스템 호출 할 때 init 호출 해야함 (잊지말기)
        private void Awake()
        {
            //if (GameSystem.Instance._gameManager == null)
            {
                //GameSystem.Instance._gameManager = this;
                GameSystem.Instance.Init();
            }
        }

        public void Init(Mode gameMode)
        {
            Debug.Log("game manager init");

            currentMode = gameMode;
            
            // 유저 데이터 로드 용
            DataLoder.Instance.LoadUserData();

            // 캐릭터 데이터 로드 용
            DataLoder.Instance.LoadCharacterData();
            
            // 전투 정보 로드 용
            DataLoder.Instance.LoadBattleData();

            // 스킬 정보 로드 용
            DataLoder.Instance.LoadSkill();

            // 캠프 만드는거 논의 해보아야 함
            var myCamp = DataPool.CharacterInfo.Instance.FindCharacterDataInfo(UserInfo.Instance.MyInfo.campId);
            DataPool.CharacterInfo.Instance.CreateCamp(myCamp, false);

            var enemyCamp = DataPool.CharacterInfo.Instance.FindCharacterDataInfo(BattleInfo.Instance.BattleData.campId);
            DataPool.CharacterInfo.Instance.CreateCamp(enemyCamp, true);

            navMeshSurface.BuildNavMesh();  // start 빌드용
        }

        // 이거도 고민해보아야 합니다.
        public void SetHit(float damage, CharacterDataInfo info, bool isEnemy)
        {
            // 테스트로 일단 리턴
            return;


            if (info.type == CharacterType.Character)
            {
                var list = isEnemy ? _myCharacterList : _enemyCharacterList;
                
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].CharacterDataInfo.id == info.id)
                    {
                        list[i].GetComponent<Character>().Damage(damage);
                    }
                }
            }
            else if (info.type == CharacterType.Camp)
            {
                if (isEnemy)
                    _myCamp.Damage(damage);
                else
                    _enemyCamp.Damage(damage);
            }
        }

        public void ChangeGameState(State state, bool isWin = false)
        {
            switch (state)
            {
                case State.Enter:
                    Enter();
                    break;
                case State.Start:
                    Start();
                    break;
                case State.Pause:
                    Pause();
                    break;
                case State.EndAndResult:
                    Result(isWin);
                    break;
                case State.Exit:
                    Exit();
                    break;
            }
        }

        private void Enter()
        {
            // 진입 시 처리할 부분이 있는지
        }

        private void Start()
        {
            // 시작 시 처리할 부분이 있는지
        }

        public void Update()
        {
            // 진행 중에 변경되야 할 정보가 있는지
        }

        private void Pause()
        {
            // 일시정지 중에 되어야 할 작업
        }

        public void SpeedUp()
        {
            // 게임 배속
        }

        private void Result(bool isWin)
        {
            // 결과 화면에서 처리 할 작업

            // 게임 일시 정지
            Time.timeScale = 0;

            if (_actionResult != null)
                _actionResult(isWin);
        }

        private void Exit()
        {
            // 나갈때 처리 할 작업

            // 일시정지 풀어주기

            EnemyInfo.Instance.Clear();
            BattleInfo.Instance.Clear();

            Time.timeScale = 1;
        }
    }
}