/// CharacterInfo.cs
/// Author : KimJuHee

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DFramework.Misc;
using DFramework.Game;
using DFramework.Common;
using NLibCs;

namespace DFramework.DataPool
{
    public struct CharacterDataInfo
    {
        public int id;                                  // 고유 id
        public string name;                             // 이름
        public CharacterType type;                      // 타입
        public int tier;                                // 등급
        public CharacterClassType classType;            // 직업
        public int skillGroupId;                        // 랜덤 확률로 가질 패시브 스킬
        public int skillId;                             // 기본 공격 id
        public string meshRes;                          // 모델링
        public Dictionary<StatType, float> statData;    // 캐릭터 스텟 정보

        public CharacterStatInfo statInfo;              // 계산된 공식 저장한 정보
    }

    // 스텟 테이블
    public struct StatInfo
    {
        public int id;
        public int unitId;
        public List<StatData> data;
    }

    public struct StatData
    {
        public StatType type;
        public float value;
    }

    public enum StatType
    {
        None = -1,
        STR = 0,
        CON,
        VIT,
        DEX,
        INT,
        MEN,
        HP_base,
        ATK_STR_base,
        ATK_DEX_base,
        mATK_base,
        Rcv_base,
        RES_base,
        HIT_base,
        EVA_base,
        CRI_base,
        CRIPow_base,
        aSPD_base,
        maSPD_base,
        mSPD_base,
        sightRange,
        rviTime,
        fieldRcv,
        handRcv,
    }

    // 캐릭터 스텟 (계산 후 저장 할 예정)
    public struct CharacterStatInfo
    {
        public float HP;
        public float ATK;
        public float MoveSpeed;
        public float HandRecovery;
        public float HealerRecovery;
        public float Critical;
        public float mATK;
        public float hitRate;
        public float evasiveRate;

        public float AttackRange;   // 공격 범위 스킬 테이블로 빠질예정 (2020.11.04)
        public float AttackSpeed;   // 공격 속도 스킬 테이블로 빠질예정 (2020.11.04)
    }

    public enum CharacterType
    {
        Character = 1,
        Camp,
        Tower,
    }

    public enum CharacterClassType
    {
        None = 0,
        Warrior = 1,    // 전사
        Thief,          // 도적
        Archer,         // 아처
        Gunner,         // 슈터
        Sorcerer,       // 마법사
        Healer,         // 힐러
    }

    public enum TrainType
    {
        Offence = 1,    // 공격훈련
        Defense,        // 방어훈련
        Physical,       // 육체훈련
        Speed,          // 속도훈련
        Magic,          // 마법훈련
        Mental,         // 정신훈련
    }

    // base info 가 하나 필요합니다. (후에 꼭 수정해야해)
    public sealed class CharacterInfo : DSingleton<CharacterInfo>
    {
        public Action _actionAdd = null;
        public Action _actionUpdate = null;
        public Action _actionDelete = null;

        private List<CharacterDataInfo> _characterInfoList = new List<CharacterDataInfo>();
        
        private Dictionary<int, List<StatData>> _dicStatData = new Dictionary<int, List<StatData>>();

        private int _statCount = 0;
        public void Add(CharacterDataInfo dataInfo)
        {
            Nullable<CharacterDataInfo> info = dataInfo;
            if (info == null)
                return;

            dataInfo.statData = GetStatData(dataInfo.id);
            dataInfo.statInfo = GetStatInfo(dataInfo);

            if (_characterInfoList.Contains(dataInfo) == false)
                _characterInfoList.Add(dataInfo);

            if (_actionAdd != null)
                _actionAdd();
        }

        public void Update(NDataSection dataSection)
        {
            foreach (NDataReader.Row row in dataSection)
            {
                CharacterDataInfo info = new CharacterDataInfo();

                row.GetColumn(out info.id);
                row.GetColumn(out info.name);
                row.GetColumn(out info.type);
                row.GetColumn(out info.tier);
                row.GetColumn(out info.classType);
                row.GetColumn(out info.skillGroupId);
                row.GetColumn(out info.skillId);
                row.GetColumn(out info.meshRes);

                Add(info);
            }

            // pool에 업데이트 할게 있으면 여기
            if (_actionUpdate != null)
                _actionUpdate();
        }

        public void UpdateStat(NDataSection dataSection)
        {
            foreach (NDataReader.Row row in dataSection)
            {
                StatInfo info = new StatInfo();
                row.GetColumn(out info.id);
                row.GetColumn(out info.unitId);

                SetStatData(info.unitId, row[2]);
                SetStatData(info.unitId, row[3]);
                SetStatData(info.unitId, row[4]);
                SetStatData(info.unitId, row[5]);
                SetStatData(info.unitId, row[6]);
                SetStatData(info.unitId, row[7]);
                SetStatData(info.unitId, row[8]);
                SetStatData(info.unitId, row[9]);
                SetStatData(info.unitId, row[10]);
                SetStatData(info.unitId, row[11]);
                SetStatData(info.unitId, row[12]);
                SetStatData(info.unitId, row[13]);
                SetStatData(info.unitId, row[14]);
                SetStatData(info.unitId, row[15]);
                SetStatData(info.unitId, row[16]);
                SetStatData(info.unitId, row[17]);
                SetStatData(info.unitId, row[18]);
                SetStatData(info.unitId, row[19]);
                SetStatData(info.unitId, row[20]);
                SetStatData(info.unitId, row[21]);
                SetStatData(info.unitId, row[22]);
                SetStatData(info.unitId, row[23]);
                SetStatData(info.unitId, row[24]);

                _statCount = 0;
            }
        }

        private void SetStatData(int unitId, float value)
        {
            if (_dicStatData.ContainsKey(unitId) == false)
                _dicStatData.Add(unitId, new List<StatData>());

            var data = new StatData()
            {
                type = (StatType)_statCount,
                value = value,
            };

            _dicStatData[unitId].Add(data);
            _statCount++;
        }

        private Dictionary<StatType, float> GetStatData(int id)
        {
            var dic = new Dictionary<StatType, float>();
            if (_dicStatData.ContainsKey(id) == false)
                return null;

            for (int i = 0; i < _dicStatData[id].Count; i++)
                dic.Add(_dicStatData[id][i].type, _dicStatData[id][i].value);


            return dic;
        }
        
        //TODO:: 스텟 계산 해 둔 곳 (테이블 픽스되면 해당 부분 수정 요망)
        // 계산해서 가져오는거 하나 씩 분리 시켜야함 (예 : GetATK 메소드)
        private CharacterStatInfo GetStatInfo(CharacterDataInfo dataInfo)
        {
            // 최종적으로 나올 스텟
            CharacterStatInfo statInfo = new CharacterStatInfo();
            // 해당 직업 계수
            ClassValue classValue = ClassStatInfo.Instance.FindClassValue(dataInfo.classType);

            float vit = dataInfo.statData[StatType.VIT];
            float str = dataInfo.statData[StatType.STR];
            float dex = dataInfo.statData[StatType.DEX];
            
            statInfo.HP = (dataInfo.statData[StatType.HP_base]) + (vit * classValue.HP_value);

            statInfo.MoveSpeed = dataInfo.statData[StatType.mSPD_base];

            statInfo.HandRecovery = vit * classValue.Rcv_value;

            //// 스킬 추가되면 스킬 피해량 추가 해야합니다.
            statInfo.ATK = ((str * classValue.ATK_STR_value) + (dex * classValue.ATK_DEX_value)) * dataInfo.statData[StatType.CRIPow_base];

            // 임의값 지정. 테이블 정해지면 값 채워 넣어야 합니다. (스킬로 빠질 가능성 있음)
            statInfo.AttackRange = 1f;

            //statInfo.AttackSpeed = classValue.aSPD_base;
            statInfo.AttackSpeed = 1f;  // 정확한 계산식 협의되면 변경

            return statInfo;
        }

        private void RefreshStat()
        {
            // 여기에 스킬때문에 스텟 리플레시 하는거 추가 할 거임
        }

        public void Delete(CharacterDataInfo dataInfo)
        {
            _characterInfoList.Remove(dataInfo);

            if (_actionDelete != null)
                _actionDelete();
        }

        public CharacterDataInfo FindCharacterDataInfo(int id)
        {
            for (int i = 0; i < _characterInfoList.Count; i++)
            {
                if (id == _characterInfoList[i].id)
                    return _characterInfoList[i];
            }

            return new CharacterDataInfo?().Value;
        }

        //public Character FindCharacter

        public bool CreateCharacter(CharacterDataInfo info, Vector3 pos, Vector3 scale, bool isEnemy)/* where T : class */// 생성 실패 확인해야함
        {
            var prefab = FindCharacterPrefab(info.meshRes);
            if (prefab == null)
                return false;

            GameObject parent;
            if (isEnemy)
                parent = GameSystem.Instance._gameManager._enemyCharacterListParent;
            else
                parent = GameSystem.Instance._gameManager._characterListParent;

            var go = CommonTools.CreateGameObject(prefab, pos, scale, parent);
            if (go != null)
            {
                go.name = info.name;
                Character character = go.GetComponent<Character>();
                Transform campTransform;

                if (isEnemy)
                {
                    go.tag = Constants.GAME_ENEMY_PLAYER_TAG;
                    GameSystem.Instance._gameManager._enemyCharacterList.Add(character);
                    campTransform = GameSystem.Instance._gameManager._myCamp.transform;
                }
                else
                {
                    go.tag = Constants.GAME_MY_PLAYER_TAG;
                    GameSystem.Instance._gameManager._myCharacterList.Add(character);

                    campTransform = GameSystem.Instance._gameManager._enemyCamp.transform;
                }

                character.Init(info, campTransform, isEnemy);

                return true;
            }

            return false;
        }

        public void RemoveCharacterInfo(int id, bool isEnemy)
        {
            var list = new List<Character>();
            if (isEnemy)
                list = GameSystem.Instance._gameManager._enemyCharacterList;
            else
                list = GameSystem.Instance._gameManager._myCharacterList;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].CharacterDataInfo.id == id)
                    list.Remove(list[i]);
            }
        }

        public GameObject FindCharacterPrefab(string resourcesName)
        {
            // 현재 오브젝트가 애셋으로 빠지지 않아서 리소스 로드 하는 것으로 작업 진행함.

            //switch (id)
            //{
            //    case 1: // 1번은 임시 루민
            //        //return (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Game/Prefabs/Character/Lumin_Archer.prefab", typeof(GameObject));
            //        return GameSystem.Instance._gameManager._luminPrefab;
            //        //return Resources.Load<GameObject>("");
            //    case 2: // 2번은 임시 알라딘
            //        //return (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Game/Prefabs/Character/Aladin.prefab", typeof(GameObject));
            //        return GameSystem.Instance._gameManager._aladdinPrefab;
            //    case 100:   // 임시 적군
            //        return GameSystem.Instance._gameManager._luminPrefab;
            //    default:
            //        return null;
            //}

            return Resources.Load<GameObject>("Character/" + resourcesName);
        }

        public bool CreateCamp(CharacterDataInfo info, bool isEnemy)
        {
            // 나중에 캠프 아이디 + 스킨 아이디로 프리팹 가져오는거 추가해야합니다.
            var prefab = FindCharacterPrefab(info.meshRes);
            if (prefab == null)
                return false;

            GameObject parent;
            if (isEnemy)
                parent = GameSystem.Instance._gameManager._enemyCampParent;
            else
                parent = GameSystem.Instance._gameManager._myCampParent;

            var go = CommonTools.CreateGameObject(prefab, parent);
            if (go != null)
            {
                Camp camp = go.GetComponent<Camp>();

                if (isEnemy)
                    GameSystem.Instance._gameManager._enemyCamp = camp;
                else
                    GameSystem.Instance._gameManager._myCamp = camp;

                camp.Init(info, null, isEnemy);
            }

            return false;
        }
    }
}