using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using CAPYBARA.Definition;
using CAPYBARA.lobby;

namespace CAPYBARA.Core
{
    public class StaticData
    {
        public static StaticDataWrapper Wrapper { get; private set; }
        
        private static Dictionary<ErrorCode, LobbyErrorInfo> _lobbyErrorInfoDict;
        private static Dictionary<ItemID, ItemNameInfo> _itemNameInfoDict;

        public static async UniTask Load()
        {
            //#if UNITY_EDITOR
            var dataCommonType = typeof(StaticDataWrapper);
            var dataCommonFields = dataCommonType.GetFields()
                .Where(f => !f.FieldType.IsGenericType || f.FieldType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
                .ToArray();
            StringBuilder sb = new StringBuilder(1000);
            sb.Append("{");

            foreach (var dataCommonField in dataCommonFields)
            {
                var filename = $"{dataCommonField.Name}";
                var filepath = $"JsonData/{filename}";
                sb.Append('\"');
                sb.Append(dataCommonField.Name);
                sb.Append('\"');
                sb.Append(':');
                //                Debug.Log(filepath);
                sb.Append(FileRead(filepath));
                sb.Append(',');
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("}");

            Wrapper = JsonConvert.DeserializeObject<StaticDataWrapper>(sb.ToString());
            
            // 에러 정보 딕셔너리 초기화
            InitErrorInfoDict();
            
            // 아이템 이름 정보 딕셔너리 초기화
            InitItemNameInfoDict();
            //로컬라이징 세팅
            LocalizeSetting();

            await UniTask.Yield();
            //#else
            //   var tempWrapper =await FirebaseRD.LoadTableDataFromFirebase();
            //   Wrapper = tempWrapper;
            //#endif
        }
        
        private static void InitErrorInfoDict()
        {
            _lobbyErrorInfoDict = new Dictionary<ErrorCode, LobbyErrorInfo>();
            if (Wrapper?.lobbyErrorInfo != null)
            {
                foreach (var info in Wrapper.lobbyErrorInfo)
                {
                    if (!_lobbyErrorInfoDict.ContainsKey(info.errorCode))
                    {
                        _lobbyErrorInfoDict.Add(info.errorCode, info);
                    }
                }
            }
        }
        
        /// <summary>
        /// 로비 에러 코드로 에러 정보 조회
        /// </summary>
        public static LobbyErrorInfo GetLobbyErrorInfo(ErrorCode errorCode)
        {
            if (_lobbyErrorInfoDict != null && _lobbyErrorInfoDict.TryGetValue(errorCode, out var info))
            {
                return info;
            }
            return null;
        }
        
        /// <summary>
        /// 로비 에러 코드로 한국어 메시지 조회 (없으면 기본 메시지 반환)
        /// </summary>
        public static string GetLobbyErrorMessage(ErrorCode errorCode, string defaultMessage = null)
        {
            var info = GetLobbyErrorInfo(errorCode);
            if (info != null && !string.IsNullOrEmpty(info.message_Kr))
            {
                return info.message_Kr;
            }
            return defaultMessage ?? StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.UnknownError].StringToLocal;
        }
        
        private static void InitItemNameInfoDict()
        {
            _itemNameInfoDict = new Dictionary<ItemID, ItemNameInfo>();
            if (Wrapper?.itemNameInfo != null)
            {
                foreach (var info in Wrapper.itemNameInfo)
                {
                    if (!_itemNameInfoDict.ContainsKey(info.itemID))
                    {
                        _itemNameInfoDict.Add(info.itemID, info);
                    }
                }
            }
        }

        private static void LocalizeSetting()
        {
            Wrapper.localizeddescDict = new Dictionary<LocalizeDescKeys, Localizeddesc>();
            for (int i = 0; i < Wrapper.localizeddesc.Length; i++)
            {
                Wrapper.localizeddescDict.Add(Wrapper.localizeddesc[i].key, Wrapper.localizeddesc[i]);
            }
            
            Wrapper.localizednameDict = new Dictionary<LocalizeNameKeys, Localizedname>();
            for (int i = 0; i < Wrapper.localizedname.Length; i++)
            {
                Wrapper.localizednameDict.Add(Wrapper.localizedname[i].key, Wrapper.localizedname[i]);
            }
        }
        
        /// <summary>
        /// 아이템 ID로 아이템 이름 정보 조회
        /// </summary>
        public static ItemNameInfo GetItemNameInfo(ItemID itemId)
        {
            if (_itemNameInfoDict != null && _itemNameInfoDict.TryGetValue(itemId, out var info))
            {
                return info;
            }
            return null;
        }
        
        /// <summary>
        /// 아이템 ID 문자열로 아이템 이름 조회 (한국어)
        /// </summary>
        public static string GetItemName(string itemIdString, string defaultName = "")
        {
            if (string.IsNullOrEmpty(itemIdString))
                return defaultName;
            
            if (System.Enum.TryParse<ItemID>(itemIdString, out var itemId))
            {
                var info = GetItemNameInfo(itemId);
                if (info != null && !string.IsNullOrEmpty(info.message_Kr))
                {
                    return info.message_Kr;
                }
            }
            return defaultName;
        }

        static string FileRead(string path)
        {
            TextAsset jsonString = Resources.Load<TextAsset>(path.ToString());

            return jsonString.text;
        }
    }

}