using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Core;
using UnityEngine;

namespace CAPYBARA
{
    public class CardRankCalculater
    {
        // Rank 매핑
        private static readonly Dictionary<string, Rank> rankMap = new Dictionary<string, Rank>
        {
            { "2", Rank.Two }, { "3", Rank.Three }, { "4", Rank.Four }, { "5", Rank.Five }, { "6", Rank.Six },
            { "7", Rank.Seven }, { "8", Rank.Eight }, { "9", Rank.Nine }, { "T", Rank.Ten },
            { "J", Rank.Jack }, { "Q", Rank.Queen }, { "K", Rank.King }, { "A", Rank.Ace }
        };

        // Suit 매핑
        private static readonly Dictionary<string, Suit> suitMap = new Dictionary<string, Suit>
        {
            { "♣", Suit.Clubs }, { "♦", Suit.Diamonds },{ "♥", Suit.Hearts },  { "♠", Suit.Spades }
        };

        public static (int rankValue, Suit suit) ParseCard(string input)
        {
            // Extension.eLog($"input string: {input}",Color.yellow);

            // 문자열에서 무늬 기호 추출
            foreach (var kv in suitMap)
            {
                if (input.Contains(kv.Key))
                {
                    string rankStr = input.Replace(kv.Key, ""); // 기호 제거 -> 숫자/문자만 남음
                    if (!rankMap.TryGetValue(rankStr, out var rank))
                        throw new ArgumentException($"Invalid rank: {rankStr}");

                    int index = (int)rank;
                    return (index, kv.Value);
                }
            }

            throw new ArgumentException($"Invalid card input: {input}");
        }

        public static int GetCardIndex(string card)
        {
            if (string.IsNullOrEmpty(card) || card.Length < 2)
            {
                Debug.LogError($"::{card}::잘못된 카드 문자열입니다.");
            }


            char suit = card[1];

            string rankStr = card.Substring(0, 1);

            int rankValue;
            switch (rankStr)
            {
                case "T": rankValue = 8; break;
                case "J": rankValue = 9; break;
                case "Q": rankValue = 10; break;
                case "K": rankValue = 11; break;
                case "A": rankValue = 12; break;
                default:
                    if (!int.TryParse(rankStr, out rankValue))
                    {
                        Debug.LogError($"잘못된 카드 숫자입니다: {rankStr}");
                        throw new ArgumentException($"잘못된 카드 숫자입니다: {rankStr}");
                    }

                    rankValue = rankValue - 2;
                    break;
            }

            int suitOffset = 0;
            switch (suit)
            {
                case '♣': suitOffset = 0; break;
                case '♦': suitOffset = 13; break;
                case '♥': suitOffset = 26; break;
                case '♠': suitOffset = 39; break;
                default:
                    Debug.LogError($"잘못된 문양입니다: {suit}");
                    throw new ArgumentException($"잘못된 문양입니다: {suit}");
            }

            return suitOffset + rankValue;
        }

        public static string GetCardString(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex > 51)
                throw new ArgumentOutOfRangeException(nameof(cardIndex), "카드 인덱스는 0~51 사이여야 합니다.");

            // 1. 문양(suit) 계산
            int suitIndex = cardIndex / 13; // 0~3
            int rankIndex = cardIndex % 13; // 0~12 (2~A)

            string suitSymbol;
            switch (suitIndex)
            {
                case 0: suitSymbol = "♣"; break;
                case 1: suitSymbol = "♦"; break;
                case 2: suitSymbol = "♥"; break;
                case 3: suitSymbol = "♠"; break;
                default: throw new ArgumentException($"잘못된 suitIndex: {suitIndex}");
            }

            //string rankSymbol;

            string rankSymbol;

            switch (rankIndex)
            {
                case 0: rankSymbol = "2"; break;
                case 1: rankSymbol = "3"; break;
                case 2: rankSymbol = "4"; break;
                case 3: rankSymbol = "5"; break;
                case 4: rankSymbol = "6"; break;
                case 5: rankSymbol = "7"; break;
                case 6: rankSymbol = "8"; break;
                case 7: rankSymbol = "9"; break;
                case 8: rankSymbol = "T"; break;
                case 9: rankSymbol = "J"; break;
                case 10: rankSymbol = "Q"; break;
                case 11: rankSymbol = "K"; break;
                case 12: rankSymbol = "A"; break;
                default: throw new ArgumentException($"잘못된 rankIndex: {rankIndex}");
            }

            return $"{rankSymbol}{suitSymbol}";
        }

        private string FormatBestHand(string bestHand)
        {
            if (string.IsNullOrEmpty(bestHand))
                return string.Empty;

            // 1) 탑 패일 경우 [ ] 만 제거
            if (bestHand.Contains(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.High].StringToLocal))
            {
                return bestHand
                    .Replace("[", "")
                    .Replace("]", "");
            }

            // 2) 그 외: 공백 없애고 ']' 뒤에 있는 문자열만
            var noSpaces = bestHand.Replace(" ", "");
            int idx = noSpaces.IndexOf(']');
            if (idx >= 0)
            {
                // ']' 다음 문자부터 끝까지
                return (idx < noSpaces.Length - 1)
                    ? noSpaces.Substring(idx + 1)
                    : string.Empty;
            }

            // ']'가 없으면 공백만 제거한 전체 반환
            return noSpaces;
        }

        private static int GetRank(int card) => card % 13 + 2; // 0~12 -> 2~14(A)
        private static int GetSuit(int card) => card / 13; // 0~3 -> 클로버, 다이아, 하트, 스페이드

        public static (string HandName, List<int> CardIndices) EvaluateHandLocalizedInHoldem(List<int> cards)
        {
            // 랭크와 슈트 추출
            var ranks = cards.Select(GetRank).OrderBy(rank => rank).ToList();
            var suits = cards.Select(GetSuit).ToList();
            var groupedRanks = ranks.GroupBy(rank => rank).ToDictionary(g => g.Key, g => g.Count());
            var groupedSuits = suits.GroupBy(suit => suit).ToDictionary(g => g.Key, g => g.Count());

            // 족보 판정
            foreach (var suitGroup in groupedSuits)
            {
                if (suitGroup.Value >= 5)
                {
                    var suitedCards = cards
                        .Select((c, i) => new { c, i })
                        .Where(x => GetSuit(x.c) == suitGroup.Key)
                        .ToList();

                    var suitedRanks = suitedCards.Select(x => GetRank(x.c)).ToList();
                    var straightRanks = GetBestStraightRanks(suitedRanks);

                    if (straightRanks.Count == 0)
                        continue; // 이 슈트는 스트레이트 플러시 없음

                    // 로티플 체크 (10-J-Q-K-A)
                    if (straightRanks.OrderBy(r => r).SequenceEqual(new[] { 10, 11, 12, 13, 14 }))
                    {
                        var indices = suitedCards
                            .Where(x => new[] { 10, 11, 12, 13, 14 }.Contains(GetRank(x.c)))
                            .Select(x => x.c)
                            .ToList();
                        return (StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.RoyalStraightFlush].StringToLocal, indices);
                    }

                    // 백스티플 체크 (A-2-3-4-5)
                    if (straightRanks.OrderBy(r => r).SequenceEqual(new[] { 2, 3, 4, 5, 14 }))
                    {
                        var indices = suitedCards
                            .Where(x => new[] { 2, 3, 4, 5, 14 }.Contains(GetRank(x.c)))
                            .Select(x => x.c)
                            .ToList();
                        return (StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.StraightFlush].StringToLocal, indices);
                    }

                    // 일반 스티플
                    var sfIndices = suitedCards
                        .Where(x => straightRanks.Contains(GetRank(x.c)))
                        .Select(x => x.c)
                        .ToList();

                    //return ($"{string.Join(", ", ConvertRanksToSymbols(straightRanks))} 스티플", sfIndices);
                    return (StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.StraightFlush].StringToLocal, sfIndices);
                }
            }

            // 2. 포카드
            if (groupedRanks.Values.Any(count => count == 4))
            {
                int rank = groupedRanks.First(pair => pair.Value == 4).Key;
                var indices = cards.Select((c, i) => new { c, i })
                    .Where(x => GetRank(x.c) == rank)
                    .Select(x => x.c).ToList();
                return ($"{ConvertRankToSymbol(rank)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FourCard].StringToLocal}", indices);
            }

            // 3. 풀하우스
            if (groupedRanks.Values.Count(count => count >= 3) >= 1 &&
                groupedRanks.Values.Count(count => count >= 2) >= 2)
            {
                int triplet = groupedRanks.Where(pair => pair.Value >= 3).Max(pair => pair.Key);
                int pair = groupedRanks.Where(pair => pair.Value >= 2 && pair.Key != triplet).Max(pair => pair.Key);

                var indices = cards.Select((c, i) => new { c, i })
                    .Where(x => GetRank(x.c) == triplet || GetRank(x.c) == pair)
                    .Select(x => x.c).ToList();

                return ($"{ConvertRankToSymbol(triplet)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FullHouse].StringToLocal}", indices);
            }

            // 4. 플러쉬
            if (groupedSuits.Values.Any(count => count >= 5))
            {
                int suit = groupedSuits.First(pair => pair.Value >= 5).Key;
                var flushCards = cards.Select((c, i) => new { c, i })
                    .Where(x => GetSuit(x.c) == suit)
                    .OrderByDescending(x => GetRank(x.c))
                    .Take(5)
                    .ToList();
                var flushRanks = flushCards.Select(x => GetRank(x.c)).ToList();
                var indices = flushCards.Select(x => x.c).ToList();

                return ($"{ConvertRankToSymbol(flushRanks.First())} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FlushKorean].StringToLocal}", indices);
            }

            // 5. 스트레이트
            if (HasStraight(ranks))
            {
                var straightRanks = GetBestStraightRanks(ranks);
                var indices = cards.Select((c, i) => new { c, i })
                    .Where(x => straightRanks.Contains(GetRank(x.c)))
                    .Select(x => x.c).ToList();
                var highSymbol = ConvertRanksToSymbols(new List<int> { straightRanks.Last() }).First();
                return ($"{highSymbol} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Straight].StringToLocal}", indices);
            }

            // 6. 트리플
            if (groupedRanks.Values.Any(count => count == 3))
            {
                int triplet = groupedRanks.First(pair => pair.Value == 3).Key;
                var indices = cards.Select((c, i) => new { c, i })
                    .Where(x => GetRank(x.c) == triplet)
                    .Select(x => x.c).ToList();

                return ($"{ConvertRankToSymbol(triplet)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Triple].StringToLocal}", indices);
            }

            // 7. 투 페어
            if (groupedRanks.Values.Count(count => count == 2) >= 2)
            {
                var pairs = groupedRanks.Where(pair => pair.Value == 2)
                    .Select(pair => pair.Key)
                    .OrderByDescending(rank => rank)
                    .Take(2)
                    .ToList();

                var indices = cards.Select((c, i) => new { c, i })
                    .Where(x => pairs.Contains(GetRank(x.c)))
                    .Select(x => x.c).ToList();

                return ($"{string.Join(", ", ConvertRanksToSymbols(pairs))} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.TwoPair].StringToLocal}", indices);
            }

            // 8. 원 페어
            if (groupedRanks.Values.Any(count => count == 2))
            {
                int pair = groupedRanks.First(pair => pair.Value == 2).Key;
                var indices = cards.Select((c, i) => new { c, i })
                    .Where(x => GetRank(x.c) == pair)
                    .Select(x => x.c).ToList();

                return ($"{ConvertRankToSymbol(pair)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OnePair].StringToLocal}", indices);
            }

            // 9. 탑 (High Card)
            if (ranks.Count > 0)
            {
                int top = ranks.Max(); // 가장 높은 숫자
                var indices = cards.Select((c, i) => new { c, i })
                    .Where(x => GetRank(x.c) == top)
                    .Select(x => x.c).ToList();

                return ($"{ConvertRankToSymbol(top)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.High].StringToLocal}", indices);
            }

            return (StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Unknown].StringToLocal, new List<int>());
        }

        private static string RankToStringBadugi(int rank)
        {
            // rank는 Badugi 평가 시 이미 Ace가 1로 변환되어 있음.
            return rank switch
            {
                1 => "A", // Ace
                11 => "J", // Jack
                12 => "Q", // Queen
                13 => "K", // King
                _ => rank.ToString()
            };
        }

        public static (string HandName, List<int> CardIndices) EvaluateBadugiHand(List<int> cards)
        {
            (string HandName, List<int> CardIndices) handinfo;
            handinfo.CardIndices = new List<int>();
            handinfo.HandName = "";

            if (cards == null || cards.Count != 4)
            {
                handinfo.HandName = "";
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

            handinfo.CardIndices = validBadugi;

            int cardCount = finalRanks.Count;
            if (cardCount == 4)
            {
                // 4장인 경우 특별 조합 확인 (예시: 1,2,3,4)
                if (finalRanks[0] == 1 && finalRanks[1] == 2 && finalRanks[2] == 3 && finalRanks[3] == 4)
                {
                    //handinfo.HandName = $"{FormatRanks(finalRanks)} 골프";
                    handinfo.HandName = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Golf].StringToLocal;
                }
                else if (finalRanks[0] == 1 && finalRanks[1] == 2 && finalRanks[2] == 3 && finalRanks[3] == 5)
                {
                    handinfo.HandName = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Second].StringToLocal;
                }
                else if (finalRanks[0] == 1 && finalRanks[1] == 2 && finalRanks[2] == 4 && finalRanks[3] == 5)
                {
                    handinfo.HandName = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Third].StringToLocal;
                }
                else
                {
                    handinfo.HandName = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MaidTop].StringToLocal} {RankToStringBadugi(finalRanks[3])}탑";
                }
            }
            else if (cardCount == 3)
            {
                handinfo.HandName = $"{FormatRanks(finalRanks)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Base].StringToLocal}";
            }
            else if (cardCount == 2)
            {
                handinfo.HandName = $"{FormatRanks(finalRanks)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.TwoBase].StringToLocal}";
            }
            else if (cardCount == 1)
            {
                handinfo.HandName = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.NoPattern].StringToLocal;
            }
            else
            {
                handinfo.HandName = "Error: Hand Not Recognized";
            }


            return handinfo;
        }

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

        private static int GetBadugiRank(int card)
        {
            int rank = (card % 13) + 2; // 홀덤 방식: 2 ~ 14 (Ace=14)
            return rank == 14 ? 1 : rank; // Badugi에서는 Ace(14)를 1로 변환
        }

        private static string ConvertRankToSymbol(int rank)
        {
            return rank switch
            {
                11 => "J",
                12 => "Q",
                13 => "K",
                14 => "A",
                _ => rank.ToString() // 숫자 그대로 반환
            };
        }

        private static List<string> ConvertRanksToSymbols(IEnumerable<int> ranks)
        {
            return ranks.Select(ConvertRankToSymbol).ToList();
        }


        private static List<int> GetBestStraightRanks(List<int> ranks)
        {
            var distinct = ranks.Distinct().OrderBy(r => r).ToList();

            // 일반 스트레이트: high 14(A) ~ 6 까지 내려가면서 체크
            for (int high = 14; high >= 6; high--)
            {
                var need = new[] { high - 4, high - 3, high - 2, high - 1, high };
                if (need.All(distinct.Contains))
                    return need.ToList(); // 가장 높은 스트레이트를 반환
            }

            // 백스트레이트 A-2-3-4-5
            if (new[] { 14, 2, 3, 4, 5 }.All(distinct.Contains))
                return new List<int> { 2, 3, 4, 5, 14 }; // 표시는 네가 원하는 순서대로

            return new List<int>();
        }


        private static bool HasStraight(List<int> ranks)
        {
            ranks = ranks.Distinct().OrderBy(rank => rank).ToList();

            for (int i = 0; i <= ranks.Count - 5; i++)
            {
                if (ranks[i + 4] - ranks[i] == 4)
                {
                    return true;
                }
            }

            // A, 2, 3, 4, 5 처리
            if (ranks.Contains(14) && ranks.Take(4).SequenceEqual(new List<int> { 2, 3, 4, 5 }))
            {
                return true;
            }

            return false;
        }

        private class EvaluatedHand
        {
            public int Category;
            public List<int> TieBreakers;
        }

        public static List<int> HighlightBestHand(List<int> holeCards, List<int> communityCards, bool maskOn)
        {
            int totalCards = holeCards.Count + communityCards.Count;

            // 전체 카드
            List<int> allCards = new List<int>(holeCards);
            allCards.AddRange(communityCards);

            List<int> highlightIds = new List<int>();


            // 프리플랍(5장 미만) - 원페어만 두 장 표시
            if (totalCards < 5)
            {
                if (holeCards.Count == 2)
                {
                    int r1 = GetRank(holeCards[0]);
                    int r2 = GetRank(holeCards[1]);
                    if (r1 == r2)
                    {
                        if (!highlightIds.Contains(holeCards[0])) highlightIds.Add(holeCards[0]);
                        if (!highlightIds.Contains(holeCards[1])) highlightIds.Add(holeCards[1]);
                    }
                }
            }
            else
            {
                // 내(로컬) 7장 기준의 베스트 5장과 평가(하이라이트는 내 카드+커뮤니티만 건드리니까 이걸 기준)
                List<int> myBest = GetBestHandCards(allCards);
                EvaluatedHand myEval = Evaluate5CardHand(myBest);

                // 1) 족보 만든 카드 기본 하이라이트
                switch (myEval.Category)
                {
                    case 10: // 로얄
                    case 9: // 스트플
                    case 6: // 플러시
                    case 5: // 스트레이트
                        foreach (int id in myBest)
                            if (!highlightIds.Contains(id))
                                highlightIds.Add(id);
                        break;

                    case 8: // (프로젝트마다 매핑 다를 수 있지만) 여기서는 킥커 없음 처리
                        foreach (int id in myBest)
                            if (!highlightIds.Contains(id))
                                highlightIds.Add(id);
                        break;

                    case 7: // 풀하우스(3+2) - 킥커 없음
                    {
                        int threeR = myEval.TieBreakers[0];
                        int pairR = myEval.TieBreakers[1];
                        foreach (int id in myBest)
                        {
                            int r = GetRank(id);
                            if ((r == threeR || r == pairR) && !highlightIds.Contains(id))
                                highlightIds.Add(id);
                        }

                        break;
                    }

                    case 4: // 트리플 -> 3장만
                    {
                        int trip = myEval.TieBreakers[0];
                        foreach (int id in myBest)
                            if (GetRank(id) == trip && !highlightIds.Contains(id))
                                highlightIds.Add(id);
                        break;
                    }

                    case 3: // 투페어 -> 두 페어 4장만
                    {
                        int highPair = myEval.TieBreakers[0];
                        int lowPair = myEval.TieBreakers[1];
                        foreach (int id in myBest)
                        {
                            int r = GetRank(id);
                            if ((r == highPair || r == lowPair) && !highlightIds.Contains(id))
                                highlightIds.Add(id);
                        }

                        break;
                    }

                    case 2: // 원페어 -> 페어 2장만
                    {
                        int pairR = myEval.TieBreakers[0];
                        foreach (int id in myBest)
                            if (GetRank(id) == pairR && !highlightIds.Contains(id))
                                highlightIds.Add(id);
                        break;
                    }

                    case 1: // 하이카드 -> maskOn 일 때만 최고 한 장
                    default:
                    {
                        if (maskOn)
                        {
                            int top = myEval.TieBreakers[0];
                            foreach (int id in myBest)
                                if (GetRank(id) == top && !highlightIds.Contains(id))
                                {
                                    highlightIds.Add(id);
                                    break;
                                }
                        }

                        break;
                    }
                }
            }

            return highlightIds;
        }

        private static List<int> GetBestHandCards(List<int> sevenCards)
        {
            EvaluatedHand bestEval = null;
            List<int> bestCombo = null;

            // 7장 중 5장을 뽑는 모든 조합(21가지) 평가
            foreach (var combo in GetCombinations(sevenCards, 5))
            {
                var eval = Evaluate5CardHand(combo);
                if (bestEval == null || CompareHands(eval, bestEval) > 0)
                {
                    bestEval = eval;
                    bestCombo = new List<int>(combo);
                }
            }

            // 혹시 null 체크
            return bestCombo ?? new List<int>();
        }

        private static int CompareHands(EvaluatedHand h1, EvaluatedHand h2)
        {
            if (h1.Category != h2.Category)
                return h1.Category.CompareTo(h2.Category);
            for (int i = 0; i < System.Math.Min(h1.TieBreakers.Count, h2.TieBreakers.Count); i++)
                if (h1.TieBreakers[i] != h2.TieBreakers[i])
                    return h1.TieBreakers[i].CompareTo(h2.TieBreakers[i]);
            return 0;
        }

        private static IEnumerable<List<int>> GetCombinations(List<int> list, int k)
        {
            if (k == 0)
            {
                yield return new List<int>();
            }
            else
            {
                for (int i = 0; i <= list.Count - k; i++)
                    foreach (var tail in GetCombinations(list.GetRange(i + 1, list.Count - (i + 1)), k - 1))
                    {
                        var combo = new List<int> { list[i] };
                        combo.AddRange(tail);
                        yield return combo;
                    }
            }
        }

        // 5장 핸드 평가
        private static EvaluatedHand Evaluate5CardHand(List<int> cards)
        {
            // 카드가 정확히 5장인지 검증 (필요시)
            if (cards == null || cards.Count != 5)
                return null;

            var ranks = cards.Select(GetRank).ToList();
            var suits = cards.Select(GetSuit).ToList();
            var sortedDesc = ranks.OrderByDescending(x => x).ToList();
            bool isFlush = suits.Distinct().Count() == 1;

            // 스트레이트 검사
            bool isStraight = false;
            int straightHigh = 0;
            var distinct = ranks.Distinct().OrderBy(x => x).ToList();
            if (distinct.Count >= 5)
            {
                for (int i = 0; i <= distinct.Count - 5; i++)
                {
                    if (distinct[i + 4] - distinct[i] == 4)
                    {
                        isStraight = true;
                        straightHigh = distinct[i + 4];
                        break;
                    }
                }

                if (!isStraight && distinct.Contains(14))
                {
                    var aceLow = distinct.Where(x => x != 14)
                        .Concat(new[] { 1 })
                        .OrderBy(x => x)
                        .ToList();
                    for (int i = 0; i <= aceLow.Count - 5; i++)
                    {
                        if (aceLow[i + 4] - aceLow[i] == 4)
                        {
                            isStraight = true;
                            straightHigh = 5;
                            break;
                        }
                    }
                }
            }

            // 그룹 카운트 (랭크별 개수 집계)
            var groups = ranks
                .GroupBy(x => x)
                .Select(g => new { Rank = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenByDescending(g => g.Rank)
                .ToList();

            int category;
            var tie = new List<int>();

            // 1) 로열 플러시 / 스트레이트 플러시
            if (isFlush && isStraight)
            {
                if (straightHigh == 14)
                {
                    category = 10; // 로열 플러시
                }
                else
                {
                    category = 9; // 스트레이트 플러시
                }

                tie.Add(straightHigh);
            }
            // 2) 포카드
            else if (groups[0].Count == 4)
            {
                category = 8;
                tie.Add(groups[0].Rank); // 포카드 랭크
                tie.Add(groups.Skip(1).First().Rank); // 남은 카드 랭크
            }
            // 3) 풀하우스 (트리플 + 페어)
            else if (groups[0].Count == 3 && groups.Count(g => g.Count >= 2) >= 2)
            {
                category = 7;
                int threeRank = groups[0].Rank;
                int pairRank = groups.Where(g => g.Count >= 2 && g.Rank != threeRank)
                    .Max(g => g.Rank);
                tie.Add(threeRank);
                tie.Add(pairRank);
            }
            // 4) 플러시
            else if (isFlush)
            {
                category = 6;
                tie = sortedDesc; // 높은 카드 순서대로
            }
            // 5) 스트레이트
            else if (isStraight)
            {
                category = 5;
                tie.Add(straightHigh);
            }
            // 6) 트리플
            else if (groups[0].Count == 3)
            {
                category = 4;
                tie.Add(groups[0].Rank);
                tie.AddRange(groups.Skip(1)
                    .Select(g => g.Rank)
                    .OrderByDescending(x => x));
            }
            // 7) 투페어
            else if (groups.Count(g => g.Count == 2) >= 2)
            {
                category = 3;
                var pairRanks = groups.Where(g => g.Count == 2)
                    .Select(g => g.Rank)
                    .OrderByDescending(x => x)
                    .Take(2)
                    .ToList();
                tie.Add(pairRanks[0]);
                tie.Add(pairRanks[1]);
                tie.Add(groups.Where(g => g.Count == 1)
                    .Max(g => g.Rank)); // 나머지 싱글카드
            }
            // 8) 원페어
            else if (groups.Any(g => g.Count == 2))
            {
                category = 2;
                int pairRank = groups.First(g => g.Count == 2).Rank;
                tie.Add(pairRank);
                tie.AddRange(groups.Where(g => g.Rank != pairRank)
                    .Select(g => g.Rank)
                    .OrderByDescending(x => x));
            }
            // 9) 하이카드
            else
            {
                category = 1;
                tie = sortedDesc;
            }

            return new EvaluatedHand
            {
                Category = category,
                TieBreakers = tie
            };
        }


        #region SevenPoker

        public static (string HandName, List<int> CardIndices) EvaluateSevenPokerHand(List<int> cards)
        {
            var result = (HandName: "", CardIndices: new List<int>());

            if (cards == null || cards.Count < 4 || cards.Count > 7)
                return result;

            HandScore best;

            if (cards.Count == 4)
            {
                best = Evaluate4CardStateReturnCore(cards);
            }
            else
            {
                best = null;

                // 5장 이상은: 가능한 모든 5장 조합 중 최고 선택 (7장은 21개뿐)
                foreach (var combo in Combinations(cards, 5))
                {
                    var score = Evaluate5CardHandReturnCore(combo);
                    if (best == null || score.CompareTo(best) > 0)
                        best = score;
                }
            }

            result.HandName = best.Name;
            result.CardIndices = best.CardsUsed.ToList(); // ✅ 족보 구성 카드만
            return result;
        }

        // =========================================================
        // 4장 평가: 메이드 우선 + (메이드 없으면) 드로우 표시
        // 리턴은 "구성 카드만"
        // =========================================================
        // Category (Compare용) :
        // 7 포카드, 3 트리플, 2 투페어, 1 원페어, 0 하이카드,
        // -1 플러시드로우, -2 스트레이트드로우 (메이드 없을 때만)
        private static HandScore Evaluate4CardStateReturnCore(List<int> four)
        {
            var ranks = four.Select(GetRank).ToList(); // 2..14
            var suits = four.Select(GetSuit).ToList(); // 0..3

            var rankGroups = ranks
                .GroupBy(r => r)
                .Select(g => new { Rank = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.Rank)
                .ToList();

            // ---- 메이드 핸드 ----
            if (rankGroups[0].Count == 4)
            {
                int quad = rankGroups[0].Rank;
                var core = PickByRank(four, quad); // 4장
                return new HandScore(7, new List<int> { quad }, $"{RankToString(quad)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FourCard].StringToLocal}", core);
            }

            if (rankGroups[0].Count == 3)
            {
                int trip = rankGroups[0].Rank;
                int kicker = rankGroups[1].Rank;
                var core = PickByRank(four, trip); // 3장
                return new HandScore(3, new List<int> { trip, kicker }, $"{RankToString(trip)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Triple].StringToLocal}", core);
            }

            if (rankGroups[0].Count == 2 && rankGroups[1].Count == 2)
            {
                int p1 = rankGroups[0].Rank;
                int p2 = rankGroups[1].Rank;
                int highPair = Math.Max(p1, p2);
                int lowPair = Math.Min(p1, p2);

                var core = PickByRank(four, highPair, lowPair); // 4장
                return new HandScore(2, new List<int> { highPair, lowPair }, $"{RankToString(highPair)},{RankToString(lowPair)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.TwoPair].StringToLocal}", core);
            }

            if (rankGroups[0].Count == 2)
            {
                int pair = rankGroups[0].Rank;
                var kickers = rankGroups.Skip(1).Select(x => x.Rank).OrderByDescending(x => x).ToList();

                // Compare용에는 킥커까지 넣어야 “4장 상황에서도” 타이브레이크 가능
                var tie = new List<int> { pair };
                tie.AddRange(kickers);

                var core = PickByRank(four, pair); // 2장
                return new HandScore(1, tie, $"{RankToString(pair)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OnePair].StringToLocal}", core);
            }

            // ---- 하이카드: 1장만 리턴 ----
            var hiRanks = ranks.OrderByDescending(x => x).ToList();
            int top = hiRanks[0];
            var coreHigh = PickSingleByRank(four, top); // 1장
            return new HandScore(0, hiRanks, $"{RankToString(top)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.High].StringToLocal}", coreHigh);
        }

        // =========================================================
        // 5장 평가: Compare는 정상(킥커 포함), Return은 코어만
        // =========================================================
        // Category:
        // 9 로열SF, 8 SF, 7 포카드, 6 풀하우스, 5 플러시, 4 스트레이트,
        // 3 트리플, 2 투페어, 1 원페어, 0 하이카드
        private static HandScore Evaluate5CardHandReturnCore(List<int> five)
        {
            var ranks = five.Select(GetRank).ToList();
            var suits = five.Select(GetSuit).ToList();

            var rankGroups = ranks
                .GroupBy(r => r)
                .Select(g => new { Rank = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.Rank)
                .ToList();

            bool isFlush = suits.Distinct().Count() == 1;
            bool isStraight = TryGetStraightHigh(ranks, out int straightHigh);

            // 스트레이트 플러시 / 로열 (코어=5장)
            if (isFlush && isStraight)
            {
                bool isRoyal = ranks.Distinct().OrderBy(x => x)
                    .SequenceEqual(new[] { 10, 11, 12, 13, 14 });

                if (isRoyal)
                    return new HandScore(9, new List<int> { 14 }, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.RoyalStraightFlush].StringToLocal, five);

                // 백스트레이트 플러시 (A,2,3,4,5) 체크
                bool isBackStraightFlush = ranks.Distinct().OrderBy(x => x)
                    .SequenceEqual(new[] { 2, 3, 4, 5, 14 });

                if (isBackStraightFlush)
                    return new HandScore(8, new List<int> { 5 }, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BackStraightFlush].StringToLocal, five);

                return new HandScore(8, new List<int> { straightHigh }, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.StraightFlush].StringToLocal, five);

            }

            // 포카드 (코어=4장)
            if (rankGroups[0].Count == 4)
            {
                int quad = rankGroups[0].Rank;
                int kicker = rankGroups[1].Rank;
                var core = PickByRank(five, quad); // 4장만
                return new HandScore(7, new List<int> { quad, kicker }, $"{RankToString(quad)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FourCard].StringToLocal}", core);
            }

            // 풀하우스 (코어=5장)
            if (rankGroups[0].Count == 3 && rankGroups[1].Count == 2)
            {
                int trip = rankGroups[0].Rank;
                int pair = rankGroups[1].Rank;
                return new HandScore(6, new List<int> { trip, pair }, $"{RankToString(trip)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FullHouse].StringToLocal}", five);
            }

            // 플러시 (코어=5장)
            if (isFlush)
            {
                var sorted = ranks.OrderByDescending(x => x).ToList();
                return new HandScore(5, sorted, $"{RankToString(sorted[0])} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Flush].StringToLocal}", five);
            }

            // 스트레이트 (코어=5장)
            if (isStraight)
            {
                // 백스트레이트 (A,2,3,4,5) 체크
                bool isBackStraight = ranks.Distinct().OrderBy(x => x)
                    .SequenceEqual(new[] { 2, 3, 4, 5, 14 });

                if (isBackStraight)
                    return new HandScore(4, new List<int> { 5 }, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BackStraight].StringToLocal, five);

                return new HandScore(4, new List<int> { straightHigh }, $"{RankToString(straightHigh)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Straight].StringToLocal}", five);
            }


            // 트리플 (코어=3장)
            if (rankGroups[0].Count == 3)
            {
                int trip = rankGroups[0].Rank;
                var kickers = rankGroups.Skip(1).Select(x => x.Rank).OrderByDescending(x => x).ToList();

                var tie = new List<int> { trip };
                tie.AddRange(kickers);

                var core = PickByRank(five, trip); // 3장
                return new HandScore(3, tie, $"{RankToString(trip)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Triple].StringToLocal}", core);
            }

            // 투페어 (코어=4장)
            if (rankGroups[0].Count == 2 && rankGroups[1].Count == 2)
            {
                int p1 = rankGroups[0].Rank;
                int p2 = rankGroups[1].Rank;
                int highPair = Math.Max(p1, p2);
                int lowPair = Math.Min(p1, p2);
                int kicker = rankGroups[2].Rank;

                var core = PickByRank(five, highPair, lowPair); // 4장
                return new HandScore(2, new List<int> { highPair, lowPair, kicker }, $"{RankToString(highPair)},{RankToString(lowPair)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.TwoPair].StringToLocal}", core);
            }

            // 원페어 (코어=2장)
            if (rankGroups[0].Count == 2)
            {
                int pair = rankGroups[0].Rank;
                var kickers = rankGroups.Skip(1).Select(x => x.Rank).OrderByDescending(x => x).ToList();

                var tie = new List<int> { pair };
                tie.AddRange(kickers);

                var core = PickByRank(five, pair); // 2장
                return new HandScore(1, tie, $"{RankToString(pair)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OnePair].StringToLocal}", core);
            }

            // 하이카드 (코어=1장)
            var hiRanks = ranks.OrderByDescending(x => x).ToList();
            int topRank = hiRanks[0];
            var coreHigh = PickSingleByRank(five, topRank);
            return new HandScore(0, hiRanks, $"{RankToString(topRank)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.High].StringToLocal}", coreHigh);
        }

        // =========================================================
        // Straight helpers
        // =========================================================
        private static bool TryGetStraightHigh(List<int> ranks14, out int high)
        {
            var distinct = ranks14.Distinct().OrderBy(x => x).ToList();
            high = 0;
            if (distinct.Count != 5) return false;

            // A2345
            if (distinct.SequenceEqual(new[] { 2, 3, 4, 5, 14 }))
            {
                high = 5;
                return true;
            }

            for (int i = 1; i < 5; i++)
                if (distinct[i] != distinct[0] + i)
                    return false;

            high = distinct[4];
            return true;
        }

        // =========================================================
        // Core card pickers (rank 기준으로 카드ID 뽑기)
        // =========================================================
        private static List<int> PickByRank(List<int> cards, params int[] ranks14)
        {
            var set = new HashSet<int>(ranks14);
            return cards.Where(c => set.Contains(GetRank(c))).ToList();
        }

        private static List<int> PickSingleByRank(List<int> cards, int rank14)
        {
            for (int i = 0; i < cards.Count; i++)
                if (GetRank(cards[i]) == rank14)
                    return new List<int> { cards[i] };

            return new List<int>();
        }

        // =========================================================
        // Card encoding helpers (너 규칙 그대로)
        // =========================================================

        private static string RankToString(int r) => r switch
        {
            14 => "A",
            13 => "K",
            12 => "Q",
            11 => "J",
            _ => r.ToString()
        };

        // =========================================================
        // Combinations nCk
        // =========================================================
        private static IEnumerable<List<int>> Combinations(List<int> src, int k)
        {
            int n = src.Count;
            int[] idx = Enumerable.Range(0, k).ToArray();

            while (true)
            {
                var comb = new List<int>(k);
                for (int i = 0; i < k; i++) comb.Add(src[idx[i]]);
                yield return comb;

                int t = k - 1;
                while (t >= 0 && idx[t] == n - k + t) t--;
                if (t < 0) yield break;

                idx[t]++;
                for (int i = t + 1; i < k; i++) idx[i] = idx[i - 1] + 1;
            }
        }

        // =========================================================
        // Score: Compare용(TieBreakRanks) + 리턴용(CardsUsed)
        // =========================================================
        private sealed class HandScore : IComparable<HandScore>
        {
            public int Category; // Compare용
            public List<int> TieBreakRanks; // Compare용(킥커 포함)
            public string Name; // 표시용
            public List<int> CardsUsed; // ✅ 리턴용(구성카드만)

            public HandScore(int category, List<int> tie, string name, List<int> usedCore)
            {
                Category = category;
                TieBreakRanks = tie;
                Name = name;
                CardsUsed = usedCore;
            }

            public int CompareTo(HandScore other)
            {
                if (Category != other.Category) return Category.CompareTo(other.Category);

                int len = Math.Max(TieBreakRanks.Count, other.TieBreakRanks.Count);
                for (int i = 0; i < len; i++)
                {
                    int a = (i < TieBreakRanks.Count) ? TieBreakRanks[i] : 0;
                    int b = (i < other.TieBreakRanks.Count) ? other.TieBreakRanks[i] : 0;
                    if (a != b) return a.CompareTo(b);
                }

                return 0;
            }
        }

        #endregion
    }
}