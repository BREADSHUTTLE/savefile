using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CAPYBARA;
using CAPYBARA.Core;

public static class HandEvaluator
{
    /// <summary>
    /// Badugi 핸드를 평가합니다.
    /// 입력된 카드가 4장이 아니면 빈 문자열을 반환합니다.
    /// 내부적으로 Ace는 홀덤 방식으로 14로 처리되지만, Badugi 평가 시에는 Ace를 1로 간주합니다.
    /// </summary>
    public static string EvaluateHand(List<int> cards)
    {
        if (cards == null || cards.Count != 4)
        {
            return "";
        }
       
        // 최적의 Badugi 핸드를 구합니다.
        List<int> validBadugi = GetOptimalBadugiHand(cards);

        // 유효 카드들의 Badugi 랭크(즉, Ace는 1로 변환)로 오름차순 정렬
        validBadugi.Sort((a, b) => GetBadugiRank(a).CompareTo(GetBadugiRank(b)));
        List<int> finalRanks = validBadugi.ConvertAll(card => GetBadugiRank(card));

        string FormatRanks(List<int> ranks)
        {
            return string.Join(",", ranks.Select(r => RankToStringBadugi(r)));
        }

        int cardCount = finalRanks.Count;
        if (cardCount == 4)
        {
            // 4장인 경우 특별 조합 확인 (예시: 1,2,3,4)
            if (finalRanks[0] == 1 && finalRanks[1] == 2 && finalRanks[2] == 3 && finalRanks[3] == 4)
            {
                return $"[{FormatRanks(finalRanks)}] {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Golf].StringToLocal}";
            }
            else if (finalRanks[0] == 1 && finalRanks[1] == 2 && finalRanks[2] == 3 && finalRanks[3] == 5)
            {
                return $"[{FormatRanks(finalRanks)}] {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Second].StringToLocal}";
            }
            else if (finalRanks[0] == 1 && finalRanks[1] == 2 && finalRanks[2] == 4 && finalRanks[3] == 5)
            {
                return $"[{FormatRanks(finalRanks)}] {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Third].StringToLocal}";
            }
            else
            {
                return $"[{FormatRanks(finalRanks)}] {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MaidTop].StringToLocal} {RankToStringBadugi(finalRanks[3])}탑";
            }
        }
        else if (cardCount == 3)
        {
            return $"[{FormatRanks(finalRanks)}] {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Base].StringToLocal}";
        }
        else if (cardCount == 2)
        {
            return $"[{FormatRanks(finalRanks)}] {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.TwoBase].StringToLocal}";
        }
        else if (cardCount == 1)
        {
            return $"[{FormatRanks(finalRanks)}] {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.NoPattern].StringToLocal}";
        }

        return "Error: Hand Not Recognized";
    }

    /// <summary>
    /// 내부적으로 Ace는 14로 처리되지만, Badugi 평가에서는 Ace를 1로 간주하도록 변환합니다.
    /// </summary>
    private static int GetBadugiRank(int card)
    {
        int rank = (card % 13) + 2; // 홀덤 방식: 2 ~ 14 (Ace=14)
        return rank == 14 ? 1 : rank; // Badugi에서는 Ace(14)를 1로 변환
    }

    /// <summary>
    /// Badugi 평가용 문자열 변환 (1은 "A", 그 외는 그대로 문자열 변환)
    /// </summary>
private static string RankToStringBadugi(int rank)
{
    // rank는 Badugi 평가 시 이미 Ace가 1로 변환되어 있음.
    return rank switch
    {
        1 => "A",      // Ace
        11 => "J",     // Jack
        12 => "Q",     // Queen
        13 => "K",     // King
        _ => rank.ToString()
    };
}

    // 최적의 Badugi 핸드를 구하는 함수 (exhaustive search)
    private static List<int> GetOptimalBadugiHand(List<int> cards)
    {
        List<int> bestSet = new List<int>();
        int n = cards.Count;
        for (int mask = 0; mask < (1 << n); mask++)
        {
            List<int> subset = new List<int>();
            HashSet<int> usedSuits = new HashSet<int>();
            HashSet<int> usedBadugiRanks = new HashSet<int>();
            bool valid = true;
            for (int i = 0; i < n; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    int card = cards[i];
                    int suit = card / 13;
                    int badugiRank = GetBadugiRank(card);
                    if (usedSuits.Contains(suit) || usedBadugiRanks.Contains(badugiRank))
                    {
                        valid = false;
                        break;
                    }
                    usedSuits.Add(suit);
                    usedBadugiRanks.Add(badugiRank);
                    subset.Add(card);
                }
            }
            if (!valid)
                continue;

            subset.Sort((a, b) => GetBadugiRank(a).CompareTo(GetBadugiRank(b)));
            if (subset.Count > bestSet.Count)
            {
                bestSet = new List<int>(subset);
            }
            else if (subset.Count == bestSet.Count)
            {
                bool isBetter = false;
                for (int i = 0; i < subset.Count; i++)
                {
                    if (GetBadugiRank(subset[i]) < GetBadugiRank(bestSet[i]))
                    {
                        isBetter = true;
                        break;
                    }
                    else if (GetBadugiRank(subset[i]) > GetBadugiRank(bestSet[i]))
                    {
                        break;
                    }
                }
                if (isBetter)
                {
                    bestSet = new List<int>(subset);
                }
            }
        }
        return bestSet;
    }
}
