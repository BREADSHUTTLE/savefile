using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CAPYBARA
{
    public static class BadWordFilter
    {
        private static HashSet<string> _badWords;
        private static bool _isInitialized = false;
        private static readonly string Chosungs = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";

        [Serializable]
        private class BadWordData
        {
            public string[] badwords;
        }

        public static void Initialize()
        {
            if (_isInitialized) return;

            _badWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var json = Resources.Load<TextAsset>("badwords");
                if (json != null)
                {
                    var data = JsonUtility.FromJson<BadWordData>(json.text);
                    if (data?.badwords != null)
                    {
                        foreach (var word in data.badwords)
                        {
                            if (!string.IsNullOrEmpty(word))
                                _badWords.Add(word.ToLower());
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[BadWordFilter] badwords.json을 찾을 수 없습니다.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BadWordFilter] 초기화 실패: {e.Message}");
            }

            _isInitialized = true;
        }

        public static bool ContainsBadWord(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            if (!_isInitialized)
                Initialize();

            var normalized = RemoveSpecialChars(input).ToLower();
            if (_badWords.Contains(normalized))
                return true;

            foreach (var badWord in _badWords)
            {
                if (normalized.Contains(badWord))
                    return true;
            }

            var chosung = ExtractChosung(normalized);
            foreach (var badWord in _badWords)
            {
                if (chosung.Contains(badWord))
                    return true;
            }

            return false;
        }

        public static string Filter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            if (!_isInitialized)
                Initialize();

            var result = input;
            var lowerInput = input.ToLower();

            foreach (var badWord in _badWords)
            {
                if (lowerInput.Contains(badWord))
                {
                    var mask = new string('*', badWord.Length);
                    result = ReplaceIgnoreCase(result, badWord, mask);
                    lowerInput = result.ToLower();
                }
            }

            return result;
        }

        private static string RemoveSpecialChars(string input)
        {
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if ((c >= '가' && c <= '힣') || (c >= 'ㄱ' && c <= 'ㅎ') || (c >= 'ㅏ' && c <= 'ㅣ') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||(c >= '0' && c <= '9'))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static string ExtractChosung(string input)
        {
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (c >= '가' && c <= '힣')
                {
                    int index = (c - '가') / 588;
                    sb.Append(Chosungs[index]);
                }
                else if (Chosungs.Contains(c.ToString()))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string ReplaceIgnoreCase(string source, string oldValue, string newValue)
        {
            var sb = new StringBuilder();
            int index = 0;
            int lastIndex = 0;

            while ((index = source.IndexOf(oldValue, lastIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                sb.Append(source.Substring(lastIndex, index - lastIndex));
                sb.Append(newValue);
                lastIndex = index + oldValue.Length;
            }

            sb.Append(source.Substring(lastIndex));
            return sb.ToString();
        }

        public static void Reload()
        {
            _isInitialized = false;
            _badWords?.Clear();
            Initialize();
        }
    }
}
