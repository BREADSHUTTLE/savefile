using System;
using System.Collections.Generic;
using System.Linq;

public class Solution
{
    public class Gift
    {
        public int give { get; set; }
        public int accept { get; set; }
        public int count { get; set; }
    }

    public class Give
    {
        //public string? name { get; set; }
        public string name { get; set; }
        public int count { get; set; }
    }

    public class Result
    {
        public int accept { get; set; }
        public List<string> acceptName = new List<string>();
    }

    // 상대에게 준 횟수
    private int GetGiveCount(Dictionary<string, List<Give>> dicMyGive, string from, string to)
    {
        var list = dicMyGive[from];
        var found = list.FirstOrDefault(g => g.name == to);
        return found == null ? 0 : found.count;
    }

    public int solution(string[] friends, string[] gifts)
    {
        int answer = 0;
        var dicMyGive = new Dictionary<string, List<Give>>();    // 본인이 주고 받은 사람들, 본인/받은사람
        var dicGift = new Dictionary<string, Gift>();            // 통합
        var result = new Dictionary<string, Result>();

        for (int i = 0; i < friends.Length; i++)
        {
            dicMyGive.Add(friends[i], new List<Give>());
            dicGift.Add(friends[i], new Gift());
            result.Add(friends[i], new Result());
        }

        for (int i = 0; i < gifts.Length; i++)
        {
            string[] name = gifts[i].Split(' ');
            bool isGiveFriend = dicMyGive[name[0]].Any(g => g.name == name[1]);

            if (isGiveFriend)
                dicMyGive[name[0]].First(g => g.name == name[1]).count++;
            else
                dicMyGive[name[0]].Add(new Give { name = name[1], count = 1 });

            dicGift[name[0]].give++;        // 준 사람 +
            dicGift[name[1]].accept++;      // 받은 사람 +
        }

        // 지수 계산
        foreach (var gift in dicGift)
            gift.Value.count = gift.Value.give - gift.Value.accept;

        for (int i = 0; i < friends.Length; i++)
        {
            for (int j = i + 1; j < friends.Length; j++)        // 자기 자신을 제외한 카운팅
            {
                string a = friends[i];
                string b = friends[j];

                int giveAB = GetGiveCount(dicMyGive, a, b); // A→B 준 횟수
                int giveBA = GetGiveCount(dicMyGive, b, a); // B→A 준 횟수

                if (giveAB > giveBA)
                {
                    // a가 b에게 1개 받음
                    result[a].accept++;
                }
                else if (giveAB < giveBA)
                {
                    // b가 a에게 1개 받음
                    result[b].accept++;
                }
                else
                {
                    // 두 사람이 선물을 주고받은 기록이 하나도 없거나 주고받은 수가 같다면,
                    // 선물 지수가 더 큰 사람이 선물 지수가 더 작은 사람에게 선물을 하나 받습니다.
                    int countA = dicGift[a].count;
                    int countB = dicGift[b].count;

                    if (countA > countB)
                        result[a].accept++;
                    else if (countA < countB)
                        result[b].accept++;
                    // 두 사람의 선물 지수도 같다면 다음 달에 선물을 주고받지 않습니다.
                }
            }
        }

        // 제일 큰 사람 찾기
        foreach (var kv in result)
        {
            if (kv.Value.accept > answer)
                answer = kv.Value.accept;
        }

        return answer;
    }
}