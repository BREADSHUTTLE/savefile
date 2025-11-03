using System;
using System.Collections.Generic;

public class Solution
{
    /// 택배 상자 꺼내기 - 2025 프로그래머스 코드챌린지 2차 예선
    /// <summary>
    /// 리스트를 미리 열 갯수만큼 만들고,
    /// 행과 열을 계산해서 홀수행 일땐 왼->오, 짝수행 일땐 오->왼 순으로 리스트를 만들고
    /// 그 리스트에서 같은 열에 가진 숫자를 비교해서 꺼내려는 박스보다 숫자가 높으면 본인을 포함하여 count 시킴
    /// </summary>
    /// 
    public int solution(int n, int w, int num)
    {
        int answer = 0;
        answer = BoxCount(n, w, num);
        return answer;
    }

    public int BoxCount(int n, int w, int num)
    {
        int cnt = 0;
        var boxList = new List<List<int>>();
        for (int i = 1; i <= n; i++)
        {
            int r = (i - 1) / w;
            int c = (i - 1) % w;
            if (r == boxList.Count)
                boxList.Add(new List<int>(new int[w]));

            if (r % 2 == 0)
                boxList[r][c] = i;
            else
                boxList[r][(w - 1) - c] = i;
        }

        int col = (num - 1) / w;
        int idx = boxList[col].IndexOf(num);
        //Console.WriteLine(idx);

        //Console.WriteLine(boxList[col][idx]);

        for (int i = 0; i < boxList.Count; i++)
        {
            //if (boxList[i][idx] != null && boxList[i][idx] >= num)
            if (boxList[i][idx] >= num)
                cnt++;
        }

        return cnt;
    }
}