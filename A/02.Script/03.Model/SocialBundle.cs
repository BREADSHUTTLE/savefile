using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlackTree.Bundles
{
    [CreateAssetMenu(fileName = "SocialBundle", menuName = "Bundle/SocialBundle", order = 506)]
    public class SocialBundle : ScriptableObject
    {
        public static SocialBundle Loaded;

        [Serializable]
        public class EmojiData
        {
            public string EmojiId;
            public Sprite EmojiSprite;
        }

        public List<EmojiData> Emojis = new List<EmojiData>();

        private const string BundleResourcekey = "SocialBundle";

        public static AsyncOperationHandle<SocialBundle> BeginLoad()
        {
            return Addressables.LoadAssetAsync<SocialBundle>(BundleResourcekey);
        }
        public static async UniTask StartLoadAsset(CancellationTokenSource cts)
        {
            Loaded = await Addressables.LoadAssetAsync<SocialBundle>(BundleResourcekey).WithCancellation(cts.Token);
        }

        public Sprite GetEmojiSprite(string emojiId)
        {
            var emoji = Emojis.Find(e => e.EmojiId == emojiId);
            return emoji?.EmojiSprite;
        }

        public EmojiData GetEmojiById(string emojiId)
        {
            return Emojis.Find(e => e.EmojiId == emojiId);
        }

        public EmojiData GetEmojiByIndex(int index)
        {
            if (index >= 0 && index < Emojis.Count)
                return Emojis[index];
            return null;
        }
    }
}

