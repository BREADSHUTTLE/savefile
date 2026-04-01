using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlackTree.Bundles
{
    [CreateAssetMenu(fileName = "ItemBundle", menuName = "Bundle/ItemBundle", order = 505)]
    public class ItemBundle : ScriptableObject
    {
        public static ItemBundle Loaded;

        [Serializable]
        public class CoinData
        {
            public string Id;
            public Sprite Sprite;
        }

        [Serializable]
        public class AvatarData
        {
            public string AvatarId;
            public Sprite AvatarSprite;
            public Sprite AvatarShadowSprite;
            public Sprite AvatarIcon;
            public Sprite AvatarInGameIcon;
            public Vector2 Offset;
        }

        [Serializable]
        public class EmoticonData
        {
            public string EmoticonId;
            public Sprite EmoticonSprite;
        }

        [Serializable]
        public class PointItemData
        {
            public string PointItemId;
            public Sprite PointItemSprite;
        }

        [Serializable]
        public class ClassData
        {
            public string ClassId;  // CLASS_B, CLASS_A, CLASS_S
            public Sprite ClassSprite;
        }

        [Serializable]
        public class ConsumableItem
        {
            public string ConsumableId;
            public string ProductId;        // message_50, message_20, message_100 등
            public Sprite ConsumableSprite;
        }

        public List<CoinData> coinSprites = new List<CoinData>();
        public List<CoinData> coinShopSprites = new List<CoinData>();
        public List<AvatarData> avatars = new List<AvatarData>();
        public List<EmoticonData> emoticons = new List<EmoticonData>();
        public List<PointItemData> pointItems = new List<PointItemData>();
        public List<ClassData> classItems = new List<ClassData>();
        public List<ConsumableItem> consumableItems = new List<ConsumableItem>();

        private const string BundleResourcekey = "ItemBundle";

        public static AsyncOperationHandle<ItemBundle> BeginLoad()
        {
            return Addressables.LoadAssetAsync<ItemBundle>(BundleResourcekey);
        }

        public static async UniTask StartLoadAsset(CancellationTokenSource cts)
        {
            Loaded = await Addressables.LoadAssetAsync<ItemBundle>(BundleResourcekey).WithCancellation(cts.Token);
        }

        public Sprite GetItemSprite(string id, bool miniSprite = false, string productId = null)
        {
            if (id.Contains("AVATAR"))
            {
                for (int i = 0; i < avatars.Count; i++)
                {
                    if (string.Equals(avatars[i].AvatarId, id, StringComparison.OrdinalIgnoreCase))
                    {
                        if (miniSprite)
                            return avatars[i].AvatarIcon;
                        else
                            return avatars[i].AvatarSprite;
                    }
                }
            }
            else if (id.Contains("DEFAULT_CURRENCY"))
            {
                return coinSprites[2].Sprite;   // 이거 디폴트로 무슨 코인 아이콘 쓸건지는 ...논의가 필요함.
            }
            else if (id.Contains("COIN"))
            {
                for (int i = 0; i < coinSprites.Count; i++)
                {
                    if (coinSprites[i].Id == id)
                        return coinSprites[i].Sprite;
                }
            }
            else if (id.Contains("EMOTICON"))
            {
                for (int i = 0; i < emoticons.Count; i++)
                {
                    if (emoticons[i].EmoticonId == id)
                        return emoticons[i].EmoticonSprite;
                }
            }
            else if (id.Contains("BOOSTER"))
            {
                for (int i = 0; i < pointItems.Count; i++)
                {
                    if (pointItems[i].PointItemId == id)
                        return pointItems[i].PointItemSprite;
                }
            }
            else if (id.Contains("POCKET"))
            {
                for (int i = 0; i < pointItems.Count; i++)
                {
                    if (pointItems[i].PointItemId == id)
                        return pointItems[i].PointItemSprite;
                }
            }
            else if (id.Contains("CLASS"))
            {
                for (int i = 0; i < classItems.Count; i++)
                {
                    if (classItems[i].ClassId == id)
                        return classItems[i].ClassSprite;
                }
            }
            else if (id.Contains("MESSAGE"))
            {
                for (int i = 0; i < consumableItems.Count; i++)
                {
                    // productId가 null이면 ConsumableId만 매칭, 있으면 둘 다 매칭
                    if (consumableItems[i].ConsumableId == id && (productId == null || consumableItems[i].ProductId == productId))
                        return consumableItems[i].ConsumableSprite;
                }
            }
            else if (id.Contains("NICKNAME"))
            {
                for (int i = 0; i < consumableItems.Count; i++)
                {
                    if (consumableItems[i].ConsumableId == id)
                        return consumableItems[i].ConsumableSprite;
                }
            }
            return null;
        }

        #region Coin
        public Sprite GetCoinSprite(string id, bool useShop = false)
        {
            CoinData data;
            if (useShop)
                data = coinShopSprites.Find(s => s.Id == id);
            else
                data = coinSprites.Find(s => s.Id == id);
            return data?.Sprite;
        }

        public Sprite GetCoinSpriteByIndex(int index, bool useShop = false)
        {
            if (useShop)
            {
                if (index >= 0 && index < coinShopSprites.Count)
                    return coinShopSprites[index].Sprite;
            }
            else
            {
                if (index >= 0 && index < coinSprites.Count)
                    return coinSprites[index].Sprite;
            }
            return null;
        }

        public CoinData GetCoinDataById(string id, bool useShop = false)
        {
            if (useShop)
                return coinShopSprites.Find(s => s.Id == id);
            else
                return coinSprites.Find(s => s.Id == id);
        }

        public int CoinCount => coinSprites.Count;
        #endregion

        #region Avatar
        public Sprite GetAvatarSprite(string avatarId)
        {
            var avatar = avatars.Find(a => string.Equals(a.AvatarId, avatarId, StringComparison.OrdinalIgnoreCase));
            return avatar?.AvatarSprite;
        }

        public Sprite GetAvatarShadowSprite(string avatarId)
        {
            var avatar = avatars.Find(a => string.Equals(a.AvatarId, avatarId, StringComparison.OrdinalIgnoreCase));
            return avatar?.AvatarShadowSprite;
        }

        public Sprite GetAvatarIcon(string avatarId)
        {
            var avatar = avatars.Find(a => string.Equals(a.AvatarId, avatarId, StringComparison.OrdinalIgnoreCase));
            return avatar?.AvatarIcon;
        }

        public Sprite GetAvatarInGameIcon(string avatarId)
        {
            var avatar = avatars.Find(a => string.Equals(a.AvatarId, avatarId, StringComparison.OrdinalIgnoreCase));
            return avatar?.AvatarInGameIcon;
        }

        public AvatarData GetAvatarByIndex(int index)
        {
            if (index >= 0 && index < avatars.Count)
                return avatars[index];
            return null;
        }

        public AvatarData GetAvatarById(string avatarId)
        {
            return avatars.Find(a => string.Equals(a.AvatarId, avatarId, StringComparison.OrdinalIgnoreCase));
        }

        public Vector2 GetAvatarOffset(string avatarId)
        {
            var avatar = avatars.Find(a => string.Equals(a.AvatarId, avatarId, StringComparison.OrdinalIgnoreCase));
            return avatar?.Offset ?? Vector2.zero;
        }
        #endregion

        #region Emoticon
        public Sprite GetEmoticonSprite(string emoticonId)
        {
            var emoticon = emoticons.Find(e => e.EmoticonId == emoticonId);
            return emoticon?.EmoticonSprite;
        }

        public EmoticonData GetEmoticonById(string emoticonId)
        {
            return emoticons.Find(e => e.EmoticonId == emoticonId);
        }

        public EmoticonData GetEmoticonByIndex(int index)
        {
            if (index >= 0 && index < emoticons.Count)
                return emoticons[index];
            return null;
        }
        #endregion

        #region Point
        public Sprite GetPointItemSprite(string pointItemId)
        {
            var pointItem = pointItems.Find(p => p.PointItemId == pointItemId);
            return pointItem?.PointItemSprite;
        }

        public PointItemData GetPointItemById(string pointItemId)
        {
            return pointItems.Find(p => p.PointItemId == pointItemId);
        }

        public PointItemData GetPointItemByIndex(int index)
        {
            if (index >= 0 && index < pointItems.Count)
                return pointItems[index];
            return null;
        }

        public int PointItemCount => pointItems.Count;
        #endregion

        #region Class
        public Sprite GetClassSprite(string classId)
        {
            var classItem = classItems.Find(c => c.ClassId == classId);
            return classItem?.ClassSprite;
        }

        public ClassData GetClassById(string classId)
        {
            return classItems.Find(c => c.ClassId == classId);
        }

        public ClassData GetClassByIndex(int index)
        {
            if (index >= 0 && index < classItems.Count)
                return classItems[index];
            return null;
        }

        public int ClassCount => classItems.Count;
        #endregion

        #region Consumable
        public Sprite GetConsumableSprite(string consumableId, string productId = null)
        {
            var consumable = consumableItems.Find(c => c.ConsumableId == consumableId && (productId == null || c.ProductId == productId));
            return consumable?.ConsumableSprite;
        }

        public ConsumableItem GetConsumableById(string consumableId, string productId = null)
        {
            return consumableItems.Find(c => c.ConsumableId == consumableId && (productId == null || c.ProductId == productId));
        }

        public ConsumableItem GetConsumableByProductId(string productId)
        {
            return consumableItems.Find(c => c.ProductId == productId);
        }

        public ConsumableItem GetConsumableByIndex(int index)
        {
            if (index >= 0 && index < consumableItems.Count)
                return consumableItems[index];
            return null;
        }

        public int ConsumableCount => consumableItems.Count;
        #endregion
    }
}