using System;
using System.Collections.Generic;

public class Solution
{
    /// 비밀 코드 해독 [프로그래머스 - 2025 프로그래머스 코드챌린지 1차 예선 문제]
    /// <summary>
    /// 재귀함수를 이용해서 current list에 숫자 조합을 뽑아서,
    /// 그 뽑은 조합으로 ans와 비교하여 가능한 숫자 조합을 찾아서 answer 반환하기
    /// </summary>
    /// 
    int answer = 0;
    public int solution(int n, int[,] q, int[] ans)
    {
        combine(n, q, ans, 1, new List<int>());

        return answer;
    }

    public bool check(int[,] q, int[] ans, List<int> combine)
    {
        for (int i = 0; i < q.GetLength(0); i++)
        {
            int cnt = 0;
            for (int j = 0; j < q.GetLength(1); j++)
            {
                for (int v = 0; v < combine.Count; v++)
                {
                    if (q[i, j] == combine[v])
                    {
                        cnt++;
                        break;
                    }
                }
            }
            if (cnt != ans[i])
                return false;
        }
        return true;
    }

    public void combine(int n, int[,] q, int[] ans, int start, List<int> current)
    {
        if (current.Count == 5)
        {
            if (check(q, ans, current))
                answer++;
            return;
        }

        for (int i = start; i <= n; i++)
        {
            current.Add(i);
            combine(n, q, ans, i + 1, current);
            current.RemoveAt(current.Count - 1); // 이전 상태로 되돌리기
        }
    }
}