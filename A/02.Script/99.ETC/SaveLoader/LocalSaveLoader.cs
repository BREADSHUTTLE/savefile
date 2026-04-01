using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using CAPYBARA.Model;
using UnityEngine;

namespace CAPYBARA.Core
{
    public static class LocalSaveLoader
    {
        private static readonly string privateKey = "1718hy9dsf0jsdlfjds0pa9ids78ahgf81h32re";
        private const string EncryptedPrefix = "ENC:";

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false); // BOM 없음
        private static readonly byte[] Key16;

        static LocalSaveLoader()
        {
            // 키 16바이트 고정(기존 동작과 동일)
            var src = Utf8.GetBytes(privateKey);
            Key16 = new byte[16];
            Buffer.BlockCopy(src, 0, Key16, 0, Math.Min(16, src.Length));
        }

        public static bool ExistsUserDataKey() => PlayerPrefs.HasKey("userclouddata");
        public static bool ExistsLoginKey() => PlayerPrefs.HasKey("loginsavedata");
        public static bool ExistsLoginAutoToken() => PlayerPrefs.HasKey("loginToken");

        public static void SaveUserCloudData()
        {
            CPPlayer.Cloud.optionValue.lastSaveTime = System.DateTime.Now.Ticks.ToString();
            CPPlayer.Cloud.optionValue.lastSaveDay = TodayYmdKst();
            Save<Model.UserCloudData>("userclouddata", CPPlayer.Cloud);
        }

        public static Model.UserCloudData LoadUserCloudData()
        {
            var userclouddata = Load<Model.UserCloudData>("userclouddata");
            return userclouddata;
        }

        public static void SaveIPPortData()
        {
            Save<Model.IPPortData>("ipportdata", CPPlayer.IpPortData);
        }

        public static Model.IPPortData LoadIPPortData()
        {
            var ipPort = Load<Model.IPPortData>("ipportdata");
            return ipPort;
        }

        public static int TodayYmdKst()
        {
            var nowKst = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9));
            return nowKst.Year * 10000 + nowKst.Month * 100 + nowKst.Day;
        }

        public static bool IsNewDay(int lastYmd)
        {
            return TodayYmdKst() != lastYmd;
        }

        public static void SaveLoginCloudData()
        {
            Save<LoginCloudData>("loginsavedata", LoginData.Cloud);
        }

        public static LoginCloudData LoadLoginCloudData()
        {
            var loginCloudData = Load<LoginCloudData>("loginsavedata");
            return loginCloudData;
        }

        public static void DeleteUserAutoToken()
        {
            PlayerPrefs.DeleteKey("loginToken");
        }
        public static void SaveLoginToken()
        {
            PlayerPrefs.SetString("loginToken",LoginData.Cloud.loginValue.userAutoToken);
        }
        
        public static string LoadLoginToken()
        {
            string token=PlayerPrefs.GetString("loginToken","");
            return token;
        }
        
        public static bool ExistsUserIdListaKey() => PlayerPrefs.HasKey("autologinIdList");
        public static void DeleteUserListData()
        {
            PlayerPrefs.DeleteKey("autologinIdList");
        }
        public static void SaveAutoLoginUserIdList()
        {
            var data = LoginData.Cloud.loginValue.uidList;
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("autologinIdList",json);
            PlayerPrefs.Save();
        }
        
        public static LoginCloudData.UidListWrapper LoadAutoLoginUserIdList()
        {
            var jsonstr=PlayerPrefs.GetString("autologinIdList","");
            if (string.IsNullOrEmpty(jsonstr))
                return new LoginCloudData.UidListWrapper(); // list 빈 상태
            
            var obj = JsonUtility.FromJson<LoginCloudData.UidListWrapper>(jsonstr);
            if (obj == null) obj = new LoginCloudData.UidListWrapper();
            if (obj.list == null) obj.list = new List<LoginCloudData.UserSavedInfo>();
            return obj;
        }
        
        public static void DeleteCloudData()
        {
            PlayerPrefs.DeleteKey("userclouddata");
            DeleteUserAutoToken();
        }

        public static T Load<T>(string key) where T : new()
        {
            var raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(raw))
                return new T();

            // 1) 접두사로 암호문 여부 판단
            if (raw.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
            {
                var cipherB64 = raw.Substring(EncryptedPrefix.Length);
                try
                {
                    var json = DecryptToString(cipherB64);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var obj = JsonUtility.FromJson<T>(json);
                        if (obj != null) return obj;
                    }
                }
                catch
                {
                    // 복호 실패 -> 기본값
                }

                return new T();
            }
            else
            {
                // 과거 데이터(평문/혹은 과거 암호 포맷) 호환
                var obj = JsonUtility.FromJson<T>(raw);
                if (obj != null) return obj;

                // 혹시 과거에 접두사 없이 Base64-암호문을 썼다면 마지막 시도로 복호화 시도
                try
                {
                    var json = DecryptToString(raw);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var o2 = JsonUtility.FromJson<T>(json);
                        if (o2 != null) return o2;
                    }
                }
                catch
                {
                    // 무시
                }

                return new T();
            }
        }

        public static void Save<T>(string key, T data)
        {
            var json = JsonUtility.ToJson(data);
            var enc = EncryptFromString(json);
            PlayerPrefs.SetString(key, EncryptedPrefix + enc);
            PlayerPrefs.Save();
        }

        // ====== 내부: 암복호 ======

        private static string EncryptFromString(string plain)
        {
            var plainBytes = Utf8.GetBytes(plain);

            using (var aes = new RijndaelManaged
                   {
                       Key = Key16,
                       Mode = CipherMode.ECB, // 기존과 동일
                       Padding = PaddingMode.ISO10126 // 기존과 동일
                   })
            using (var encryptor = aes.CreateEncryptor())
            {
                var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(cipherBytes);
            }
        }

        private static string DecryptToString(string base64)
        {
            var cipherBytes = Convert.FromBase64String(base64);

            using (var aes = new RijndaelManaged
                   {
                       Key = Key16,
                       Mode = CipherMode.ECB,
                       Padding = PaddingMode.ISO10126
                   })
            using (var decryptor = aes.CreateDecryptor())
            {
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Utf8.GetString(plainBytes);
            }
        }
    }

}