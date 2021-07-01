using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NLibCs;
using DFramework.Common;
using DFramework.Misc;

namespace DFramework.DataPool
{
    public enum SkillType
    {
        None = 0,
        Attack,
        Passive,
        Active,
        Player,
    }

    public enum SkillTarget
    {
        // 아군인지 적군인지, 타워인지 등등 추가 되어야 함.
        // 일단 기능 구현으로 나랑 적군 스킬 만들어보자.
        Self,
        Enemy,

    }


    // 발동 조건
    public enum SkillUseType
    {
        None,
        HPLess,
        HPOver,
    }

    public enum SkillRangeType
    {
        None,
        Circle,
    }

    public struct SkillData
    {
        public int id;                          // 스킬 고유 아이디
        public SkillType type;                  // 스킬 타입 (액티븐지..패시븐지..)
        public CharacterClassType classType;    // 이 스킬을 사용 할 수 있는 클래스 조건
        public SkillTarget target;              // 스킬 타겟
        public SkillUseType conditionType;      // 발동 조건
        public int conditionValue;              // 발동 조건 값
        public int projectile;                  // 발사체 여부 0,1 ?
        public int projectileSpeed;             // 발사체 속도
        public SkillRangeType rangeType;        // 범위 종류 (부채꼴인지.. 원인지 범위 스킬 종류?)
        public int range;                       // 범위 값
        public int delayTime;                   // 지연 시간
        public int sustainmentTime;             // 지속 시간
        public int effectGroupId;               // 효과 그룹 id
    }

    public struct SkillEffect
    {
        public int id;
        public int groupId;
        public int index;
        public EffectType effectType;
        public EffectValue effectValue;
        public int addValue;
        public int ratioValue;
    }

    public struct SkillEffectData
    {
        public int index;
        public EffectType effectType;
        public EffectValue effectValue;
        public int addValue;
        public int ratioValue;
    }

    public enum EffectType
    {
        Stat = 0,
        Buff,
    }

    public enum EffectValue
    {
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

    public class SkillInfo : DSingleton<SkillInfo>
    {
        private List<SkillData> _skillDatas = new List<SkillData>();

        
        private Dictionary<int, List<SkillEffectData>> _dicSkillEffectDatas = new Dictionary<int, List<SkillEffectData>>();

        public void UpdateSkillData(NDataSection section)
        {
            _skillDatas.Clear();

            foreach (NDataReader.Row row in section)
            {
                SkillData data = new SkillData();

                row.GetColumn(out data.id);
                row.GetColumn(out data.type);
                row.GetColumn(out data.classType);
                row.GetColumn(out data.target);
                row.GetColumn(out data.conditionType);
                row.GetColumn(out data.conditionValue);
                row.GetColumn(out data.projectile);
                row.GetColumn(out data.projectileSpeed);
                row.GetColumn(out data.rangeType);
                row.GetColumn(out data.range);
                row.GetColumn(out data.delayTime);
                row.GetColumn(out data.sustainmentTime);
                row.GetColumn(out data.effectGroupId);

                _skillDatas.Add(data);
            }
        }

        private void AddSkillEffectData(SkillEffect data)
        {
            Nullable<SkillEffect> info = data;
            if (info == null)
                return;
            
            var item = new SkillEffectData()
            {
                index = data.index,
                effectType = data.effectType,
                effectValue = data.effectValue,
                addValue = data.addValue,
                ratioValue = data.ratioValue,
            };
            
            if (_dicSkillEffectDatas.ContainsKey(data.groupId) == false)
                _dicSkillEffectDatas.Add(data.groupId, new List<SkillEffectData>());

            _dicSkillEffectDatas[data.groupId].Add(item);

        }

        public void UpdateSkillEffectData(NDataSection section)
        {
            _dicSkillEffectDatas.Clear();

            foreach (NDataReader.Row row in section)
            {
                SkillEffect data = new SkillEffect();
                row.GetColumn(out data.id);
                row.GetColumn(out data.groupId);
                row.GetColumn(out data.index);
                row.GetColumn(out data.effectType);
                row.GetColumn(out data.effectValue);
                row.GetColumn(out data.addValue);
                row.GetColumn(out data.ratioValue);

                AddSkillEffectData(data);
            }
        }

        public SkillData GetSkillData(int id)
        {
            for (int i = 0; i < _skillDatas.Count; i++)
            {
                if (_skillDatas[i].id == id)
                    return _skillDatas[i];
            }

            return new SkillData?().Value;
        }

        public List<SkillEffectData> FindSkillEffectData(int groupId)
        {
            if (_dicSkillEffectDatas.ContainsKey(groupId) == false)
                return null;

            return _dicSkillEffectDatas[groupId];
        }
    }
}