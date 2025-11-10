using System;
using System.Linq;
using System.Reflection;
using static Solution;

/// <summary>
/// 이거 채점 테스트 오류나는 버전임
/// 수정을 해야함. 테스트 케이스만 통과한 상태
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Solution s = new Solution();
        int result = s.solution(new string[] { "muzi", "ryan", "frodo", "neo" }, new string[] { "muzi frodo", "muzi frodo", "ryan muzi", "ryan muzi", "ryan muzi", "frodo muzi", "frodo ryan", "neo muzi" });
        //int result = s.solution(new string[] { "joy", "brad", "alessandro", "conan", "david" }, new string[] { "alessandro brad", "alessandro joy", "alessandro conan", "david alessandro", "alessandro david" });
        //int result = s.solution(new string[] { "a", "b", "c" }, new string[] { "a b", "b a", "c a", "a c", "a c", "c a" });
        Console.WriteLine(result);
    }
}

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
        public string? name { get; set; }
        public int count { get; set; }
    }

    public class Result
    {
        public int accept { get; set; }
        public List<string> acceptName = new List<string>();
    }

    public int solution(string[] friends, string[] gifts)
    {
        int answer = 0;
        Dictionary<string, List<Give>> dicMyGive = new Dictionary<string, List<Give>>();    // 본인이 주고 받은 사람들, 본인/받은사람
        Dictionary<string, Gift> dicGift = new Dictionary<string, Gift>();                  // 통합
        Dictionary<string, Result> result = new Dictionary<string, Result>();

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

        foreach (var my in  dicMyGive)
        {
            string myName = my.Key;
            List<Give> listGive = my.Value; //내가 준 사람

            for (int i = 0; i < listGive.Count; i++)
            {
                int giveCount = listGive[i].count;  // 내가 상대에게 준 갯수
                bool isMyGive = dicMyGive[listGive[i].name].Any(g => g.name == myName); //내가 준 사람에게 내가 받은적이 있나
                if (isMyGive)
                {
                    //있으면
                    int acceptCount = dicMyGive[listGive[i].name].First(g => g.name == myName).count;
                    if (giveCount > acceptCount)
                    {
                        result[myName].accept++;           // 나는 받은거 ++;
                        result[myName].acceptName.Add(listGive[i].name);    //나에게 선물 준 애
                    }
                    else if (giveCount < acceptCount)
                    {
                        result[listGive[i].name].accept++;
                        result[listGive[i].name].acceptName.Add(myName);    // 내가 줌
                    }
                    else if (giveCount == acceptCount)  // 서로 같으면
                    {
                        if (dicGift[myName].count > dicGift[listGive[i].name].count)
                        {
                            if (!result[myName].acceptName.Contains(listGive[i].name))
                            {
                                result[myName].accept++;
                                result[myName].acceptName.Add(listGive[i].name);
                            }
                        }
                        else if (dicGift[myName].count < dicGift[listGive[i].name].count)
                        {
                            if (!result[listGive[i].name].acceptName.Contains(myName))
                            {
                                result[listGive[i].name].accept++;
                                result[listGive[i].name].acceptName.Add(myName);
                            }
                        }
                    }
                    else    // 서로 안줌
                    {
                        int myCount = dicGift[myName].give - dicGift[myName].accept;
                        int youCount = dicGift[listGive[i].name].give - dicGift[myName].accept;
                        if (myCount > youCount)
                        {
                            result[myName].accept++;
                            result[myName].acceptName.Add(listGive[i].name);
                        }
                        else if (myCount < youCount)
                        {
                            result[listGive[i].name].accept++;
                            result[listGive[i].name].acceptName.Add(myName);    // 내가 줌
                        }
                    }
                }
                else
                {
                    // 없으면
                    result[myName].accept++;
                }
            }
        }

        // 제일 큰 사람 찾기
        string maxName = result.Aggregate((x, y) => x.Value.accept > y.Value.accept ? x : y).Key;
        answer = result[maxName].accept;

        return answer;
    }
}