using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA
{
    public static class ConfigDataManager
    {
        private static Dictionary<string, long> serverVersions = new Dictionary<string, long>();
        private static Dictionary<string, long> localVersions = new Dictionary<string, long>();
        
        private const string VERSION_FILE_NAME = "config_versions.json";
        private const string CACHE_FOLDER = "ConfigCache";
        
        private static string CacheFolderPath => Path.Combine(Application.persistentDataPath, CACHE_FOLDER);
        private static string VersionFilePath => Path.Combine(CacheFolderPath, VERSION_FILE_NAME);
        
        
        public static long GetLocalVersion(string configName)
        {
            return localVersions.TryGetValue(configName, out var version) ? version : 0;
        }
        
        private static void SaveLocalVersion(string configName, long version)
        {
            localVersions[configName] = version;
            SaveVersionsToFile();
        }
        
        private static void LoadVersionsFromFile()
        {
            try
            {
                if (!File.Exists(VersionFilePath))
                    return;
                
                var json = File.ReadAllText(VersionFilePath);
                var data = JsonUtility.FromJson<VersionFileData>(json);
                if (data?.Versions != null)
                {
                    localVersions.Clear();
                    foreach (var item in data.Versions)
                        localVersions[item.Name] = item.Version;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] LoadVersionsFromFile failed: {e.Message}");
            }
        }
        
        private static void SaveVersionsToFile()
        {
            try
            {
                if (!Directory.Exists(CacheFolderPath))
                    Directory.CreateDirectory(CacheFolderPath);
                
                var data = new VersionFileData
                {
                    Versions = localVersions.Select(kv => new VersionItem { Name = kv.Key, Version = kv.Value }).ToArray()
                };
                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(VersionFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] SaveVersionsToFile failed: {e.Message}");
            }
        }
        
        [Serializable]
        private class VersionFileData
        {
            public VersionItem[] Versions;
        }
        
        [Serializable]
        private class VersionItem
        {
            public string Name;
            public long Version;
        }
        
        public static long GetServerVersion(string configName)
        {
            return serverVersions.TryGetValue(configName, out var version) ? version : 0;
        }
        
        public static bool IsVersionChanged(string configName)
        {
            return GetLocalVersion(configName) != GetServerVersion(configName);
        }
        
        public static async UniTask<bool> LoadServerVersionsAsync()
        {
            var result = await Services.Lobby.ConfigVersionGetReqAsync();
            if (!result.IsSuccess || result.Data?.ConfigData == null)
            {
                Debug.LogWarning($"[ConfigDataManager] LoadServerVersions failed: {result.Error}");
                return false;
            }
            
            serverVersions.Clear();
            foreach (var config in result.Data.ConfigData)
                serverVersions[config.Name] = config.Version;
            
            return true;
        }
        
       #region Point 
        public const string CONFIG_NAME_POINTS = "_points_reward";
        private static List<ConfigPoints> configPoints = new List<ConfigPoints>();
        public static IReadOnlyList<ConfigPoints> points => configPoints;
        
        public static async UniTask<bool> LoadPointsAsync()
        {
            return await LoadConfigAsync(CONFIG_NAME_POINTS,
                async (version) =>
                {
                    var result = await Services.Lobby.ConfigPointsGetReqAsync(version);
                    if (!result.IsSuccess || result.Data?.ConfigPoints == null)
                        return false;
                    
                    configPoints = result.Data.ConfigPoints.ToList();
                    return true;
                },
                () => TryLoadPointsFromCache(),
                (version) => SavePointsToCache(version)
            );
        }
        
        private static string GetCacheFilePath(string configName)
        {
            return Path.Combine(CacheFolderPath, $"{configName}.json");
        }
        
        private static bool TryLoadPointsFromCache()
        {
            var filePath = GetCacheFilePath(CONFIG_NAME_POINTS);
            if (!File.Exists(filePath))
                return false;
            
            try
            {
                var json = File.ReadAllText(filePath);
                var cached = JsonUtility.FromJson<ConfigPointsCache>(json);
                if (cached?.Items == null || cached.Items.Length == 0)
                    return false;
                
                configPoints = cached.Items.Select(item => new ConfigPoints
                {
                    RewardType = item.rewardType,
                    PointsType = item.pointsType,
                    PointsMin = item.pointsMin,
                    Amount = item.amount,
                    TxType = item.txType
                }).ToList();
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] Points cache parse failed: {e.Message}");
                return false;
            }
        }
        
        private static void SavePointsToCache(long version)
        {
            try
            {
                if (!Directory.Exists(CacheFolderPath))
                    Directory.CreateDirectory(CacheFolderPath);
                
                var cache = new ConfigPointsCache
                {
                    Items = configPoints.Select(p => new ConfigPointsCacheItem
                    {
                        rewardType = p.RewardType,
                        pointsType = p.PointsType,
                        pointsMin = p.PointsMin,
                        amount = p.Amount,
                        txType = p.TxType
                    }).ToArray()
                };
                
                var json = JsonUtility.ToJson(cache, true);
                var filePath = GetCacheFilePath(CONFIG_NAME_POINTS);
                File.WriteAllText(filePath, json);
                
                SaveLocalVersion(CONFIG_NAME_POINTS, version);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] Points cache save failed: {e.Message}");
            }
        }
        
        public static ConfigPoints GetPointsByRewardType(string rewardType)
        {
            return configPoints.FirstOrDefault(p => p.RewardType == rewardType);
        }
        
        public static List<ConfigPoints> GetPointsByType(string pointsType)
        {
            return configPoints.Where(p => p.PointsType == pointsType).ToList();
        }
        
        [Serializable]
        internal class ConfigPointsCache
        {
            public ConfigPointsCacheItem[] Items;
        }

        [Serializable]
        internal class ConfigPointsCacheItem
        {
            public string rewardType;
            public string pointsType;
            public long pointsMin;
            public long amount;
            public string txType;
        }
        #endregion
        
        #region Items
        public const string CONFIG_NAME_ITEMS = "_items";
        private static List<ConfigItems> configItems = new List<ConfigItems>();
        public static IReadOnlyList<ConfigItems> items => configItems;
        
        public static async UniTask<bool> LoadItemsAsync()
        {
            return await LoadConfigAsync(CONFIG_NAME_ITEMS,
                async (version) =>
                {
                    var result = await Services.Lobby.ConfigItemsGetReqAsync(version);
                    if (!result.IsSuccess || result.Data?.ConfigItems == null)
                        return false;
                    
                    configItems = result.Data.ConfigItems.ToList();
                    return true;
                },
                () => TryLoadItemsFromCache(),
                (version) => SaveItemsToCache(version)
            );
        }
        
        private static bool TryLoadItemsFromCache()
        {
            var filePath = GetCacheFilePath(CONFIG_NAME_ITEMS);
            if (!File.Exists(filePath))
                return false;
            
            try
            {
                var json = File.ReadAllText(filePath);
                var cached = JsonUtility.FromJson<ConfigItemsCache>(json);
                if (cached?.Items == null || cached.Items.Length == 0)
                    return false;
                
                configItems = cached.Items.Select(item => new ConfigItems
                {
                    ItemId = item.itemId,
                    ItemType = item.itemType
                }).ToList();
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] Items cache parse failed: {e.Message}");
                return false;
            }
        }
        
        private static void SaveItemsToCache(long version)
        {
            try
            {
                if (!Directory.Exists(CacheFolderPath))
                    Directory.CreateDirectory(CacheFolderPath);
                
                var cache = new ConfigItemsCache
                {
                    Items = configItems.Select(i => new ConfigItemsCacheItem
                    {
                        itemId = i.ItemId,
                        itemType = i.ItemType
                    }).ToArray()
                };
                
                var json = JsonUtility.ToJson(cache, true);
                var filePath = GetCacheFilePath(CONFIG_NAME_ITEMS);
                File.WriteAllText(filePath, json);
                
                SaveLocalVersion(CONFIG_NAME_ITEMS, version);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] Items cache save failed: {e.Message}");
            }
        }
        
        public static ConfigItems GetItemById(string itemId)
        {
            return configItems.FirstOrDefault(i => i.ItemId == itemId);
        }
        
        public static List<ConfigItems> GetItemsByType(string itemType)
        {
            return configItems.Where(i => i.ItemType == itemType).ToList();
        }
        
        [Serializable]
        internal class ConfigItemsCache
        {
            public ConfigItemsCacheItem[] Items;
        }

        [Serializable]
        internal class ConfigItemsCacheItem
        {
            public string itemId;
            public string itemType;
        }
        #endregion
        
        #region InAppItems
        public const string CONFIG_NAME_IN_APP_ITEMS = "_in_app_items";
        private static List<ConfigInAppItems> configInAppItems = new List<ConfigInAppItems>();
        public static IReadOnlyList<ConfigInAppItems> inAppItems => configInAppItems;
        
        public static async UniTask<bool> LoadInAppItemsAsync()
        {
            return await LoadConfigAsync(CONFIG_NAME_IN_APP_ITEMS,
                async (version) =>
                {
                    var result = await Services.Lobby.ConfigInAppItemsGetReqAsync(version);
                    if (!result.IsSuccess || result.Data?.ConfigInAppItems == null)
                        return false;
                    
                    configInAppItems = result.Data.ConfigInAppItems.ToList();
                    return true;
                },
                () => TryLoadInAppItemsFromCache(),
                (version) => SaveInAppItemsToCache(version)
            );
        }
        
        private static bool TryLoadInAppItemsFromCache()
        {
            var filePath = GetCacheFilePath(CONFIG_NAME_IN_APP_ITEMS);
            if (!File.Exists(filePath))
                return false;
            
            try
            {
                var json = File.ReadAllText(filePath);
                var cached = JsonUtility.FromJson<ConfigInAppItemsCache>(json);
                if (cached?.Items == null || cached.Items.Length == 0)
                    return false;
                
                configInAppItems = cached.Items.Select(item => new ConfigInAppItems
                {
                    InAppItemType = item.inAppItemType,
                    InAppItemId = item.inAppItemId,
                    Platform = item.platform,
                    ProductId = item.productId,
                    Price = item.price,
                    ItemId = item.itemId,
                    Amount = item.amount
                }).ToList();
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] InAppItems cache parse failed: {e.Message}");
                return false;
            }
        }
        
        private static void SaveInAppItemsToCache(long version)
        {
            try
            {
                if (!Directory.Exists(CacheFolderPath))
                    Directory.CreateDirectory(CacheFolderPath);
                
                var cache = new ConfigInAppItemsCache
                {
                    Items = configInAppItems.Select(i => new ConfigInAppItemsCacheItem
                    {
                        inAppItemType = i.InAppItemType,
                        inAppItemId = i.InAppItemId,
                        platform = i.Platform,
                        productId = i.ProductId,
                        price = i.Price,
                        itemId = i.ItemId,
                        amount = i.Amount
                    }).ToArray()
                };
                
                var json = JsonUtility.ToJson(cache, true);
                var filePath = GetCacheFilePath(CONFIG_NAME_IN_APP_ITEMS);
                File.WriteAllText(filePath, json);
                
                SaveLocalVersion(CONFIG_NAME_IN_APP_ITEMS, version);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] InAppItems cache save failed: {e.Message}");
            }
        }
        
        public static ConfigInAppItems GetInAppItemByProductId(string productId)
        {
            return configInAppItems.FirstOrDefault(i => i.ProductId == productId);
        }
        
        public static ConfigInAppItems GetInAppItemByInAppItemId(string inAppItemId)
        {
            return configInAppItems.FirstOrDefault(i => i.InAppItemId == inAppItemId);
        }

        public static List<ConfigInAppItems> GetInAppItemsByProductId(string productId)
        {
            return configInAppItems.Where(i => i.ProductId == productId).ToList();
        }
        
        public static List<ConfigInAppItems> GetInAppItemsByType(string inAppItemType)
        {
            return configInAppItems.Where(i => i.InAppItemType == inAppItemType).ToList();
        }
        
        public static List<ConfigInAppItems> GetInAppItemsByPlatform(string platform)
        {
            return configInAppItems.Where(i => i.Platform == platform).ToList();
        }
        
        [Serializable]
        internal class ConfigInAppItemsCache
        {
            public ConfigInAppItemsCacheItem[] Items;
        }

        [Serializable]
        internal class ConfigInAppItemsCacheItem
        {
            public string inAppItemType;
            public string inAppItemId;
            public string platform;
            public string productId;
            public long price;
            public string itemId;
            public long amount;
        }
        #endregion
        
        #region Quests (미션/업적)
        public const string CONFIG_NAME_QUESTS = "_quests_reward";
        private static List<Quest> configQuests = new List<Quest>();
        public static IReadOnlyList<Quest> quests => configQuests;
        
        public static async UniTask<bool> LoadQuestsAsync()
        {
            return await LoadConfigAsync(CONFIG_NAME_QUESTS,
                async (version) =>
                {
                    var result = await Services.Lobby.ConfigQuestsGetReqAsync(version);
                    if (!result.IsSuccess || result.Data?.QuestList == null)
                        return false;
                    
                    configQuests = result.Data.QuestList.ToList();
                    return true;
                },
                () => TryLoadQuestsFromCache(),
                (version) => SaveQuestsToCache(version)
            );
        }
        
        private static bool TryLoadQuestsFromCache()
        {
            var filePath = GetCacheFilePath(CONFIG_NAME_QUESTS);
            if (!File.Exists(filePath))
                return false;
            
            try
            {
                var json = File.ReadAllText(filePath);
                var cached = JsonUtility.FromJson<ConfigQuestsCache>(json);
                if (cached?.Items == null || cached.Items.Length == 0)
                    return false;
                
                configQuests = cached.Items.Select(item => new Quest
                {
                    QuestId = item.questId,
                    QuestType = item.questType,
                    Type = item.type,
                    QuestValue = item.questValue,
                    MaxCount = item.maxCount,
                    RewardItemId = item.rewardItemId,
                    RewardValue = item.rewardValue,
                    ReceivedRewardValue = item.receivedRewardValue
                }).ToList();
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] Quests cache parse failed: {e.Message}");
                return false;
            }
        }
        
        private static void SaveQuestsToCache(long version)
        {
            try
            {
                if (!Directory.Exists(CacheFolderPath))
                    Directory.CreateDirectory(CacheFolderPath);
                
                var cache = new ConfigQuestsCache
                {
                    Items = configQuests.Select(q => new ConfigQuestsCacheItem
                    {
                        questId = q.QuestId,
                        questType = q.QuestType,
                        type = q.Type,
                        questValue = q.QuestValue,
                        maxCount = q.MaxCount,
                        rewardItemId = q.RewardItemId,
                        rewardValue = q.RewardValue,
                        receivedRewardValue = q.ReceivedRewardValue
                    }).ToArray()
                };
                
                var json = JsonUtility.ToJson(cache, true);
                var filePath = GetCacheFilePath(CONFIG_NAME_QUESTS);
                File.WriteAllText(filePath, json);
                
                SaveLocalVersion(CONFIG_NAME_QUESTS, version);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] Quests cache save failed: {e.Message}");
            }
        }
        
        public static Quest GetQuestById(string questId)
        {
            return configQuests.FirstOrDefault(q => q.QuestId == questId);
        }
        
        public static List<Quest> GetQuestsByType(string questType)
        {
            return configQuests.Where(q => q.QuestType == questType).ToList();
        }
        
        [Serializable]
        internal class ConfigQuestsCache
        {
            public ConfigQuestsCacheItem[] Items;
        }

        [Serializable]
        internal class ConfigQuestsCacheItem
        {
            public string questId;
            public string questType;
            public string type;
            public int questValue;
            public int maxCount;
            public string rewardItemId;
            public int rewardValue;
            public int receivedRewardValue;
        }
        #endregion
        
        private static async UniTask<bool> LoadConfigAsync(string configName, Func<long, UniTask<bool>> loadFromServer, Func<bool> loadFromCache, Action<long> saveToCache)
        {
            var serverVersion = GetServerVersion(configName);
            if (serverVersion == 0)
            {
                Debug.LogWarning($"[ConfigDataManager] {configName} version not found.");
                return false;
            }

            var localVersion = GetLocalVersion(configName);
            
            // 버전이 같으면 캐시에서 로드 시도
            if (localVersion == serverVersion && loadFromCache())
                return true;

            // 서버에 요청할 버전 - 로컬에 데이터가 없으면 -1로 요청 // proto 에서 0 으로 보내니까 안보내짐 없는걸로 취급함.
            var requestVersion = localVersion > 0 ? localVersion : -1;
            
            if (!await loadFromServer(requestVersion))
            {
                Debug.LogWarning($"[ConfigDataManager] Load {configName} from server failed");
                return false;
            }

            saveToCache(serverVersion);
            return true;
        }
        
        
        public static async UniTask<bool> InitializeAsync()
        {
            // 로컬에 저장된 버전 정보 로드
            LoadVersionsFromFile();
            
            if (!await LoadServerVersionsAsync())
                return false;
            
            // 여기에 다른 Config 로드 할 것이 있으면 추가 하시면 됩니다!
            
            if (!await LoadPointsAsync())
                return false;
            
            if (!await LoadItemsAsync())
                return false;
            
            if (!await LoadInAppItemsAsync())
                return false;
            
            if (!await LoadQuestsAsync())
                return false;
            
            return true;
        }
        
        public static void ClearCache(string configName)
        {
            try
            {
                var filePath = GetCacheFilePath(configName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
                
                if (localVersions.ContainsKey(configName))
                {
                    localVersions.Remove(configName);
                    SaveVersionsToFile();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] ClearCache failed: {e.Message}");
            }
        }
        
        public static void ClearAllCache()
        {
            try
            {
                if (Directory.Exists(CacheFolderPath))
                    Directory.Delete(CacheFolderPath, true);
                
                localVersions.Clear();
                serverVersions.Clear();
                configPoints.Clear();
                configItems.Clear();
                configInAppItems.Clear();
                configQuests.Clear();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigDataManager] ClearAllCache failed: {e.Message}");
            }
        }
    }
}
