using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CAPYBARA.Bundles
{
    [CreateAssetMenu(fileName = "LobbyResourcesBundle", menuName = "Bundle/LobbyResourcesBundle", order = 504)]
    public class LobbyResourcesBundle : ScriptableObject
    {
        public static LobbyResourcesBundle Loaded;

        public Sprite[] roomSlotBGSprites;
        public Sprite[] roomSlotHighlightBGSprites;

        [Header("inventory resources")]
        public Sprite[] itemSpriteList;
        
        [Header("mission resources")]
        public Sprite[] achieveMainImage;
        public Sprite[] achieveMissionIcons;


        [Header("boostart resource")]
        public Sprite[] boosterSpriteList;
        
        
        private const string BundleResourcekey = "LobbyResourcesBundle";
        public static AsyncOperationHandle<LobbyResourcesBundle> BeginLoad()
        {
            return Addressables.LoadAssetAsync<LobbyResourcesBundle>(BundleResourcekey);
        }
        public static async UniTask StartLoadAsset(CancellationTokenSource cts)
        {
           Loaded = await Addressables.LoadAssetAsync<LobbyResourcesBundle>(BundleResourcekey).WithCancellation(cts.Token);
        }
    }

}
