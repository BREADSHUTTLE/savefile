using System;
using System.Collections.Generic;
using System.Text;
using CAPYBARA.Core;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace CAPYBARA
{
    public static class Extension
    {
        public static T StringToEnum<T>(this string value) where T : struct
        {
            if (Enum.TryParse<T>(value, true, out var gameType)) // ignoreCase: true
            {
                return gameType;
            }
            else
            {
                Debug.LogWarning($"Enum 변환 실패: {value} → {typeof(T).Name}");
                return default(T);
            }
        }

        public static T OrEmpty<T>(this T obj) where T : class, new()
        {
            return obj ?? new T();
        }

        public static string ToRemainingTimeString(string limitedAt, bool emptyText = false)
        {
            if (!DateTime.TryParse(limitedAt, out DateTime endTime))
            {
                if (emptyText)
                    return "";
                else
                    return StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InvalidTime].StringToLocal;
            }

            TimeSpan diff = endTime - DateTime.Now;
            if (diff.TotalSeconds <= 0)
                return StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PeriodExpired].StringToLocal;

            int totalMinutes = (int)Math.Round(diff.TotalMinutes, MidpointRounding.AwayFromZero);
            if (totalMinutes == 0) totalMinutes = 1;

            int hours = totalMinutes / 60;
            int days = hours / 24;

            if (days >= 1)
                return $"남은기간 {days}일";
            else if (hours >= 1)
                return $"남은기간 {hours}시간";
            else
                return $"남은기간 {totalMinutes}분";
        }
        
        public static string ToRemainingTimeStringFull(int startTime, int endTime, bool emptyText = false)
        {
            if (startTime == 0 || endTime == 0)
            {
                if (emptyText)
                    return "";
                else
                    return StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InvalidTime].StringToLocal;
            }

            DateTime start = DateTimeOffset.FromUnixTimeSeconds(startTime).LocalDateTime;
            DateTime end = DateTimeOffset.FromUnixTimeSeconds(endTime).LocalDateTime;
            
            if (end <= start)
                return StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PeriodExpired].StringToLocal;

            int years = end.Year - start.Year;
            int months = end.Month - start.Month;
            int days = end.Day - start.Day;
            int hours = end.Hour - start.Hour;
            int minutes = end.Minute - start.Minute;

            if (minutes < 0)
            {
                minutes += 60;
                hours--;
            }
            if (hours < 0)
            {
                hours += 24;
                days--;
            }
            if (days < 0)
            {
                days += DateTime.DaysInMonth(start.Year, start.Month);
                months--;
            }
            if (months < 0)
            {
                months += 12;
                years--;
            }

            var sb = new StringBuilder();
            if (years > 0)
                sb.Append($"{years}년 ");
            if (months > 0)
                sb.Append($"{months}월 ");
            if (days > 0)
                sb.Append($"{days}일 ");
            if (hours > 0)
                sb.Append($"{hours}시간 ");
            if (minutes > 0)
                sb.Append($"{minutes}분");

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.LessThanOneMinute].StringToLocal : result;
        }
        
        public static string ToEndDateTimeString(int timestamp)
        {
            if (timestamp == 0)
                return "";
            
            DateTime endTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
            return $"{endTime.Month}월{endTime.Day}일 {endTime.Hour:D2}:{endTime.Minute:D2}분까지";
        }

        public static string ToRemainingTimeString(int limitedAt, bool emptyText = false)
        {
            if (limitedAt == 0)
            {
                if (emptyText)
                    return "";
                else
                    return StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InvalidTime].StringToLocal;
            }

            DateTime endTime = DateTimeOffset.FromUnixTimeSeconds(limitedAt).LocalDateTime;
            TimeSpan diff = endTime - DateTime.Now;
            if (diff.TotalSeconds <= 0)
                return StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PeriodExpired].StringToLocal;

            int totalMinutes = (int)Math.Round(diff.TotalMinutes, MidpointRounding.AwayFromZero);
            if (totalMinutes == 0) totalMinutes = 1;

            int hours = totalMinutes / 60;
            int days = hours / 24;

            if (days >= 1)
                return $"남은기간 {days}일";
            else if (hours >= 1)
                return $"남은기간 {hours}시간";
            else
                return $"남은기간 {totalMinutes}분";
        }
        
        public static string FormatGold(double value)
        {
            long v = (long)Math.Floor(value);

            const long JO = 1_000_000_000_000;
            const long EOK = 100_000_000;
            const long MAN = 10_000;

            long jo = v / JO;
            v %= JO;

            long eok = v / EOK;
            v %= EOK;

            long man = v / MAN;
            long one = v % MAN;

            // 조 단위
            if (jo > 0)
            {
                if (eok > 0)
                    return $"{jo}조 {eok}억";

                if (man > 0)
                    return $"{jo}조 {man}만";

                return one > 0 ? $"{jo}조{one}" : $"{jo}조";
            }

            // 억 단위
            if (eok > 0)
            {
                if (man > 0)
                    return $"{eok}억 {man}만";

                return one > 0 ? $"{eok}억 {one}" : $"{eok}억";
            }

            // 만 단위
            if (man > 0)
            {
                return one > 0 ? $"{man}만 {one}" : $"{man}만";
            }

            // 1 단위
            return one.ToString();
        }


        public enum KoreanFormatMode
        {
            Original,
            Planning
        }

        public static string ToKoreanFormat(long value, KoreanFormatMode mode = KoreanFormatMode.Original)
        {
            long v = value < 0 ? -value : value;

            // 10만 미만은 그냥 3자리 콤마
            // if (v < 100_000)
            // 1만 미만은 그냥 3자리 콤마
            if (v < 10_000) // 기획 변경 사항
                return ((long)value).ToString("N0");

            switch (mode)
            {
                case KoreanFormatMode.Planning:
                    return ToKoreanFormatPlanning(value, v);
                default:
                    return ToKoreanFormatOriginal(value, v);
            }
        }

        private static string ToKoreanFormatOriginal(long value, long v)
        {
            string[] units = { "", "만", "억", "조", "경", "해" };
            long[] parts = new long[units.Length];
 
            long temp = v;
            int idx = 0;
            while (temp > 0 && idx < units.Length)
            {
                parts[idx++] = temp % 10_000;
                temp /= 10_000;
            }

            int hi = idx - 1;

            var sb = new System.Text.StringBuilder();
            for (int i = hi; i >= 1; i--)
            {
                if (parts[i] > 0)
                    sb.Append(parts[i]).Append(units[i]).Append(' ');
            }

            if (parts[0] > 0)
                sb.Append(parts[0]);
            else if (sb.Length > 0)
                sb.Length--; // 마지막 공백 제거

            if (value < 0)
                sb.Insert(0, "-");

            return sb.ToString();
        }

        private static string ToKoreanFormatPlanning(long value, long v)
        {
            string[] units = { "", "만", "억", "조", "경", "해" };
            long[] parts = new long[units.Length];

            long temp = v;
            for (int i = 0; i < parts.Length && temp > 0; i++)
            {
                parts[i] = temp % 10_000;
                temp /= 10_000;
            }

            var sb = new System.Text.StringBuilder();

            void AppendPart(string text)
            {
                if (sb.Length > 0)
                    sb.Append(" ");

                sb.Append(text);
            }

            // 조/경/해는 존재하는 것만 그대로 표시
            for (int i = parts.Length - 1; i >= 3; i--)
            {
                if (parts[i] > 0)
                    AppendPart(parts[i] + units[i]);
            }

            long eok = parts[2];
            long man = parts[1];
            long one = parts[0];

            if (eok > 0)
            {
                AppendPart(eok + "억");
                if (man > 0)
                    AppendPart(man + "만"); // 억, 만 단위가 같이 있으면 1 단위는 표시하지 않음
                else if (one > 0)
                    AppendPart(one.ToString()); // 억 단위가 있고 만 단위가 없으면 1 단위 표시
            }
            else
            {
                if (man > 0)
                {
                    AppendPart(man + "만");
                    if (one > 0)
                        AppendPart(one.ToString());
                }
                else if (one > 0)
                {
                    AppendPart(one.ToString());
                }
            }

            if (value < 0)
                sb.Insert(0, "-");

            return sb.ToString();
        }

        public static string ToKoreanFormatReward(long value)
        {
            long v = value < 0 ? -value : value;

            // 1만 미만은 그냥 3자리 콤마
            if (v < 10_000) return ((long)value).ToString("N0");

            string[] units = { "", "만", "억", "조", "경", "해" };
            long[] parts = new long[units.Length];

            int idx = 0;
            while (v > 0 && idx < units.Length)
            {
                parts[idx++] = v % 10_000;
                v /= 10_000;
            }

            int hi = idx - 1;

            var sb = new System.Text.StringBuilder();
            for (int i = hi; i >= 1; i--)
            {
                if (parts[i] > 0)
                    sb.Append(parts[i]).Append(units[i]);
            }

            if (parts[0] > 0)
                sb.Append(parts[0]);

            if (value < 0) sb.Insert(0, "-");
            return sb.ToString();
        }


        public static string ToJson<T>(T list)
        {
            return JsonConvert.SerializeObject(list);
        }

        // JSON 문자열 → List<int[]>
        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static byte[] ToBytes<T>(T obj)
        {
            string json = JsonUtility.ToJson(obj);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public static T FromBytes<T>(byte[] data)
        {
            string json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<T>(json);
        }

        // string[] -> byte[]
        public static byte[] StringArrayToBytes(string[] arr)
        {
            string json = JsonHelper.ToJson(arr); // 커스텀 헬퍼 필요 (아래 있음)
            return Encoding.UTF8.GetBytes(json);
        }

        // byte[] -> string[]
        public static string[] BytesToStringArray(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonHelper.FromJson<string>(json);
        }

        public static byte[] IntArrayToBytes(int[] arr)
        {
            string json = JsonHelper.ToJson(arr);
            return Encoding.UTF8.GetBytes(json);
        }

        // byte[] -> int[]
        public static int[] BytesToIntArray(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonHelper.FromJson<int>(json);
        }

        public static Color HexToColor(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return color;
        }





        public static void eLog(string message, Color? color = null)
        {
#if UNITY_EDITOR
            LogWithColor(message, color);
#endif
        }

        public static void eLogBuild(string message, Color? color = null)
        {
            LogWithColor(message, color);
        }

        private static void LogWithColor(string message, Color? color)
        {
            Color c = color ?? UnityEngine.Color.white;
            string hexColor = ColorUtility.ToHtmlStringRGB(c);

            string timeStamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            string frameInfo = $"Frame: {Time.frameCount}";

            UnityEngine.Debug.Log(
                $"<color=#{hexColor}>[{timeStamp}] [{frameInfo}] {message}</color>"
            );
        }

        public static (QuestRewardType type, object value) ParseQuestReward(string jsonValue)
        {
            if (string.IsNullOrEmpty(jsonValue))
                return (QuestRewardType.None, null);

            try
            {
                var rewardDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonValue);
                if (rewardDict == null || rewardDict.Count == 0)
                    return (QuestRewardType.None, null);

                foreach (var kvp in rewardDict)
                {
                    if (Enum.TryParse<QuestRewardType>(kvp.Key, out var rewardType))
                    {
                        return rewardType switch
                        {
                            QuestRewardType.GAME_MONEY => (rewardType, (object)Convert.ToInt64(kvp.Value)),
                            QuestRewardType.ITEM_ID => (rewardType, (object)kvp.Value.ToString()),
                            _ => (rewardType, (object)kvp.Value.ToString())
                        };
                    }
                    else
                    {
                        return (QuestRewardType.None, (object)kvp.Value.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse reward value: {jsonValue}, Error: {e.Message}");
            }

            return (QuestRewardType.None, null);
        }
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array, bool prettyPrint = false)
        {
            Wrapper<T> wrapper = new Wrapper<T> { Items = array };
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }
    
    
}
