using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NLibCs;
using DFramework.Misc;

namespace DFramework.DataPool
{
    public sealed class DataLoder : DMonoPersistSingleton<DataLoder>
    {
        public void LoadUserData()
        {
            LoadData("user", (userValue) =>
            {
                UserInfo.Instance.Update(userValue);
            });
        }

        public void LoadBattleData()
        {
            LoadData("Stage", (battle) =>
            {
                LoadData("EnemyGroup", (enemy) =>
                {
                    BattleInfo.Instance.Update(battle);
                    EnemyInfo.Instance.Update(enemy);
                });
            });
        }

        public void LoadCharacterData()
        {
            LoadData("Stat", (statData) =>
            {
                LoadData("ClassValue", (classValue) =>
                {
                    LoadData("BattleUnit", (characterValue) =>
                    {
                        CharacterInfo.Instance.UpdateStat(statData);
                        ClassStatInfo.Instance.Update(classValue);
                        CharacterInfo.Instance.Update(characterValue);
                    });
                });
            });
        }

        public void LoadSkill()
        {
            LoadData("Skill", (skillData) =>
            {
                LoadData("SkillEffect", (effectData) =>
                {
                    SkillInfo.Instance.UpdateSkillData(skillData);
                    SkillInfo.Instance.UpdateSkillEffectData(effectData);
                });
            });
        }

        private void LoadData(string dataName, Action<NDataSection> callback)
        {
            StringBuilder sb = new StringBuilder();
            sb.Length = 0;


#if UNITY_ANDROID && !UNITY_EDITOR
            string assetPath = Application.persistentDataPath;
#else
            string assetPath = Application.dataPath;
#endif
            //TODO:: 나중에 서버에서 테이블 받아서 할 때 여기 수정되어야 할듯.      
            string path = sb.Append(assetPath).Append("/Table/").Append(dataName).Append(".ndt").ToString();


            NDataSection section = LoadSection(dataName, path);
            if (section == null)
            {
                OnError(dataName);
            }
            else
            {
                if (callback != null)
                    callback(section);
            }
        }

        private NDataSection LoadSection(string dataName, string path)
        {
            NDataReader ndr = new NDataReader();
            string str = ndr.GetFileString(path);

            bool result = ndr.LoadFrom(str);

            if (result == false)
                return null;

            return ndr[dataName];

            //foreach (NDataReader.Row row in ndr[dataName])
            //{
            //    Debug.Log(row.ToDataString());
            //}
        }

        private void OnError(string dataName)
        {
            //TODO:: 테이블 로드 시, 에러나는 상황에 따라 필요한 경우 여기에 작성
            switch (dataName)
            {
#if UNITY_EDITOR
                default:

                    break;
#else
            default:

                break;
#endif
            }
        }
    }
}