using System;
using System.Collections.Generic;
using CAPYBARA;
using CAPYBARA.Core;
using UnityEngine;

public class HoldemPocketRank : MonoBehaviour
{
    // 포켓 등급 데이터
    private static readonly Dictionary<string, float> pocketRankings = new Dictionary<string, float>
    {

        // ★★★★★ (5)
        { "AA", 5f },
        { "KK", 5f },
        { "QQ", 5f },
        { "AKs", 5f },
        { "AKo", 5f },

        // ★★★★ (4)
        { "JJ", 4f },
        { "TT", 4f },
        { "AQs", 4f },
        { "99", 4f },
        { "AJs", 4f },
        { "KQs", 4f },
        { "88", 4f },
        { "ATs", 4f },
        { "AQo", 4f },
        { "KJs", 4f },
        { "KTs", 4f },
        { "QJs", 4f },
        { "AJo", 4f },

        // ★★★ (3)
        { "KQo", 3f },
        { "QTs", 3f },
        { "A9s", 3f },
        { "77", 3f },
        { "ATo", 3f },
        { "JTs", 3f },
        { "A5s", 3f },
        { "KJo", 3f },
        { "A8s", 3f },
        { "K9s", 3f },
        { "QJo", 3f },
        { "A7s", 3f },
        { "KTo", 3f },
        { "Q9s", 3f },
        { "66", 3f },
        { "A6s", 3f },
        { "QTo", 3f },
        { "J9s", 3f },
        { "A9o", 3f },
        { "T9s", 3f },
        { "A4s", 3f },
        { "K8s", 3f },
        { "JTo", 3f },

        // ★★ (2)
        { "K7s", 2f },
        { "A8o", 2f },
        { "A3s", 2f },
        { "Q8s", 2f },
        { "K9o", 2f },
        { "A2s", 2f },
        { "K6s", 2f },
        { "J8s", 2f },
        { "T8s", 2f },
        { "A7o", 2f },
        { "55", 2f },
        { "Q9o", 2f },
        { "98s", 2f },
        { "K5s", 2f },
        { "Q7s", 2f },
        { "J9o", 2f },
        { "A5o", 2f },
        { "T9o", 2f },
        { "A6o", 2f },
        { "K4s", 2f },
        { "K8o", 2f },
        { "Q6s", 2f },
        { "J7s", 2f },
        { "T7s", 2f },
        { "A4o", 2f },
        { "97s", 2f },
        { "K3s", 2f },
        { "87s", 2f },
        { "Q5s", 2f },
        { "K7o", 2f },
        { "44", 2f },
        { "Q8o", 2f },
        { "A3o", 2f },

        // ★ (1)
        { "K2s", 1f },
        { "J8o", 1f },
        { "Q4s", 1f },
        { "T8o", 1f },
        { "J6s", 1f },
        { "K6o", 1f },
        { "A2o", 1f },
        { "T6s", 1f },
        { "98o", 1f },
        { "76s", 1f },
        { "86s", 1f },
        { "96s", 1f },
        { "Q3s", 1f },
        { "J5s", 1f },
        { "K5o", 1f },
        { "Q7o", 1f },
        { "Q2s", 1f },
        { "J4s", 1f },
        { "33", 1f },
        { "65s", 1f },
        { "J7o", 1f },
        { "T7o", 1f },
        { "K4o", 1f },
        { "75s", 1f },
        { "T5s", 1f },
        { "Q6o", 1f },
        { "J3s", 1f },
        { "95s", 1f },
        { "87o", 1f },
        { "85s", 1f },
        { "97o", 1f },
        { "T4s", 1f },
        { "K3o", 1f },
        { "J2s", 1f },
        { "54s", 1f },
        { "Q5o", 1f },
        { "64s", 1f },
        { "T3s", 1f },
        { "22", 1f },

        { StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Other].StringToLocal, 0f }


    };

    /// <summary>
    /// 두 카드로 포켓 등급을 가져옵니다.
    /// </summary>
    /// <param name="card1">첫 번째 카드 (0~51)</param>
    /// <param name="card2">두 번째 카드 (0~51)</param>
    /// <returns>포켓 등급 (1~5)</returns>
    public float GetPocketRank(int card1, int card2)
    {
        // 두 카드의 정수 랭크 (2~14)를 계산 (0~12 + 2)
        int rankValue1 = (card1 % 13) + 2;
        int rankValue2 = (card2 % 13) + 2;

        // 카드 랭크 문자열을 가져옴 (예: "A", "K", "T", "J", "Q", "9", …)
        string cardRank1 = GetCardRank(card1);
        string cardRank2 = GetCardRank(card2);

        string key;

        // 만약 두 카드가 같은 랭크라면 (예: AA, KK 등) 
        // 키를 두 카드 랭크의 결합으로 만들고, 무늬 표시(s 또는 o)는 붙이지 않습니다.
        if (cardRank1 == cardRank2)
        {
            key = cardRank1 + cardRank2;
        }
        else
        {
            // 높은 랭크가 앞에 오도록 문자열 결합
            key = (rankValue1 > rankValue2) ? cardRank1 + cardRank2 : cardRank2 + cardRank1;

            // 두 카드의 무늬가 같으면 "s", 다르면 "o"를 붙입니다.
            key += AreSameSuit(card1, card2) ? "s" : "o";
        }

//        Debug.Log("등급: " + key);
        return pocketRankings.ContainsKey(key) ? pocketRankings[key] : pocketRankings[StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Other].StringToLocal];
    }
    /// <summary>
    /// 카드의 랭크를 반환합니다. (2, 3, …, 9, T, J, Q, K, A)
    /// </summary>
    private string GetCardRank(int card)
    {
        int rank = card % 13;
        return rank switch
        {
            0 => "2",
            1 => "3",
            2 => "4",
            3 => "5",
            4 => "6",
            5 => "7",
            6 => "8",
            7 => "9",
            8 => "T",
            9 => "J",
            10 => "Q",
            11 => "K",
            12 => "A",
            _ => ""
        };
    }

    /// <summary>
    /// 같은 무늬인지 확인합니다.
    /// </summary>
    private bool AreSameSuit(int card1, int card2)
    {
        return (card1 / 13) == (card2 / 13); // 같은 무늬 여부 확인
    }
    
    private static readonly Dictionary<string, int> pocketTiers = new Dictionary<string, int>
    {
        // Tier 1
        { "AA", 1 },

        // Tier 2
        { "KK", 2 },
        { "QQ", 2 },
        { "AKs", 2 },

        // Tier 3
        { "AKo", 3 },
        { "JJ", 3 },
        { "TT", 3 },
        { "AQs", 3 },

        // Tier 4
        { "99", 4 },
        { "AJs", 4 },
        { "KQs", 4 },
        { "88", 4 },
        { "ATs", 4 },
        { "AQo", 4 },
        { "KJs", 4 },

        // Tier 5
        { "KTs", 5 },
        { "QJs", 5 },
        { "AJo", 5 },
        { "KQo", 5 },
        { "QTs", 5 },
        { "A9s", 5 },
        { "77", 5 },
        { "ATo", 5 },
        { "JTs", 5 },
        { "A5s", 5 },
        { "KJo", 5 },
    };

    /// <summary>
    /// 두 카드의 티어를 반환합니다.
    /// (표에 없는 키는 -1 반환)
    /// </summary>
    public int GetTier(int card1, int card2)
    {
        // 카드 랭크(2~14) 계산
        int rankValue1 = (card1 % 13) + 2;
        int rankValue2 = (card2 % 13) + 2;

        // 예: "A", "K", "Q", "J", "T", "9", …
        string cardRank1 = GetCardRank(card1);
        string cardRank2 = GetCardRank(card2);

        // 키 생성
        string key;
        if (cardRank1 == cardRank2)
        {
            // 예: AA, KK, QQ 등 (동랭크면 s/o 없이)
            key = cardRank1 + cardRank2;
        }
        else
        {
            // 높은 랭크가 앞에 오도록
            key = (rankValue1 > rankValue2) ? cardRank1 + cardRank2 : cardRank2 + cardRank1;
            // 무늬가 같으면 s, 다르면 o
            key += AreSameSuit(card1, card2) ? "s" : "o";
        }

//        Debug.Log("등급 키: " + key);

        // Dictionary에 있으면 해당 티어, 없으면 -1
        return pocketTiers.ContainsKey(key) ? pocketTiers[key] : -1;
    }

}
