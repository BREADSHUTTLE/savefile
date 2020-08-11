using UnityEngine;
using System.Collections.Generic;
using TangentFramework;
using JaykayFramework;
using System;
using System.Collections;

public class CharacterVoiceMgr : MonoSingleton<CharacterVoiceMgr>, IListener
{

    public enum CharacterVoiceType
    {
        Start,
        Run,
        ChangeChar,
        UseSkill,
        ChoiceChar,
        GainItem,
        Die,
        end
    }

    static int VoiceCharacterIndex = 0; // 아웃게임에서 캐릭터 변경될때마다 어떤 캐릭터인지 알아야하기때문에.. 

    public static void SetVoiceCharacterIndex(int Id)
    {
        VoiceCharacterIndex = Id;
    }

    public override void Init()
    {
        // GameObject obj = new GameObject("CharacterVoiceMgr");
        ListenerManager.instance.AddListener(this as IListener);
    }

    bool IListener.HandleEvent(string eventName, object data)
    {
        switch (eventName)
        {
            case "CharacterVoice_Start":
                PlayCharacterVoice(CharacterVoiceType.Start);
                break;
            case "CharacterVoice_Run":
                PlayCharacterVoice(CharacterVoiceType.Run);
                break;
            case "CharacterVoice_ChangeChar":
                PlayCharacterVoice(CharacterVoiceType.ChangeChar);
                break;
            case "CharacterVoice_UseSkill":
                PlayCharacterVoice(CharacterVoiceType.UseSkill);
                break;
            case "CharacterVoice_ChoiceChar":
                PlayCharacterVoice(CharacterVoiceType.ChoiceChar);
                break;
            case "CharacterVoice_Die":
                PlayCharacterVoice(CharacterVoiceType.Die);
                break;
            case "CharacterVoice_GainItem":
                PlayCharacterVoice(CharacterVoiceType.GainItem);
                break;

        }
    
        return true;
    }

    void PlayCharacterVoice(CharacterVoiceType type)
    {
        TomInGameManager m_ingameManager = (TomInGameManager)InGameManager.Instance;
        TomRunnerController character = m_ingameManager.GetMainCharacter();

        ItemInfo characterinfo = null;

        if (character != null && StageMgr.instance.CurStageName == "StageInGame")
            characterinfo = ItemTableMgr.instance.GetItemData(character.GetID());
        else
        {
            characterinfo = ItemTableMgr.instance.GetItemData(VoiceCharacterIndex != 0 ? VoiceCharacterIndex : PlayerData.instance.GetCurCharacterID());
        }

        if(characterinfo == null)
        {
            CLog.LogError("MainCharacter Null");
            return;
        }
       

        VoiceCharacterIndex = 0;
        string audioname = "";

        
        audioname = characterinfo.OriginalCharacterName.ToString() + "_" + type.ToString();

        //음향이름 = 캐릭터 종류 + _ + 행동타입.
        //기존 사운드 스탑.
        SoundMgr.StopSoundsByName(ESOUND_TYPE.Effect, audioname);
        SoundMgr.PlaySound2D(ESOUND_TYPE.Effect, audioname, 1, 1);

        //기존 사운드 유지;
        //if (!SoundMgr.IsPlayingSoundCheck(ESOUND_TYPE.Effect, audioname))
        //{
        //    SoundMgr.SetSpaticalOption(ESOUND_TYPE.Effect, audioname, 0);
        //    SoundMgr.PlaySound2D(ESOUND_TYPE.Effect, audioname, 1, 1);

        //}

    }
}
