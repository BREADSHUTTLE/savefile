using System;
using System.Collections.Generic;
using UnityEngine;

namespace CAPYBARA.Core
{
    public enum AudioSourceKey
    {
        None=-1,
        Dealcard_0 = 0,
        Golf,
        Second,
        Third,
        Showdown_0,
        Showdown_1,
        Winner,
        TimeCount,
        LogoPop,
        LogoEnd,
        IntroBGM,
        LobbyBGM,
        Die,
        Bing,
        Dadang,
        Call,
        Check,
        Quarter,
        Half,
        Allin,
        Max,
        Pass,
        DealSound,
        ChangeSound,
        FlipSound,
        CommunityOpen,
        ChipSound_0,
        ChipSound_1,
        ChipSound_2,
        ChipSound_3,
        ChipSound_4,
        ChipSound_5,
        ChipSound_6,
        ChipSound_7,
        ChipSound_8,
        ChipSound_9,
        ChipSound_10,
        CollectChip,
        Emotion_0,
        Emotion_1,
        Emotion_2,
        Emotion_3,
        Emotion_4,
        Emotion_5,
        Emotion_6,
        Emotion_7,
        Error,
        Ok,
        Close,
        Win,
        Alarm,
        Click_GameType,
        Click_Mid,
        Click_Big,
        Click_Small,
        
        END
    }

    public enum SoundType
    {
        BGM,
        Effect,
        Voice,
        End
    }

    public class AudioManager : MonoSingleton<AudioManager>
    {
        [System.Serializable]
        public class AudioPoolSetting
        {
            public AudioSourceKey key;
            public SoundType soundType;
            public AudioClip clip;
            public int count;
        }

        public AudioPoolSetting[] audioPoolSettings;
        public Dictionary<AudioSourceKey, AudioPool> pools;

        private float _bgmvolume = 1.0f;
        private float _effectvolume = 1.0f;
        private float _voicevolume = 1.0f;
        private float _allSoundVolume = 1.0f;
        
        private bool _bgmMuted = false;
        private bool _effectMuted = false;
        private bool _voiceMuted = false;
        private bool _allMuted = false;
        
        readonly LinkedList<AudioPool> _playingAudio = new LinkedList<AudioPool>();

        [ContextMenu("set")]
        public void Set()
        {
            for (int i = 0; i < audioPoolSettings.Length; i++)
            {
                audioPoolSettings[i].soundType = SoundType.Effect;
            }
        }
        protected override void Init()
        {
            pools = new Dictionary<AudioSourceKey, AudioPool>();

            foreach (var setting in audioPoolSettings)
            {
                var go = new GameObject();
                go.name = setting.key.ToString();
                go.transform.SetParent(this.transform);

                AudioSource[] sources = new AudioSource[setting.count];
                for (int i = 0; i < setting.count; i++)
                {
                    var g = new GameObject();
                    g.name = "AudioPool";
                    sources[i] = g.AddComponent<AudioSource>();
                    sources[i].transform.SetParent(go.transform);
                    sources[i].clip = setting.clip;
                    sources[i].playOnAwake = false;
                    sources[i].loop = setting.soundType==SoundType.BGM;

                }

                pools.Add(setting.key, new AudioPool
                {
                    _source = sources,
                    SoundType = setting.soundType
                });

                pools[setting.key].SetUp();
            }
        }

        public void SetVolume(SoundType soundType, float volume)
        {
            float _value = Mathf.Clamp(volume, 0, 1);

            switch (soundType)
            {
                case SoundType.BGM:
                    _bgmvolume = _value;
                    break;
                case SoundType.Effect:
                    _effectvolume = _value;
                    break;
                case SoundType.Voice:
                    _voicevolume = _value;
                    break;
                case SoundType.End:
                    break;
            }
        }
        
        
        public void SetMute(SoundType soundType, bool isSoundOn)
        {
            switch (soundType)
            {
                case SoundType.BGM:   _bgmMuted   = !isSoundOn; break;
                case SoundType.Effect:_effectMuted= !isSoundOn; break;
                case SoundType.Voice: _voiceMuted = !isSoundOn; break;
                case SoundType.End:   return;
            }
        }

        public void SetAllVolume(float volume)
        {
            float _value = Mathf.Clamp(volume, 0, 1);
            _allSoundVolume = _value;
        }
        private void Update()
        {
            var node = _playingAudio.First;
            while (node != null)
            {
                var audio = node.Value;
                if (audio.IsPlaying())
                {
                    switch (audio.SoundType)
                    {
                        case SoundType.BGM:
                            audio.SetVolume(_bgmvolume*_allSoundVolume);
                            audio.SetMute(_bgmMuted);
                            break;
                        case SoundType.Effect:
                            audio.SetVolume(_effectvolume*_allSoundVolume);
                            audio.SetMute(_effectMuted);
                            break;
                        case SoundType.Voice:
                            audio.SetVolume(_voicevolume*_allSoundVolume);
                            audio.SetMute(_voiceMuted);
                            break;
                        case SoundType.End:
                            break;
                    }
                }
                else
                {
                    _playingAudio.Remove(node);
                }
                node = node.Next;
            }
            
          
        }

        public void Play(AudioSourceKey key)
        {
            switch (pools[key].SoundType)
            {
                case SoundType.BGM:
                    pools[key].SetVolume(_bgmvolume*_allSoundVolume);
                    break;
                case SoundType.Effect:
                    pools[key].SetVolume(_effectvolume*_allSoundVolume);
                    break;
                case SoundType.Voice:
                    pools[key].SetVolume(_voicevolume*_allSoundVolume);
                    break;
                case SoundType.End:
                    break;
            }
            _playingAudio.AddLast(pools[key]);
            pools[key].Play();
        }

        public void Stop(AudioSourceKey key)
        {
            if (pools.ContainsKey(key))
            {
                pools[key].Stop();
            }
        }



        protected override void Release()
        {
            base.Release();
        }
    }
    [System.Serializable]
    public class AudioPool
    {
        public AudioSource[] _source;
        public SoundType SoundType;

        int seq = 0;

        public void SetUp()
        {
            seq %= _source.Length;
        }

        public void Play()
        {
            _source[seq++].Play();
            seq %= _source.Length;
        }
        public void Stop()
        {
            _source[seq].Stop();
        }

        public void SetVolume(float volume)
        {
            _source[seq].volume = volume;
        }
        
        public void SetMute(bool isMuted)
        {
            _source[seq].mute = isMuted;
        }

        public bool IsPlaying()
        {
            return _source[seq].isPlaying;
        }
    }
}
