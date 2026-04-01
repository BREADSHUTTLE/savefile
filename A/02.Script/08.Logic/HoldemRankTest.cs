using System;
using System.Collections.Generic;
using System.Linq;

namespace HoldemRankTest
{
    // 홀덤 플레이어의 순위 정보를 담는 클래스
    public class PlayerHoldemRanking
    {
        public int PlayerId { get; set; }
        public List<int> HoleCards { get; set; }
        public List<int> BestHand { get; set; } // 최종 5장 조합
        public int HandRank { get; set; }       // 족보 순위 (숫자가 높을수록 강한 패)
        public string HandRankString { get; set; }
        public int Rank { get; set; }           // 최종 순위 (1등, 2등 등)
    }

    public class HoldemRanker
    {
        /// <summary>
        /// 각 플레이어의 홀카드(2장)와 커뮤니티 카드(5장)를 바탕으로
        /// 플레이어의 최종 핸드(최상의 5장 조합)를 평가하고 순위를 매겨 반환합니다.
        /// </summary>
        /// <param name="playerHoleCards">Key: PlayerID, Value: 홀카드 2장의 리스트</param>
        /// <param name="communityCards">커뮤니티 카드 5장의 리스트</param>
        /// <returns>순위가 매겨진 플레이어 랭킹 리스트 (내림차순 정렬)</returns>
        public List<PlayerHoldemRanking> GetHoldemRanking(Dictionary<int, List<int>> playerHoleCards, List<int> communityCards)
        {
            List<PlayerHoldemRanking> rankingList = new List<PlayerHoldemRanking>();

            foreach (var kvp in playerHoleCards)
            {
                int playerId = kvp.Key;
                List<int> holeCards = kvp.Value;

                // 전체 카드: 홀카드 2장 + 커뮤니티 카드 5장
                List<int> allCards = new List<int>(holeCards);
                allCards.AddRange(communityCards);

                // 족보 평가 문자열 (예시 구현 – 실제 로직에 맞게 수정)
                string handRankStr = EvaluateHandLocalized(allCards);
                int handRank = GetHandRankValue(handRankStr);

                // 최상의 5장 조합 계산 (예시: GetBest5CardHand)
                List<int> bestHand = GetBest5CardHand(allCards);

                rankingList.Add(new PlayerHoldemRanking
                {
                    PlayerId = playerId,
                    HoleCards = new List<int>(holeCards),
                    BestHand = bestHand,
                    HandRank = handRank,
                    HandRankString = handRankStr
                });
            }

            // 정렬: 높은 HandRank(족보 등급) 우선, 동일하면 CompareHands 결과 (더 강한 패가 먼저)
            rankingList.Sort((a, b) =>
            {
                if (a.HandRank != b.HandRank)
                {
                    return b.HandRank.CompareTo(a.HandRank); // 내림차순 (높은 족보가 우선)
                }
                // 족보 등급이 같다면, 5장 패를 비교하여 결정
                return CompareHands(a.BestHand, b.BestHand);
            });

            // 순위 할당 (동점이면 같은 순위)
            int currentRank = 1;
            if (rankingList.Count > 0)
            {
                rankingList[0].Rank = currentRank;
                for (int i = 1; i < rankingList.Count; i++)
                {
                    bool isTie = false;
                    if (rankingList[i].HandRank == rankingList[i - 1].HandRank)
                    {
                        if (CompareHands(rankingList[i].BestHand, rankingList[i - 1].BestHand) == 0)
                        {
                            isTie = true;
                        }
                    }
                    if (!isTie)
                    {
                        currentRank = i + 1;
                    }
                    rankingList[i].Rank = currentRank;
                }
            }

            return rankingList;
        }

        #region 홀덤 핸드 평가 & 비교 (예시 구현)

        // 예시로 간단하게 전체 카드 합계로 족보 문자열을 결정 (실제 룰과 다를 수 있음)
        public string EvaluateHandLocalized(List<int> cards)
        {
            // 카드의 랭크는 (card % 13) + 2 (2~14; Ace=14)
            int sum = cards.Sum(card => (card % 13) + 2);
            // 단순 예시: 합계가 낮으면 강한 패라고 가정 (실제 홀덤은 별도 알고리즘 필요)
            if (sum < 40)
                return "Royal Flush"; // 최고 족보
            else if (sum < 45)
                return "Straight Flush";
            else if (sum < 50)
                return "Four of a Kind";
            else if (sum < 55)
                return "Full House";
            else if (sum < 60)
                return "Flush";
            else if (sum < 65)
                return "Straight";
            else if (sum < 70)
                return "Three of a Kind";
            else if (sum < 75)
                return "Two Pair";
            else if (sum < 80)
                return "One Pair";
            else
                return "High Card";
        }

        // 족보 문자열을 점수로 변환 (숫자가 높을수록 강한 패)
        private int GetHandRankValue(string handRank)
        {
            // 예시 점수: Royal Flush가 10, Straight Flush 9, Four of a Kind 8, ... High Card 1
            if (handRank.Contains("Royal Flush")) return 10;
            if (handRank.Contains("Straight Flush")) return 9;
            if (handRank.Contains("Four of a Kind")) return 8;
            if (handRank.Contains("Full House")) return 7;
            if (handRank.Contains("Flush")) return 6;
            if (handRank.Contains("Straight")) return 5;
            if (handRank.Contains("Three of a Kind")) return 4;
            if (handRank.Contains("Two Pair")) return 3;
            if (handRank.Contains("One Pair")) return 2;
            return 1; // High Card
        }

        // 예시: 7장 카드 중에서 최상의 5장 조합을 선택하는 함수  
        // (여기서는 단순히 높은 카드 5장을 선택합니다. 실제는 족보 규칙에 따른 최적의 5장을 선택해야 함)
        private List<int> GetBest5CardHand(List<int> cards)
        {
            // 카드 정렬 (오름차순: Ace가 2, ..., King이 14)
            List<int> sorted = cards.OrderBy(card => (card % 13) + 2).ToList();
            // 높은 5장을 선택 (오름차순 정렬이므로 마지막 5장 선택)
            return sorted.Skip(sorted.Count - 5).ToList();
        }

        // 두 5장 조합을 비교하여 더 강한 패가 있으면 음수가 아닌 값을 반환하도록 합니다.
        // (여기서는 각 카드의 랭크를 내림차순으로 비교하는 간단한 방법)
        private int CompareHands(List<int> handA, List<int> handB)
        {
            List<int> sortedA = handA.OrderByDescending(card => (card % 13) + 2).ToList();
            List<int> sortedB = handB.OrderByDescending(card => (card % 13) + 2).ToList();

            for (int i = 0; i < Math.Min(sortedA.Count, sortedB.Count); i++)
            {
                int rankA = (sortedA[i] % 13) + 2;
                int rankB = (sortedB[i] % 13) + 2;
                if (rankA != rankB)
                {
                    return rankA.CompareTo(rankB);
                }
            }
            return 0;
        }

        #endregion
    }

    // 테스트용 Main 함수
    public class Program
    {
        public static void Main(string[] args)
        {
            // 예시 플레이어 홀카드 데이터 (플레이어당 2장)
            var playerHoleCards = new Dictionary<int, List<int>>
            {
                { 1, new List<int> { 29, 30 } }, // 예시: 29, 30
                { 2, new List<int> { 28, 31 } },
                { 3, new List<int> { 5, 11 } }
                // 추가 플레이어 가능
            };

            // 예시 커뮤니티 카드 (5장)
            List<int> communityCards = new List<int> { 38, 46, 10, 22, 35 };

            // HoldemRanker 생성 및 랭킹 계산
            HoldemRanker ranker = new HoldemRanker();
            List<PlayerHoldemRanking> rankings = ranker.GetHoldemRanking(playerHoleCards, communityCards);

            // 결과 출력
            Console.WriteLine("홀덤 플레이어 핸드 평가 및 순위 결과:");
            foreach (var ranking in rankings)
            {
                Console.WriteLine($"Player {ranking.PlayerId}: Hand = {ranking.HandRankString}, Score = {ranking.HandRank}, Rank = {ranking.Rank}");
                Console.WriteLine($"Best Hand (card IDs): {string.Join(", ", ranking.BestHand)}");
            }

            Console.WriteLine("엔터를 눌러 종료합니다...");
            Console.ReadLine();
        }
    }
}
